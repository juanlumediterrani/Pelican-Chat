using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace PelicanChat.Framework
{
    /// <summary>
    /// Captura parches Harmony propios y permite desactivarlos/reactivarlos en caliente sin reiniciar.
    /// </summary>
    public sealed class HarmonyPatchToggle
    {
        private sealed record CapturedPatch(
            MethodBase Original,
            HarmonyPatchType Kind,
            MethodInfo PatchMethod,
            int Index,
            int Priority,
            string[] Before,
            string[] After,
            bool Debug);

        private readonly List<CapturedPatch> captured = new();
        private readonly HashSet<(MethodBase Original, HarmonyPatchType Kind, MethodInfo PatchMethod)> capturedKeys = new();

        public void CaptureOwnedPatches(Harmony harmony)
        {
            if (harmony == null)
                throw new ArgumentNullException(nameof(harmony));

            string owner = harmony.Id;
            foreach (MethodBase original in harmony.GetPatchedMethods())
            {
                HarmonyLib.Patches? info = Harmony.GetPatchInfo(original);
                if (info == null)
                    continue;

                CaptureGroup(original, HarmonyPatchType.Prefix, info.Prefixes, owner);
                CaptureGroup(original, HarmonyPatchType.Postfix, info.Postfixes, owner);
                CaptureGroup(original, HarmonyPatchType.Transpiler, info.Transpilers, owner);
                CaptureGroup(original, HarmonyPatchType.Finalizer, info.Finalizers, owner);
            }

            captured.Sort((a, b) => a.Index.CompareTo(b.Index));
        }

        public void Disable(Harmony harmony)
        {
            CaptureOwnedPatches(harmony);
            harmony.UnpatchAll(harmony.Id);
        }

        public void Enable(Harmony harmony)
        {
            if (harmony == null)
                throw new ArgumentNullException(nameof(harmony));

            string owner = harmony.Id;
            foreach (CapturedPatch entry in captured)
            {
                if (IsAlreadyApplied(entry, owner))
                    continue;

                var hm = new HarmonyMethod(entry.PatchMethod, entry.Priority, entry.Before, entry.After, entry.Debug);
                switch (entry.Kind)
                {
                    case HarmonyPatchType.Prefix:
                        harmony.Patch(entry.Original, prefix: hm);
                        break;
                    case HarmonyPatchType.Postfix:
                        harmony.Patch(entry.Original, postfix: hm);
                        break;
                    case HarmonyPatchType.Transpiler:
                        harmony.Patch(entry.Original, transpiler: hm);
                        break;
                    case HarmonyPatchType.Finalizer:
                        harmony.Patch(entry.Original, finalizer: hm);
                        break;
                }
            }
        }

        private void CaptureGroup(MethodBase original, HarmonyPatchType kind, IReadOnlyCollection<Patch>? group, string owner)
        {
            if (group == null)
                return;

            foreach (Patch patch in group)
            {
                if (!string.Equals(patch.owner, owner, StringComparison.Ordinal))
                    continue;

                var key = (original, kind, patch.PatchMethod);
                if (!capturedKeys.Add(key))
                    continue;

                captured.Add(new CapturedPatch(
                    original, kind, patch.PatchMethod, patch.index, patch.priority,
                    patch.before ?? Array.Empty<string>(), patch.after ?? Array.Empty<string>(), patch.debug));
            }
        }

        private static bool IsAlreadyApplied(CapturedPatch entry, string owner)
        {
            HarmonyLib.Patches? info = Harmony.GetPatchInfo(entry.Original);
            if (info == null)
                return false;

            IReadOnlyCollection<Patch>? group = entry.Kind switch
            {
                HarmonyPatchType.Prefix => info.Prefixes,
                HarmonyPatchType.Postfix => info.Postfixes,
                HarmonyPatchType.Transpiler => info.Transpilers,
                HarmonyPatchType.Finalizer => info.Finalizers,
                _ => null
            };

            if (group == null)
                return false;

            foreach (Patch patch in group)
            {
                if (string.Equals(patch.owner, owner, StringComparison.Ordinal) && patch.PatchMethod == entry.PatchMethod)
                    return true;
            }

            return false;
        }
    }
}
