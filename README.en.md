<p align="right">🌐 <a href="README.md">ภาษาไทย</a> · <b>English</b></p>

<div align="center">

# 🌾 Stardew MCP

**A mod that lets AI play Stardew Valley by itself.**

Let an AI play alone, play alongside you, or put three AIs on the same farm.
Works with Claude, GPT, Gemini or a model running on your own machine.

[![Latest release](https://img.shields.io/github/v/release/duckxy166/stardew-ai-agent?style=flat-square&label=release&color=4caf50)](https://github.com/duckxy166/stardew-ai-agent/releases/latest)
[![GitHub stars](https://img.shields.io/github/stars/duckxy166/stardew-ai-agent?style=flat-square&color=f5c518)](https://github.com/duckxy166/stardew-ai-agent/stargazers)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue?style=flat-square)](LICENSE)
![Stardew Valley 1.6+](https://img.shields.io/badge/Stardew_Valley-1.6+-7cb342?style=flat-square)
![SMAPI 4.0+](https://img.shields.io/badge/SMAPI-4.0+-5c6bc0?style=flat-square)
[![YouTube](https://img.shields.io/badge/YouTube-watch_EP.1-ff0000?style=flat-square&logo=youtube&logoColor=white)](https://youtu.be/Drd5lWh3kFI)

<a href="https://youtu.be/Drd5lWh3kFI">
  <img src="docs/assets/ep01-cover.png" width="720" alt="Watch on YouTube: Claude, GPT and Gemini share one farm in Stardew Valley">
</a>

▶️ **[Watch EP.1 on YouTube](https://youtu.be/Drd5lWh3kFI)** (43 min, in Thai)
Claude, GPT and Gemini farm together for five days and end up harvesting each other's crops.

</div>

## What this mod is

Stardew MCP lets an AI control a farmer in the game. The AI walks, farms, fishes, chats with villagers, makes deals
with the other farmers and goes to bed, all without you pressing a key.

How it works, in short:

1. The mod tells the AI what its farmer can see: where it is, what it carries, which crops still need water.
2. The AI decides what to do next and picks a tool, such as `water_all` (water every crop), `walk_to` (walk to a
   tile) or `say` (talk).
3. The mod carries it out in the game and sends the result back, and the AI decides again. This repeats until the
   day ends.

Words you will meet on this page:

- **SMAPI**: the mod loader for Stardew Valley. You need it before you can use this mod.
- **MCP** (Model Context Protocol): a standard that lets AI apps such as Claude Code or Cursor call outside tools.
  This mod runs an MCP server *inside the game*, so outside AIs can connect and play.
- **Seat**: one player screen, P1 to P4. Each seat can be played by an AI, by you, or switched off.
- **Brain**: the AI provider that drives a seat, such as `anthropic`, `openai` or `gemini`.

## What happened in the video

Three AIs moved onto one farm as strangers. Nobody gave them roles, teams or goals. Each one sees only its own
screen, hears only who is standing near it, and writes its own diary every night.

Over five in-game days:

- 👋 Gemini greeted the whole town on day 1, without anyone asking it to.
- 🪙 Claude spent its money down to 0g for three last seeds.
- 💭 GPT's private thoughts slipped into another language mid-sentence.
- 🚪 Two days in a row they queued outside a shop before it opened.
- 🥕 On day 5 every parsnip was ripe at once. *"So we don't pick the same ones"*, one said, and then they all picked
  each other's. The result: **Claude took 14 of the others' crops, GPT 11, Gemini 0.**
- 🌱 When they finally split up the field, one of them was left without a plot.

None of it was scripted. The mod logs every action, thought and line of chat, and the story in the video comes from
those logs.

## Highlights

- 🤖 **Plays most of the game.** 70 tools for farming, fishing, mining, combat, cooking, crafting, shopping,
  animals, buildings, festivals, quests, the Community Center, the museum, friendship and marriage.
  → [Everything it can do](docs/features.md)
- 👥 **Up to three AIs on one farm** in split-screen co-op, with or without you. Each has its own money, crops and
  chests, and the mod notices when someone takes what isn't theirs.
- 🗣️ **They only hear what is near them.** A `say` reaches only the farmers whose screen shows the speaker. A
  `think` is heard by no farmer; only viewers see it. Plans, deals and lies have to happen face to face, and a lie
  can be caught, because every AI sees what the others are *actually* doing.
- ⏸️ **Game time stops while an AI thinks**, so a slow model loses no game time. Turn it off when you play along.
- 🧠 **Memory across days.** Every evening each AI writes a diary, lessons, a plan and a note on who it is. Their
  characters grow out of what happens to them in the game, not out of a script.
- 🎥 **Ready for streams and videos.** An overlay page for OBS, a day-by-day replay page, logs, diaries and daily
  stats.
- 🔌 **Use any AI.** Give the mod's built-in AI your own API key (Anthropic, OpenAI, Gemini, OpenRouter, Ollama,
  LM Studio), connect an MCP app (Claude Code, Claude Desktop, Cursor...), or call it over plain HTTP.

## Choose how to use it

There are three ways. If you're not sure, start with the first: it needs no other programs.

| You want to... | Use | You need |
|---|---|---|
| Watch AIs play your farm, or play alongside them | The mod's **built-in AI**, set up in the game's menu or in `config.json` | An API key (Anthropic, OpenAI, Gemini) or a local model (Ollama, LM Studio) |
| Give orders to your own AI, e.g. "water the crops, then visit Abigail" | Any **MCP app** (Claude Code, Claude Desktop, Cursor...) connected to `http://localhost:8770/mcp` | That app |
| Build your own bot, stream overlay or automation | The **[HTTP API](docs/api.md)** (`/api/state`, `/api/control`, `/overlay`, `/replay`...) | Any programming language that speaks HTTP |

## Install

You need:

- Stardew Valley 1.6 or newer (PC)
- [SMAPI](https://smapi.io) 4.0 or newer
- (optional) [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098), to change the mod's
  settings in the game instead of editing a file

Steps:

1. Install SMAPI and start the game through it once.
2. Download the latest `StardewMCP` zip from the [Releases](https://github.com/duckxy166/stardew-ai-agent/releases) page.
3. Unzip it into `Stardew Valley/Mods/`, so you get `Mods/StardewMCP/` (not a folder nested twice).
4. Start the game through SMAPI. If the install worked, the SMAPI console shows
   `Agent MCP at http://localhost:8770/mcp`.

On the first start the mod creates its settings file, `Mods/StardewMCP/config.json`.
With Generic Mod Config Menu you can change the settings in the game: title screen → the cog → *Stardew MCP*.

## Quick start

### Let the AI play by itself

1. Open the mod's settings (in the game, or edit `config.json`).
2. On seat **P1**, set the AI provider (`Brain`) and your API key. Leave the model (`Model`) empty to use the
   default.

   | `Brain` | Models | Get a key / endpoint |
   |---|---|---|
   | `anthropic` | `claude-haiku-5-5` (default), `claude-sonnet-5-5`, ... | [console.anthropic.com](https://console.anthropic.com) |
   | `openai` | `gpt-5-mini` (default), `gpt-5`, ... | [platform.openai.com](https://platform.openai.com), or any other server with the OpenAI format through a `BaseUrl`, such as OpenRouter (`https://openrouter.ai/api/v1`), Ollama (`http://localhost:11434/v1`, no key needed), LM Studio |
   | `gemini` | `gemini-3.8-flash` (default), ... | [aistudio.google.com](https://aistudio.google.com) |
   | `none` | | No built-in AI: an MCP app drives this seat instead |

   In `config.json` it looks like this:

   ```json
   "Seats": [
     { "Role": "ai", "Brain": "anthropic", "ApiKey": "sk-ant-..." }
   ]
   ```

3. Load any save. The AI starts playing right away, and a small panel on its screen shows what it thinks, says and
   does.

### Several AIs together, or you with AIs

1. Turn on `SplitScreen`. The screen splits 2×2 like the game's co-op mode, up to four screens on one farm.
2. Give each seat a `Role`:
   - `ai`: an AI plays it
   - `human`: you play it (P1 only)
   - `camera`: a screen that only watches, nobody plays
   - `off`: no screen
3. Load the save. If the farm has too few cabins, the mod builds them and brings the other farmers into the game by
   itself, so you don't need gamepads.

Example: you play P1, two AIs play with you, and time keeps running.

```json
"SplitScreen": true,
"FreezeTimeWhileThinking": false,
"Seats": [
  { "Role": "human" },
  { "Role": "ai", "Brain": "anthropic", "ApiKey": "sk-ant-...", "Name": "Claude" },
  { "Role": "ai", "Brain": "gemini", "ApiKey": "AIza...", "Name": "Gemini" },
  { "Role": "off" }
]
```

`FreezeTimeWhileThinking` decides whether time stops while an AI thinks:

- `true` (default): the whole game world stops while an AI thinks, so a slow model loses no game time.
- `false`: time runs as usual. The better choice when you play along.

Every setting is listed in [Configuration](docs/configuration.md).

### Connect your own AI app (MCP)

1. Start the game and load a save.
2. Add the mod as an MCP server in your app. For [Claude Code](https://claude.com/claude-code):

   ```bash
   claude mcp add --transport http stardew http://localhost:8770/mcp
   ```

3. Ask something like *"Water my crops, then go talk to Abigail."*

Any other app that speaks MCP over streamable HTTP can use the same URL.
In split-screen each farmer has its own URL: `/mcp/p1` (same as `/mcp`), `/mcp/p2`, `/mcp/p3`.
Set the seats you drive from an app to `"Brain": "none"`.

### Pause

Press `F8` in the game window. The mod lets the AI finish what it is doing, then stops the whole game. Press `F8`
again to continue. To stop at the end of the day, or for other run controls, see [HTTP API](docs/api.md#run-control).

## Good to know

- **Cost.** Every time the AI does one thing, the mod calls the model once and sends the game state with it.
  One in-game day costs roughly 50,000–300,000 input tokens per AI, so cheap models (Haiku, Flash, GPT mini, a local
  model) are the sensible choice. With `DailyTokenBudget` set, a seat that uses up its tokens stops until the next
  morning.
- **Your API keys** are stored as plain text in `config.json`. Don't share that file.
- **Language.** The AIs think and talk in the game's language. To force one, set `ThoughtLanguage`, e.g.
  `"English"` or `"Thai"`.
- **Local models** work through `openai` + `BaseUrl`, but small models often get confused with 70 tools to choose
  from.
- **How the AI plays** is set in `Mods/StardewMCP/assets/system_prompt.md`. Edit that file to change its behaviour.
- **Hardware.** Three or four screens at once are heavy: a 2560×1440 window at 60 FPS needs a mid-range GPU or
  better.

## Documentation

| Page | What's inside |
|---|---|
| 📋 [What it can do](docs/features.md) | Every feature, area by area, and what is still missing |
| 🧰 [Tools](docs/tools.md) | All MCP tools with their arguments, grouped by job |
| 🌐 [HTTP API](docs/api.md) | Endpoints, run control, tool results and reason codes |
| ⚙️ [Configuration](docs/configuration.md) | Every `config.json` setting, seats and roles, overlay language |
| 🗺️ [Game coverage](docs/coverage.md) | Which game systems work, are tested, or are not there yet |
| 📖 [Glossary](CONTEXT.md) | The words this project uses: seat, brain, freeze, interrupt... |
| 🤝 [Contributing](CONTRIBUTING.md) | Bug reports, tests, adding a tool |

## Troubleshooting

| Problem | Fix |
|---|---|
| The SMAPI console doesn't show `Agent MCP at ...` | Check that the folder is `Mods/StardewMCP/` and not nested twice, then read the SMAPI log for errors |
| The AI never starts | Read the SMAPI console after loading the save. A seat that can't start logs `pN: built-in AI not started: <problem>`, such as a missing key or a misspelled provider. Type `mcp_brain` in the console to see which AIs are running |
| Port 8770 is used by another program | Change `HttpPort` and use the same port in your MCP app's URL |
| The game looks stuck and the clock doesn't move | Usually the game is waiting for an AI to think. Open `/api/state` to see who is thinking. An AI that stays silent longer than `ThinkTimeoutSeconds` no longer holds the game |
| The AI keeps repeating the same failing action | The mod's loop guard first adds hints, then refuses the repeat. Tune it with `RepeatHintAfter`, `RepeatBlockAfter`, `NoProgressAfter` |
| In split-screen, P2–P4 don't join the game | Allow the Windows Firewall prompt, and keep `AutoBuildCabins` on (or build enough cabins yourself) |

If you find a bug, open an issue with a link to your SMAPI log; it helps a lot. How: [CONTRIBUTING.md](CONTRIBUTING.md#reporting-a-bug).

## ⭐ Enjoying it?

If watching three AIs argue over parsnips made you smile, **give the repo a star**.
It helps other players and AI tinkerers find the project, and it tells us the next episode is worth making.

<a href="https://star-history.com/#duckxy166/stardew-ai-agent&Date">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/svg?repos=duckxy166/stardew-ai-agent&type=Date&theme=dark">
    <img alt="Star history" src="https://api.star-history.com/svg?repos=duckxy166/stardew-ai-agent&type=Date" width="600">
  </picture>
</a>

More ways to help:

- 📺 Follow the series on YouTube: [@LDuckxy](https://www.youtube.com/@LDuckxy)
- 💖 Support development through [GitHub Sponsors](https://github.com/sponsors/duckxy166)
- 🐛 Report bugs and send pull requests: [CONTRIBUTING.md](CONTRIBUTING.md)

## Build from source

```bash
cd mod/StardewMCP
dotnet build -c Release
```

[ModBuildConfig](https://www.nuget.org/packages/Pathoschild.Stardew.ModBuildConfig) finds your game folder, copies
the mod into `Mods/` and creates a release zip in `bin/`.
How to run the tests that don't need the game: [CONTRIBUTING.md](CONTRIBUTING.md#tests).

This repository has no scripts for letting AIs play many days unattended (memory between days, save backups, a crash
watchdog, OBS recording). Everything such scripts need is in the [HTTP API](docs/api.md), and
[docs/recording.md](docs/recording.md) describes one recording setup as an example.

## Credits

Originally forked from [Hunter-Thompson/stardew-mcp](https://github.com/Hunter-Thompson/stardew-mcp),
which first bridged Stardew Valley to MCP through a Go server and a WebSocket mod. This project has
since been rewritten as a standalone mod with its own in-game MCP server, and the original Go server
and WebSocket code have been removed.

## License

[MIT](LICENSE): free to download, use, change and build on, as long as you keep the license text.
Stardew Valley is a trademark of ConcernedApe; this project is a fan-made mod and is not affiliated with
ConcernedApe. No game files are included.
