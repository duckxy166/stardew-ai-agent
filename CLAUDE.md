# Stardew MCP

SMAPI mod (C#, net6.0) that hosts an MCP server inside Stardew Valley so AI agents can play.
This repository is **the mod only**: gameplay tools, the per-player runtime, the HTTP API and the
overlay / replay pages the mod serves itself. Build, tests, what not to break and which doc page
changes with what: `CONTRIBUTING.md`. The project's words (seat, brain, freeze, interrupt, self
note...): `CONTEXT.md`; use them.

The video production around it (runner, OBS, prompts) lives in a separate private repository and
only talks to this mod over HTTP. Nothing here may depend on it: no OBS, video, recording or runner
concerns in `mod/`. When the runner needs something new, add an HTTP endpoint or an event here.

## Design

- Up to 4 split-screen seats: P1 host, P2/P3 farmhands, each with its own MCP endpoint
  (`/mcp/p1`...), P4 a camera seat (no AI, excluded from scheduler, perception and interrupts).
  Single-player is `/mcp` alone. A seat's role is `ai`, `human` (P1 only), `camera` or `off`; an `ai`
  seat is driven by an external MCP client (`Brain: none`) or the mod's own loop (`Agent/Brain/`).
- **Time is event-driven**: the world (clock and movement) is frozen whenever any connected AI is
  thinking, between its last tool result and its next action. A thinking timeout lets it continue.
- `say` is heard only by AIs whose screen shows the speaker; `think` is private (overlay only).
- The AIs only ever read farmer names, never seat ids (`p1`, `p2`...).
- The model makes decisions; mechanics (pathing, aiming, button presses) are automated in the mod.

## How it works

- SMAPI raises `UpdateTicked` once per split-screen screen (`Context.ScreenId`); `Game1.player`,
  `currentLocation` and menus are per screen. Each seat has its own `AgentRuntime`, ticked on its
  screen; `AgentTools.Rt` is the seat of the screen being updated.
- MCP requests arrive on the HTTP thread (`AgentHttp.cs`, localhost:8770); the seat's `AgentRuntime`
  runs actions one at a time and instant observations in a second lane.
- Tools are `IEnumerator` coroutines defined as `ToolDef`s in the top-level `Agent/*Tools.cs` files:
  `yield return null` waits a tick, `yield return <IEnumerator>` runs a nested step. Report results
  through `CallContext` (`Result`, `Fail`). `Instant` tools only observe and keep the world frozen.
- `WorldClock.cs` freezes the world; `SplitScreen.cs` / `SplitScreenPatches.cs` join P2–P4 and keep
  frozen frames looking live; `ModConfig.cs` is `config.json`.

## Game-free code

`mod/StardewMCP` needs the game's DLLs to build. `mod/StardewMCP.Tests` does not reference it: it
compiles the mod's game-free files (no `Game1`, `GameLocation`, `Farmer`, no `StardewValley` or
`StardewModdingAPI` usings) through the `<Compile Include Link>` list in its `.csproj`. New logic that
can run without the game goes into such a file with a test; keep moves small and leave a forwarder.
Without the game installed, don't run `dotnet build` in `mod/StardewMCP` or add stubs to make it build;
say in the PR that the change needs a local build.
