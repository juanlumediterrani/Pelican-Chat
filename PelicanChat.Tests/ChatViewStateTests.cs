using System;
using PelicanChat.Core.Channels;
using PelicanChat.Core.Config;
using PelicanChat.Core.Viewport;
using Xunit;

namespace PelicanChat.Tests;

public class ChatViewStateTests
{
    private static ChatEntry MakeEntry(long id, ChatChannelType channel, int height = 30, bool isCritical = false) =>
        new()
        {
            Id = id,
            Channel = channel,
            DisplayText = $"Msg {id}",
            MeasuredHeightPx = height,
            IsCritical = isCritical
        };

    [Fact]
    public void CircularBuffer_CapsAtConfiguredCapacity()
    {
        var cfg = new ChatConfig { MaxHistoryPerChannel = 15 };
        cfg.Validate();
        var view = new ChatViewState(cfg);

        for (int i = 1; i <= 25; i++)
            view.AddEntry(MakeEntry(i, ChatChannelType.Global));

        var items = view.GetTab(ChatChannelType.Global).Buffer.Items;
        Assert.Equal(15, items.Count);
        Assert.Equal(11, items[0].Id);
        Assert.Equal(25, items[^1].Id);
    }

    [Fact]
    public void ScrollLock_PreservesReadingPositionAndCountsUnreadWhileScrolled()
    {
        var cfg = new ChatConfig { MaxVisibleHeight = 100 };
        cfg.Validate();
        var view = new ChatViewState(cfg);

        for (int i = 1; i <= 10; i++)
            view.AddEntry(MakeEntry(i, ChatChannelType.Global, height: 30));

        // Scroll up by 60px
        view.ScrollBy(60);
        Assert.Equal(60, view.CurrentTab.ScrollOffsetPx);

        // New message arrives while scrolled up
        view.AddEntry(MakeEntry(11, ChatChannelType.Global, height: 30));
        Assert.Equal(90, view.CurrentTab.ScrollOffsetPx);
        Assert.Equal(1, view.CurrentTab.UnreadWhileScrolled);

        view.ScrollToBottom();
        Assert.Equal(0, view.CurrentTab.ScrollOffsetPx);
        Assert.Equal(0, view.CurrentTab.UnreadWhileScrolled);
    }

    [Fact]
    public void UnreadCount_IncrementsOnInactiveTabsAndResetsOnSelect()
    {
        var cfg = new ChatConfig();
        cfg.Validate();
        var view = new ChatViewState(cfg);

        view.SelectTab(ChatChannelType.Global);
        view.AddEntry(MakeEntry(1, ChatChannelType.Local));
        view.AddEntry(MakeEntry(2, ChatChannelType.Discord));

        Assert.Equal(1, view.GetTab(ChatChannelType.Local).UnreadCount);
        Assert.Equal(1, view.GetTab(ChatChannelType.Discord).UnreadCount);
        Assert.Equal(0, view.GetTab(ChatChannelType.Global).UnreadCount);

        view.SelectTab(ChatChannelType.Local);
        Assert.Equal(0, view.GetTab(ChatChannelType.Local).UnreadCount);
    }

    [Fact]
    public void CriticalToast_ShowsOnOtherTabsUntilExpired()
    {
        var cfg = new ChatConfig { CriticalToastDurationSeconds = 15 };
        cfg.Validate();
        var view = new ChatViewState(cfg);
        view.SelectTab(ChatChannelType.Local);

        var t0 = new DateTimeOffset(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);
        view.AddEntry(MakeEntry(1, ChatChannelType.System, isCritical: true), t0);

        Assert.NotNull(view.GetActiveCriticalToast(t0.AddSeconds(5)));
        Assert.Null(view.GetActiveCriticalToast(t0.AddSeconds(16)));
    }
}
