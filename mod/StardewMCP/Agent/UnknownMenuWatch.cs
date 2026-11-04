using System;
using System.Linq;
using StardewValley;
using StardewValley.Menus;

namespace StardewMCP.Agent;

/// <summary>
/// Used by go_to_bed while it waits for the new day: an unexpected menu that stays open for more than
/// 10 seconds (the night normally only shows the menus listed in <see cref="Expected"/>) is logged and closed,
/// instead of leaving the call to time out after 5 minutes with the day half done.
/// </summary>
public sealed class UnknownMenuWatch
{
    /// <summary>10 seconds of stepped ticks. The action holds still while the world is frozen, so a pause never counts.</summary>
    public const int LimitTicks = 60 * 10;

    /// <summary>
    /// Menus that are part of the night: the shipping screen and level-ups (go_to_bed clicks them),
    /// the save progress screen, and the ready check that waits for the other farmers to go to bed.
    /// Matched by type name so this file needs no more game types.
    /// </summary>
    private static readonly string[] Expected = { "ShippingMenu", "LevelUpMenu", "SaveGameMenu", "ReadyCheckDialog" };

    private IClickableMenu? _menu;
    private int _ticks;
    private int _attempts;

    /// <summary>Call once per tick of the sleep wait.</summary>
    public void Tick(AgentRuntime rt)
    {
        var menu = Game1.activeClickableMenu;
        if (menu == null || Expected.Contains(menu.GetType().Name))
        {
            _menu = null;
            _ticks = 0;
            _attempts = 0;
            return;
        }
        if (!ReferenceEquals(menu, _menu))
        {
            _menu = menu;
            _ticks = 0;
            _attempts = 0;
        }
        if (++_ticks < LimitTicks)
            return;

        _ticks = 0;
        _attempts++;
        var type = menu.GetType().Name;
        rt.Log.Add("system", text: $"Unknown menu {type} open for more than 10 s while sleeping: closing it (try {_attempts})");
        try
        {
            if (_attempts == 1)
                menu.exitThisMenu();
            else
                Game1.exitActiveMenu(); // exitThisMenu did not close it: force it
        }
        catch (Exception ex)
        {
            rt.Log.Add("system", text: $"Closing {type} failed: {ex.Message}");
        }
    }
}
