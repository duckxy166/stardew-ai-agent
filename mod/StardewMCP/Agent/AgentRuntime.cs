using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using StardewModdingAPI;
using StardewValley;

namespace StardewMCP.Agent;

/// <summary>Who sent a tool call. Decides its lane and whether run control may hold or refuse it.</summary>
public enum CallSource
{
    /// <summary>The smoke-test hooks (<c>/api/debug/*</c>): always the action lane, never held or refused by run control.</summary>
    Debug,
    /// <summary>An MCP client on the seat's endpoint (<c>/mcp/pN</c>).</summary>
    Agent,
    /// <summary>The mod's built-in AI for the seat (Agent/Brain).</summary>
    Brain,
    /// <summary>A program reading through the HTTP API on the seat's screen (/api/briefing, /api/relationships): not the AI.</summary>
    Runner
}

/// <summary>
/// A tool call waiting for (or running on) the game thread. Its life: queued, running (<see cref="AgentRuntime.CurrentCall"/>),
/// answered. The answer goes out once, through <see cref="Answer"/>, from whoever ends it: the lane when the coroutine finishes,
/// run control or a client deadline when it never started, <see cref="ReplyDeadline"/> when a sleeper is answered early
/// while go_to_bed keeps running. A running call that was answered early still finishes in its lane, but its later result
/// only goes to the log.
/// </summary>
public class PendingCall
{
    public ToolDef Tool = null!;
    public JsonElement Args;
    /// <summary>Observes only: keeps the world frozen and may answer during the seat's own action.</summary>
    public bool Instant;
    public CallSource Source;
    /// <summary>Sent by the AI playing the seat (MCP client or built-in brain): <see cref="RunControl"/> holds and refuses these.</summary>
    public bool FromAgent => Source is CallSource.Agent or CallSource.Brain;
    public DateTime Queued = DateTime.UtcNow;
    /// <summary>Completes with the answer. Await it; complete it only through <see cref="Answer"/>.</summary>
    public TaskCompletionSource<(bool ok, string text)> Done = new(TaskCreationOptions.RunContinuationsAsynchronously);
    /// <summary>Reason code, partial flag and counts of the answer; set together with it by <see cref="Answer"/>.</summary>
    public ToolOutcome? Outcome { get; private set; }

    /// <summary>The caller has its answer (the call may still be running, see the class summary).</summary>
    public bool Answered => Done.Task.IsCompleted;

    /// <summary>Give the caller its answer. Only the first answer counts; false when the call was answered already.</summary>
    public bool Answer(bool ok, string text, ToolOutcome outcome)
    {
        lock (Done)
        {
            if (Done.Task.IsCompleted) return false;
            Outcome = outcome; // before the task completes: the awaiting side reads it right after
            return Done.TrySetResult((ok, text));
        }
    }
}

/// <summary>
/// Runs one seat's tool calls on the game thread as coroutines. Actions run one at a time in
/// order; instant observations (look, status, think...) run in their own lane so they answer
/// even while that seat's own action is running. Freezing the world is <see cref="WorldClock"/>'s job.
/// </summary>
public class AgentRuntime
{
    private readonly IMonitor _monitor;
    private readonly ConcurrentQueue<PendingCall> _actions = new();
    private readonly ConcurrentQueue<PendingCall> _observations = new();
    private readonly Lane _actionLane;
    private readonly Lane _observeLane;
    private int _globalTicks;
    private int _eventTicks;

    public PlayerLog Log { get; }

    /// <summary>This seat's numbers for the day in progress; written as stats_NNN.json when the day rolls over (#25).</summary>
    public DayStats Stats { get; }

    /// <summary>Profession choices (skill, level) waiting for choose_profession, and the game day they were offered.</summary>
    public List<Microsoft.Xna.Framework.Point> PendingProfessions { get; } = new();
    public int PendingProfessionDay { get; set; }
    /// <summary>Farm day the "too early for bed" note was given (once a day, see go_to_bed).</summary>
    public int EarlyBedDay { get; set; }
    public ModConfig Config { get; }
    public IModHelper Helper { get; }

