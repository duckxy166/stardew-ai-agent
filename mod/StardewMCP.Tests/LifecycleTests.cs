using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using StardewMCP.Agent;
using Xunit;

namespace StardewMCP.Tests;

/// <summary>The API contract, the browser Origin rule, game-thread work from HTTP requests and the end of a save.</summary>
public class ApiContractTests
{
    private static readonly JsonSerializerOptions Camel = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public void Contract_lists_every_tool_sorted_with_its_lane()
    {
        var tools = SampleTools.All().Values.ToList();
        var c = ApiContract.Build("1.2.3", tools, new[] { "ok", "no_path" }, new ApiContract.Timing(540, 300, 360));

        Assert.Equal(ApiContract.ApiVersion, c.ApiVersion);
        Assert.Equal("1.2.3", c.ModVersion);
        Assert.Equal(tools.Select(t => t.Name).OrderBy(n => n, StringComparer.Ordinal), c.Tools.Select(t => t.Name));
        Assert.True(c.Tools.Single(t => t.Name == "look").Instant);
        Assert.False(c.Tools.Single(t => t.Name == "walk_to").Instant);
        Assert.Equal(new[] { "ok", "no_path" }, c.Reasons);
    }

    [Fact]
    public void Contract_serializes_with_the_field_names_clients_read()
    {
        var c = ApiContract.Build("0.4.0", SampleTools.All().Values, new[] { "ok" }, new ApiContract.Timing(540, 300, 360));
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(c, Camel));
        var root = doc.RootElement;
        Assert.Equal(1, root.GetProperty("apiVersion").GetInt32());
        Assert.Equal("0.4.0", root.GetProperty("modVersion").GetString());
        var tool = root.GetProperty("tools")[0];
        Assert.True(tool.TryGetProperty("name", out _));
        Assert.True(tool.TryGetProperty("instant", out _));
        Assert.True(tool.TryGetProperty("instantFor", out _));
        var timing = root.GetProperty("timing");
        Assert.Equal(540, timing.GetProperty("maxHeldSeconds").GetInt32());
        Assert.Equal(300, timing.GetProperty("thinkTimeoutSeconds").GetInt32());
        Assert.Equal(360, timing.GetProperty("actionTimeoutSeconds").GetInt32());
    }
}

public class OriginPolicyTests
{
    [Theory]
    [InlineData("POST", "/mcp/p1", null)]
    [InlineData("POST", "/mcp/p1", "")]
    [InlineData("POST", "/api/control", "http://localhost:8770")]
    [InlineData("POST", "/mcp", "http://127.0.0.1:3000")]
    [InlineData("POST", "/api/debug/p2/warp", "http://[::1]:8770")]
    [InlineData("GET", "/api/feed", "https://example.com")]
    [InlineData("GET", "/api/state", "null")]
    public void Allows(string method, string path, string? origin) =>
        Assert.True(OriginPolicy.Allows(method, path, origin));

    [Theory]
    [InlineData("POST", "/mcp/p1", "https://example.com")]
    [InlineData("POST", "/mcp", "null")]
    [InlineData("POST", "/api/control", "http://localhost.example.com")]
    [InlineData("POST", "/api/release_freeze", "http://192.168.1.5:8770")]
    [InlineData("POST", "/api/meta", "https://evil.test")]
    [InlineData("GET", "/mcp/p1", "https://example.com")]
    [InlineData("GET", "/api/debug/status", "https://example.com")]
    [InlineData("DELETE", "/api/day", "https://example.com")]
    public void Refuses(string method, string path, string origin) =>
        Assert.False(OriginPolicy.Allows(method, path, origin));
}

public class GameThreadTests
{
    [Fact]
    public async Task Work_runs_on_drain_and_returns_its_value()
    {
        var gt = new GameThread();
        var pending = gt.Run(() => 42, TimeSpan.FromSeconds(5));
        Assert.False(pending.IsCompleted);
        gt.Drain();
        Assert.Equal(42, await pending);
    }

    [Fact]
    public async Task An_exception_reaches_the_caller()
    {
        var gt = new GameThread();
        var pending = gt.Run<int>(() => throw new InvalidOperationException("boom"), TimeSpan.FromSeconds(5));
        gt.Drain();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => pending);
        Assert.Equal("boom", ex.Message);
    }

    [Fact]
    public async Task A_loop_that_never_drains_times_out_and_the_late_work_is_dropped()
    {
        var gt = new GameThread();
        bool ran = false;
        await Assert.ThrowsAsync<TimeoutException>(() => gt.Run(() => ran = true, TimeSpan.FromMilliseconds(50)));
        gt.Drain();
        Assert.False(ran);
    }
}

[Collection("GameSession")] // static state: never in parallel with another test class using it
public class GameSessionTests
{
    [Fact]
    public void End_runs_every_reset_in_order_and_survives_a_failing_one()
    {
        var seen = new List<string>();
        GameSession.OnEnd("test a", () => seen.Add("a"));
        GameSession.OnEnd("test boom", () => throw new InvalidOperationException("broken"));
        GameSession.OnEnd("test b", () => seen.Add("b"));
        int before = GameSession.Generation;

        var failures = GameSession.End();

        Assert.Equal(new[] { "a", "b" }, seen);
        Assert.Contains("test boom: broken", failures);
        Assert.Equal(before + 1, GameSession.Generation);
    }

    [Fact]
    public void Calls_are_admitted_only_while_a_save_is_open_and_carry_its_generation()
    {
        GameSession.Begin();
        Assert.True(GameSession.TryAdmit(out int before));
        Assert.Equal(GameSession.Generation, before);

        GameSession.End(); // the host returned to the title
        Assert.False(GameSession.Open);
        Assert.False(GameSession.TryAdmit(out _));
        Assert.NotEqual(before, GameSession.Generation); // a call admitted before the end is stale now

        GameSession.Begin(); // the next save loaded
        Assert.True(GameSession.TryAdmit(out int after));
        Assert.Equal(GameSession.Generation, after);
        Assert.NotEqual(before, after);
    }

    [Fact]
    public void Registering_a_name_again_replaces_it()
    {
        int first = 0, second = 0;
        GameSession.OnEnd("test replace", () => first++);
        GameSession.OnEnd("test replace", () => second++);
        GameSession.End();
        Assert.Equal(0, first);
        Assert.Equal(1, second);
        Assert.Single(GameSession.Names, n => n == "test replace");
    }
}
