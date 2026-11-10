using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Objects;

namespace StardewMCP.Agent;

/// <summary>
/// Frees a chest lock that no open menu explains. A chest opens for one farmer at a time through its <c>NetMutex</c>, and
/// in local split-screen every screen shares that one mutex, with one pending callback. When two farmers ask for it in the
/// same moment, the callback can run on the other screen: the lock goes to a farmer whose chest menu never opens, and as
/// long as that farmer stays on the map, nobody can open the chest again (2026-11-10, issue #77: a chest on the farm
/// answered "Nothing happened" to every farmer for the rest of the day, while a new chest next to it opened fine).
/// Each screen checks the chests on its own map once a second: one whose lock this screen's farmer holds with no menu
/// open on this screen, two checks in a row, is released. Any open menu counts (the cooking page holds the fridge).
/// </summary>
internal static class ChestLocks
{
    private static readonly PerScreen<HashSet<Chest>> Stale = new(() => new HashSet<Chest>());

    /// <summary>Run on each screen's tick; <paramref name="check"/> once a second.</summary>
    public static void Tick(bool check, IMonitor monitor)
    {
        if (!check || Game1.player == null || Game1.currentLocation is not { } loc) return;
        var seen = Stale.Value;
        if (Game1.activeClickableMenu != null || Game1.eventUp)
        {
            seen.Clear();
            return;
        }
        var held = Chests(loc).Where(c => c.GetMutex().IsLockHeld()).ToList();
        foreach (var chest in held.Where(seen.Contains))
        {
            chest.GetMutex().ReleaseLock();
            monitor.Log($"Released the lock {Game1.player.Name} held on the chest at {loc.NameOrUniqueName} ({chest.TileLocation.X},{chest.TileLocation.Y}) with no menu open");
        }
        seen.Clear();
        foreach (var chest in held.Where(c => c.GetMutex().IsLockHeld()))
            seen.Add(chest);
    }

    private static IEnumerable<Chest> Chests(GameLocation loc)
    {
        foreach (var obj in loc.objects.Values)
            if (obj is Chest chest)
                yield return chest;
        if (loc.GetFridge(false) is { } fridge)
            yield return fridge;
    }
}
