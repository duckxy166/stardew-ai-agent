using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace StardewMCP.Agent;

/// <summary>
/// Game-free rules of walking up to things, shared by <see cref="WalkController"/> and talk_to. No game types, so the
/// unit tests (mod/StardewMCP.Tests) compile this file.
/// </summary>
public static class WalkMath
{
    public const int TileSize = 64;

    /// <summary>
    /// The game's own test (PathFindController.moveCharacter) for a walker standing on a tile of its route: its bounding
    /// box inside the tile narrowed by 2 px on each side (or, for a box wider than that such as a horse, its centre inside
    /// it), and its bottom edge at least 2 px above the tile's.
    /// </summary>
    public static bool OnTile(int tileX, int tileY, int boxX, int boxY, int boxWidth, int boxHeight)
    {
        int left = tileX * TileSize + 2, width = TileSize - 4, top = tileY * TileSize, height = TileSize;
        bool inside = left <= boxX && boxX + boxWidth <= left + width && top <= boxY && boxY + boxHeight <= top + height;
        int cx = boxX + boxWidth / 2, cy = boxY + boxHeight / 2;
        bool centre = boxWidth > width && left <= cx && cx < left + width && top <= cy && cy < top + height;
        return (inside || centre) && top + height - (boxY + boxHeight) >= 2;
    }

    /// <summary>
    /// Whether the action button reaches <paramref name="target"/> from <paramref name="me"/>: the game checks a tile
    /// within one of the farmer's own, diagonals included (Utility.tileWithinRadiusOfPlayer).
    /// </summary>
    public static bool InReach(Point me, Point target) =>
        Math.Abs(me.X - target.X) <= 1 && Math.Abs(me.Y - target.Y) <= 1;

    /// <summary>The eight tiles around <paramref name="p"/>: the four sides first, then the corners.</summary>
    public static IEnumerable<Point> Around(Point p)
    {
        yield return new Point(p.X, p.Y - 1);
        yield return new Point(p.X + 1, p.Y);
        yield return new Point(p.X, p.Y + 1);
        yield return new Point(p.X - 1, p.Y);
        yield return new Point(p.X - 1, p.Y - 1);
        yield return new Point(p.X + 1, p.Y - 1);
        yield return new Point(p.X + 1, p.Y + 1);
        yield return new Point(p.X - 1, p.Y + 1);
    }

    /// <summary>Tiles between two tiles when moving in eight directions (the larger of the two axis distances).</summary>
    public static int Steps(Point a, Point b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
}
