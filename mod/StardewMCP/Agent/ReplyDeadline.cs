using System;
using StardewModdingAPI;
using StardewValley;

namespace StardewMCP.Agent;

/// <summary>
/// The MCP client's own deadline (<c>/mcp/pN?reply_within=N</c>, <see cref="PlayerSlot.ReplyWithinSeconds"/>). A client that
/// gives up on a call after a few minutes (agy: 3, 2026-10-20) is answered before that. A sleeper is told its day is over
/// while go_to_bed keeps running, so it stays in bed; any other action stops at its next safe point; a call that never
/// started (behind another action, or held by a pause) is answered and dropped.
/// This is a transport rule, not perception: it lives next to the runtime and only borrows Perception's "safe to stop"
/// checks. Ticked by <see cref="AgentRuntime.Tick"/> just before <see cref="Perception.Tick"/>.
/// </summary>
public static class ReplyDeadline
{
    public static void Tick(AgentRuntime rt)
    {
        if (Players.ById(rt.Log.Player) is not { Role: SeatRole.Ai } seat || !Context.IsWorldReady) return;
        int limit = seat.ReplyWithinSeconds;
        if (limit <= 0) return;

        var call = rt.CurrentCall;
        if (call is { Instant: false, Tool.Name: "go_to_bed" } && call.Answered && !rt.WakingUp)
        {
            // told its day is over, the client went on anyway: those calls would otherwise run on tomorrow's farmer
            if (rt.AnswerQueued("You are asleep in bed and your day is over: the night begins when everyone is in bed. Stop here.") > 0)
                rt.Log.Add("system", text: "Refused a call sent after go_to_bed was answered: asleep");
        }
        else if (rt.AnswerQueued(RunControl.Holding ? "The game is paused, so this call never started. Call it again."
                                     : $"This call never started: your {rt.CurrentTool ?? "earlier action"} was still running. Call it again.",
                                 limit) > 0)
            rt.Log.Add("system", text: $"Answered calls that waited {limit}s without starting");

        if (call == null || call.Instant || call.Answered || (DateTime.UtcNow - call.Queued).TotalSeconds < limit) return;
        if (Perception.Asleep(call, rt))
        {
            // go_to_bed keeps running (the farmer stays in bed); only its answer goes out now
            call.Answer(true, "You are asleep in bed, waiting for the others to go to bed too; the night begins " +
                              "when everyone is in bed. Your day is over: stop here.",
                        new ToolOutcome(true, null, false, null));
            rt.Log.Add("system", text: "Answered go_to_bed early: asleep, waiting for the others");
            return;
        }
        if (!Perception.SafeToStop(call, asleep: false, bedWalk: true)) return; // runs to its end; the action timeout still applies
        var where = Perception.Where();
        if (Perception.Stop(rt, $"Paused: your {call.Tool.Name} ran long and was stopped at {where} at {WorldText.FormatTime(Game1.timeOfDay)}; " +
                                "nothing happened to you. Call it again to go on, or decide something else."))
            rt.Log.Add("interrupt", call.Tool.Name, "reply_within", args: new { seconds = limit });
    }
}
