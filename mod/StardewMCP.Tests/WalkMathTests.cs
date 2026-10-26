using System.Linq;
using Microsoft.Xna.Framework;
using StardewMCP.Agent;
using Xunit;

namespace StardewMCP.Tests;

public class WalkMathTests
{
    // A farmer's bounding box: 48 x 32 at its feet, 8 px in from its position's left edge.
    private const int FarmerWidth = 48, FarmerHeight = 32;

    private static bool FarmerOn(int tileX, int tileY, int boxX, int boxY) =>
        WalkMath.OnTile(tileX, tileY, boxX, boxY, FarmerWidth, FarmerHeight);

    [Fact]
    public void A_farmer_standing_in_the_middle_of_a_tile_is_on_it()
    {
        Assert.True(FarmerOn(3, 2, 3 * 64 + 8, 2 * 64 + 16));
    }

    [Fact]
    public void A_farmer_within_two_pixels_of_a_side_is_not_on_the_tile_yet()
    {
        Assert.True(FarmerOn(3, 2, 3 * 64 + 2, 2 * 64 + 16));
        Assert.False(FarmerOn(3, 2, 3 * 64 + 1, 2 * 64 + 16));
        Assert.True(FarmerOn(3, 2, 3 * 64 + 62 - FarmerWidth, 2 * 64 + 16));
        Assert.False(FarmerOn(3, 2, 3 * 64 + 63 - FarmerWidth, 2 * 64 + 16));
    }

    [Fact]
    public void The_feet_must_be_two_pixels_above_the_tiles_bottom_edge()
    {
        Assert.True(FarmerOn(3, 2, 3 * 64 + 8, 3 * 64 - 2 - FarmerHeight));
        Assert.False(FarmerOn(3, 2, 3 * 64 + 8, 3 * 64 - 1 - FarmerHeight));
        Assert.False(FarmerOn(3, 2, 3 * 64 + 8, 2 * 64 - 1)); // still partly on the tile above
    }

    [Fact]
    public void A_box_wider_than_the_tile_counts_by_its_centre()
    {
        // A horse's box (wider than 60 px) can never fit inside, so its centre decides.
        Assert.True(WalkMath.OnTile(3, 2, 3 * 64 - 10, 2 * 64 + 16, 84, 32));
        Assert.False(WalkMath.OnTile(3, 2, 3 * 64 + 30, 2 * 64 + 16, 84, 32));
    }

    [Fact]
    public void The_action_button_reaches_every_tile_around_and_no_further()
    {
        var me = new Point(10, 10);
        Assert.True(WalkMath.InReach(me, new Point(10, 10)));
        Assert.True(WalkMath.InReach(me, new Point(11, 9)));
        Assert.True(WalkMath.InReach(me, new Point(9, 11)));
        Assert.False(WalkMath.InReach(me, new Point(12, 10)));
        Assert.False(WalkMath.InReach(me, new Point(11, 12)));
    }

    [Fact]
    public void Around_lists_the_eight_neighbours_sides_first()
    {
        var around = WalkMath.Around(new Point(5, 5)).ToList();
        Assert.Equal(8, around.Count);
        Assert.Equal(8, around.Distinct().Count());
        Assert.All(around, p => Assert.Equal(1, WalkMath.Steps(p, new Point(5, 5))));
        Assert.All(around.Take(4), p => Assert.True(p.X == 5 || p.Y == 5));
    }
}
