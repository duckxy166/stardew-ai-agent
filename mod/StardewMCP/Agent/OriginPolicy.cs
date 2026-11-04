using System;

namespace StardewMCP.Agent;

/// <summary>
/// Which browser pages may use the HTTP API. The listener only binds localhost, but any web page the player has open can still
/// send a request to it (a POST with a text/plain body needs no CORS preflight), and with <c>Access-Control-Allow-Origin: *</c>
/// it could even read the answer. So a request that changes something (any POST: MCP tool calls, run control, release_freeze,
/// meta, debug) is refused when it carries an <c>Origin</c> header that is not this machine. Programs that are not browsers
/// (the runner, MCP clients, curl) send no Origin and are not affected; the overlay and replay pages are served from
/// localhost and only read. Reading endpoints stay open to every origin, so an overlay opened as a local file still works.
/// The MCP spec asks servers to validate Origin for the same reason. Game-free (unit tests).
/// </summary>
public static class OriginPolicy
{
    /// <summary>True when a request with this method, path and Origin header (null = none) may go on.</summary>
    public static bool Allows(string method, string path, string? origin)
    {
        if (string.IsNullOrEmpty(origin) || IsLoopback(origin))
            return true;
        if (path == "/mcp" || path.StartsWith("/mcp/", StringComparison.Ordinal) ||
            path.StartsWith("/api/debug/", StringComparison.Ordinal))
            return false;
        return method is "GET" or "HEAD" or "OPTIONS";
    }

    /// <summary>An origin on this machine: http(s)://localhost, 127.0.0.1 or [::1], any port. "null" (file pages, sandboxed frames) is not.</summary>
    public static bool IsLoopback(string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            return false;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;
        return uri.Host is "localhost" or "127.0.0.1" or "[::1]" or "::1";
    }
}
