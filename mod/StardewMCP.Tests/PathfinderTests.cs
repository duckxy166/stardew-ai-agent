using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Xunit;

namespace StardewMCP.Tests;

public class PathfinderTests
{
    private static List<Vector2>? Route(TextGrid map, Vector2 start, Vector2 goal) => new Pathfinder().FindPath(map, start, goal);

    private static List<(int X, int Y)> Steps(IEnumerable<Vector2> route) => route.Select(t => ((int)t.X, (int)t.Y)).ToList();

    /// <summary>
    /// The route must start next to <paramref name="start"/>, move one tile at a time, only touch open tiles and end on
    /// <paramref name="goal"/>. Only the first step out of a start walled in on all four sides may be diagonal.
    /// </summary>
    private static void AssertValidRoute(TextGrid map, Vector2 start, Vector2 goal, List<Vector2> route)
    {
        Assert.NotEmpty(route);
        var previous = (X: (int)start.X, Y: (int)start.Y);
        bool first = true;
        foreach (var tile in Steps(route))
        {
            int dx = Math.Abs(tile.X - previous.X), dy = Math.Abs(tile.Y - previous.Y);
            if (first && map.BoxedIn(previous.X, previous.Y))
                Assert.True(dx == 1 && dy == 1, $"step out of a walled-in start must be diagonal, was ({tile.X},{tile.Y})");
            else
                Assert.Equal(1, dx + dy);
            first = false;
            Assert.True(map.Open(tile.X, tile.Y), $"route steps on blocked tile ({tile.X},{tile.Y})");
            previous = tile;
        }
        Assert.Equal((X: (int)goal.X, Y: (int)goal.Y), previous);
    }

    [Fact]
    public void Already_at_the_goal_gives_an_empty_route()
    {
        var map = new TextGrid("...");
        var route = Route(map, new Vector2(1, 0), new Vector2(1, 0));
        Assert.NotNull(route);
        Assert.Empty(route!);
    }

    [Fact]
    public void Straight_corridor_lists_each_step_without_the_start_and_ends_on_the_goal()
    {
        var map = new TextGrid("S...G");
        var route = Route(map, map.Find('S'), map.Find('G'));
        Assert.NotNull(route);
        Assert.Equal(new List<(int X, int Y)> { (1, 0), (2, 0), (3, 0), (4, 0) }, Steps(route!));
    }

    [Fact]
    public void Goes_around_a_wall_the_short_way()
    {
        var map = new TextGrid(
            "S.#..",
            "..#..",
            "..#..",
            "....G");
        var route = Route(map, map.Find('S'), map.Find('G'));
        Assert.NotNull(route);
        AssertValidRoute(map, map.Find('S'), map.Find('G'), route!);
        Assert.Equal(7, route!.Count); // down 3, right 4: the only gap is on the bottom row
    }

    [Fact]
    public void Unreachable_goal_gives_null()
    {
        var map = new TextGrid(
            "S.#.G",
            "..#..",
            "..#..");
        Assert.Null(Route(map, map.Find('S'), map.Find('G')));
    }

    [Fact]
    public void Goal_on_a_blocked_tile_gives_null()
    {
        var map = new TextGrid("S.#");
        Assert.Null(Route(map, map.Find('S'), new Vector2(2, 0)));
    }

    [Fact]
    public void Goal_off_the_map_gives_null()
    {
        var map = new TextGrid("S..");
        Assert.Null(Route(map, map.Find('S'), new Vector2(5, 0)));
        Assert.Null(Route(map, map.Find('S'), new Vector2(-1, 0)));
    }

    [Fact]
    public void Start_tile_itself_need_not_be_walkable()
    {
        // The farmer can stand in a doorway the collision check calls blocked; the route still leads out.
        var map = new TextGrid("#..G");
        var route = Route(map, new Vector2(0, 0), map.Find('G'));
        Assert.NotNull(route);
        Assert.Equal(3, route!.Count);
    }

