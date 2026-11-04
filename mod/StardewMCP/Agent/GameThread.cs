using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace StardewMCP.Agent;

/// <summary>
/// Work an HTTP request needs done on the game thread (it reads or changes game state) without being a tool call:
/// the request thread posts it with <see cref="Run{T}"/> and awaits the answer; the host screen's tick runs it in
/// <see cref="Drain"/>. If the game loop does not get to it in time (hung, or the title screen is loading) the caller
/// gets a <see cref="TimeoutException"/> instead of hanging, and the work is dropped. Game-free (unit tests).
/// </summary>
public sealed class GameThread
{
    private readonly ConcurrentQueue<Action> _work = new();

    /// <summary>The one queue the mod drains on the host screen's tick (ModEntry).</summary>
    public static GameThread Host { get; } = new();

    /// <summary>Run <paramref name="work"/> on the next drain; throws <see cref="TimeoutException"/> after <paramref name="timeout"/>.</summary>
    public async Task<T> Run<T>(Func<T> work, TimeSpan timeout)
    {
        var done = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _work.Enqueue(() =>
        {
            if (done.Task.IsCompleted) return; // timed out while it waited: don't run it late
            try { done.TrySetResult(work()); }
            catch (Exception ex) { done.TrySetException(ex); }
        });
        var first = await Task.WhenAny(done.Task, Task.Delay(timeout)).ConfigureAwait(false);
        if (first != done.Task && done.TrySetCanceled())
            throw new TimeoutException("The game loop did not answer in time.");
        return await done.Task.ConfigureAwait(false);
    }

    /// <summary>Run everything posted so far. Game thread only.</summary>
    public void Drain()
    {
        while (_work.TryDequeue(out var next))
            next();
    }
}
