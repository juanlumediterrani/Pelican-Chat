#!/usr/bin/env python3
"""
Verificador y enrutador operativo del Project Brain de PelicanChat (EST-001 v1.2).

Modos de uso:
  python3 scripts/verify_brain.py --check
      Verifica invariantes estructurales del Brain (AGENTS.md, CLAUDE.md,
      unicidad de ADRs canónicos, limpieza de docs/, enlaces relativos).
      Devuelve 0 si todo está en orden, 1 si hay fallos.

  python3 scripts/verify_brain.py --diff
      Analiza cambios locales en el árbol de trabajo (git status --porcelain),
      muestra el enrutamiento condicional hacia ADRs/contratos y recuerda el
      protocolo de write-back cuando procede.

  python3 scripts/verify_brain.py --since <ref>
      Igual que --diff, pero evaluando git diff --name-only <ref>...HEAD.
"""
from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path
from typing import Iterable

MAX_AGENTS_BYTES = 20_000
MAX_AGENTS_LINES = 150
TRANSLATION_SUFFIXES = (".en.md", ".zh-CN.md")
ADR_FILENAME_RE = re.compile(r"^(\d{4})-.*\.md$")
FENCED_CODE_RE = re.compile(r"```.*?```", re.S)
INLINE_CODE_RE = re.compile(r"`[^`\n]+`")
MD_LINK_RE = re.compile(r"\[[^\]]+\]\(([^)#\s]+)(?:#[^)]*)?\)")

# Tabla de enrutamiento espejo de AGENTS.md §3.
ROUTING_RULES: list[tuple[str, tuple[str, ...], tuple[str, ...]]] = [
    (
        "Dualidad autónomo/integrado y puente ModRegistry.GetApi",
        (
            "PelicanChat/ModEntry.cs",
            "PelicanChat/Integration/",
            "PelicanChat/Framework/IPelican4EverChatApi.cs",
            "PelicanChat/manifest.json",
        ),
        (
            "docs/adr/0001-standalone-mod-and-pelican4ever-bridge.md",
            "architecture.md",
        ),
    ),
    (
        "Parches ChatBox, viewport con scroll, pestañas y HarmonyPatchToggle",
        (
            "PelicanChat/Patches/",
            "PelicanChat/UI/",
            "PelicanChat/Modules/ChatViewModule.cs",
            "PelicanChat/Framework/HarmonyPatchToggle.cs",
            "PelicanChat.Core/Viewport/",
        ),
        (
            "docs/adr/0002-chatbox-harmony-viewport-and-tabs.md",
            "GSS.md",
        ),
    ),
    (
        "Sanitización anti-inyección, relevo Discord-only (Yuki) y ruteo de canales",
        (
            "PelicanChat/Modules/DiscordBridgeModule.cs",
            "PelicanChat/Modules/ChatTransportModule.cs",
            "PelicanChat/Models/ChatNetworkModels.cs",
            "PelicanChat.Core/Discord/",
            "PelicanChat.Core/Channels/",
        ),
        (
            "docs/adr/0003-discord-bridge-and-message-sanitization.md",
            "docs/adr/0004-discord-channel-only-and-yuki-relay.md",
            "architecture.md",
        ),
    ),
    (
        "Configuración e internacionalización (default, es, zh-CN)",
        (
            "PelicanChat/i18n/",
            "PelicanChat/Framework/I18n.cs",
            "PelicanChat.Core/Config/ChatConfig.cs",
        ),
        (
            "design.md",
            ".agents/skills/pelican-chat-dev/SKILL.md",
        ),
    ),
]


def find_repo_root(start: Path | None = None) -> Path:
    here = (start or Path(__file__)).resolve()
    for p in (here, *here.parents):
        if (p / "PelicanChat.sln").is_file() and (p / "docs").is_dir():
            return p
    raise RuntimeError("No se encontró la raíz del repositorio PelicanChat.")


def is_translation(path: Path | str) -> bool:
    name = path.name if isinstance(path, Path) else path
    return any(name.endswith(sfx) for sfx in TRANSLATION_SUFFIXES)


