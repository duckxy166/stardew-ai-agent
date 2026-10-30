namespace StardewMCP.Agent;

/// <summary>
/// When a message box on an AI's screen is closed for it at bedtime (<see cref="SleepWaitWatch"/>). Game-free, so the tests
/// compile it. Ported from Pichaya31847/stardew-ai-agent (MIT, DaySafety.CloseStaleMessage in commit f94719b; their day 56:
/// one AI waited in bed about five real minutes while the other sat in a message in Pierre's shop).
/// </summary>
public static class StaleMessage
{
    /// <summary>Seconds a message box with no text may stay up before it is closed (there is nothing to read in it).</summary>
    public const double EmptySeconds = 2;

    /// <summary>Seconds of running world time a message box may hold a farmer up while another farmer waits in bed.</summary>
    public const double WaitingSeconds = 20;

    /// <summary>
    /// Whether the box should be closed: at once when it has no text, and when someone already lies in bed waiting for this
    /// farmer and the box has stayed up <see cref="WaitingSeconds"/> while the world ran. Time the world stood frozen (an AI
    /// thinking, maybe this one reading the box) does not count. Questions are always left for the farmer to answer.
    /// </summary>
    public static bool ShouldClose(double secondsOpen, double runningSecondsOpen, bool emptyText, bool isQuestion, bool othersWaitingInBed) =>
        !isQuestion && (emptyText ? secondsOpen >= EmptySeconds : othersWaitingInBed && runningSecondsOpen >= WaitingSeconds);
}
