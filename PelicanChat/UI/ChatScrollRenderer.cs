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
    /// Renderiza el historial acotado con recorte vertical (ScissorRectangle), scrollbar nativa de Stardew
    /// y la píldora clicable de mensajes nuevos cuando el usuario está leyendo arriba.
    /// </summary>
    public sealed class ChatScrollRenderer
    {
        private const int ScrollbarWidth = 24;
        private const int ArrowSize = 24;
        private const int BoxPadding = 12;

        private static readonly RasterizerState ScissorRasterizer = new()
        {
            ScissorTestEnable = true,
            CullMode = CullMode.None
        };

        private Rectangle upArrowBounds;
        private Rectangle downArrowBounds;
        private Rectangle trackBounds;
        private Rectangle thumbBounds;
        private Rectangle newMessagesPillBounds;
        private bool isDraggingThumb;
        private int dragStartY;
        private int dragStartScrollPx;

        public void DrawOpen(
            SpriteBatch b,
            ChatBox chatBox,
            ChatViewState viewState,
            Func<long, ChatMessage?> resolveVisualMessage,
            ChatTabBarRenderer tabBar)
        {
            int viewportHeight = Math.Max(60, viewState.GetOpenViewportHeightPx());
            int totalBoxHeight = viewportHeight + 20;
            int boxX = chatBox.xPositionOnScreen;
            int boxY = chatBox.yPositionOnScreen - totalBoxHeight;
            int boxWidth = chatBox.chatBox.Width;

            // Fondo principal del historial
            IClickableMenu.drawTextureBox(
                b,
                Game1.mouseCursors,
                new Rectangle(301, 288, 15, 15),
                boxX,
                boxY,
                boxWidth,
                totalBoxHeight,
                Color.White,
                4f,
                drawShadow: false);

            // Barra de pestañas encima del historial
            tabBar.Draw(b, viewState, boxX, boxY, boxWidth);

            // Aviso crítico transversal si aplica
            ChatEntry? criticalToast = viewState.GetActiveCriticalToast();
            if (criticalToast != null)
            {
                DrawCriticalToast(b, criticalToast, boxX, boxY - ChatTabBarRenderer.TabBarHeight - 34, boxWidth);
            }

            // Área recortada para mensajes
            var contentArea = new Rectangle(
                boxX + 8,
                boxY + 8,
                boxWidth - ScrollbarWidth - 16,
                viewportHeight + 4);

            DrawClippedMessages(b, contentArea, viewState.CurrentTab, resolveVisualMessage);

            // Scrollbar a la derecha
            DrawScrollbar(b, viewState.CurrentTab, viewportHeight, boxX + boxWidth - ScrollbarWidth - 6, boxY + 10, viewportHeight);

            // Píldora "↓ N nuevos" si hay scroll hacia arriba y mensajes nuevos
            newMessagesPillBounds = Rectangle.Empty;
            if (viewState.CurrentTab.ScrollOffsetPx > 0 && viewState.CurrentTab.UnreadWhileScrolled > 0)
            {
                DrawNewMessagesPill(b, viewState.CurrentTab.UnreadWhileScrolled, boxX + boxWidth - ScrollbarWidth - 170, boxY + totalBoxHeight - 36);
            }

            // Etiqueta del canal activo junto a la caja de texto
            DrawActiveChannelTag(b, viewState.ActiveTab, chatBox);
        }

        public void DrawClosed(
            SpriteBatch b,
            ChatBox chatBox,
            ChatViewState viewState,
            Func<long, ChatMessage?> resolveVisualMessage)
        {
            IReadOnlyList<ChatEntry> items = viewState.GetTab(ChatChannelType.All).Buffer.Items;
            int maxHeight = viewState.MaxClosedHeightPx;
            int visibleHeight = 0;
            var toDraw = new List<(ChatEntry Entry, ChatMessage Msg)>();

            for (int i = items.Count - 1; i >= 0; i--)
            {
                ChatEntry entry = items[i];
                if (entry.Alpha <= 0.01f)
                    continue;

                ChatMessage? msg = resolveVisualMessage(entry.Id);
                if (msg == null)
                    continue;

                int h = Math.Max(1, msg.verticalSize);
                if (visibleHeight + h > maxHeight && toDraw.Count > 0)
                    break;

                visibleHeight += h;
                toDraw.Add((entry, msg));
            }

            if (toDraw.Count == 0)
                return;

            int boxX = chatBox.xPositionOnScreen;
            int baseBottomY = chatBox.yPositionOnScreen + chatBox.chatBox.Height;
            int boxY = baseBottomY - visibleHeight - 20;

            IClickableMenu.drawTextureBox(
                b,
                Game1.mouseCursors,
                new Rectangle(301, 288, 15, 15),
                boxX,
                boxY,
                chatBox.chatBox.Width,
                visibleHeight + 20,
                Color.White,
                4f,
                drawShadow: false);

            int currentY = baseBottomY - 8;
            for (int i = 0; i < toDraw.Count; i++)
            {
                ChatMessage msg = toDraw[i].Msg;
                msg.alpha = toDraw[i].Entry.Alpha;
                currentY -= msg.verticalSize;
                msg.draw(b, boxX + 12, currentY);
            }
        }

        public bool HandleLeftClick(int x, int y, ChatViewState viewState)
        {
            if (newMessagesPillBounds != Rectangle.Empty && newMessagesPillBounds.Contains(x, y))
            {
                viewState.ScrollToBottom();
                Game1.playSound("smallSelect");
                return true;
            }

            if (upArrowBounds.Contains(x, y))
            {
                viewState.ScrollBy(36);
                Game1.playSound("shiny4");
                return true;
            }

            if (downArrowBounds.Contains(x, y))
            {
                viewState.ScrollBy(-36);
                Game1.playSound("shiny4");
                return true;
            }

            if (thumbBounds != Rectangle.Empty && thumbBounds.Contains(x, y))
            {
                isDraggingThumb = true;
                dragStartY = y;
                dragStartScrollPx = viewState.CurrentTab.ScrollOffsetPx;
                return true;
            }

            if (trackBounds != Rectangle.Empty && trackBounds.Contains(x, y))
            {
                int page = Math.Max(40, viewState.MaxVisibleHeightPx - 32);
                if (y < thumbBounds.Y)
                    viewState.ScrollBy(page);
                else
                    viewState.ScrollBy(-page);
                Game1.playSound("smallSelect");
                return true;
            }

            return false;
        }

        public void UpdateDrag(ChatViewState viewState)
        {
            if (!isDraggingThumb)
                return;

            if (Game1.input.GetMouseState().LeftButton != Microsoft.Xna.Framework.Input.ButtonState.Pressed)
            {
                isDraggingThumb = false;
                return;
            }

            int maxScroll = viewState.CurrentTab.GetMaxScrollPx(viewState.MaxVisibleHeightPx);
            int usableTrack = Math.Max(1, trackBounds.Height - thumbBounds.Height);
            int mouseY = Game1.getMouseY();
            int dy = mouseY - dragStartY;

            // Arrastrar hacia arriba aumenta ScrollOffsetPx
            int deltaScroll = -(int)Math.Round(dy * (maxScroll / (double)usableTrack));
            int target = Math.Clamp(dragStartScrollPx + deltaScroll, 0, maxScroll);
            int diff = target - viewState.CurrentTab.ScrollOffsetPx;
            if (diff != 0)
                viewState.ScrollBy(diff);
        }

        private static void DrawClippedMessages(
            SpriteBatch b,
            Rectangle clipArea,
            ChatTabState tab,
            Func<long, ChatMessage?> resolveVisualMessage)
        {
            GraphicsDevice gd = Game1.graphics.GraphicsDevice;
            Rectangle prevScissor = gd.ScissorRectangle;

            b.End();
            gd.ScissorRectangle = Rectangle.Intersect(clipArea, gd.Viewport.Bounds);
            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, ScissorRasterizer);

            try
            {
                IReadOnlyList<ChatEntry> items = tab.Buffer.Items;
                int drawBottomY = clipArea.Bottom - 2 + tab.ScrollOffsetPx;
                int cursorY = drawBottomY;

                for (int i = items.Count - 1; i >= 0; i--)
                {
                    ChatEntry entry = items[i];
                    ChatMessage? msg = resolveVisualMessage(entry.Id);
                    if (msg == null)
                        continue;

                    int h = Math.Max(1, msg.verticalSize);
                    cursorY -= h;

                    if (cursorY > clipArea.Bottom)
                        continue;
                    if (cursorY + h < clipArea.Top)
                        break;

                    msg.alpha = 1f;
                    msg.draw(b, clipArea.X + 4, cursorY);
                }
            }
            finally
            {
                b.End();
                gd.ScissorRectangle = prevScissor;
                b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
            }
        }

        private void DrawScrollbar(SpriteBatch b, ChatTabState tab, int viewportHeight, int x, int y, int height)
        {
            upArrowBounds = new Rectangle(x, y, ArrowSize, ArrowSize);
            downArrowBounds = new Rectangle(x, y + height - ArrowSize, ArrowSize, ArrowSize);
            trackBounds = new Rectangle(x + 4, y + ArrowSize + 2, 16, Math.Max(12, height - (ArrowSize * 2) - 4));

            b.Draw(Game1.mouseCursors, upArrowBounds, new Rectangle(421, 459, 11, 12), Color.White);
            b.Draw(Game1.mouseCursors, downArrowBounds, new Rectangle(421, 472, 11, 12), Color.White);

            IClickableMenu.drawTextureBox(
                b,
                Game1.mouseCursors,
                new Rectangle(403, 383, 6, 6),
                trackBounds.X,
                trackBounds.Y,
                trackBounds.Width,
                trackBounds.Height,
                Color.White * 0.7f,
                2f,
                drawShadow: false);

            int maxScroll = tab.GetMaxScrollPx(viewportHeight);
            if (maxScroll <= 0)
            {
                thumbBounds = Rectangle.Empty;
                return;
            }

            float visibleRatio = Math.Clamp(viewportHeight / (float)Math.Max(1, tab.TotalContentHeightPx), 0.15f, 1f);
            int thumbHeight = Math.Max(20, (int)(trackBounds.Height * visibleRatio));
            float scrollFraction = 1f - (tab.ScrollOffsetPx / (float)maxScroll); // 1 = abajo, 0 = arriba
            int thumbY = trackBounds.Y + (int)((trackBounds.Height - thumbHeight) * scrollFraction);
            thumbBounds = new Rectangle(trackBounds.X + 2, thumbY, trackBounds.Width - 4, thumbHeight);

            b.Draw(Game1.mouseCursors, thumbBounds, new Rectangle(435, 463, 6, 10), Color.White);
        }

        private void DrawNewMessagesPill(SpriteBatch b, int count, int x, int y)
        {
            string text = I18n.Get("ui.new_messages", new { count });
            Vector2 size = Game1.smallFont.MeasureString(text);
            newMessagesPillBounds = new Rectangle(x, y, (int)size.X + 24, 30);

            IClickableMenu.drawTextureBox(
                b,
                Game1.mouseCursors,
                new Rectangle(301, 288, 15, 15),
                newMessagesPillBounds.X,
                newMessagesPillBounds.Y,
                newMessagesPillBounds.Width,
                newMessagesPillBounds.Height,
                new Color(255, 235, 160),
                2f,
                drawShadow: false);

            Utility.drawTextWithShadow(
                b,
                text,
                Game1.smallFont,
                new Vector2(newMessagesPillBounds.X + 12, newMessagesPillBounds.Y + 4),
                new Color(60, 40, 10));
        }

        private static void DrawCriticalToast(SpriteBatch b, ChatEntry toast, int x, int y, int width)
        {
            string text = toast.DisplayText;
            Vector2 size = Game1.smallFont.MeasureString(text);
            int height = Math.Max(30, (int)size.Y + 12);

            IClickableMenu.drawTextureBox(
                b,
                Game1.mouseCursors,
                new Rectangle(301, 288, 15, 15),
                x,
                y,
                width,
                height,
                new Color(255, 220, 140),
                3f,
                drawShadow: false);

            Utility.drawTextWithShadow(
                b,
                text,
                Game1.smallFont,
                new Vector2(x + 12, y + 6),
                new Color(140, 20, 20));
        }

        private static void DrawActiveChannelTag(SpriteBatch b, ChatChannelType activeTab, ChatBox chatBox)
        {
            string tag = ChatTabBarRenderer.GetChannelTag(activeTab);
            Vector2 size = Game1.smallFont.MeasureString(tag);
            float x = chatBox.chatBox.X + chatBox.chatBox.Width - size.X - 58;
            float y = chatBox.chatBox.Y + 14;
            Utility.drawTextWithShadow(b, tag, Game1.smallFont, new Vector2(x, y), ChatTabBarRenderer.GetChannelColor(activeTab));
        }
    }
}
