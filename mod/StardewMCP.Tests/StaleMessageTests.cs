using StardewMCP.Agent;
using Xunit;

namespace StardewMCP.Tests;

public class StaleMessageTests
{
    [Fact]
    public void An_empty_box_closes_after_two_seconds_even_while_frozen()
    {
        Assert.False(StaleMessage.ShouldClose(1.9, 0, emptyText: true, isQuestion: false, othersWaitingInBed: false));
        Assert.True(StaleMessage.ShouldClose(2, 0, emptyText: true, isQuestion: false, othersWaitingInBed: false));
    }

    [Fact]
    public void A_message_closes_only_while_someone_waits_in_bed()
    {
        Assert.False(StaleMessage.ShouldClose(300, 300, emptyText: false, isQuestion: false, othersWaitingInBed: false));
        Assert.True(StaleMessage.ShouldClose(20, 20, emptyText: false, isQuestion: false, othersWaitingInBed: true));
    }

    [Fact]
    public void Frozen_time_does_not_count_for_a_message()
    {
        // Five real minutes, but the world ran for only ten seconds: the AI may still be reading it.
        Assert.False(StaleMessage.ShouldClose(300, 10, emptyText: false, isQuestion: false, othersWaitingInBed: true));
    }

    [Fact]
    public void A_question_is_never_closed()
    {
        Assert.False(StaleMessage.ShouldClose(600, 600, emptyText: false, isQuestion: true, othersWaitingInBed: true));
        Assert.False(StaleMessage.ShouldClose(600, 600, emptyText: true, isQuestion: true, othersWaitingInBed: true));
    }
}