def normalize_rel_path(path: str) -> str:
    norm = path.strip().replace("\\", "/")
    while norm.startswith("./"):
        norm = norm[2:]
    return norm


def strip_code_regions(markdown: str) -> str:
    without_fences = FENCED_CODE_RE.sub("", markdown)
    return INLINE_CODE_RE.sub("code", without_fences)


def collect_markdown_targets(repo_root: Path) -> list[Path]:
    candidates: list[Path] = [
        repo_root / "AGENTS.md",
        repo_root / "CLAUDE.md",
        repo_root / "README.md",
        repo_root / "architecture.md",
        repo_root / "design.md",
        repo_root / "GSS.md",
        repo_root / "backlog.md",
    ]
    for folder in (
        repo_root / "docs",
        repo_root / "docs" / "adr",
        repo_root / "docs" / "verifications",
    ):
        if folder.is_dir():
            candidates.extend(
                sorted(p for p in folder.glob("*.md") if not is_translation(p))
            )
    archive_readme = repo_root / "docs" / "archive" / "README.md"
    if archive_readme.is_file():
        candidates.append(archive_readme)
    return candidates


def run_check(repo_root: Path) -> list[str]:
    errors: list[str] = []

    agents_md = repo_root / "AGENTS.md"
    claude_md = repo_root / "CLAUDE.md"
    if not agents_md.is_file():
        errors.append("Falta AGENTS.md en la raíz del repositorio.")
    else:
        raw_bytes = agents_md.read_bytes()
        text = raw_bytes.decode("utf-8", errors="replace")
        line_count = len(text.splitlines())
        if text.lstrip().startswith("---"):
            errors.append("AGENTS.md no debe incluir frontmatter YAML ('---').")
        if len(raw_bytes) >= MAX_AGENTS_BYTES:
            errors.append(
                f"AGENTS.md pesa {len(raw_bytes)} bytes (>= {MAX_AGENTS_BYTES}); debe mantenerse compacto."
            )
        if line_count >= MAX_AGENTS_LINES:
            errors.append(
                f"AGENTS.md tiene {line_count} líneas (>= {MAX_AGENTS_LINES}); debe mantenerse compacto."
            )
        for token in (
            "docs/archive/",
            "docs/verifications/",
            ".en.md",
            ".zh-CN.md",
            "write-back",
            "ERPNext",
            "Yuki",
        ):
            if token.lower() not in text.lower():
                errors.append(
                    f"AGENTS.md debe mencionar explícitamente '{token}' en sus reglas/enrutamiento."
                )

    if not claude_md.is_file():
        errors.append("Falta CLAUDE.md en la raíz del repositorio.")
    else:
        claude_text = claude_md.read_text(encoding="utf-8", errors="replace")
        if "@AGENTS.md" not in claude_text:
            errors.append("CLAUDE.md debe incluir '@AGENTS.md'.")

    adr_dir = repo_root / "docs" / "adr"
    adr_readme = adr_dir / "README.md"
    if not adr_readme.is_file():
        errors.append("Falta docs/adr/README.md.")
        adr_readme_text = ""
    else:
        adr_readme_text = adr_readme.read_text(encoding="utf-8")

    seen_nums: dict[str, str] = {}
    canonical_adrs = sorted(
        p
        for p in adr_dir.glob("*.md")
        if not is_translation(p) and ADR_FILENAME_RE.match(p.name)
    )
    if not canonical_adrs:
        errors.append("No se encontraron ADRs canónicos en docs/adr/.")
    for adr_path in canonical_adrs:
        num = ADR_FILENAME_RE.match(adr_path.name).group(1)
        if num in seen_nums:
            errors.append(
                f"Colisión de número ADR {num}: '{seen_nums[num]}' y '{adr_path.name}'."
            )
        else:
            seen_nums[num] = adr_path.name
        if adr_path.name not in adr_readme_text:
            errors.append(
                f"ADR '{adr_path.name}' no está enlazado en docs/adr/README.md."
            )
        head_text = "\n".join(adr_path.read_text(encoding="utf-8").splitlines()[:20])
        for meta_key in ("**Estado:**", "**Fecha:**", "**Ámbito:**"):
            if meta_key not in head_text:
                errors.append(
                    f"ADR '{adr_path.name}' debe declarar '{meta_key}' en las primeras 20 líneas."
                )

    docs_dir = repo_root / "docs"
    archive_dir = docs_dir / "archive"
    archive_names = (
        {p.name for p in archive_dir.glob("*.md")} if archive_dir.is_dir() else set()
    )
    for doc_file in sorted(docs_dir.glob("*.md")):
        if "-verification" in doc_file.name:
            errors.append(
                f"Reporte de verificación fuera de docs/verifications/: '{doc_file.relative_to(repo_root)}'."
            )
        if doc_file.name != "README.md" and doc_file.name in archive_names:
            errors.append(
                f"Colisión de nombre entre docs/{doc_file.name} y docs/archive/{doc_file.name}."
            )

    for md_path in collect_markdown_targets(repo_root):
        if not md_path.is_file():
            errors.append(f"Falta documento base esperado: {md_path.relative_to(repo_root)}")
            continue
        cleaned = strip_code_regions(md_path.read_text(encoding="utf-8"))
        for match in MD_LINK_RE.finditer(cleaned):
            target = match.group(1)
            if target.startswith(("http://", "https://", "mailto:", "file://")):
                continue
            resolved = (md_path.parent / target).resolve()
            if not str(resolved).startswith(str(repo_root.resolve())) and not resolved.parent.exists():
                continue
            if not resolved.exists():
                rel_src = md_path.relative_to(repo_root)
                errors.append(f"Enlace roto en {rel_src}: '{target}'.")

    return errors