    /// <summary>Heart events this seat plays instead of skipping them, and friendship milestones (NpcStory.cs).</summary>
    public StoryWatcher Story { get; private set; } = new();

    /// <summary>Stuck/loop detector for this seat's calls: repeated failures, no progress. Hints per day: <c>Guard.HintsToday</c>. See LoopGuard.cs.</summary>
    public LoopGuard Guard { get; private set; }

    /// <summary>What the farmer walked past during the running action (forage, spots to dig...). See <see cref="Sightings"/>.</summary>
    public SightLog Sights { get; } = new();

    /// <summary>True while an action that lets game time pass is running (or a heart event plays by itself).</summary>
    public bool ActionRunning => _actionLane.Current is { Instant: false } || Story.Playing;
    public string? CurrentTool => _actionLane.Current?.Tool.Name;
    /// <summary>The call in the action lane (check <see cref="PendingCall.Instant"/>), or null. <see cref="Perception"/> reads it.</summary>
    public PendingCall? CurrentCall => _actionLane.Current;

    /// <summary>
    /// Stop the running action now and give <paramref name="result"/> to the AI as its result (not a failure).
    /// False when no non-instant action is running. Used by <see cref="Perception"/>; any other event source can use it too.
    /// </summary>
    public bool Interrupt(string result) => _actionLane.Interrupt(result);

    /// <summary>go_to_bed is past the day change and waiting for this farmer to wake up (needs the world running).</summary>
    public bool WakingUp => _actionLane.Current?.Tool.Name == "go_to_bed" && Context.IsWorldReady && Game1.Date.TotalDays > _actionLane.CallStartDay;
    /// <summary>The "reason" the running action was given (what the viewers see), or null when no action runs. Used by the pass-out record.</summary>
    public string? CurrentReason => _actionLane.Current is { Instant: false } c ? AgentTools.Reason(c.Args) : null;

    public AgentRuntime(IModHelper helper, IMonitor monitor, ModConfig config, PlayerLog log)
    {
        Helper = helper;
        _monitor = monitor;
        Config = config;
        Log = log;
        Stats = new DayStats(log);
        _actionLane = new Lane(this, pausable: true);
        _observeLane = new Lane(this, pausable: false);

        Guard = new LoopGuard(config, log, monitor);
    }

    /// <summary>
    /// Queue a call. Tools marked <see cref="ToolDef.Instant"/> go to the observation lane (they answer even during the
    /// seat's own action), except debug ops: those and everything else wait their turn in the action lane.
    /// </summary>
    public Task<(bool ok, string text)> Enqueue(ToolDef tool, JsonElement args, CallSource source) =>
        EnqueueCall(tool, args, source).Done.Task;

    /// <summary>Like <see cref="Enqueue"/>, but returns the call so the caller can read its <see cref="PendingCall.Outcome"/> after <see cref="PendingCall.Done"/>.</summary>
    public PendingCall EnqueueCall(ToolDef tool, JsonElement args, CallSource source)
    {
        var call = new PendingCall { Tool = tool, Args = args, Instant = tool.Instant || tool.InstantFor?.Invoke(args) == true, Source = source };
        if (call.FromAgent && RunControl.StopNow)
        {
            call.Answer(false, RunControl.StopMessage, new ToolOutcome(false, Reasons.Interrupted, false, null));
            return call;
        }
        (source != CallSource.Debug && call.Instant ? _observations : _actions).Enqueue(call);
        return call;
    }

    /// <summary>
    /// The save ended (<see cref="GameSession"/>): answer every waiting and running call so no HTTP request or brain waits on a
    /// farmer that is gone, and forget this seat's per-save memory (loop guard, heart events, sightings, profession offers).
    /// Game thread. The runtime itself stays: seats keep theirs for the next save.
    /// </summary>
    public void Reset()
    {
        const string text = "The save was closed (back to the title screen). Wait until the game is loaded again.";
        var outcome = new ToolOutcome(false, Reasons.NotReady, false, null);
        _actionLane.Abort(text, outcome);
        _observeLane.Abort(text, outcome);
        foreach (var q in new[] { _actions, _observations })
            while (q.TryDequeue(out var call))
                call.Answer(false, text, outcome);
        Guard = new LoopGuard(Config, Log, _monitor);
        Story = new StoryWatcher();
        Sights.Clear();
        PendingProfessions.Clear();
        PendingProfessionDay = 0;
        EarlyBedDay = 0;
        _eventTicks = 0;
    }

