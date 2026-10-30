using JiraTime.Components;
using JiraTime.Domain;
using Microsoft.AspNetCore.Components;

namespace JiraTime.Tests;

/// <summary>Modals: results, cancel, the queue, modals on top and closing them all.</summary>
public class ModalsTests
{
    private readonly Modals _modals = new();

    private Task<string?> Show(bool onTop = false) => _modals.ShowAsync<Modal, string>(onTop: onTop);

    [Fact]
    public async Task A_modal_returns_the_result_it_closed_with()
    {
        var shown = Show();

        _modals.Open[0].Close("yes");

        Assert.Equal("yes", await shown);
        Assert.Empty(_modals.Open);
    }

    [Fact]
    public async Task A_cancelled_modal_returns_null()
    {
        var shown = Show();

        _modals.Open[0].Cancel();

        Assert.Null(await shown);
    }

    [Fact]
    public async Task A_typed_result_comes_back_as_it_is()
    {
        var shown = _modals.ShowAsync<Modal, DateOnly?>();

        _modals.Open[0].Close(new DateOnly(2026, 10, 30));

        Assert.Equal(new DateOnly(2026, 10, 30), await shown);
    }

    [Fact]
    public async Task Modals_asked_for_while_one_is_open_wait_and_open_in_order()
    {
        var first = Show();
        var second = Show();
        var third = Show();
        Assert.Single(_modals.Open);

        _modals.Open[0].Close("1");
        _modals.Open[0].Close("2");
        _modals.Open[0].Close("3");

        Assert.Equal(new[] { "1", "2", "3" }, new[] { await first, await second, await third });
    }

    [Fact]
    public async Task A_modal_on_top_shows_above_and_the_one_below_comes_back_when_it_closes()
    {
        var below = Show();
        var waiting = Show();
        var onTop = Show(onTop: true);
        Assert.Equal(2, _modals.Open.Count);
        var bottom = _modals.Open[0];

        _modals.Open[1].Close("top");

        Assert.Equal("top", await onTop);
        Assert.Equal([bottom], _modals.Open);
        bottom.Close("below");
        Assert.Equal("below", await below);
        Assert.Single(_modals.Open); // now the waiting one
        Assert.False(waiting.IsCompleted);
    }

    [Fact]
    public async Task Closing_all_cancels_the_open_and_the_waiting_modals()
    {
        var open = Show();
        var waiting = Show();

        _modals.CloseAll();

        Assert.Null(await open);
        Assert.Null(await waiting);
        Assert.Empty(_modals.Open);
    }

    [Fact]
    public void Each_open_close_and_busy_change_is_announced()
    {
        var changes = 0;
        _modals.Changed += () => changes++;

        _ = Show();
        var modal = _modals.Open[0];
        modal.Busy = true;
        modal.Busy = true; // no change
        modal.Cancel();
        modal.Cancel(); // closed already

        Assert.Equal(3, changes);
    }

    [Fact]
    public async Task A_busy_modal_can_not_be_dismissed_until_its_work_ends()
    {
        _ = Show();
        var modal = _modals.Open[0];
        var work = new TaskCompletionSource();

        var running = modal.WhileBusyAsync(() => work.Task);

        Assert.False(modal.CanDismiss);
        work.SetResult();
        await running;
        Assert.True(modal.CanDismiss);
    }

    [Fact]
    public void A_closed_modal_is_no_longer_found()
    {
        _ = Show();
        var modal = _modals.Open[0];

        modal.Cancel();

        Assert.Null(_modals.Find(modal.Id));
    }

    [Modal(Size = ModalSize.Small, Dismiss = false)]
    private sealed class MustAnswer : ComponentBase;

    [Fact]
    public void A_modal_without_the_attribute_is_medium_and_can_be_dismissed()
    {
        _ = Show();

        Assert.Equal(ModalSize.Medium, _modals.Open[0].Size);
        Assert.True(_modals.Open[0].CanDismiss);
    }

    [Fact]
    public void The_attribute_sets_the_size_and_whether_esc_and_the_backdrop_close_it()
    {
        _ = _modals.ShowAsync<MustAnswer, bool>();

        Assert.Equal(ModalSize.Small, _modals.Open[0].Size);
        Assert.False(_modals.Open[0].CanDismiss);
    }

    [Fact]
    public async Task A_result_button_s_text_is_read_as_the_type_asked_for()
    {
        var confirmed = _modals.ShowAsync<MustAnswer, bool>();
        _modals.Open[0].Close("true");
        Assert.True(await confirmed);

        Assert.Equal(4, ModalResult.As<int?>("4"));
        Assert.Equal(DayOffReason.Sick, ModalResult.As<DayOffReason>("sick"));
        Assert.Equal("x", ModalResult.As<string>("x"));
    }

    [Fact]
    public void A_result_that_can_not_be_read_or_a_cancel_gives_the_default()
    {
        Assert.False(ModalResult.As<bool>(null));
        Assert.Null(ModalResult.As<int?>("lots"));
        Assert.Null(ModalResult.As<string>(42));
    }
}