def git_changed_paths_status(repo_root: Path) -> list[str]:
    res = subprocess.run(
        ["git", "-C", str(repo_root), "status", "--porcelain"],
        capture_output=True,
        text=True,
        check=True,
    )
    paths: list[str] = []
    for line in res.stdout.splitlines():
        if not line.strip():
            continue
        raw = line[3:].strip()
        if " -> " in raw:
            raw = raw.split(" -> ", 1)[1].strip()
        paths.append(raw.strip('"').replace("\\", "/"))
    return sorted(set(paths))


def git_changed_paths_since(repo_root: Path, ref: str) -> list[str]:
    res = subprocess.run(
        ["git", "-C", str(repo_root), "diff", "--name-only", f"{ref}...HEAD"],
        capture_output=True,
        text=True,
        check=False,
    )
    if res.returncode != 0:
        res = subprocess.run(
            ["git", "-C", str(repo_root), "diff", "--name-only", f"{ref}..HEAD"],
            capture_output=True,
            text=True,
            check=True,
        )
    return sorted({p.strip().replace("\\", "/") for p in res.stdout.splitlines() if p.strip()})


def match_routing(path: str) -> list[tuple[str, tuple[str, ...]]]:
    matches: list[tuple[str, tuple[str, ...]]] = []
    norm = normalize_rel_path(path)
    for domain, patterns, docs in ROUTING_RULES:
        if any(norm == pat or norm.startswith(pat) for pat in patterns):
            matches.append((domain, docs))
    return matches


def is_governed_runtime_path(path: str) -> bool:
    norm = normalize_rel_path(path)
    return (
        norm.startswith("PelicanChat/") or norm.startswith("PelicanChat.Core/")
    ) and not norm.startswith("PelicanChat/i18n/")


def analyze_changed_paths(changed: Iterable[str]) -> dict[str, object]:
    changed_list = sorted({normalize_rel_path(p) for p in changed if normalize_rel_path(p)})
    path_set = set(changed_list)

    matched_docs: dict[str, set[str]] = {}
    for path in changed_list:
        for domain, docs in match_routing(path):
            matched_docs.setdefault(domain, set()).update(docs)

    archive_touched = [p for p in changed_list if p.startswith("docs/archive/")]
    translations_without_source = []
    for p in changed_list:
        for sfx in TRANSLATION_SUFFIXES:
            if p.endswith(sfx):
                canonical = p[: -len(sfx)] + ".md"
                if canonical not in path_set:
                    translations_without_source.append((p, canonical))

    runtime_touched = [p for p in changed_list if is_governed_runtime_path(p)]
    adr_or_arch_touched = any(
        (p.startswith("docs/adr/") and not is_translation(p) and p != "docs/adr/README.md")
        or p in ("architecture.md", "design.md", "GSS.md")
        for p in changed_list
    )

    return {
        "changed": changed_list,
        "matched_docs": {k: sorted(v) for k, v in matched_docs.items()},
        "archive_touched": archive_touched,
        "translations_without_source": translations_without_source,
        "runtime_touched": runtime_touched,
        "writeback_reminder": bool(runtime_touched) and not adr_or_arch_touched,
    }


