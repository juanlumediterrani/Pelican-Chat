using System;
using System.Collections.Generic;
using PelicanChat.Core.Channels;
using PelicanChat.Core.Config;

namespace PelicanChat.Core.Viewport
{
    /// <summary>
    /// Estado completo de la vista de chat (pestañas, buffers circulares, scroll y avisos críticos transversales).
    /// </summary>
    public sealed class ChatViewState
    {
        public static readonly IReadOnlyList<ChatChannelType> TabOrder = new[]
        {
            ChatChannelType.All,
            ChatChannelType.Local,
            ChatChannelType.Global,
            ChatChannelType.Discord,
            ChatChannelType.Whisper,
            ChatChannelType.System
        };

        private readonly Dictionary<ChatChannelType, ChatTabState> tabs = new();
        private long nextEntryId = 1;

        public ChatChannelType ActiveTab { get; private set; }
        public int MaxVisibleHeightPx { get; }
        public int MaxClosedHeightPx { get; }
        public bool IncludeDiscordInAllTab { get; }
        public int CriticalToastDurationSeconds { get; }

        public ChatEntry? ActiveCriticalToast { get; private set; }
        public DateTimeOffset CriticalToastExpiresUtc { get; private set; }

        public ChatViewState(ChatConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            MaxVisibleHeightPx = config.MaxVisibleHeight;
            MaxClosedHeightPx = config.MaxClosedHeight;
            IncludeDiscordInAllTab = config.IncludeDiscordInAllTab;
            CriticalToastDurationSeconds = config.CriticalToastDurationSeconds;
            ActiveTab = config.ParsedDefaultTab;

            foreach (ChatChannelType channel in TabOrder)
            {
                tabs[channel] = new ChatTabState(channel, config.MaxHistoryPerChannel);
            }
        }

        public ChatTabState CurrentTab => tabs[ActiveTab];

        public ChatTabState GetTab(ChatChannelType channel) => tabs[channel];

        public long NextId() => nextEntryId++;

        public void AddEntry(ChatEntry entry, DateTimeOffset? nowUtc = null)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            ChatChannelType targetChannel = entry.Channel == ChatChannelType.All
                ? ChatChannelType.Global
                : entry.Channel;

            tabs[targetChannel].Append(entry, ActiveTab == targetChannel, MaxVisibleHeightPx);

            if (targetChannel != ChatChannelType.Discord || IncludeDiscordInAllTab)
            {
                tabs[ChatChannelType.All].Append(entry, ActiveTab == ChatChannelType.All, MaxVisibleHeightPx);
            }

            if (entry.IsCritical && CriticalToastDurationSeconds > 0 && ActiveTab != ChatChannelType.All && ActiveTab != targetChannel)
            {
                ActiveCriticalToast = entry;
                CriticalToastExpiresUtc = (nowUtc ?? DateTimeOffset.UtcNow).AddSeconds(CriticalToastDurationSeconds);
            }
        }

        public ChatEntry? GetActiveCriticalToast(DateTimeOffset? nowUtc = null)
        {
            if (ActiveCriticalToast == null)
                return null;

            if ((nowUtc ?? DateTimeOffset.UtcNow) >= CriticalToastExpiresUtc)
            {
                ActiveCriticalToast = null;
                return null;
            }

            return ActiveCriticalToast;
        }

        public void SelectTab(ChatChannelType channel)
        {
            if (!tabs.ContainsKey(channel))
                return;

            ActiveTab = channel;
            tabs[channel].MarkRead();

            if (channel is ChatChannelType.All or ChatChannelType.System)
            {
                ActiveCriticalToast = null;
            }
        }

        public void CycleTab(int step)
        {
            int idx = -1;
            for (int i = 0; i < TabOrder.Count; i++)
            {
                if (TabOrder[i] == ActiveTab)
                {
                    idx = i;
                    break;
                }
            }

            if (idx < 0)
                idx = 0;

            int next = ((idx + step) % TabOrder.Count + TabOrder.Count) % TabOrder.Count;
            SelectTab(TabOrder[next]);
        }

        public void ScrollBy(int deltaPx) => CurrentTab.ScrollBy(deltaPx, MaxVisibleHeightPx);

        public void ScrollToBottom() => CurrentTab.ScrollToBottom();

        public void ScrollToTop() => CurrentTab.ScrollToTop(MaxVisibleHeightPx);

        /// <summary>
        /// Calcula la altura del cuadro de mensajes abiertos (acotada por MaxVisibleHeightPx).
        /// </summary>
        public int GetOpenViewportHeightPx()
        {
            int content = CurrentTab.TotalContentHeightPx;
            if (content <= 0)
                return 0;

            return Math.Min(MaxVisibleHeightPx, content);
        }

        /// <summary>
        /// Actualiza los contadores de desvanecimiento (ticks) igual que vanilla.
        /// </summary>
        public void TickFadeTimers(bool isChatBoxSelected)
        {
            IReadOnlyList<ChatEntry> allItems = tabs[ChatChannelType.All].Buffer.Items;
            for (int i = 0; i < allItems.Count; i++)
            {
                ChatEntry item = allItems[i];
                if (item.TimeLeftToDisplay > 0)
                    item.TimeLeftToDisplay--;

                if (isChatBoxSelected)
                    item.Alpha = 1f;
                else if (item.TimeLeftToDisplay < 75)
                    item.Alpha = Math.Max(0f, item.TimeLeftToDisplay / 75f);
            }
        }
    }
}
