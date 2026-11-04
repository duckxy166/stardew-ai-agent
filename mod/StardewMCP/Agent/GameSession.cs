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
    private static int _generation;
    private static bool _open;

    /// <summary>Times <see cref="End"/> ran; lets threads off the game loop notice that the save they started on is gone.</summary>
    public static int Generation
    {
        get { lock (Lock) return _generation; }
    }

    /// <summary>A save is loaded: between <see cref="Begin"/> (host SaveLoaded) and <see cref="End"/>.</summary>
    public static bool Open
    {
        get { lock (Lock) return _open; }
    }

    /// <summary>The host loaded a save: the AI's calls are admitted again (<see cref="TryAdmit"/>).</summary>
    public static void Begin()
    {
        lock (Lock) _open = true;
    }

    /// <summary>
    /// May an AI's call be queued now, and for which save? False while no save is open. A call admitted just before
    /// <see cref="End"/> carries the old <paramref name="generation"/>, so the runtime can drop it instead of running it on
    /// the next save (an MCP batch goes on after its first call was answered by the reset, 2026-11 review).
    /// </summary>
    public static bool TryAdmit(out int generation)
    {
        lock (Lock)
        {
            generation = _generation;
            return _open;
        }
    }

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
            _generation++; // before the resets drain the queues: anything admitted earlier is stale from here on
            _open = false;
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
