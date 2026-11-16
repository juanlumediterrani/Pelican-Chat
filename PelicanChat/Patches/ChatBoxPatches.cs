using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PelicanChat.Core.Channels;
using PelicanChat.Core.Viewport;
using PelicanChat.Framework;
using PelicanChat.UI;
using StardewValley;
using StardewValley.Menus;

namespace PelicanChat.Patches
{
    /// <summary>
    /// Parches Harmony sobre StardewValley.Menus.ChatBox para añadir scroll, pestañas y ruteo contextual
    /// manteniendo intacta la instancia nativa de Game1.chatBox (ADR-0002).
    /// </summary>
    internal static class ChatBoxPatches
    {
        private static readonly ChatTabBarRenderer TabBar = new();
        private static readonly ChatScrollRenderer ScrollRenderer = new();
        private static readonly Dictionary<long, ChatMessage> VisualMessages = new();
        private static readonly Queue<long> VisualOrder = new();

        private static bool isInternalInject;
        private static ChatChannelType? pendingExplicitChannel;
        private static ExternalOrigin pendingOrigin = ExternalOrigin.None;
        private static bool pendingCritical;
        private static string? pendingSenderName;
        private static string? pendingCleanBody;

        public static event Action<ChatEntry>? OnLocalEntryCaptured;

        public static void Apply(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(ChatBox), nameof(ChatBox.receiveChatMessage)),
                postfix: new HarmonyMethod(typeof(ChatBoxPatches), nameof(Postfix_ReceiveChatMessage)));

            harmony.Patch(
                original: AccessTools.Method(typeof(ChatBox), nameof(ChatBox.addMessage), new[] { typeof(string), typeof(Color) }),
                postfix: new HarmonyMethod(typeof(ChatBoxPatches), nameof(Postfix_AddMessage)));

            harmony.Patch(
                original: AccessTools.Method(typeof(ChatBox), nameof(ChatBox.receiveScrollWheelAction)),
                postfix: new HarmonyMethod(typeof(ChatBoxPatches), nameof(Postfix_ReceiveScrollWheelAction)));

            harmony.Patch(
                original: AccessTools.Method(typeof(ChatBox), nameof(ChatBox.isWithinBounds)),
                postfix: new HarmonyMethod(typeof(ChatBoxPatches), nameof(Postfix_IsWithinBounds)));

            harmony.Patch(
                original: AccessTools.Method(typeof(ChatBox), nameof(ChatBox.receiveLeftClick), new[] { typeof(int), typeof(int), typeof(bool) }),
                prefix: new HarmonyMethod(typeof(ChatBoxPatches), nameof(Prefix_ReceiveLeftClick)));

            harmony.Patch(
                original: AccessTools.Method(typeof(ChatBox), nameof(ChatBox.receiveKeyPress)),
                prefix: new HarmonyMethod(typeof(ChatBoxPatches), nameof(Prefix_ReceiveKeyPress)));

            harmony.Patch(
                original: AccessTools.Method(typeof(ChatBox), nameof(ChatBox.textBoxEnter), new[] { typeof(string) }),
                prefix: new HarmonyMethod(typeof(ChatBoxPatches), nameof(Prefix_TextBoxEnter)));

            harmony.Patch(
                original: AccessTools.Method(typeof(ChatBox), nameof(ChatBox.update)),
                postfix: new HarmonyMethod(typeof(ChatBoxPatches), nameof(Postfix_Update)));

