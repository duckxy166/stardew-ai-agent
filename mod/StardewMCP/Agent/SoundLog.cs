using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Audio;
using StardewValley.GameData;

namespace StardewMCP.Agent;

/// <summary>
/// Split-screen plays every screen's sounds through one output, so one seat's sound cannot be recorded on its own.
/// With <see cref="ModConfig.SoundLog"/> on, every game sound and music change goes to <c>sounds_NNN.jsonl</c> with the
/// seat whose screen played it and how loud each seat would hear it, so a track per seat can be rebuilt afterwards.
/// Covers <see cref="SoundsHelper.PlayLocal"/> (Game1.playSound, location sounds) and <see cref="Game1.changeMusicTrack"/>.
/// Not yet: ambient loops the game plays straight from the sound bank (wind, rain, AmbientLocationSounds).
/// </summary>
public static class SoundLog
{
    private static AgentLog _log = null!;
    private static IMonitor _monitor = null!;
    private static readonly object _lock = new();
    private static List<(int day, string line)> _pending = new();
    private static bool _warned;

    /// <summary>Patches only when the option is on, so a run without it plays exactly as before.</summary>
    public static void Apply(IManifest manifest, ModConfig config, AgentLog log, IMonitor monitor)
    {
        if (!config.SoundLog)
            return;
        _log = log;
        _monitor = monitor;
        var h = new Harmony(manifest.UniqueID);
        h.Patch(AccessTools.Method(typeof(SoundsHelper), nameof(SoundsHelper.PlayLocal)),
            postfix: new HarmonyMethod(typeof(SoundLog), nameof(PlayLocal_Postfix)));
        h.Patch(AccessTools.Method(typeof(Game1), nameof(Game1.changeMusicTrack)),
            postfix: new HarmonyMethod(typeof(SoundLog), nameof(ChangeMusic_Postfix)));
        GameSession.OnEnd("sound log", Flush);
        monitor.Log("Sound log on: sounds_NNN.jsonl in the log folder.", LogLevel.Info);
    }

    private static void PlayLocal_Postfix(string cueName, GameLocation location, Vector2? position, int? pitch, SoundContext context, bool __result)
    {
        try
        {
            if (!Context.IsWorldReady)
                return;
            Write(new
            {
                kind = "sound",
                ts = Now(),
                day = AgentLog.FarmDay,
                time = Game1.timeOfDay,
                by = Players.Current.Id,
                cue = cueName,
                loc = location?.NameOrUniqueName,
                tile = position is { } p ? new[] { p.X, p.Y } : null,
                pitch,
                ctx = context.ToString(),
                played = __result,
                hear = Hearing(location, position)
            });
        }
        catch (Exception ex)
        {
            WarnOnce(ex);
        }
    }

    private static void ChangeMusic_Postfix(string newTrackName, MusicContext music_context)
    {
        try
        {
            if (!Context.IsWorldReady || newTrackName == null)
                return;
            Write(new
            {
                kind = "music",
                ts = Now(),
                day = AgentLog.FarmDay,
                time = Game1.timeOfDay,
                by = Players.Current.Id,
                track = newTrackName,
                ctx = music_context.ToString(),
                loc = Game1.currentLocation?.NameOrUniqueName
            });
        }
        catch (Exception ex)
        {
            WarnOnce(ex);
        }
    }

    /// <summary>Seats that would hear the sound and how loud (0..1). The playing screen uses its live view; the
    /// others use their last <see cref="ScreenState"/> (up to 15 ticks old).</summary>
    private static Dictionary<string, float> Hearing(GameLocation? location, Vector2? tile)
    {
        var hear = new Dictionary<string, float>();
        foreach (var slot in Players.Slots.Take(Players.ScreenCount))
        {
            string? map;
            Rectangle view;
            if (slot.Screen == Context.ScreenId)
            {
                map = Game1.currentLocation?.NameOrUniqueName;
                view = new Rectangle(Game1.viewport.X, Game1.viewport.Y, Game1.viewport.Width, Game1.viewport.Height);
            }
            else if (slot.State is ScreenState { Ready: true } s)
            {
                map = s.Map;
                view = s.View;
            }
            else
                continue;
            float v = Volume(location?.NameOrUniqueName, location != null, tile, map, view);
            if (v > 0f)
                hear[slot.Id] = MathF.Round(v, 2);
        }
        return hear;
    }

    /// <summary>The game's rule (SoundsHelper.GetVolumeForDistance) for any screen's map and view: full volume on
    /// screen, fading to silent <see cref="SoundsHelper.MaxDistanceFromScreen"/> tiles outside it.</summary>
    internal static float Volume(string? soundMap, bool hasLocation, Vector2? tile, string? screenMap, Rectangle view)
    {
        if (!hasLocation)
            return 1f;
        if (soundMap != screenMap)
            return 0f;
        if (tile is not { } t)
            return 1f;
        float dx = t.X * 64f - view.X, dy = t.Y * 64f - view.Y;
        float cx = MathHelper.Clamp(dx, 0f, view.Width - 1), cy = MathHelper.Clamp(dy, 0f, view.Height - 1);
        float d = MathF.Sqrt((dx - cx) * (dx - cx) + (dy - cy) * (dy - cy)) / 64f;
        int max = SoundsHelper.MaxDistanceFromScreen;
        return d <= 0f ? 1f : d >= max ? 0f : 1f - d / max;
    }

    private static string Now() => DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");

    private static void Write(object entry)
    {
        var line = JsonSerializer.Serialize(entry, AgentLog.Json);
        lock (_lock)
            _pending.Add((AgentLog.FarmDay, line));
    }

    /// <summary>Appends what was queued (called about once a second from the host tick, and when the save closes).</summary>
    public static void Flush()
    {
        if (_log == null)
            return;
        List<(int day, string line)> batch;
        lock (_lock)
        {
            if (_pending.Count == 0)
                return;
            batch = _pending;
            _pending = new();
        }
        try
        {
            foreach (var day in batch.GroupBy(e => e.day))
            {
                var sb = new StringBuilder();
                foreach (var (_, line) in day)
                    sb.Append(line).Append('\n');
                File.AppendAllText(_log.SoundFile(day.Key), sb.ToString(), Encoding.UTF8);
            }
        }
        catch (Exception ex)
        {
            WarnOnce(ex);
        }
    }

    private static void WarnOnce(Exception ex)
    {
        if (_warned)
            return;
        _warned = true;
        _monitor.Log($"Sound log failed (further errors hidden): {ex.Message}", LogLevel.Warn);
    }
}
