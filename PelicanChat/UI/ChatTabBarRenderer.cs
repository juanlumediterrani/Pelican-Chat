using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PelicanChat.Core.Channels;
using PelicanChat.Core.Viewport;
using PelicanChat.Framework;
using StardewValley;
using StardewValley.Menus;

namespace PelicanChat.UI
{
    /// <summary>
    /// Dibuja y gestiona los clics sobre la barra de pestañas encima del historial de ChatBox.
    /// </summary>
    public sealed class ChatTabBarRenderer
    {
        public const int TabBarHeight = 38;
        private const int TabSpacing = 4;

        private readonly List<(ChatChannelType Channel, Rectangle Bounds)> tabBounds = new();

        public void Draw(SpriteBatch b, ChatViewState viewState, int boxX, int boxTopY, int boxWidth)
        {
            tabBounds.Clear();

            int tabY = boxTopY - TabBarHeight + 4;
            int cursorX = boxX + 4;
            SpriteFont font = Game1.smallFont;

            foreach (ChatChannelType channel in ChatViewState.TabOrder)
            {
                string label = GetTabLabel(channel);
                int unread = viewState.GetTab(channel).UnreadCount;
                string display = unread > 0 ? $"{label} ({unread})" : label;

                Vector2 textSize = font.MeasureString(display);
                int tabWidth = Math.Max(72, (int)textSize.X + 24);
                if (cursorX + tabWidth > boxX + boxWidth - 4)
                    break;

                var rect = new Rectangle(cursorX, tabY, tabWidth, TabBarHeight - 4);
                tabBounds.Add((channel, rect));

                bool isActive = viewState.ActiveTab == channel;
                Color bgTint = isActive
                    ? Color.White
                    : unread > 0 ? new Color(255, 240, 200) : new Color(205, 195, 180);

                IClickableMenu.drawTextureBox(
                    b,
                    Game1.mouseCursors,
                    new Rectangle(301, 288, 15, 15),
                    rect.X,
                    rect.Y,
                    rect.Width,
                    rect.Height,
                    bgTint,
                    3f,
                    drawShadow: false);

                Color textColor = isActive
                    ? GetChannelColor(channel)
                    : unread > 0 ? new Color(255, 215, 0) : new Color(60, 45, 30);

                Vector2 textPos = new(
                    rect.X + (rect.Width - textSize.X) / 2f,
                    rect.Y + (rect.Height - textSize.Y) / 2f + 1f);

                Utility.drawTextWithShadow(b, display, font, textPos, textColor, 1f, 0.99f, -1, -1, 0.5f);

                cursorX += tabWidth + TabSpacing;
            }
        }

        public bool TryGetClickedTab(int x, int y, out ChatChannelType channel)
        {
            for (int i = 0; i < tabBounds.Count; i++)
            {
                if (tabBounds[i].Bounds.Contains(x, y))
                {
                    channel = tabBounds[i].Channel;
                    return true;
                }
            }

            channel = ChatChannelType.All;
            return false;
        }

        public static string GetTabLabel(ChatChannelType channel) => channel switch
        {
            ChatChannelType.All => I18n.Get("tab.all"),
            ChatChannelType.Local => I18n.Get("tab.local"),
            ChatChannelType.Global => I18n.Get("tab.global"),
            ChatChannelType.Discord => I18n.Get("tab.discord"),
            ChatChannelType.Whisper => I18n.Get("tab.whisper"),
            ChatChannelType.System => I18n.Get("tab.system"),
            _ => channel.ToString()
        };

        public static string GetChannelTag(ChatChannelType channel) => channel switch
        {
            ChatChannelType.Local => I18n.Get("ui.channel_tag.local"),
            ChatChannelType.Discord => I18n.Get("ui.channel_tag.discord"),
            ChatChannelType.Whisper => I18n.Get("ui.channel_tag.whisper"),
            ChatChannelType.System => I18n.Get("ui.channel_tag.system"),
            _ => I18n.Get("ui.channel_tag.global")
        };

        public static Color GetChannelColor(ChatChannelType channel) => channel switch
        {
            ChatChannelType.Local => new Color(90, 210, 90),
            ChatChannelType.Discord => new Color(114, 137, 218),
            ChatChannelType.Whisper => new Color(0, 180, 180),
            ChatChannelType.System => new Color(255, 200, 40),
            _ => new Color(80, 40, 20)
        };
    }
}