    [Fact]
    public void Every_tile_is_asked_about_at_most_once_per_search()
    {
        var map = new TextGrid(
            "S....",
            ".###.",
            ".#G#.",
            "..#..");
        Route(map, map.Find('S'), map.Find('G')); // G is walled in, so the search gives up after visiting every reachable tile
        Assert.NotEmpty(map.Probes);
        Assert.All(map.Probes, probe => Assert.Equal(1, probe.Value));
    }

    [Fact]
    public void Same_input_gives_the_same_route()
    {
        var map = new TextGrid(
            "S.....",
            "......",
            "......",
            ".....G");
        var first = Route(map, map.Find('S'), map.Find('G'));
        var second = Route(map, map.Find('S'), map.Find('G'));
        Assert.NotNull(first);
        Assert.Equal(Steps(first!), Steps(second!));
    }

    [Fact]
    public void Big_open_map_is_solved_with_a_route_of_the_manhattan_length()
    {
        var rows = Enumerable.Repeat(new string('.', 120), 120).ToArray();
        var map = new TextGrid(rows);
        var route = Route(map, new Vector2(0, 0), new Vector2(119, 119));
        Assert.NotNull(route);
        AssertValidRoute(map, new Vector2(0, 0), new Vector2(119, 119), route!);
        Assert.Equal(238, route!.Count);
    }

    [Fact]
    public void Routes_match_a_breadth_first_search_on_random_maps()
    {
        var random = new Random(20240607);
        for (int round = 0; round < 300; round++)
        {
            const int size = 14;
            var rows = new string[size];
            for (int y = 0; y < size; y++)
                rows[y] = new string(Enumerable.Range(0, size).Select(_ => random.NextDouble() < 0.3 ? '#' : '.').ToArray());
            var map = new TextGrid(rows);
            var start = new Vector2(random.Next(size), random.Next(size));
            var goal = new Vector2(random.Next(size), random.Next(size));

            var route = Route(map, start, goal);
            int? expected = map.ShortestSteps((int)start.X, (int)start.Y, (int)goal.X, (int)goal.Y);

            if (expected == null)
                Assert.Null(route);
            else
            {
                Assert.NotNull(route);
                Assert.Equal(expected.Value, route!.Count);
                if (route.Count > 0)
                    AssertValidRoute(map, start, goal, route);
            }
        }
    }

    [Fact]
    public void A_start_walled_in_on_four_sides_steps_out_diagonally()
    {
        // Town on day 4 of the soundtest run: the farmer at (64,85), wedged into Lewis's house wall after Lewis walked
        // through it, with only (65,86) open around it. Every walk failed for 15 game hours.
        var map = new TextGrid(
            "#####...",
            "##S##...",
            "###.....",
            "........");
        var start = map.Find('S');
        var route = Route(map, start, new Vector2(0, 3));
        Assert.NotNull(route);
        Assert.Equal((3, 2), Steps(route!)[0]);
        AssertValidRoute(map, start, new Vector2(0, 3), route!);

        var oneStep = Route(map, start, new Vector2(3, 2));
        Assert.Equal(new List<(int X, int Y)> { (3, 2) }, Steps(oneStep!));
    }

    [Fact]
    public void A_start_with_one_open_side_never_cuts_a_corner()
    {
        var map = new TextGrid(
            "#.###",
            "#S###",
            "###..",
            ".....");
        // (2,2) is diagonal to S, but S can walk up, so it stays shut in with (1,0).
        Assert.Null(Route(map, map.Find('S'), new Vector2(4, 3)));
        Assert.NotNull(Route(map, map.Find('S'), new Vector2(1, 0)));
    }

    [Fact]
    public void A_start_walled_in_on_all_eight_sides_has_no_route()
    {
        var map = new TextGrid(
            "###..",
            "#S#..",
            "###..");
        Assert.Null(Route(map, map.Find('S'), new Vector2(4, 1)));
    }

