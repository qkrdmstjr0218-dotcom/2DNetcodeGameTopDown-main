# Working Preferences

- Minimize token use across model/mode changes and resumed sessions. Keep replies and updates concise; avoid repeating history, broad file dumps, speculative exploration, and unnecessary tool calls.
- Inspect only relevant code. Use focused verification proportional to risk; do not repeat successful checks without a change or unresolved concern. Do not skip essential correctness or visual checks.
- Do not launch subagents or change models/reasoning settings unless the user requests it.
- Preserve existing artwork, weapon geometry, character scale, and working behavior outside the requested change. Prefer small edits and reuse existing assets.
- Treat brainstorming and future requests as notes, not implementation authorization. Distinguish confirmed requirements, candidates, and unresolved choices.
- Read `HANDOFF-2026-10-19.ko.md` for the current home-to-academy handoff; `HANDOFF-2026-10-16.ko.md` is historical. The latest crouch pose is unfinished and explicitly not accepted by the user. Preserve working motion and original scenes; inspect current state before further edits. Browser source: `LocalTools/Analysis/FrontShootingSandbox`. Local pose trials are separate from the production/network player.
- Read `LocalTools/Analysis/combat-system-design-notes.ko.md` for the planned combat direction when relevant.
