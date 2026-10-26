<p align="right">🌐 <b>ภาษาไทย</b> · <a href="README.en.md">English</a></p>

<div align="center">

# 🌾 Stardew MCP

**Mod ที่ให้ AI เล่น Stardew Valley ได้เอง**

ปล่อยให้ AI เล่นคนเดียว เล่นไปพร้อมกับเรา หรือให้ AI 3 ตัวทำฟาร์มเดียวกันก็ได้
ใช้ได้กับ Claude, GPT, Gemini หรือโมเดลที่รันในเครื่องตัวเอง

[![Latest release](https://img.shields.io/github/v/release/duckxy166/stardew-ai-agent?style=flat-square&label=release&color=4caf50)](https://github.com/duckxy166/stardew-ai-agent/releases/latest)
[![GitHub stars](https://img.shields.io/github/stars/duckxy166/stardew-ai-agent?style=flat-square&color=f5c518)](https://github.com/duckxy166/stardew-ai-agent/stargazers)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue?style=flat-square)](LICENSE)
![Stardew Valley 1.6+](https://img.shields.io/badge/Stardew_Valley-1.6+-7cb342?style=flat-square)
![SMAPI 4.0+](https://img.shields.io/badge/SMAPI-4.0+-5c6bc0?style=flat-square)
[![YouTube](https://img.shields.io/badge/YouTube-watch_EP.1-ff0000?style=flat-square&logo=youtube&logoColor=white)](https://youtu.be/Drd5lWh3kFI)

<a href="https://youtu.be/Drd5lWh3kFI">
  <img src="docs/assets/ep01-cover.png" width="720" alt="ดูบน YouTube: ปล่อย Claude, GPT และ Gemini ทำฟาร์มด้วยกันใน Stardew Valley">
</a>

▶️ **[ดู EP.1 บน YouTube](https://youtu.be/Drd5lWh3kFI)** (43 นาที)
ปล่อย Claude, GPT และ Gemini ทำฟาร์มด้วยกัน 5 วัน แล้วสุดท้ายก็แย่งผักกันเอง

</div>

## Mod นี้คืออะไร

Stardew MCP เป็น mod ที่เปิดช่องให้ AI ควบคุมตัวละครในเกมได้ AI จะเดิน ทำฟาร์ม ตกปลา คุยกับชาวเมือง
ต่อรองกับเพื่อนร่วมฟาร์ม และเข้านอนเองทั้งหมด โดยเราไม่ต้องกดอะไรเลย

หลักการทำงานแบบสั้น ๆ:

1. mod ส่งสิ่งที่ตัวละครเห็นอยู่ให้ AI เช่น อยู่ตรงไหน มีของอะไร ผักต้นไหนยังไม่ได้รดน้ำ
2. AI คิดว่าจะทำอะไรต่อ แล้วเลือกเครื่องมือ เช่น `water_all` (รดน้ำทั้งแปลง), `walk_to` (เดินไปจุดที่กำหนด), `say` (พูด)
3. mod ทำตามในเกม แล้วส่งผลลัพธ์กลับไปให้ AI คิดต่อ วนแบบนี้ไปจนจบวัน

คำที่จะเจอบ่อยในหน้านี้:

- **SMAPI** โปรแกรมโหลด mod ของ Stardew Valley ต้องลงก่อนถึงจะใช้ mod นี้ได้
- **MCP** (Model Context Protocol) มาตรฐานที่ให้แอป AI อย่าง Claude Code หรือ Cursor เรียกใช้เครื่องมือภายนอกได้
  mod นี้เปิด MCP server *อยู่ในเกม* AI ภายนอกจึงต่อเข้ามาเล่นได้
- **ที่นั่ง (seat)** จอผู้เล่น P1 ถึง P4 แต่ละที่นั่งจะให้ AI เล่น ให้เราเล่นเอง หรือปิดไว้ก็ได้
- **Brain** ผู้ให้บริการ AI ที่คุมที่นั่งนั้น เช่น `anthropic`, `openai`, `gemini`

## ในคลิปเกิดอะไรขึ้น

AI 3 ตัวย้ายมาอยู่ฟาร์มเดียวกันโดยไม่รู้จักกันมาก่อน ไม่มีใครกำหนดบทบาท ทีม หรือเป้าหมายให้
แต่ละตัวเห็นแค่จอของตัวเอง ได้ยินแค่คนที่ยืนอยู่ใกล้ ๆ และเขียนไดอารี่ของตัวเองทุกคืน

สิ่งที่เกิดขึ้นใน 5 วันในเกม:

- 👋 พี่ฟ้า (Gemini) เดินทักคนทั้งเมืองตั้งแต่วันแรก ทั้งที่ไม่มีใครสั่ง
- 🪙 พี่ส้ม (Claude) ใช้เงินจนเหลือ 0 เพื่อซื้อเมล็ด 3 เม็ดสุดท้าย
- 💭 ความคิดของพี่เขียว (GPT) มีภาษาอื่นโผล่มากลางประโยค
- 🚪 ไปยืนรอหน้าร้านตั้งแต่เช้าทั้งที่ร้านยังไม่เปิด เป็นแบบนี้สองวันติด
- 🥕 เช้าวันที่ 5 ผักสุกพร้อมกันทั้งแปลง มีตัวหนึ่งพูดว่า *"จะได้ไม่เก็บทับกัน"* แล้วทุกตัวก็ก้มเก็บผักของเพื่อน
  ผลคือ **Claude เก็บผักเพื่อนไป 14 ต้น GPT 11 ต้น Gemini 0 ต้น**
- 🌱 พอแบ่งแปลงกันเสร็จ กลับเหลืออยู่ตัวหนึ่งที่ไม่ได้แปลงเลย

ไม่มีฉากไหนเขียนบทไว้ก่อน mod บันทึกทุกการกระทำ ทุกความคิด และทุกประโยคที่คุยกัน เรื่องทั้งหมดในคลิปมาจาก log

## จุดเด่น

- 🤖 **เล่นได้เกือบทั้งเกม** มีเครื่องมือ 70 ตัว ครอบคลุมการทำฟาร์ม ตกปลา ลงเหมือง ต่อสู้ ทำอาหาร คราฟต์ ซื้อขาย
  เลี้ยงสัตว์ สร้างอาคาร เทศกาล เควสต์ Community Center พิพิธภัณฑ์ ความสัมพันธ์ และการแต่งงาน
  → [ดูทุกอย่างที่ทำได้](docs/features.th.md)
- 👥 **AI สูงสุด 3 ตัวในฟาร์มเดียว** เล่นแบบจอแบ่ง (split-screen co-op) จะร่วมเล่นด้วยหรือไม่ก็ได้
  แต่ละตัวมีเงิน แปลงผัก และหีบของตัวเอง ถ้าใครไปหยิบของคนอื่น mod จะรู้
- 🗣️ **ได้ยินแค่คนที่อยู่ใกล้** คำพูด (`say`) ไปถึงเฉพาะคนที่มองเห็นผู้พูดบนจอ ส่วนความคิด (`think`) ไม่มีตัวละครไหนได้ยิน
  มีแค่คนดูที่เห็น การวางแผน การต่อรอง และการโกหกจึงต้องเกิดตอนเจอหน้ากัน
  และโกหกก็โดนจับได้ เพราะ AI ทุกตัวเห็นว่าคนอื่น *ทำอะไรอยู่จริง ๆ*
- ⏸️ **เวลาในเกมหยุดระหว่างที่ AI คิด** โมเดลตอบช้าก็ไม่เสียเวลาในเกม ถ้าเราเล่นด้วยก็ปิดได้
- 🧠 **จำเรื่องข้ามวันได้** ทุกเย็น AI แต่ละตัวจะเขียนไดอารี่ บทเรียน แผน และบันทึกว่าตัวเองเป็นใคร
  นิสัยของแต่ละตัวจึงค่อย ๆ เกิดจากสิ่งที่เจอในเกม ไม่ได้มาจากบทที่ใครเขียนไว้
- 🎥 **พร้อมสำหรับสตรีมและทำคลิป** มีหน้า overlay ใส่ใน OBS ได้ หน้า replay ดูย้อนหลังทีละวัน
  พร้อม log ไดอารี่ และสถิติรายวัน
- 🔌 **ใช้ AI เจ้าไหนก็ได้** ใส่ API key ของเราเองให้ AI ในตัว mod (Anthropic, OpenAI, Gemini, OpenRouter, Ollama, LM Studio)
  ต่อผ่านแอป MCP (Claude Code, Claude Desktop, Cursor...) หรือเรียกผ่าน HTTP ตรง ๆ

## เลือกวิธีใช้

มี 3 แบบ ถ้าไม่แน่ใจ ให้เริ่มจากแบบแรก เพราะไม่ต้องลงโปรแกรมอื่นเพิ่ม

| อยากทำอะไร | ใช้แบบไหน | ต้องมีอะไร |
|---|---|---|
| ดู AI เล่นฟาร์มของเรา หรือเล่นไปด้วยกัน | **AI ในตัว mod** ตั้งค่าในเมนูเกมหรือใน `config.json` | API key (Anthropic, OpenAI, Gemini) หรือโมเดลในเครื่อง (Ollama, LM Studio) |
| สั่งงาน AI ของเราเอง เช่น "รดน้ำผักแล้วไปหา Abigail" | **แอป MCP** ตัวไหนก็ได้ (Claude Code, Claude Desktop, Cursor...) ต่อที่ `http://localhost:8770/mcp` | แอปตัวนั้น |
| เขียนบอท overlay สำหรับสตรีม หรือระบบอัตโนมัติของตัวเอง | **[HTTP API](docs/api.md)** (`/api/state`, `/api/control`, `/overlay`, `/replay`...) | ภาษาโปรแกรมอะไรก็ได้ที่เรียก HTTP ได้ |

## ติดตั้ง

สิ่งที่ต้องมี:

- Stardew Valley เวอร์ชัน 1.6 ขึ้นไป (PC)
- [SMAPI](https://smapi.io) เวอร์ชัน 4.0 ขึ้นไป
- (ไม่บังคับ) [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) ถ้าลงไว้ จะตั้งค่า mod ในเกมได้ ไม่ต้องแก้ไฟล์เอง

ขั้นตอน:

1. ลง SMAPI แล้วเปิดเกมผ่าน SMAPI หนึ่งครั้ง
2. ดาวน์โหลดไฟล์ zip `StardewMCP` ตัวล่าสุดจากหน้า [Releases](https://github.com/duckxy166/stardew-ai-agent/releases)
3. แตกไฟล์ไว้ใน `Stardew Valley/Mods/` ให้ได้โฟลเดอร์ `Mods/StardewMCP/` (ระวังอย่าให้ซ้อนกันสองชั้น)
4. เปิดเกมผ่าน SMAPI ถ้าติดตั้งถูก หน้าต่างคอนโซลของ SMAPI จะขึ้นข้อความ `Agent MCP at http://localhost:8770/mcp`

ตอนเปิดเกมครั้งแรก mod จะสร้างไฟล์ตั้งค่า `Mods/StardewMCP/config.json` ให้เอง
ถ้าลง Generic Mod Config Menu ไว้ ตั้งค่าในเกมได้ที่: หน้าไตเติล → กดรูปฟันเฟือง → *Stardew MCP*

## เริ่มใช้งาน

### ให้ AI เล่นเอง

1. เปิดหน้าตั้งค่าของ mod (ในเกม หรือเปิดแก้ `config.json`)
2. ในที่นั่ง **P1** ให้ใส่ผู้ให้บริการ AI (`Brain`) และ API key ส่วนโมเดล (`Model`) ถ้าเว้นว่างจะใช้ค่าเริ่มต้น

   | `Brain` | โมเดล | สมัคร key / ที่อยู่ server |
   |---|---|---|
   | `anthropic` | `claude-haiku-5-5` (ค่าเริ่มต้น), `claude-sonnet-5-5`, ... | [console.anthropic.com](https://console.anthropic.com) |
   | `openai` | `gpt-5-mini` (ค่าเริ่มต้น), `gpt-5`, ... | [platform.openai.com](https://platform.openai.com) หรือ server อื่นที่ใช้รูปแบบเดียวกับ OpenAI โดยใส่ `BaseUrl` เช่น OpenRouter (`https://openrouter.ai/api/v1`), Ollama (`http://localhost:11434/v1` ไม่ต้องใช้ key), LM Studio |
   | `gemini` | `gemini-3.8-flash` (ค่าเริ่มต้น), ... | [aistudio.google.com](https://aistudio.google.com) |
   | `none` | | ไม่ใช้ AI ในตัว mod ให้แอป MCP มาคุมที่นั่งนี้แทน |

   ถ้าแก้ใน `config.json` จะหน้าตาประมาณนี้:

   ```json
   "Seats": [
     { "Role": "ai", "Brain": "anthropic", "ApiKey": "sk-ant-..." }
   ]
   ```

3. โหลดเซฟไหนก็ได้ AI จะเริ่มเล่นทันที บนจอจะมีแผงเล็ก ๆ บอกว่า AI กำลังคิดอะไร พูดอะไร และทำอะไรอยู่

### ให้ AI หลายตัวเล่นด้วยกัน หรือเราเล่นกับ AI

1. เปิด `SplitScreen` จอจะแบ่งเป็น 2×2 แบบโหมด co-op ของเกม ได้สูงสุด 4 จอในฟาร์มเดียว
2. กำหนด `Role` ให้แต่ละที่นั่ง:
   - `ai` ให้ AI เล่น
   - `human` เราเล่นเอง (ใช้ได้เฉพาะ P1)
   - `camera` จอไว้ดูอย่างเดียว ไม่มีใครเล่น
   - `off` ปิดจอนี้
3. โหลดเซฟ ถ้าฟาร์มมีกระท่อมไม่พอ mod จะสร้างให้และพาตัวละครที่เหลือเข้าเกมเอง ไม่ต้องมีจอย

ตัวอย่าง: เราเล่น P1 มี AI สองตัวเล่นด้วย และให้เวลาเดินตลอด

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

`FreezeTimeWhileThinking` คือการหยุดเวลาตอน AI คิด:

- `true` (ค่าเริ่มต้น) ทั้งโลกในเกมหยุดระหว่างที่ AI คิด โมเดลช้าก็ไม่เสียเวลาในเกม
- `false` เวลาเดินต่อตามปกติ เหมาะกับตอนที่เราเล่นด้วย

ค่าตั้งทั้งหมดดูได้ที่ [Configuration](docs/configuration.md) (ภาษาอังกฤษ)

### ต่อกับแอป AI ของเราเอง (MCP)

1. เปิดเกมและโหลดเซฟให้เรียบร้อย
2. เพิ่ม mod เป็น MCP server ในแอปที่ใช้ ตัวอย่างสำหรับ [Claude Code](https://claude.com/claude-code):

   ```bash
   claude mcp add --transport http stardew http://localhost:8770/mcp
   ```

3. ลองสั่ง เช่น *"รดน้ำผักให้หน่อย แล้วไปคุยกับ Abigail"*

แอปอื่นที่รองรับ MCP แบบ streamable HTTP ก็ใช้ URL เดียวกันได้
ถ้าเปิดจอแบ่ง ตัวละครแต่ละตัวจะมี URL ของตัวเอง: `/mcp/p1` (เหมือน `/mcp`), `/mcp/p2`, `/mcp/p3`
และต้องตั้งที่นั่งที่จะคุมผ่านแอปเป็น `"Brain": "none"`

### หยุดชั่วคราว

กด `F8` ในหน้าต่างเกม mod จะรอให้ AI ทำสิ่งที่ทำอยู่ให้เสร็จก่อน แล้วค่อยหยุดทั้งเกม กด `F8` อีกครั้งเพื่อเล่นต่อ
ถ้าอยากสั่งให้หยุดตอนจบวัน หรือควบคุมแบบอื่น ดูที่ [HTTP API](docs/api.md#run-control)

## ควรรู้ก่อนใช้

- **ค่าใช้จ่าย** ทุกครั้งที่ AI ทำอะไรหนึ่งอย่าง จะเรียกโมเดลหนึ่งครั้งพร้อมส่งสถานะเกมไปด้วย
  หนึ่งวันในเกมใช้ประมาณ 50,000–300,000 input tokens ต่อ AI หนึ่งตัว
  จึงแนะนำโมเดลราคาถูก (Haiku, Flash, GPT mini หรือโมเดลในเครื่อง)
  ตั้ง `DailyTokenBudget` ได้ ถ้าใช้ token ครบ ที่นั่งนั้นจะหยุดจนถึงเช้าวันถัดไป
- **API key** ถูกเก็บเป็นข้อความธรรมดาใน `config.json` อย่าส่งไฟล์นี้ให้ใคร
- **ภาษา** AI จะคิดและพูดตามภาษาที่ตั้งไว้ในเกม ถ้าอยากบังคับภาษา ให้ตั้ง `ThoughtLanguage` เช่น `"Thai"` หรือ `"English"`
- **โมเดลในเครื่อง** ใช้ได้ผ่าน `openai` + `BaseUrl` แต่โมเดลเล็กมักสับสนเพราะมีเครื่องมือให้เลือกถึง 70 ตัว
- **นิสัยการเล่น** กำหนดไว้ในไฟล์ `Mods/StardewMCP/assets/system_prompt.md` แก้ไฟล์นี้เพื่อเปลี่ยนวิธีเล่นของ AI ได้
- **สเปกเครื่อง** เปิด 3–4 จอพร้อมกันค่อนข้างกินเครื่อง หน้าต่าง 2560×1440 ที่ 60 FPS ต้องใช้การ์ดจอระดับกลางขึ้นไป

## เอกสาร

| หน้า | มีอะไร |
|---|---|
| 📋 [ทำอะไรได้บ้าง](docs/features.th.md) | ความสามารถทั้งหมดแยกตามหมวด และสิ่งที่ยังทำไม่ได้ |
| 🧰 [เครื่องมือ](docs/tools.th.md) | เครื่องมือ MCP ทุกตัวพร้อม argument แยกตามงาน |
| 🌐 [HTTP API](docs/api.md) (อังกฤษ) | endpoint, การควบคุมการรัน, ผลลัพธ์ของเครื่องมือและ reason code |
| ⚙️ [Configuration](docs/configuration.md) (อังกฤษ) | ค่าตั้งทุกตัวใน `config.json`, ที่นั่งและ role, ภาษาของ overlay |
| 🗺️ [Game coverage](docs/coverage.md) (อังกฤษ) | ระบบไหนของเกมใช้ได้แล้ว ทดสอบแล้ว หรือยังไม่มี |
| 📖 [Glossary](CONTEXT.md) (อังกฤษ) | คำศัพท์ของโปรเจกต์: seat, brain, freeze, interrupt... |
| 🤝 [Contributing](CONTRIBUTING.md) (อังกฤษ) | แจ้งบั๊ก รันเทสต์ เพิ่มเครื่องมือ |

## แก้ปัญหา

| อาการ | วิธีแก้ |
|---|---|
| คอนโซล SMAPI ไม่ขึ้น `Agent MCP at ...` | เช็กว่าโฟลเดอร์คือ `Mods/StardewMCP/` ไม่ได้ซ้อนสองชั้น แล้วดู error ใน SMAPI log |
| AI ไม่เริ่มเล่น | ดูคอนโซล SMAPI หลังโหลดเซฟ ที่นั่งที่เริ่มไม่ได้จะขึ้น `pN: built-in AI not started: <สาเหตุ>` เช่น ไม่ได้ใส่ key หรือพิมพ์ชื่อผู้ให้บริการผิด พิมพ์คำสั่ง `mcp_brain` ในคอนโซลเพื่อดูว่า AI ตัวไหนรันอยู่ |
| พอร์ต 8770 มีโปรแกรมอื่นใช้อยู่ | เปลี่ยน `HttpPort` แล้วแก้พอร์ตใน URL ของแอป MCP ให้ตรงกัน |
| เกมดูเหมือนค้าง นาฬิกาไม่เดิน | ส่วนใหญ่คือเกมหยุดรอ AI คิดอยู่ เปิด `/api/state` เพื่อดูว่าใครกำลังคิด ถ้า AI เงียบนานเกิน `ThinkTimeoutSeconds` เกมจะเดินต่อโดยไม่รอ |
| AI ทำสิ่งเดิมที่ล้มเหลวซ้ำไปเรื่อย ๆ | mod มีตัวกันวนลูป จะใส่คำใบ้ให้ก่อน และถ้ายังซ้ำจะไม่ยอมให้ทำ ปรับได้ที่ `RepeatHintAfter`, `RepeatBlockAfter`, `NoProgressAfter` |
| ตอนจอแบ่ง ตัวละคร P2–P4 ไม่เข้าเกม | กดอนุญาตตอน Windows Firewall ถาม และเปิด `AutoBuildCabins` ไว้ (หรือสร้างกระท่อมให้พอเอง) |

ถ้าเจอบั๊ก เปิด issue พร้อมแนบลิงก์ SMAPI log จะช่วยได้มาก วิธีดูได้ใน [CONTRIBUTING.md](CONTRIBUTING.md#reporting-a-bug)

## ⭐ ชอบไหม?

ถ้าดู AI สามตัวแย่งหัวผักกาดกันแล้วยิ้มได้ **ฝากกด Star ให้ repo นี้หน่อย**
ช่วยให้คนเล่นเกมและคนสาย AI เจอโปรเจกต์นี้ และเป็นกำลังใจให้ทำตอนต่อไป

<a href="https://star-history.com/#duckxy166/stardew-ai-agent&Date">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/svg?repos=duckxy166/stardew-ai-agent&type=Date&theme=dark">
    <img alt="Star history" src="https://api.star-history.com/svg?repos=duckxy166/stardew-ai-agent&type=Date" width="600">
  </picture>
</a>

ช่วยได้อีกหลายทาง:

- 📺 ติดตามซีรีส์บน YouTube: [@LDuckxy](https://www.youtube.com/@LDuckxy)
- 💖 สนับสนุนผู้พัฒนาผ่าน [GitHub Sponsors](https://github.com/sponsors/duckxy166)
- 🐛 แจ้งบั๊กหรือส่ง pull request: [CONTRIBUTING.md](CONTRIBUTING.md)

## Build เอง

```bash
cd mod/StardewMCP
dotnet build -c Release
```

[ModBuildConfig](https://www.nuget.org/packages/Pathoschild.Stardew.ModBuildConfig) จะหาโฟลเดอร์เกมให้เอง
คัดลอก mod ไปไว้ใน `Mods/` และสร้างไฟล์ zip ไว้ใน `bin/`
วิธีรันเทสต์ที่ไม่ต้องมีเกม อยู่ใน [CONTRIBUTING.md](CONTRIBUTING.md#tests)

repo นี้ไม่มีสคริปต์สำหรับให้ AI เล่นหลายวันต่อเนื่องแบบไม่ต้องเฝ้า (ความจำข้ามวัน สำรองเซฟ เฝ้าเกมแครช อัด OBS)
แต่ทุกอย่างที่สคริปต์แบบนั้นต้องใช้มีอยู่ใน [HTTP API](docs/api.md)
และ [docs/recording.md](docs/recording.md) มีตัวอย่างการตั้งค่าอัดคลิปหนึ่งแบบ

## เครดิต

โปรเจกต์นี้เริ่มจากการ fork [Hunter-Thompson/stardew-mcp](https://github.com/Hunter-Thompson/stardew-mcp)
ซึ่งเป็นคนแรกที่เชื่อม Stardew Valley กับ MCP ผ่าน Go server และ mod แบบ WebSocket
ตอนนี้โค้ดถูกเขียนใหม่ทั้งหมดเป็น mod ที่มี MCP server ในตัว และเอาโค้ด Go server กับ WebSocket เดิมออกแล้ว

## สัญญาอนุญาต

[MIT](LICENSE) ดาวน์โหลด ใช้ แก้ไข และนำไปพัฒนาต่อได้ฟรี แค่คงข้อความ license ไว้
Stardew Valley เป็นเครื่องหมายการค้าของ ConcernedApe โปรเจกต์นี้เป็น mod ที่แฟนทำขึ้นเอง ไม่เกี่ยวข้องกับ ConcernedApe
และไม่มีไฟล์ของเกมรวมอยู่
