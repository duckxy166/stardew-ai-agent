using StardewMCP.Agent;
using Xunit;

namespace StardewMCP.Tests;

public class SightLogTests
{
    private static bool All(Sight _) => true;

    [Fact]
    public void Nothing_seen_gives_no_line()
    {
        Assert.Equal("", new SightLog().Line("Town", 0, 0, All));
    }

    [Fact]
    public void Finds_on_this_map_come_nearest_first_with_how_to_get_them()
    {
        var log = new SightLog();
        log.Add(new Sight("Town", 30, 30, "Leek", SightKind.Forage));
        log.Add(new Sight("Town", 12, 10, "Artifact Spot", SightKind.DigSpot));
        log.Add(new Sight("Town", 11, 11, "Daffodil", SightKind.Forage));

        Assert.Equal(
            "Seen on the way, still there: Daffodil (forage) at (11,11), Artifact Spot (dig it with the Hoe) at (12,10), " +
            "Leek (forage) at (30,30). Forage: harvest on its tile picks it up.",
            log.Line("Town", 10, 10, All));
    }

    [Fact]
    public void Earlier_maps_follow_grouped_by_map_the_last_one_left_first()
    {
        var log = new SightLog();
        log.Add(new Sight("Farm", 5, 5, "Wood", SightKind.Dropped));
        log.Add(new Sight("BusStop", 7, 7, "Bush", SightKind.Bush));
        log.Add(new Sight("Town", 20, 20, "Leek", SightKind.Forage));

        Assert.Equal(
            "Seen on the way, still there: Leek (forage) at (20,20); on BusStop: Bush (interact to shake it) at (7,7); " +
            "on Farm: Wood (on the ground) at (5,5). Forage: harvest on its tile picks it up.",
            log.Line("Town", 20, 21, All));
    }

    [Fact]
    public void Finds_that_are_gone_are_left_out()
    {
        var log = new SightLog();
        log.Add(new Sight("Town", 1, 1, "Leek", SightKind.Forage));
        log.Add(new Sight("Town", 2, 2, "Artifact Spot", SightKind.DigSpot));

        var line = log.Line("Town", 0, 0, s => s.Name != "Leek");
        Assert.Equal("Seen on the way, still there: Artifact Spot (dig it with the Hoe) at (2,2).", line);
        Assert.Equal("", log.Line("Town", 0, 0, _ => false));
    }

    [Fact]
    public void A_tile_keeps_its_first_sighting()
    {
        var log = new SightLog();
        log.Add(new Sight("Town", 1, 1, "Leek", SightKind.Forage));
        log.Add(new Sight("Town", 1, 1, "Something else", SightKind.Dropped));
        Assert.Equal(1, log.Count);
        Assert.Contains("Leek (forage)", log.Line("Town", 0, 0, All));
    }

    [Fact]
    public void Long_lists_are_cut_and_the_rest_counted()
    {
        var log = new SightLog();
        for (int i = 0; i < SightLog.MaxListed + 3; i++)
            log.Add(new Sight("Beach", i, 0, "Clam", SightKind.Forage));

        var line = log.Line("Beach", 0, 0, All);
        Assert.Equal(SightLog.MaxListed, line.Split(" at (").Length - 1);
        Assert.Contains("(+3 more)", line);
    }

    [Fact]
    public void Clear_forgets_everything()
    {
        var log = new SightLog();
        log.Add(new Sight("Town", 1, 1, "Leek", SightKind.Forage));
        log.Clear();
        Assert.Equal(0, log.Count);
        Assert.Equal("", log.Line("Town", 0, 0, All));
    }
}