def print_diff_report(analysis: dict[str, object], label: str) -> None:
    changed: list[str] = analysis["changed"]  # type: ignore[assignment]
    if not changed:
        print(f"[{label}] Sin cambios detectados.")
        return

    print(f"[{label}] Archivos analizados: {len(changed)}")
    matched_docs: dict[str, list[str]] = analysis["matched_docs"]  # type: ignore[assignment]
    if matched_docs:
        print("\nEnrutamiento condicional (lectura/revisión obligatoria según AGENTS.md):")
        for domain, docs in matched_docs.items():
            print(f"  * {domain}:")
            for doc in docs:
                print(f"      - {doc}")
    else:
        print("\nSin coincidencias directas en las reglas de enrutamiento runtime.")

    warnings: list[str] = []
    archive_touched: list[str] = analysis["archive_touched"]  # type: ignore[assignment]
    if archive_touched:
        warnings.append(
            "Se tocaron archivos en docs/archive/ (solo permitido en auditorías históricas): "
            + ", ".join(archive_touched)
        )

    translations_without_source: list[tuple[str, str]] = analysis["translations_without_source"]  # type: ignore[assignment]
    for tr, canon in translations_without_source:
        warnings.append(
            f"Traducción modificada sin actualizar su fuente canónica: {tr} (falta revisar {canon})."
        )

    if analysis["writeback_reminder"]:
        warnings.append(
            "RECORDATORIO WRITE-BACK: hay cambios en código runtime (PelicanChat/ o PelicanChat.Core/) "
            "sin cambios en docs/adr/*.md ni architecture.md. Comprueba si la tarea altera contratos, "
            "parches Harmony o decisiones y actualiza el Brain antes de cerrar."
        )

    if warnings:
        print("\nAvisos del Brain:")
        for w in warnings:
            print(f"  ! {w}")
    else:
        print("\nOK: Sin avisos de write-back ni rutas prohibidas.")


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Verifica invariantes del Brain de PelicanChat y enruta cambios."
    )
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--check", action="store_true", help="Valida integridad del Brain (por defecto).")
    mode.add_argument("--ci", action="store_true", help="Alias de --check para ejecución en CI.")
    mode.add_argument("--diff", action="store_true", help="Analiza cambios locales en git status.")
    mode.add_argument("--since", metavar="REF", help="Analiza cambios de git diff --name-only <REF>...HEAD.")
    args = parser.parse_args()

    repo_root = find_repo_root()

    if args.diff:
        paths = git_changed_paths_status(repo_root)
        print_diff_report(analyze_changed_paths(paths), "diff local")
        return 0

    if args.since:
        paths = git_changed_paths_since(repo_root, args.since)
        print_diff_report(analyze_changed_paths(paths), f"since {args.since}")
        return 0

    errors = run_check(repo_root)
    if errors:
        print("ERROR: Falló la verificación del Brain:", file=sys.stderr)
        for err in errors:
            print(f"  - {err}", file=sys.stderr)
        return 1

    adr_count = len(
        [
            p
            for p in (repo_root / "docs" / "adr").glob("*.md")
            if not is_translation(p) and ADR_FILENAME_RE.match(p.name)
        ]
    )
    agents_bytes = (repo_root / "AGENTS.md").stat().st_size
    print(
        f"OK: Brain íntegro — AGENTS.md ({agents_bytes} B < {MAX_AGENTS_BYTES} B), "
        f"CLAUDE.md, {adr_count} ADRs únicos e indexados, docs/ raíz limpio y 0 enlaces rotos."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