    /// <summary>
    /// Answer this seat's AI calls still waiting in the action lane (only those queued at least <paramref name="olderThanSeconds"/>)
    /// with <paramref name="text"/> as a failure; the lane drops them when their turn comes. Game thread. Returns how many.
    /// </summary>
    public int AnswerQueued(string text, double olderThanSeconds = 0)
    {
        int n = 0;
        foreach (var call in _actions)
        {
            if (!call.FromAgent || call.Answered || (DateTime.UtcNow - call.Queued).TotalSeconds < olderThanSeconds)
                continue;
            if (call.Answer(false, text, new ToolOutcome(false, Reasons.NotReady, false, null))) n++;
        }
        return n;
    }

    /// <summary>Called every tick of this seat's screen (UpdateTicked).</summary>
    public void Tick()
    {
        _globalTicks++;
        AutoHandleOverlays();
        PassOut.Tick(this);
        QuestEvents.Tick(this);
        AgentTools.FishingTick(this); // plays an open fishing minigame while this seat runs the fish tool
        SocialTools.Tick(Log.Player);
        ReplyDeadline.Tick(this); // the client's own deadline first, as when it was part of Perception
        Perception.Tick(this);
        Story.Tick(this);
        WorldProgress.Tick(this);
        Romance.Tick(this);
        Stats.Tick();
        NightEvents.Tick(this);
        Sightings.Tick(this);
        _actionLane.Tick(_actions);
        _observeLane.Tick(_observations);
    }

    private void AutoHandleOverlays()
    {
        if (!Context.IsWorldReady && Game1.activeClickableMenu is not StardewValley.Menus.ShippingMenu && Game1.activeClickableMenu is not StardewValley.Menus.LevelUpMenu)
            return;

        if (_globalTicks % 60 == 0)
        {
            switch (Game1.activeClickableMenu)
            {
                case StardewValley.Menus.ShippingMenu sm:
                    AgentTools.ClickShippingOk(sm);
                    break;
                case StardewValley.Menus.LevelUpMenu lm when lm.isActive:
                    if (lm.isProfessionChooser)
                        Professions.Defer(lm, this);
                    else
                        lm.okButtonClicked();
                    break;
                case StardewValley.Menus.NamingMenu nm:
                    // Not named here any more (#53): the AI chooses with name(text); NamingTools.cs has the fallbacks.
                    Naming.Observe(this, nm);
                    break;
            }
        }

        if (Game1.eventUp && Game1.CurrentEvent != null && !Game1.CurrentEvent.isFestival)
        {
            _eventTicks++;
            if (Story.OnEvent(this))
            {
                // A heart event the StoryWatcher plays (opt-in PlayHeartEvents): not skipped, not auto-answered.
            }
            else if (_eventTicks == 90)
            {
                Log.Add("system", text: $"Skipping cutscene ({Game1.CurrentEvent.id})");
                Game1.CurrentEvent.skipEvent();
            }
            else if (_eventTicks > 180 && _eventTicks % 60 == 0 && Game1.activeClickableMenu is StardewValley.Menus.DialogueBox db)
            {
                if (db.isQuestion && db.responses?.Length > 0) db.selectedResponse = 0;
                db.receiveLeftClick(0, 0);
            }
        }
        else
        {
            _eventTicks = 0;
            Story.OnNoEvent(this);
        }
    }

