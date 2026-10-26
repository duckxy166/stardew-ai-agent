using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Pathfinding;

namespace StardewMCP.Agent;

/// <summary>
/// Walks a farmer along a planned route like the game's <see cref="PathFindController"/>, but the way a held key would.
/// Two things made every AI walk in jerky taps on its own screen, while the same farmer looked fine walking past on the
/// other screens (those draw it from its position, not from its input):
/// <list type="bullet">
/// <item>The game reads the keyboard and gamepad for every screen while its window has focus, and with no key held it
/// halts the farmer every tick. That restarts the walk animation and turns running off, so the farmer slid along on the
/// first frame of its stride. <see cref="ControlInput_Prefix"/> skips that input step on a screen whose farmer this
/// controller walks.</item>
/// <item>The base controller stands still for one tick on every tile it reaches. <see cref="moveCharacter"/> goes on
/// towards the next tile in the same tick; the last tile still ends the walk the base way.</item>
/// </list>
/// </summary>
public sealed class WalkController : PathFindController
{
    private readonly Character _walker;

    /// <param name="path">The tiles to step on, the first one on top.</param>
    public WalkController(Stack<Point> path, Character walker, GameLocation location) : base(path, walker, location)
    {
        _walker = walker;
        NPCSchedule = false;
        finalFacingDirection = -1; // keep facing the way the last step went (the base turns the farmer to face up)
    }

    protected override void moveCharacter(GameTime time)
    {
        // Warped to another map on the way: the rest of the route was for the old one, and the base update() drops an empty route.
        if (_walker.currentLocation?.NameOrUniqueName != location?.NameOrUniqueName)
        {
            pathToEndPoint.Clear();
            return;
        }
        while (pathToEndPoint.Count > 1 && Reached(pathToEndPoint.Peek()))
        {
            pathToEndPoint.Pop();
            timerSinceLastCheckPoint = 0;
        }
        base.moveCharacter(time);
    }

    private bool Reached(Point tile)
    {
        var box = _walker.GetBoundingBox();
        return WalkMath.OnTile(tile.X, tile.Y, box.X, box.Y, box.Width, box.Height);
    }

    /// <summary>Harmony prefix on <c>Game1.UpdateControlInput</c>: false (skip it) while this controller walks the screen's farmer.</summary>
    internal static bool ControlInput_Prefix() => Game1.player?.controller is not WalkController;
}
