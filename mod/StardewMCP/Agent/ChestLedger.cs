using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace StardewMCP.Agent;

/// <summary>
/// Who put what into which chest, and who took what out. When a chest menu closes on a screen, its contents are compared
/// with what it held when it opened (any path counts: the chest tool, interact and menu clicks, a human's hands). Every
/// change goes to the day log (kind "chest") and to chests.jsonl next to the logs. This sits beside
/// <see cref="Ownership"/>'s theft entries, it does not replace them: a chest still belongs to the farmer who placed it,
/// and taking from someone else's is still theft. The ledger is how the owner finds out: the chest tool's result and the
/// morning briefing say what others changed in your chests since you last used them, and chest(action=all) lists every
/// chest with its owner, contents and the recent changes.
/// Ported from Pichaya31847/stardew-ai-agent (MIT, commit f94719b), keeping chests personal.
/// </summary>
internal static class ChestLedger
{
    public sealed record Change(int Day, string Time, string Seat, string Farmer, string? Owner, string Action, string Item, int Count, string Location, int X, int Y);

    private sealed record Opened(Chest Chest, Dictionary<string, (string label, int count)> Before, string? Owner, string Location, int X, int Y);

    private const int KeepChanges = 300;
    private const int MaxStacksShown = 15;
    private static readonly List<Change> Recent = new();
    private static readonly PerScreen<Opened?> Open = new();
    private static AgentLog? _log;
    private static string? _loadedFrom;

    public static void Init(AgentLog log)
    {
        _log = log;
        Briefing.Register(55, "chests", MorningText);
    }

    /// <summary>Once per tick on each screen: notices a chest menu opening and closing.</summary>
    public static void Tick(PlayerSlot seat)
    {
        if (!Context.IsWorldReady || Game1.player == null) return;
        var chest = Game1.activeClickableMenu is ItemGrabMenu { context: Chest c } ? c : null;
        var open = Open.Value;
        if (open != null && !ReferenceEquals(open.Chest, chest))
        {
            Record(open, seat);
            Open.Value = null;
        }
        if (chest != null && Open.Value == null)
        {
            var loc = chest.Location ?? Game1.currentLocation;
            Open.Value = new Opened(chest, Contents(chest), Ownership.OwnerOf(chest), loc?.NameOrUniqueName ?? "?", (int)chest.TileLocation.X, (int)chest.TileLocation.Y);
        }
    }

    private static Dictionary<string, (string label, int count)> Contents(Chest chest)
    {
        var d = new Dictionary<string, (string label, int count)>();
        foreach (var it in chest.Items.Where(i => i != null))
        {
            var key = it.QualifiedItemId + "|" + it.Quality;
            d.TryGetValue(key, out var cur);
            d[key] = (Label(it), cur.count + it.Stack);
        }
        return d;
    }

    private static string Label(Item it) => it is Tool || it.Quality == 0 ? it.DisplayName : $"{it.DisplayName} ({AgentTools.QualityName(it.Quality)})";

    private static void Record(Opened open, PlayerSlot seat)
    {
        var after = Contents(open.Chest);
        var changes = new List<Change>();
        int day = AgentLog.FarmDay;
        var time = WorldText.FormatTime(Game1.timeOfDay);
        foreach (var key in open.Before.Keys.Union(after.Keys))
        {
            open.Before.TryGetValue(key, out var b);
            after.TryGetValue(key, out var a);
            int delta = a.count - b.count;
            if (delta == 0) continue;
            changes.Add(new Change(day, time, seat.Id, Game1.player.Name, open.Owner, delta > 0 ? "put" : "took", a.label ?? b.label, Math.Abs(delta), open.Location, open.X, open.Y));
        }
        if (changes.Count == 0) return;

        Load();
        lock (Recent)
        {
            Recent.AddRange(changes);
            if (Recent.Count > KeepChanges) Recent.RemoveRange(0, Recent.Count - KeepChanges);
        }
        var text = $"{Game1.player.Name} {Describe(changes)} {OwnerWords(open.Owner, seat.Id)} chest at {open.Location} ({open.X},{open.Y})";
        seat.Runtime.Log.Add("chest", text: text, args: new
        {
            owner = open.Owner, location = open.Location, tile = new[] { open.X, open.Y },
            changes = changes.Select(c => new { action = c.Action, item = c.Item, count = c.Count })
        });
        try
        {
            if (File() is { } file)
                System.IO.File.AppendAllLines(file, changes.Select(c => JsonSerializer.Serialize(c, AgentLog.Json)));
        }
        catch (IOException) { /* the day log still has it */ }
    }

    /// <summary>"their own", "Gemini's", "an unowned".</summary>
    private static string OwnerWords(string? owner, string by) =>
        owner == null ? "an unowned" : owner == by ? "their own" : $"{SeatName(owner)}'s";

    private static string SeatName(string seat) => Players.ById(seat) is { } s ? SocialTools.Label(s) : seat;

    /// <summary>"put 30 Pumpkin, 2 Wood into / took 5 Coal from"</summary>
    private static string Describe(IEnumerable<Change> changes)
    {
        var put = changes.Where(c => c.Action == "put").Select(c => $"{c.Count} {c.Item}").ToList();
        var took = changes.Where(c => c.Action == "took").Select(c => $"{c.Count} {c.Item}").ToList();
        var parts = new List<string>();
        if (put.Count > 0) parts.Add("put " + string.Join(", ", put) + " into");
        if (took.Count > 0) parts.Add("took " + string.Join(", ", took) + " from");
        return string.Join(" and ", parts);
    }

