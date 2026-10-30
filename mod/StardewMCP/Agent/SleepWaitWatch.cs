using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.Menus;

namespace StardewMCP.Agent;

/// <summary>
/// Closes a message box that holds up the night (<see cref="StaleMessage"/>): one with no text at once, any other (never a
/// question) once another farmer has waited in bed for it a while. The farmer hears about it on its next result and the day
/// log keeps a line. Ticked on each seat's own screen, where Game1.activeClickableMenu is that seat's menu; AI seats only.
/// Ported from Pichaya31847/stardew-ai-agent (MIT, commit f94719b).
/// </summary>
public static class SleepWaitWatch
{
    private sealed class Open
    {
        public DialogueBox Box = null!;
        public int Ticks, RunningTicks;
    }

    private static readonly Dictionary<string, Open> Seen = new();

    public static void Tick(PlayerSlot seat)
    {
        // A person at the keyboard reads at their own pace.
        if (seat.Role != SeatRole.Ai || Game1.activeClickableMenu is not DialogueBox box)
        {
            Seen.Remove(seat.Id);
            return;
        }
        if (!Seen.TryGetValue(seat.Id, out var open) || !ReferenceEquals(open.Box, box))
        {
            Seen[seat.Id] = new Open { Box = box };
            return;
        }
        open.Ticks++;
        if (!WorldClock.Frozen) open.RunningTicks++;

        var text = (box.getCurrentString() ?? "").Replace("\n", " ").Trim();
        bool othersInBed = !Game1.player.isInBed.Value && Game1.getOnlineFarmers().Any(f => f != Game1.player && f.isInBed.Value);
        if (box.transitioning || !StaleMessage.ShouldClose(open.Ticks / 60.0, open.RunningTicks / 60.0, text.Length == 0, box.isQuestion, othersInBed))
            return;

        box.closeDialogue();
        Game1.dialogueUp = false;
        Seen.Remove(seat.Id);
        var time = WorldText.FormatTime(Game1.timeOfDay);
        var why = text.Length == 0 ? "an empty message box" : $"the message \"{text}\" (open {open.RunningTicks / 60}s while another farmer waited in bed)";
        seat.Runtime.Log.Add("system", text: $"Closed {why} at {time}");
        if (text.Length > 0)
            SocialTools.Deliver(seat, $"[{time}] A message was left open while another farmer waits in bed for the night, so it was closed for you: \"{text}\". Finish up and go_to_bed.");
    }
}