            harmony.Patch(
                original: AccessTools.Method(typeof(ChatBox), nameof(ChatBox.draw), new[] { typeof(SpriteBatch) }),
                prefix: new HarmonyMethod(typeof(ChatBoxPatches), nameof(Prefix_Draw)));
        }

        public static void ClearCache()
        {
            VisualMessages.Clear();
            VisualOrder.Clear();
        }

        public static void InjectMessage(
            string formattedText,
            Color color,
            ChatChannelType explicitChannel,
            ExternalOrigin origin,
            bool isCritical = false,
            string? senderName = null,
            string? cleanBody = null)
        {
            ChatBox? box = Game1.chatBox;
            if (box == null)
                return;

            isInternalInject = true;
            pendingExplicitChannel = explicitChannel;
            pendingOrigin = origin;
            pendingCritical = isCritical;
            pendingSenderName = senderName;
            pendingCleanBody = cleanBody;
            try
            {
                box.addMessage(formattedText, color);
            }
            finally
            {
                pendingExplicitChannel = null;
                pendingOrigin = ExternalOrigin.None;
                pendingCritical = false;
                pendingSenderName = null;
                pendingCleanBody = null;
                isInternalInject = false;
            }
        }

        private static void Postfix_ReceiveChatMessage(
            ChatBox __instance,
            long sourceFarmer,
            int chatKind,
            LocalizedContentManager.LanguageCode language,
            string message)
        {
            if (__instance.messages.Count == 0)
                return;

            string formatted = __instance.formatMessage(sourceFarmer, chatKind, message);
            Color color = __instance.messageColor(chatKind);
            CaptureMessage(__instance, formatted, chatKind, language, color);
        }

        private static void Postfix_AddMessage(ChatBox __instance, string message, Color color)
        {
            if (__instance.messages.Count == 0)
                return;

            CaptureMessage(__instance, message, 0, LocalizedContentManager.CurrentLanguageCode, color);
        }

        private static void CaptureMessage(
            ChatBox chatBox,
            string formattedText,
            int chatKind,
            LocalizedContentManager.LanguageCode language,
            Color color)
        {
            ModContext? ctx = ModContext.Current;
            if (ctx == null)
                return;

            var chatColor = new ChatColor(color.R, color.G, color.B, color.A);
            ClassificationResult classified = pendingExplicitChannel.HasValue
                ? new ClassificationResult(pendingExplicitChannel.Value, pendingCritical)
                : SystemMessageClassifier.Classify(chatKind, formattedText, chatColor);

            string timestamp = ctx.PelicanBridge.GetTimestampText();
            string displayText = string.IsNullOrEmpty(timestamp) ? formattedText : $"{timestamp}{formattedText}";

            int wrapWidth = Math.Max(200, chatBox.chatBox.Width - 42);
            var visualMsg = new ChatMessage();
            string wrapped = Game1.parseText(displayText, chatBox.chatBox.Font, wrapWidth);
            visualMsg.timeLeftToDisplay = 600;
            visualMsg.verticalSize = (int)chatBox.chatBox.Font.MeasureString(wrapped).Y + 4;
            visualMsg.color = color;
            visualMsg.language = language;
            visualMsg.parseMessageForEmoji(wrapped);

            ExternalOrigin origin = isInternalInject
                ? pendingOrigin
                : (classified.Channel == ChatChannelType.Discord ? ExternalOrigin.Discord : ExternalOrigin.None);

            long id = ctx.ViewState.NextId();
            var entry = new ChatEntry
            {
                Id = id,
                Channel = classified.Channel,
                Origin = origin,
                TimestampText = timestamp,
                SenderName = isInternalInject && !string.IsNullOrWhiteSpace(pendingSenderName) ? pendingSenderName : string.Empty,
                RawText = isInternalInject && !string.IsNullOrWhiteSpace(pendingCleanBody) ? pendingCleanBody : formattedText,
                DisplayText = displayText,
                ChatKind = chatKind,
                Color = chatColor,
                IsCritical = classified.IsCritical,
                MeasuredHeightPx = visualMsg.verticalSize,
                TimeLeftToDisplay = 600,
                Alpha = 1f
            };

            StoreVisualMessage(id, visualMsg, ctx.Config.MaxHistoryPerChannel * 8);
            ctx.ViewState.AddEntry(entry);
            OnLocalEntryCaptured?.Invoke(entry);
        }

        private static void StoreVisualMessage(long id, ChatMessage msg, int maxCache)
        {
            VisualMessages[id] = msg;
            VisualOrder.Enqueue(id);
            while (VisualOrder.Count > maxCache)
            {
                long oldId = VisualOrder.Dequeue();
                VisualMessages.Remove(oldId);
            }
        }

        private static void Postfix_ReceiveScrollWheelAction(ChatBox __instance, int direction)
        {
            if (__instance.choosingEmoji || direction == 0)
                return;

            ModContext? ctx = ModContext.Current;
            if (ctx == null || !__instance.chatBox.Selected)
                return;

            int step = direction > 0 ? 48 : -48;
            ctx.ViewState.ScrollBy(step);
        }

        private static void Postfix_IsWithinBounds(ChatBox __instance, int x, int y, ref bool __result)
        {
            if (__result || !__instance.chatBox.Selected)
                return;

            ModContext? ctx = ModContext.Current;
            if (ctx == null)
                return;

            int boxX = __instance.xPositionOnScreen;
            int boxW = __instance.width;
            if (x < boxX || x >= boxX + boxW)
                return;

            int openHeight = Math.Max(60, ctx.ViewState.GetOpenViewportHeightPx()) + 20;
            int topY = __instance.yPositionOnScreen - openHeight - ChatTabBarRenderer.TabBarHeight - 6;
            int bottomY = __instance.yPositionOnScreen + __instance.height;

            if (y >= topY && y < bottomY)
                __result = true;
        }

        private static bool Prefix_ReceiveLeftClick(ChatBox __instance, int x, int y)
        {
            if (!__instance.chatBox.Selected)
                return true;

            ModContext? ctx = ModContext.Current;
            if (ctx == null)
                return true;

            if (TabBar.TryGetClickedTab(x, y, out ChatChannelType clickedTab))
            {
                ctx.ViewState.SelectTab(clickedTab);
                Game1.playSound("smallSelect");
                return false;
            }

            if (ScrollRenderer.HandleLeftClick(x, y, ctx.ViewState))
                return false;

            return true;
        }

        private static bool Prefix_ReceiveKeyPress(ChatBox __instance, Keys key)
        {
            if (!__instance.chatBox.Selected)
                return true;

            ModContext? ctx = ModContext.Current;
            if (ctx == null)
                return true;

            KeyboardState kb = Game1.input.GetKeyboardState();
            bool shift = kb.IsKeyDown(Keys.LeftShift) || kb.IsKeyDown(Keys.RightShift);
            bool ctrl = kb.IsKeyDown(Keys.LeftControl) || kb.IsKeyDown(Keys.RightControl);

            switch (key)
            {
                case Keys.Tab:
                    ctx.ViewState.CycleTab(shift ? -1 : 1);
                    Game1.playSound("smallSelect");
                    return false;

                case Keys.PageUp:
                    ctx.ViewState.ScrollBy(Math.Max(60, ctx.ViewState.MaxVisibleHeightPx - 36));
                    return false;

                case Keys.PageDown:
                    ctx.ViewState.ScrollBy(-Math.Max(60, ctx.ViewState.MaxVisibleHeightPx - 36));
                    return false;

                case Keys.Home when ctrl:
                    ctx.ViewState.ScrollToTop();
                    return false;

                case Keys.End when ctrl:
                    ctx.ViewState.ScrollToBottom();
                    return false;
            }

            return true;
        }

        private static bool Prefix_TextBoxEnter(ChatBox __instance, ref string text_to_send)
        {
            if (string.IsNullOrEmpty(text_to_send))
                return true;

            ModContext? ctx = ModContext.Current;
            if (ctx == null)
                return true;

            OutgoingResolution resolution = ChatRouterLogic.ResolveOutgoing(text_to_send, ctx.ViewState.ActiveTab);
            switch (resolution.Action)
            {
                case OutgoingActionKind.SendLocal:
                case OutgoingActionKind.SendDiscord:
                case OutgoingActionKind.ReplyWhisper:
                    text_to_send = resolution.CommandToExecute ?? text_to_send;
                    return true;

                case OutgoingActionKind.BlockReadOnlySystem:
                    __instance.addInfoMessage(I18n.Get("chat.system_tab_readonly"));
                    return false;

                default:
                    return true;
            }
        }

        private static void Postfix_Update(ChatBox __instance)
        {
            ModContext? ctx = ModContext.Current;
            if (ctx == null)
                return;

            ctx.ViewState.TickFadeTimers(__instance.chatBox.Selected);
            if (__instance.chatBox.Selected)
                ScrollRenderer.UpdateDrag(ctx.ViewState);
        }

        private static bool Prefix_Draw(ChatBox __instance, SpriteBatch b)
        {
            ModContext? ctx = ModContext.Current;
            if (ctx == null)
                return true;

            if (__instance.chatBox.Selected)
            {
                ScrollRenderer.DrawOpen(
                    b,
                    __instance,
                    ctx.ViewState,
                    id => VisualMessages.TryGetValue(id, out ChatMessage? msg) ? msg : null,
                    TabBar);

                __instance.chatBox.Draw(b, drawShadow: false);
                __instance.emojiMenuIcon.draw(b, Color.White, 0.99f);
                if (__instance.choosingEmoji)
                    __instance.emojiMenu.draw(b);

                if (__instance.isWithinBounds(Game1.getMouseX(), Game1.getMouseY()) && !Game1.options.hardwareCursor)
                {
                    Game1.mouseCursor = Game1.options.gamepadControls
                        ? Game1.cursor_gamepad_pointer
                        : Game1.cursor_default;
                }
            }
            else
            {
                ScrollRenderer.DrawClosed(
                    b,
                    __instance,
                    ctx.ViewState,
                    id => VisualMessages.TryGetValue(id, out ChatMessage? msg) ? msg : null);
            }

            return false;
        }
    }
}
