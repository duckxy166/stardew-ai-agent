using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace StardewMCP;

/// <summary>
/// Which tiles a route may step on. The game side answers with its own collision check
/// (<c>Pathfinder.IsWalkable</c>); the unit tests answer from a plain text map.
/// </summary>
public interface IWalkableGrid
{
    bool IsWalkable(int x, int y);
}

/// <summary>
/// A grid that also counts some tiles as blocked: where villagers stand, for a walk that got stuck on one. Ported from
/// Pichaya31847/stardew-ai-agent (MIT, commit 7d4f2fe).
/// </summary>
public sealed class AvoidingGrid : IWalkableGrid
{
    private readonly IWalkableGrid _inner;
    private readonly ISet<Point> _avoid;

    public AvoidingGrid(IWalkableGrid inner, ISet<Point> avoid) { _inner = inner; _avoid = avoid; }

    public bool IsWalkable(int x, int y) => !_avoid.Contains(new Point(x, y)) && _inner.IsWalkable(x, y);
}

// Game-free half of Pathfinder: the search itself only needs Point and Vector2, so the unit
// tests (mod/StardewMCP.Tests) compile this file without any Stardew assembly.
public partial class Pathfinder
{
    /// <summary>Tiles expanded before giving up; big maps (Farm, Forest) need tens of thousands.</summary>
    private const int ExpandLimit = 60000;

    private static readonly Point[] Moves = { new(0, -1), new(1, 0), new(0, 1), new(-1, 0) };
    private static readonly Point[] Diagonals = { new(1, -1), new(1, 1), new(-1, 1), new(-1, -1) };

    private readonly Dictionary<Point, bool> _walkable = new();

    /// <summary>
    /// Route from <paramref name="start"/> to <paramref name="goal"/> as the tiles to step on in
    /// order, excluding the start tile and ending on the goal. Empty when already there, null when
    /// the goal can't be stood on or can't be reached.
    /// </summary>
    public List<Vector2>? FindPath(IWalkableGrid grid, Vector2 start, Vector2 goal) =>
        FindPathToAny(grid, start, new[] { new Point((int)goal.X, (int)goal.Y) });

    /// <summary>
    /// Route to whichever of <paramref name="goals"/> is the shortest walk away, as the tiles to step on in order,
    /// excluding the start tile and ending on that goal. Empty when <paramref name="start"/> is one of the goals, null
    /// when none of them can be stood on or reached. One search, however many goals (the tiles around a villager).
    /// A start walled in on all four sides may step out diagonally: the farmer is wedged into a wall (a villager walked
    /// through it), and the first step of such a route is the one tile the walk snaps the farmer onto.
    /// </summary>
    public List<Vector2>? FindPathToAny(IWalkableGrid grid, Vector2 start, IEnumerable<Point> goals)
    {
        var from = new Point((int)start.X, (int)start.Y);
        var targets = new HashSet<Point>(goals);
        if (targets.Contains(from))
            return new List<Vector2>();
        targets.RemoveWhere(t => !Walkable(grid, t));
        if (targets.Count == 0)
            return null;

        var steps = new Dictionary<Point, int> { [from] = 0 };
        var parent = new Dictionary<Point, Point>();
        var done = new HashSet<Point>();
        var frontier = new PriorityQueue<Point, int>();
        frontier.Enqueue(from, Nearest(from, targets));
        if (BoxedIn(grid, from))
        {
            done.Add(from);
            foreach (var move in Diagonals)
            {
                var next = new Point(from.X + move.X, from.Y + move.Y);
                if (!Walkable(grid, next))
                    continue;
                steps[next] = 1;
                parent[next] = from;
                frontier.Enqueue(next, 1 + Nearest(next, targets));
            }
        }

        while (frontier.TryDequeue(out var tile, out _))
        {
            if (targets.Contains(tile))
                return Unwind(parent, from, tile);
            if (!done.Add(tile))
                continue; // stale queue entry
            if (done.Count > ExpandLimit)
                break;

            int cost = steps[tile] + 1;
            foreach (var move in Moves)
            {
                var next = new Point(tile.X + move.X, tile.Y + move.Y);
                if (done.Contains(next) || (steps.TryGetValue(next, out int known) && known <= cost))
                    continue;
                if (!Walkable(grid, next))
                    continue;
                steps[next] = cost;
                parent[next] = tile;
                frontier.Enqueue(next, cost + Nearest(next, targets));
            }
        }
        return null;
    }

    /// <summary>No tile to the four sides of <paramref name="tile"/> can be stood on.</summary>
    private bool BoxedIn(IWalkableGrid grid, Point tile)
    {
        foreach (var move in Moves)
            if (Walkable(grid, new Point(tile.X + move.X, tile.Y + move.Y)))
                return false;
        return true;
    }

    /// <summary>Cached <c>IsWalkable</c>, since one search probes the same tiles repeatedly.</summary>
    private bool Walkable(IWalkableGrid grid, Point tile)
    {
        if (!_walkable.TryGetValue(tile, out bool ok))
            _walkable[tile] = ok = grid.IsWalkable(tile.X, tile.Y);
        return ok;
    }

    private static int Distance(Point a, Point b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

    /// <summary>Distance to the closest goal: never more than the real walk, so the first goal reached is the nearest.</summary>
    private static int Nearest(Point tile, HashSet<Point> goals)
    {
        int best = int.MaxValue;
        foreach (var goal in goals)
            best = Math.Min(best, Distance(tile, goal));
        return best;
    }

    private static List<Vector2> Unwind(Dictionary<Point, Point> parent, Point from, Point to)
    {
        var route = new List<Vector2>();
        for (var tile = to; tile != from; tile = parent[tile])
            route.Add(tile.ToVector2());
        route.Reverse();
        return route;
    }
}
