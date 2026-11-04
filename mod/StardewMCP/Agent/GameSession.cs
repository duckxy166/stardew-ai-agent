using System;
using System.Collections.Generic;

namespace StardewMCP.Agent;

/// <summary>
/// The life of one loaded save, from SaveLoaded to ReturnedToTitle. Much of the mod keeps per-seat state in static fields
/// (chat inboxes, gifts in flight, queued calls, loop-guard counters...). Each such system registers what has to be
/// forgotten when the save ends with <see cref="OnEnd"/>, and ModEntry calls <see cref="End"/> once on ReturnedToTitle,
/// so loading another save (a smoke_* copy, then the real farm) never sees the last one's chat, items or calls.
/// One owner for that list instead of a reset call per system in ModEntry. Game-free (unit tests).
/// </summary>
public static class GameSession
{
    private static readonly List<(string Name, Action Reset)> Resets = new();
    private static readonly object Lock = new();

    /// <summary>Times <see cref="End"/> ran; lets threads off the game loop notice that the save they started on is gone.</summary>
    public static int Generation { get; private set; }

    /// <summary>
    /// Register <paramref name="reset"/> under <paramref name="name"/>; registering a name again replaces it. Resets run in
    /// registration order.
    /// </summary>
    public static void OnEnd(string name, Action reset)
    {
        lock (Lock)
        {
            int i = Resets.FindIndex(r => r.Name == name);
            if (i >= 0) Resets[i] = (name, reset);
            else Resets.Add((name, reset));
        }
    }

    /// <summary>
    /// The save ended: run every reset. One that throws does not stop the others; the failures come back as
    /// "name: message" so the caller can log them.
    /// </summary>
    public static List<string> End()
    {
        (string Name, Action Reset)[] resets;
        lock (Lock)
        {
            resets = Resets.ToArray();
            Generation++;
        }
        var failures = new List<string>();
        foreach (var (name, reset) in resets)
        {
            try { reset(); }
            catch (Exception ex) { failures.Add($"{name}: {ex.Message}"); }
        }
        return failures;
    }

    /// <summary>Registered names, in order (tests and the startup log).</summary>
    public static IReadOnlyList<string> Names
    {
        get { lock (Lock) return Resets.ConvertAll(r => r.Name); }
    }
}