    // ------------------------------------------------------------------ AvoidingGrid (re-plan around villagers)

    [Fact]
    public void Avoiding_grid_routes_around_the_avoided_tiles()
    {
        var map = new TextGrid(
            "S...G",
            ".....");
        var avoid = new HashSet<Point> { new(2, 0) };
        var route = new Pathfinder().FindPath(new AvoidingGrid(map, avoid), map.Find('S'), map.Find('G'));
        Assert.NotNull(route);
        Assert.DoesNotContain(new Vector2(2, 0), route!);
        Assert.Equal(6, route!.Count);
    }

    [Fact]
    public void Avoiding_grid_gives_null_when_the_avoided_tile_is_the_only_way()
    {
        var map = new TextGrid("S...G");
        Assert.Null(new Pathfinder().FindPath(new AvoidingGrid(map, new HashSet<Point> { new(2, 0) }), map.Find('S'), map.Find('G')));
        Assert.NotNull(new Pathfinder().FindPath(map, map.Find('S'), map.Find('G')));
    }

    // ------------------------------------------------------------------ FindPathToAny (the tiles around a villager)

    private static List<Vector2>? RouteToAny(TextGrid map, Vector2 start, params (int X, int Y)[] goals) =>
        new Pathfinder().FindPathToAny(map, start, goals.Select(g => new Point(g.X, g.Y)));

    [Fact]
    public void Any_goal_route_ends_on_the_goal_that_is_the_shortest_walk_away()
    {
        // (4,0) is closer as the crow flies, but the wall makes (0,3) the shorter walk.
        var map = new TextGrid(
            "S.#..",
            "..#..",
            "..#..",
            ".....");
        var route = RouteToAny(map, map.Find('S'), (4, 0), (0, 3));
        Assert.NotNull(route);
        AssertValidRoute(map, map.Find('S'), new Vector2(0, 3), route!);
        Assert.Equal(3, route!.Count);
    }

    [Fact]
    public void Any_goal_route_is_empty_when_already_on_one_of_the_goals()
    {
        var map = new TextGrid("S..");
        var route = RouteToAny(map, map.Find('S'), (2, 0), (0, 0));
        Assert.NotNull(route);
        Assert.Empty(route!);
    }

    [Fact]
    public void Any_goal_route_skips_blocked_goals_and_is_null_when_none_is_left()
    {
        var map = new TextGrid(
            "S.#",
            "..#",
            "###");
        Assert.Null(RouteToAny(map, map.Find('S'), (2, 0), (2, 2), (9, 9)));
        var route = RouteToAny(map, map.Find('S'), (2, 0), (1, 1));
        Assert.NotNull(route);
        Assert.Equal(2, route!.Count);
    }

    [Fact]
    public void Any_goal_routes_match_the_best_single_goal_route_on_random_maps()
    {
        var random = new Random(20261026);
        for (int round = 0; round < 200; round++)
        {
            const int size = 12;
            var rows = new string[size];
            for (int y = 0; y < size; y++)
                rows[y] = new string(Enumerable.Range(0, size).Select(_ => random.NextDouble() < 0.3 ? '#' : '.').ToArray());
            var map = new TextGrid(rows);
            var start = new Vector2(random.Next(size), random.Next(size));
            var goals = Enumerable.Range(0, 3).Select(_ => (X: random.Next(size), Y: random.Next(size))).ToArray();

            var route = RouteToAny(map, start, goals);
            var best = goals.Select(g => map.ShortestSteps((int)start.X, (int)start.Y, g.X, g.Y)).Where(n => n != null).Min();

            if (best == null)
                Assert.Null(route);
            else
            {
                Assert.NotNull(route);
                Assert.Equal(best.Value, route!.Count);
                if (route.Count > 0)
                    Assert.Contains(((int)route[^1].X, (int)route[^1].Y), goals);
            }
        }
    }
}
