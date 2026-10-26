using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace StardewMCP.Agent;

/// <summary>
/// Things worth picking up that a farmer walks past: forage, artifact and seed spots, bushes with berries or nuts and items
/// lying on the ground, within <see cref="Radius"/> tiles of its route. A player sees them on the screen while walking; an AI
/// only reads tool results, so walk_to, travel_to and talk_to end with a line listing the ones still there
/// (<see cref="SightLog"/>). What to do about them stays the AI's choice.
/// </summary>
internal static class Sightings
{
    /// <summary>Tiles around the farmer that count as walking past something.</summary>
    public const int Radius = 6;

    private const int ScanEvery = 15;

    private static readonly HashSet<string> Tools = new() { "walk_to", "travel_to", "talk_to" };
    private static readonly int[] Ticks = new int[Players.Seats];

    /// <summary>Whether this tool's result gets the "Seen on the way" line.</summary>
    public static bool Watches(string tool) => Tools.Contains(tool);

    /// <summary>Every tick on the seat's own screen: while one of the watched actions runs and the world moves, look around now and then.</summary>
    public static void Tick(AgentRuntime rt)
    {
        if (rt.CurrentCall is not { Instant: false } call || !Watches(call.Tool.Name) || !Context.IsWorldReady || WorldClock.WorldPaused)
            return;
        if (Players.ById(rt.Log.Player) is not { } seat || ++Ticks[seat.Screen] % ScanEvery != 0)
            return;
        var loc = Game1.currentLocation;
        if (loc == null || Game1.isWarping || Game1.fadeToBlack)
            return;

        var me = Game1.player.TilePoint;
        for (int y = me.Y - Radius; y <= me.Y + Radius; y++)
            for (int x = me.X - Radius; x <= me.X + Radius; x++)
                if (ObjectAt(loc, x, y) is { } found)
                    rt.Sights.Add(found);
        foreach (var feature in loc.largeTerrainFeatures)
            if (feature is Bush bush && Near(bush.Tile, me) && BushSight(loc, bush) is { } found)
                rt.Sights.Add(found);
        foreach (var debris in loc.debris)
            if (DebrisSight(loc, debris) is { } found && Math.Max(Math.Abs(found.X - me.X), Math.Abs(found.Y - me.Y)) <= Radius)
                rt.Sights.Add(found);
    }

    /// <summary>The line for the result of <paramref name="call"/>, or "" (not a watched tool, or nothing seen is left).</summary>
    public static string Line(AgentRuntime rt, PendingCall call)
    {
        if (call.Instant || !Watches(call.Tool.Name) || rt.Sights.Count == 0 || !Context.IsWorldReady || Game1.currentLocation is not { } loc)
            return "";
        var me = Game1.player.TilePoint;
        return rt.Sights.Line(loc.NameOrUniqueName, me.X, me.Y, s => StillThere(loc, s));
    }

    /// <summary>A find on the current map must still be there; one on a map left behind is told as it was seen.</summary>
    private static bool StillThere(GameLocation loc, Sight s)
    {
        if (s.Map != loc.NameOrUniqueName)
            return true;
        if (ObjectAt(loc, s.X, s.Y) is { } obj)
            return obj.Name == s.Name;
        foreach (var feature in loc.largeTerrainFeatures)
            if (feature is Bush bush && (int)bush.Tile.X == s.X && (int)bush.Tile.Y == s.Y)
                return BushSight(loc, bush) is { } b && b.Name == s.Name;
        foreach (var debris in loc.debris)
            if (DebrisSight(loc, debris) is { } d && d.X == s.X && d.Y == s.Y && d.Name == s.Name)
                return true;
        return false;
    }

    private static bool Near(Vector2 tile, Point me) =>
        Math.Max(Math.Abs((int)tile.X - me.X), Math.Abs((int)tile.Y - me.Y)) <= Radius;

    /// <summary>Forage (the same rule as look's <c>F</c>) or an artifact or seed spot on this tile.</summary>
    private static Sight? ObjectAt(GameLocation loc, int x, int y)
    {
        if (!loc.Objects.TryGetValue(new Vector2(x, y), out var obj))
            return null;
        if (obj.QualifiedItemId is "(O)590" or "(O)SeedSpot")
            return new Sight(loc.NameOrUniqueName, x, y, obj.DisplayName, SightKind.DigSpot);
        return obj.isForage() ? new Sight(loc.NameOrUniqueName, x, y, obj.DisplayName, SightKind.Forage) : null;
    }

    /// <summary>A wild bush that would drop berries or a walnut if shaken now (tea bushes are someone's crop).</summary>
    private static Sight? BushSight(GameLocation loc, Bush bush)
    {
        if (bush.size.Value == Bush.greenTeaBush || bush.townBush.Value || !bush.readyForHarvest() || !bush.inBloom())
            return null;
        var item = bush.GetShakeOffItem();
        if (item == null)
            return null;
        var name = ItemRegistry.GetData(item)?.DisplayName ?? "Berry";
        return new Sight(loc.NameOrUniqueName, (int)bush.Tile.X, (int)bush.Tile.Y, $"{name} bush", SightKind.Bush);
    }

    /// <summary>An item lying still on the ground (not one already flying to a farmer).</summary>
    private static Sight? DebrisSight(GameLocation loc, Debris debris)
    {
        if (debris.item == null || debris.Chunks.Count == 0 || debris.chunksMoveTowardPlayer)
            return null;
        var at = debris.Chunks[0].position.Value;
        return new Sight(loc.NameOrUniqueName, (int)(at.X / 64), (int)(at.Y / 64), debris.item.DisplayName, SightKind.Dropped);
    }
}
