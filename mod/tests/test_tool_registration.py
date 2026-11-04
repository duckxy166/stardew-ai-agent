"""Every group of tools is registered: `AgentTools.All()` is a hand-kept list of the tool groups (`CraftingToolDefs()`,
`SocialTools.All()`...), and a group missing from it would leave its tools silently unreachable on the MCP endpoint
(and absent from /api/contract). The real list needs the game to build, so this reads the source.
"""

from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
AGENT_DIR = ROOT / "mod" / "StardewMCP" / "Agent"

# `private static List<ToolDef> CraftingToolDefs() => ...`, `public static List<ToolDef> All() => ...`, also IEnumerable / arrays
GROUP_RE = re.compile(
    r"static\s+(?:List<ToolDef>|IEnumerable<ToolDef>|ToolDef\[\])\s+(\w+)\s*\(\s*\)")
CLASS_RE = re.compile(r"\bclass\s+(\w+)")


def groups() -> list[tuple[str, str, str]]:
    """(file, class, method) of every tool group in the top-level Agent files, AgentTools.All itself left out."""
    out = []
    for f in sorted(AGENT_DIR.glob("*.cs")):
        src = f.read_text(encoding="utf-8")
        for m in GROUP_RE.finditer(src):
            classes = CLASS_RE.findall(src[: m.start()])
            cls = classes[-1] if classes else ""
            if (cls, m.group(1)) != ("AgentTools", "All"):
                out.append((f.name, cls, m.group(1)))
    return out


def test_finds_the_groups():
    names = {m for _, _, m in groups()}
    assert {"Core", "CraftingToolDefs", "HorseToolDefs"} <= names


def test_every_group_is_called_from_another_group():
    """Called by name somewhere other than its own definition: from AgentTools.All() or a group it reaches (QolToolDefs)."""
    sources = {f.name: f.read_text(encoding="utf-8") for f in AGENT_DIR.glob("*.cs")}
    everything = "\n".join(sources.values())
    missing = []
    for file, cls, method in groups():
        call = rf"\b{cls}\.{method}\s*\(\s*\)" if method == "All" else rf"(?<![\w.]){method}\s*\(\s*\)|\b{cls}\.{method}\s*\(\s*\)"
        uses = [m for m in re.finditer(call, everything)
                if not re.search(r"static\s+\S+\s+$", everything[max(0, m.start() - 60): m.start()])]
        if not uses:
            missing.append(f"{file}: {cls}.{method}()")
    assert not missing, "tool groups never registered in AgentTools.All(): " + ", ".join(missing)
