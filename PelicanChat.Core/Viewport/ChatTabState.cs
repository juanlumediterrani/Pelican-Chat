using System;
using System.Collections.Generic;
using PelicanChat.Core.Channels;

namespace PelicanChat.Core.Viewport
{
    /// <summary>
    /// Estado de historial, desplazamiento (scroll) y notificaciones no leídas de una pestaña.
    /// </summary>
    public sealed class ChatTabState
    {
        public ChatChannelType Channel { get; }
        public ChatHistoryBuffer<ChatEntry> Buffer { get; }

        /// <summary>Desplazamiento vertical desde el fondo en píxeles (0 = mensajes más recientes).</summary>
        public int ScrollOffsetPx { get; private set; }

        /// <summary>Mensajes recibidos mientras esta pestaña no estaba seleccionada.</summary>
        public int UnreadCount { get; private set; }

        /// <summary>Mensajes recibidos mientras el jugador estaba leyendo arriba (ScrollOffsetPx &gt; 0).</summary>
        public int UnreadWhileScrolled { get; private set; }

        public ChatTabState(ChatChannelType channel, int capacity)
        {
            Channel = channel;
            Buffer = new ChatHistoryBuffer<ChatEntry>(capacity);
        }

        public int TotalContentHeightPx
        {
            get
            {
                int total = 0;
                IReadOnlyList<ChatEntry> list = Buffer.Items;
                for (int i = 0; i < list.Count; i++)
                    total += Math.Max(1, list[i].MeasuredHeightPx);
                return total;
            }
        }

        public int GetMaxScrollPx(int viewportHeightPx)
        {
            if (viewportHeightPx <= 0)
                return 0;
            return Math.Max(0, TotalContentHeightPx - viewportHeightPx);
        }

        public void Append(ChatEntry entry, bool isActiveTab, int viewportHeightPx)
        {
            Buffer.Add(entry);

            if (!isActiveTab)
            {
                UnreadCount++;
            }

            if (ScrollOffsetPx > 0)
            {
                // Scroll Lock: compensa la altura del nuevo mensaje para mantener fija la lectura
                int maxScroll = GetMaxScrollPx(viewportHeightPx);
                ScrollOffsetPx = Math.Min(maxScroll, ScrollOffsetPx + Math.Max(1, entry.MeasuredHeightPx));
                if (isActiveTab)
                    UnreadWhileScrolled++;
            }
        }

        public void MarkRead()
        {
            UnreadCount = 0;
        }

        public void ScrollBy(int deltaPx, int viewportHeightPx)
        {
            int maxScroll = GetMaxScrollPx(viewportHeightPx);
            int next = Math.Clamp(ScrollOffsetPx + deltaPx, 0, maxScroll);
            ScrollOffsetPx = next;
            if (ScrollOffsetPx == 0)
            {
                UnreadWhileScrolled = 0;
            }
        }

        public void ScrollToBottom()
        {
            ScrollOffsetPx = 0;
            UnreadWhileScrolled = 0;
        }

        public void ScrollToTop(int viewportHeightPx)
        {
            ScrollOffsetPx = GetMaxScrollPx(viewportHeightPx);
        }
    }
}