    /// <summary>Runs queued calls one at a time, stepping the current coroutine once per tick.</summary>
    private class Lane
    {
        private readonly AgentRuntime _rt;
        private readonly bool _pausable;
        private readonly Stack<IEnumerator> _stack = new();
        private CallContext? _ctx;
        private Stopwatch _watch = new();
        private int _ticksInCall;
        public int CallStartDay { get; private set; }
        /// <summary>This farmer's health on the previous tick of the running action, for the damage interrupt.</summary>
        private int _lastHealth;

        public PendingCall? Current { get; private set; }

        /// <param name="pausable">Action lane: non-instant coroutines hold still while the world is frozen.</param>
        public Lane(AgentRuntime rt, bool pausable)
        {
            _rt = rt;
            _pausable = pausable;
        }

        public void Tick(ConcurrentQueue<PendingCall> queue)
        {
            if (Current == null && !RunControl.ShouldHold(queue))
                while (queue.TryDequeue(out var next))
                    if (!next.Answered) // answered while it waited (AnswerQueued, run control): dropped
                    {
                        Start(next);
                        break;
                    }
            if (Current != null)
                Step();
        }

        private void Start(PendingCall call)
        {
            Current = call;
            _ctx = new CallContext();
            _ticksInCall = 0;
            CallStartDay = Context.IsWorldReady ? Game1.Date.TotalDays : 0;
            _lastHealth = Context.IsWorldReady ? Game1.player.health : 0;
            _watch = Stopwatch.StartNew();
            _stack.Clear();
            _rt.Stats.OnCallStart(call.Tool.Name, call.Instant);
            if (!call.Instant) _rt.Sights.Clear(); // a new action has seen nothing yet, even one refused below

            if (SessionBreaks.Refuse(_rt.Log, call, out var breakMessage)) // after a planned session break, no more actions (#30)
            {
                _ctx.Fail(breakMessage);
                Finish();
                return;
            }

            if (!call.Instant)
                _rt.Log.Add("action", call.Tool.Name, AgentTools.Reason(call.Args), AgentTools.ArgsForLog(call.Args));

            try
            {
                if (call.Tool.NeedsWorld && !Context.IsWorldReady)
                {
                    _ctx.Fail(Reasons.NotReady, "The game world is not loaded (title screen or loading). Wait and try again.");
                    Finish();
                    return;
                }

                // Loop guard: the same call already failed again and again, so don't run it a fifth time.
                if (_rt.Guard.Refuse(call, out var refusal))
                {
                    _ctx.Fail(refusal);
                    Finish();
                    return;
                }

                _stack.Push(call.Tool.Run(call.Args, _ctx));
            }
            catch (Exception ex)
            {
                _ctx.Fail(Reasons.Error, $"Error: {ex.Message}");
                Finish();
            }
        }

        private void Step()
        {
            // While the world is frozen (an AI is thinking) a running action holds still, so its
            // stuck/re-plan counters and timeouts don't advance either.
            if (_pausable && !Current!.Instant && WorldClock.WorldPaused)
                return;

            _ticksInCall++;
            // go_to_bed waits for the others until 2:00 AM at the latest and keeps its own, longer limit
            if (_ticksInCall > 60 * 60 * 6 && Current!.Tool.Name != "go_to_bed")
            {
                _ctx!.Fail(Reasons.Timeout, "Action timed out after 6 minutes of real time.");
                Finish();
                return;
            }

            if (Current!.Tool.Name != "go_to_bed" && CallStartDay > 0 && Context.IsWorldReady &&
                Game1.Date.TotalDays > CallStartDay && Game1.activeClickableMenu == null && !Game1.fadeToBlack && !Game1.newDay)
            {
                _rt.Log.Add("system", text: $"Passed out / new day: farm day {AgentLog.FarmDay}");
                _ctx!.Result = $"You passed out (2:00 AM or exhausted)! It is now farm day {AgentLog.FarmDay}. Stop here.";
                _ctx.Reason = Reasons.Interrupted;
                _ctx.Partial = true;
                Finish();
                return;
            }

            // Hurt by a monster close by: end the call so the AI can react (fight, eat, leave) instead of finishing a long action.
            if (!Current!.Instant && Context.IsWorldReady && AgentTools.DamageInterrupt(Current.Tool.Name, ref _lastHealth) is { } hurt)
            {
                _ctx!.Result = hurt;
                Finish();
                return;
            }

            try
            {
                // Run until the coroutine yields a "wait one tick" (null) or finishes.
                while (_stack.Count > 0)
                {
                    var top = _stack.Peek();
                    if (!top.MoveNext())
                    {
                        _stack.Pop();
                        continue;
                    }
                    if (top.Current is IEnumerator nested)
                    {
                        _stack.Push(nested);
                        continue;
                    }
                    return; // yielded null: wait for next tick
                }
            }
            catch (Exception ex)
            {
                _rt._monitor.Log($"Tool {Current?.Tool.Name} crashed: {ex}", LogLevel.Error);
                _ctx!.Fail(Reasons.Error, $"Error: {ex.Message}");
            }
            Finish();
        }

