# Demo one-pager — click sequence for recording

Terse cue-card for the [full walkthrough](demo-walkthrough.md). No prose — just the moves, in order.
Badges: Admin **1001** manager · **1002** coordinator · **1003** inspector — Terminal **7700** operator.

| # | App · login | Do | Land / see |
|---|---|---|---|
| 1 | Admin **1001** | sign in → sidebar **Products** → **Define product** → SKU `DEMO-1`, name `Demo bar 50 g`, cat `Dry goods` → **Create product** | new row in catalogue |
| 2 | Admin **1002** | sign out/in → **Inbound** → select `ASN-2208` → **Assign dock slot** `D-2` / `11:00–12:00` → **Assign** → select `ASN-2206` → **Mark arrived** | dock set; ASN Arrived |
| 3 | Terminal **7700** | sign in → tap **Receive** pile → (qty `240`) → **Confirm line** | back to hub, pile −1 |
| 4 | Admin **1003** | sign out/in → **Quality holds** → pick a batch → **Release** → reason `Temperature within range` → **Confirm release** | batch leaves worklist |
| 5 | Terminal **7700** | hub → tap **Put away** → checks ✓✓ → scan location → confirm | back to hub, pile −1 |
| 6 | Admin **1001** | sign out/in → **Stock view** → click milk row (drill) → sidebar **Movements** | ledger: receipt + put-away |
| 7 | Admin **1002** | sign out/in → **Outbound** → select `SO-4471` → drill a line **ATP by location** → **Release to wave** | Reserved → Picking |
| 8 | Terminal **7700** | hub → tap **Pick** → scan **location** + **product** → **Confirm pick** → (Packing opens) → **Close package** | back to hub |
| 9 | Admin **1002** | **Dispatch** → card **Assign carrier** → **Send pickup notice** → **Mark collected** → **Print waybill** | card → Dispatched |
| 10 | Admin **1001** | **Movements** (pick + dispatch entries) → **Today** (queues cleared) | 🎬 end |

**Side cuts (optional):** Terminal **Move stock** pile · Admin **Stocktake** → approve → ledger · Admin top-bar **Search** (`MILK-1L`) · Terminal **Lookup** scan code (`MILK-1L`) · top-bar warehouse switch **WH-02**.

**Recording:** Admin + Terminal side by side (terminal narrow), one actor per act, ~60–90 s total. For a
real end-to-end (created product flows through), run Aspire and point both apps at the Gateway.

**Already captured:** the canonical clip is [`media/golden-path-full.gif`](media/golden-path-full.gif);
per-app cuts and the full file index live in the [walkthrough → recordings](demo-walkthrough.md#the-recordings--which-file-is-what).
