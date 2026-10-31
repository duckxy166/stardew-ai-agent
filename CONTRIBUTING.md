# Contributing

Thanks for helping out! Bug reports, ideas and pull requests are all welcome.

## Reporting a bug

Open an issue with:

- what you asked the AI to do and what happened instead,
- your SMAPI log (upload it at <https://smapi.io/log> and paste the link),
- game, SMAPI and mod versions.

## Pull requests

1. Fork the repo and create a branch from `main`.
2. Build with `dotnet build` in `mod/StardewMCP` and try the change in game.
3. Run the [tests](#tests) that don't need the game.
4. Keep changes focused; match the style of the surrounding code.
5. Describe what you changed and how you tested it.

## Build

```bash
cd mod/StardewMCP
dotnet build -c Release                                       # builds, deploys to Mods/, makes the zip
dotnet build -p:EnableModDeploy=false -p:EnableModZip=false   # compile check only
```

## Tests

Two test suites run without Stardew Valley:

```bash
dotnet test mod/StardewMCP.Tests                                  # C#: xUnit, .NET 6 SDK
pip install pytest && python -m pytest -q mod/tests                 # Python: tool schema lint, overlay i18n, fishing and crop math
```

The mod project itself needs the game's DLLs (ModBuildConfig finds them in your game folder), so it
can't be built on CI or on a machine without the game. `mod/StardewMCP.Tests` therefore does **not**
reference it. It compiles the mod's game-free source files directly (see the `<Compile Include=...>`
list in its `.csproj`) and tests them: route search on text maps, tool argument helpers, the rules for
tool schemas, MCP request handling with a fake seat.

When you add logic that could run without `Game1`, `GameLocation` or `Farmer`, put it in a file of its
own that uses no game types, link that file from the test project and add a test. Keep the move small:
leave a one-line forwarder where the old code was. Logic that does need the game is checked in game.

## Don't break

- The HTTP API (`/api/*`, `/api/events`) and tool names: add to them, don't rename or remove. Other
  programs drive the mod through them.
- Another repository reads this source by path: keep `Reasons` (with `Reasons.All`) in
  `mod/StardewMCP/Agent/ToolResult.cs` above `CallContext`, and keep tool definitions in the top-level
  files of `mod/StardewMCP/Agent/`.
- No game files, saves, logs or personal paths; no code copied from other mods unless their license
  allows it (credit them if so).
- New game features follow [docs/coverage.md](docs/coverage.md): reuse the game's own entry points and
  update the feature's row in the same PR.

## Keep the docs in step

A PR that changes behaviour updates its page in the same PR:

| Change | Update |
|---|---|
| A tool added or changed | `docs/tools.md` and `docs/tools.th.md` |
| An endpoint, a result field or a log file | `docs/api.md` |
| A `config.json` setting | `docs/configuration.md` |
| Something a player would notice | `docs/features.md` and `docs/features.th.md`, plus its row in `docs/coverage.md` |
| A new term | `CONTEXT.md` |

The READMEs (`README.md` in Thai, `README.en.md` in English) stay a short landing page without reference
tables. English and Thai pages are twins with the same sections; write the Thai as natural Thai, not word
for word.

## Adding an agent tool

Tools live in `mod/StardewMCP/Agent/AgentTools.cs`. Each tool is a `ToolDef` with a name,
description, JSON input schema and a coroutine that runs on the game thread. Mark a tool
`Instant` if it only reads state, so game time stays frozen while it runs. Every other tool takes a
`reason` string so viewers can read why the AI does it. Names are snake_case, and `required` may only
list declared properties; `ToolContract` (logged at startup) and `mod/tests/test_tool_sources.py`
check this.

Add the tool to [docs/tools.md](docs/tools.md) and its Thai twin [docs/tools.th.md](docs/tools.th.md)
(`mod/tests/test_docs.py` fails until both list it), and to [docs/features.md](docs/features.md) /
[docs/features.th.md](docs/features.th.md) when players would notice it. Use the words in [CONTEXT.md](CONTEXT.md).

By contributing you agree that your contribution is licensed under the [MIT License](LICENSE).
