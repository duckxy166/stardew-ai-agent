using System.Collections.Generic;
using System.Linq;

namespace StardewMCP.Agent;

/// <summary>
/// What programs outside the game can rely on: <c>GET /api/contract</c>. Clients read <see cref="ApiVersion"/> (also in
/// <c>/api/state</c>) instead of guessing from missing fields, and take the tool names, reason codes and timing numbers from
/// here instead of copying them from the C# source. Game-free, so the unit tests build the same object.
/// </summary>
public static class ApiContract
{
    /// <summary>
    /// Raised when an endpoint, a result field or a timing rule changes in a way a client has to know about. Adding a tool
    /// or a field does not raise it; renaming or removing one is not allowed anyway (CONTRIBUTING.md, "Don't break").
    /// 1: first version with /api/contract (2026-11).
    /// </summary>
    public const int ApiVersion = 1;

    /// <summary>Real seconds a non-instant action may run before it fails with <c>timeout</c> (go_to_bed has its own limit).</summary>
    public const int ActionTimeoutSeconds = 6 * 60;

    /// <summary>Timing a client has to fit its own timeouts to.</summary>
    public sealed record Timing(int MaxHeldSeconds, int ThinkTimeoutSeconds, int ActionTimeoutSeconds);

    public sealed record ToolEntry(string Name, bool Instant, bool InstantFor);

    public sealed record Contract(int ApiVersion, string ModVersion, IReadOnlyList<ToolEntry> Tools, IReadOnlyList<string> Reasons, Timing Timing);

    /// <summary>The contract for these tools (sorted by name), reason codes and timing.</summary>
    public static Contract Build(string modVersion, IEnumerable<ToolDef> tools, IEnumerable<string> reasons, Timing timing) =>
        new(ApiVersion, modVersion,
            tools.OrderBy(t => t.Name, System.StringComparer.Ordinal)
                 .Select(t => new ToolEntry(t.Name, t.Instant, t.InstantFor != null))
                 .ToList(),
            reasons.ToList(),
            timing);
}
