# Dense first shift implementation plan

**Goal:** Five distinct, densely dressed first-shift destinations with reliable encounters.
**Architecture:** Extend ServiceProperty with encounter metadata; ServiceHorror owns one current encounter at a time. Keep editor scene authoring in ServiceBuild partial files; surface audio and UI remain separate components.
**Tech Stack:** Unity 6000.6.0f1, URP, C#, Windows x64.
**Spec:** ../specs/2026-09-09-first-shift-design.md

- [ ] Audio: obtain Nox Sound CC0 library, add varied grass/wood/gravel steps and spatial environmental cues. Implement SurfaceAt raycast using ServiceSurface markers / TerrainCollider. Verify clip existence and selection.
- [ ] Cockpit/UI: fix needle math against dial graduation positions, refine cabin details; adapt docket/map/report to five entries and encounter-specific messages. Use ServiceProperty.Address/Brief/Instructions and ServiceHorror.Headline/Instruction/DeathLine.
- [ ] Scene: inspect stock templates and entrance geometry. Build five different active properties, landscape reserved access routes and dense forest. Preserve architecture, place physical signage and furnishings. Bake navigation across first-shift bounds.
- [ ] Encounters: add per-property type/trigger/spawn/reset state and three authored monster variants. Add eight-second look-away mechanic with running failure; longer pursuit routes; capture restores only current property.
- [ ] Verification: implement V5 smoke checks for five unique templates, reachable delivery/escape, watcher rules, clockwise needle math, audio surfaces, five-row UI, route saves. Build and run checks; inspect captures and fix defects.
- [ ] Package: update credits/README, preserve v4 archives, create and CRC-check v5 Windows/source archives. Leave Hub project usable.

Ruling: work directly in the requested Unity Hub project on a new branch; avoid duplicating the imported multi-gigabyte Unity Library. Existing v4 archives and clean Git baseline preserve rollback. No pushes or publication.