    private static string? File() => _log?.Dir is { } dir ? Path.Combine(dir, "chests.jsonl") : null;

    /// <summary>The last changes from chests.jsonl, once per log folder (the run goes on after a restart).</summary>
    private static void Load()
    {
        var file = File();
        if (file == null || _loadedFrom == file) return;
        _loadedFrom = file;
        lock (Recent)
        {
            Recent.Clear();
            try
            {
                if (!System.IO.File.Exists(file)) return;
                foreach (var line in System.IO.File.ReadLines(file).TakeLast(KeepChanges))
                    if (JsonSerializer.Deserialize<Change>(line, AgentLog.Json) is { } c) Recent.Add(c);
            }
            catch (Exception) { /* a damaged ledger starts over */ }
        }
    }

    /// <summary>Every placed chest (fridges too) on the farm and in its buildings with owner and contents, then the recent changes.</summary>
    public static string Report(string me, int changes = 15)
    {
        Load();
        var sb = new StringBuilder("Chests on the farm:");
        int n = 0;
        var farm = Game1.getFarm();
        var places = new List<GameLocation> { farm };
        if (Game1.getLocationFromName("FarmHouse") is { } house) places.Add(house);
        if (Game1.getLocationFromName("Greenhouse") is { } greenhouse) places.Add(greenhouse);
        places.AddRange(farm.buildings.Select(b => b.GetIndoors()).Where(l => l != null)!);
        foreach (var loc in places.Distinct())
        {
            foreach (var (tile, obj) in loc.objects.Pairs)
            {
                if (obj is not Chest { playerChest.Value: true } chest) continue;
                n++;
                var inside = Contents(chest).Values.OrderByDescending(v => v.count).Select(v => $"{v.count} {v.label}").ToList();
                var kind = chest.fridge.Value ? "fridge" : "chest";
                var owner = Ownership.OwnerOf(chest);
                var whose = owner == null ? "" : owner == me ? " (yours)" : $" ({SeatName(owner)}'s)";
                var shown = inside.Count == 0 ? "empty" : string.Join(", ", inside.Take(MaxStacksShown)) + (inside.Count > MaxStacksShown ? $" and {inside.Count - MaxStacksShown} more" : "");
                sb.Append($"\n- {kind}{whose} at {loc.NameOrUniqueName} ({(int)tile.X},{(int)tile.Y}): {shown}");
            }
        }
        if (n == 0) sb.Append(" none placed yet (craft a Chest: 50 Wood).");
        List<Change> last;
        lock (Recent) last = Recent.TakeLast(changes).ToList();
        if (last.Count > 0)
        {
            sb.Append("\nRecent chest changes:");
            foreach (var g in last.GroupBy(c => (c.Day, c.Time, c.Farmer, c.Seat, c.Owner, c.Location, c.X, c.Y)))
                sb.Append($"\n- day {g.Key.Day} {g.Key.Time}: {g.Key.Farmer} {Describe(g)} {OwnerWords(g.Key.Owner, g.Key.Seat)} chest at {g.Key.Location} ({g.Key.X},{g.Key.Y})");
        }
        return sb.ToString();
    }

    /// <summary>What others changed in this chest since <paramref name="me"/> last touched it, for the chest tool's result; "" when none.</summary>
    public static string OthersChanged(string location, int x, int y, string me)
    {
        var others = OthersSinceLastUse(me, c => c.Location == location && c.X == x && c.Y == y);
        if (others.Count == 0) return "";
        return " Since you last used it: " + string.Join("; ", others.GroupBy(c => (c.Day, c.Time, c.Farmer))
            .Select(g => $"{g.Key.Farmer} {Describe(g).Replace(" into", "").Replace(" from", "")} (day {g.Key.Day} {g.Key.Time})")) + ".";
    }

    /// <summary>Changes by others to the chests whose change matches <paramref name="where"/>, after <paramref name="me"/> last used each.</summary>
    private static List<Change> OthersSinceLastUse(string me, Func<Change, bool> where)
    {
        Load();
        List<Change> all;
        lock (Recent) all = Recent.Where(where).ToList();
        var result = new List<Change>();
        foreach (var chest in all.GroupBy(c => (c.Location, c.X, c.Y)))
        {
            var list = chest.ToList();
            int lastOwn = list.FindLastIndex(c => c.Seat == me);
            result.AddRange(list.Skip(lastOwn + 1).Where(c => c.Seat != me));
        }
        return result;
    }

    /// <summary>Morning briefing: what others did to your own chests yesterday, after you last used them; null when nothing.</summary>
    private static string? MorningText()
    {
        var me = Players.Current.Id;
        int yesterday = AgentLog.FarmDay - 1;
        var others = OthersSinceLastUse(me, c => c.Owner == me).Where(c => c.Day >= yesterday).ToList();
        if (others.Count == 0) return null;
        return "Your chests since you last used them: " + string.Join("; ", others.GroupBy(c => (c.Day, c.Time, c.Farmer, c.Location, c.X, c.Y))
            .Select(g => $"{g.Key.Farmer} {Describe(g)} your chest at {g.Key.Location} ({g.Key.X},{g.Key.Y}) on day {g.Key.Day} at {g.Key.Time}")) + ".";
    }
}
