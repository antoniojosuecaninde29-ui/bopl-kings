# MODLOG — Bopl Kings

## Project
Mashup: Bopl Battle's blob kings + ability draft inside 9 Kings' grid auto-battler.
Host (primary): 9 Kings (custom-9-kings, Steam 2784470, Unity IL2CPP).
Companion: Bopl Battle (custom-bopl-battle, Steam 1686940, Unity Mono).
Mode: solo roguelike + head-to-head (2 players, mod's own LAN versus). License: open remix.

## Route (chosen 2026-10-12)
Pattern 1 "port the content" (mashup-mods skill): Bopl's blob kings, abilities and ring-out rules
become new 9 Kings content imitating Bopl's real mechanics; every Bopl name/color/description is read
at runtime from the player's installed copy ({game:custom-bopl-battle}), never shipped.
Loader: BepInEx 6 IL2CPP (be.788) — the established route for 9 Kings (Thunderstore BepInExPack_IL2CPP;
Clone Kings ships the same build and is one-click approved). BepInEx is LGPL-2.1: bundled with credit.
Why not passthrough (Pattern 2): 9 Kings is turn-based/auto-battle; Bopl must not run at all — its
mechanics are ported, its identity is read from its files. Lightest route that delivers the idea.

## Architecture
- src/GameCore — pure C# (net6.0), no game references: sheets loader, ring-out rules, blob units,
  king/draft logic, Bopl content reader, versus LAN protocol, Melty test mode. Unit-checkable anywhere.
- src/Plugin — BepInEx 6 IL2CPP plugin. GameRefs.cs discovers 9 Kings' real types by reflection over
  BepInEx/interop at runtime; names can be overridden per install in BoplKings/hooks-override.json
  so game updates don't force recompiles. Apply.cs applies GameCore logic through GameRefs.

## Sheets (source of truth — change the sheet before the code)
sheets/kings.json (3 blob kings) · sheets/abilities.json (8 ability cards) · sheets/units.json
(3 blob troops) · sheets/battle.json (6 ring-out rules) · sheets/hooks.json (7 hook points) ·
sheets/multiplayer.json (2-player protocol + Melty wiring).

## v1 scope (decided from user's answers)
King select (3 blob kings) + per-round Bopl ability draft + ring-out battle layer + blob troops +
head-to-head LAN blob-skirmish (best of 3) + test mode + auto-screenshots.

## Status / what is verified
- [x] Design sheets complete, preflight clean (all rows checked, no unresolved refs)
- [x] GameCore logic written
- [ ] GameCore compiled + logic tests (blocked on dotnet SDK / done in sandbox if available)
- [ ] Plugin compiled against 9 Kings' interop assemblies (needs the game on the creator's PC)
- [ ] Hook names pinned in-game (GameRefs discovery + overrides)
- [ ] Head-to-head tested host+join on two copies
- [ ] Real screenshots / clip from the running mod
- [ ] test_release in Melty on the creator's PC

## Gotchas
- 9 Kings is IL2CPP: plugin targets net6.0, references {game}/BepInEx/interop after first game launch.
- Bopl Battle content: read only names/descriptions/colors from its folder; never copy files.
- Keep Melty token out of this repo.