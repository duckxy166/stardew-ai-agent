using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StardewMCP.Agent;

/// <summary>What kind of find a <see cref="Sight"/> is, which also says how to get it.</summary>
public enum SightKind
{
    /// <summary>A forage item: <c>harvest</c> on its tile picks it up.</summary>
    Forage,
    /// <summary>An artifact spot or seed spot: dig it with the Hoe.</summary>
    DigSpot,
    /// <summary>A bush with berries or nuts: <c>interact</c> shakes it.</summary>
    Bush,
    /// <summary>An item lying on the ground: stepping next to it picks it up.</summary>
    Dropped
}

/// <summary>Something worth picking up seen near a farmer's route: the map and tile it is on, its name and its kind.</summary>
public readonly record struct Sight(string Map, int X, int Y, string Name, SightKind Kind);

/// <summary>
/// The sightings of one action (<see cref="Sightings"/> fills it while walk_to, travel_to or talk_to runs) and the line
/// that ends their result. No game types, so the unit tests (mod/StardewMCP.Tests) compile this file.
/// </summary>
public sealed class SightLog
{
    /// <summary>Most finds one result lists; the rest are counted.</summary>
    public const int MaxListed = 6;

    private readonly Dictionary<(string Map, int X, int Y), Sight> _seen = new();
    private readonly List<(string Map, int X, int Y)> _order = new();

    public int Count => _seen.Count;

    public void Clear()
    {
        _seen.Clear();
        _order.Clear();
    }

    /// <summary>Remember a find; a tile seen before keeps its first sighting.</summary>
    public void Add(Sight sight)
    {
        var key = (sight.Map, sight.X, sight.Y);
        if (_seen.TryAdd(key, sight))
            _order.Add(key);
    }

    /// <summary>
    /// The line that ends the result: the finds <paramref name="stillThere"/> keeps, first those on <paramref name="map"/>
    /// (where the farmer now stands at <paramref name="x"/>,<paramref name="y"/>) nearest first, then those on maps passed
    /// earlier, the most recent map first. At most <see cref="MaxListed"/>; "" when there is nothing to tell.
    /// </summary>
    public string Line(string map, int x, int y, Func<Sight, bool> stillThere)
    {
        var left = _order.Select(k => _seen[k]).Where(stillThere).ToList();
        if (left.Count == 0)
            return "";

        var here = left.Where(s => s.Map == map)
            .Select((s, i) => (s, i))
            .OrderBy(p => Math.Max(Math.Abs(p.s.X - x), Math.Abs(p.s.Y - y)))
            .ThenBy(p => p.i)
            .Select(p => p.s);
        // other maps grouped, the one left last first; inside a map in the order passed
        var earlier = left.Where(s => s.Map != map)
            .Select((s, i) => (s, i))
            .GroupBy(p => p.s.Map)
            .OrderByDescending(g => g.Max(p => p.i))
            .SelectMany(g => g.Select(p => p.s));
        var shown = here.Concat(earlier).Take(MaxListed).ToList();

        var sb = new StringBuilder("Seen on the way, still there: ");
        string? group = null;
        for (int i = 0; i < shown.Count; i++)
        {
            var s = shown[i];
            if (s.Map != map && s.Map != group)
            {
                sb.Append(i == 0 ? "" : "; ").Append("on ").Append(s.Map).Append(": ");
                group = s.Map;
            }
            else if (i > 0)
                sb.Append(", ");
            sb.Append(s.Name).Append(' ').Append(Note(s.Kind)).Append($" at ({s.X},{s.Y})");
        }
        if (left.Count > shown.Count)
            sb.Append($" (+{left.Count - shown.Count} more)");
        sb.Append('.');
        if (shown.Any(s => s.Kind == SightKind.Forage))
            sb.Append(" Forage: harvest on its tile picks it up.");
        return sb.ToString();
    }

    private static string Note(SightKind kind) => kind switch
    {
        SightKind.Forage => "(forage)",
        SightKind.DigSpot => "(dig it with the Hoe)",
        SightKind.Bush => "(interact to shake it)",
        _ => "(on the ground)"
    };
}