        /// <summary>Drop the running action where it stands. Disposing the coroutines runs their finally blocks (e.g. a harvest still reports its thefts).</summary>
        public bool Interrupt(string result)
        {
            if (Current == null || Current.Instant) return false;
            while (_stack.Count > 0)
            {
                try { (_stack.Pop() as IDisposable)?.Dispose(); }
                catch (Exception ex) { _rt._monitor.Log($"Tool {Current.Tool.Name} failed while being interrupted: {ex.Message}", LogLevel.Warn); }
            }
            _ctx!.Ok = true;
            _ctx.Result = result;
            Finish();
            return true;
        }

        /// <summary>Drop the running call without a result (the save ended): its coroutines are disposed, the caller gets <paramref name="text"/>.</summary>
        public void Abort(string text, ToolOutcome outcome)
        {
            if (Current == null) return;
            var call = Current;
            while (_stack.Count > 0)
            {
                try { (_stack.Pop() as IDisposable)?.Dispose(); }
                catch (Exception ex) { _rt._monitor.Log($"Tool {call.Tool.Name} failed while being dropped: {ex.Message}", LogLevel.Warn); }
            }
            Current = null;
            _ctx = null;
            call.Answer(false, text, outcome);
        }

        private void Finish()
        {
            var call = Current!;
            var ctx = _ctx!;
            _stack.Clear();
            Current = null;
            _ctx = null;

            var text = ctx.Result;
            // Loop guard: "you tried this 3 times", "you haven't done anything for a while".
            var hint = _rt.Guard.End(call, ctx.Ok, ctx.Result);
            if (hint.Length > 0) text = text.TrimEnd() + "\n" + hint;

            // Forage and other finds the farmer walked past, also when the walk was interrupted (in the log too)
            var seen = Context.IsWorldReady ? Sightings.Line(_rt, call) : "";
            if (seen.Length > 0)
            {
                ctx.Result = ctx.Result.TrimEnd() + "\n" + seen;
                text = text.TrimEnd() + "\n" + seen;
            }

            // a call answered early (Perception.ReplyInTime) leaves what was heard meanwhile for the next result
            var mail = call.Tool.Mail && !call.Answered ? Perception.TakeMail(_rt.Log.Player) : "";
            if (mail.Length > 0) text = text.TrimEnd() + "\n" + mail;
            if (call.Tool.AppendStatus && Context.IsWorldReady)
                text = text.TrimEnd() + "\n" + Observation.OneLine(AgentTools.Footer()); // one line, however many menus are open
            text = SessionBreaks.Annotate(_rt.Log, call, text); // planned session break notice (#30)

            _rt.Stats.OnCallEnd(call.Tool.Name, ctx.Ok, ctx.Result, CallStartDay);
            var outcome = ToolOutcome.From(ctx);
            if (!call.Instant || !ctx.Ok)
                _rt.Log.Add("result", call.Tool.Name, ctx.Result, ok: ctx.Ok, ms: _watch.ElapsedMilliseconds, outcome: outcome);

            call.Answer(ctx.Ok, text, outcome); // false when it was answered early: then this result only went to the log
        }
    }
}
