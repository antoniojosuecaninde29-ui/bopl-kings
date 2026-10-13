# Bopl Kings

A mashup mod for **9 Kings** that brings **Bopl Battle**'s blob fighters and spell-drafting inside it.

## What players get
- Pick one of three Bopl blob kings at the start of every 9 Kings run: the Crimson King
  (aggressive, +attack per ring-out), the Azure King (heavy, shrugs off knockback) and the
  Verdant King (swarm, regenerating blobs). Each has a passive and a starting ability.
- Every round, alongside the normal 9 Kings draft, you draft one **Bopl ability card** —
  black hole, gravity bomb, time stop, shrink ray, engine, smoke, vine, missile — and it fires
  on the battlefield with Bopl's knock-them-off-the-platform feel.
- The battlefield gains Bopl's core rule: **ring-out**. Units pushed past the edge die no matter
  how much HP they have; your blob troops squish once to survive a lethal hit; and knocking the
  enemy king off the map shatters their whole army instantly.
- Your army is built from blob troops: Blob Warriors, Blob Slingers and Blob Brutes.
- **Head-to-head (2 players):** host a versus skirmish from the mod's menu; a friend joins with
  Melty's join link. Three blob clashes, best of 3.

## What players need
- **9 Kings** (Steam) — the game you play in; the mod installs into it.
- **Bopl Battle** (Steam) — its ability names, descriptions and blob colors are read from your own
  installed copy while the mashup runs. Nothing from either game is copied into the mod.

## What's inside the package
- BepInEx 6 (be.788, IL2CPP, win-x64) — LGPL-2.1, © BepInEx team, bundled unmodified with credit.
- BoplKings.dll + data sheets — the mashup itself (open for remix).

## Multiplayer
LAN / same-network versus, 2 players, join link handled by Melty (host: `Hosting at <ip>:<port>`
in `%LOCALAPPDATA%/BoplKings/logs/latest.log`; joiner: `join=<address>` in
`%LOCALAPPDATA%/BoplKings/melty.properties`).

## Test mode (used by Melty's test runs only)
Reads `%LOCALAPPDATA%/Melty/test/bopl-kings.json` (seed, checks path), walks king select →
ability draft → a ring-out battle, and appends `pass <check>` / `fail <check>: why` per check.
Also writes auto-screenshots to `%LOCALAPPDATA%/BoplKings/screenshots/`.