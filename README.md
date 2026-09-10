# Service

A first-person atmospheric horror game prototype built in Unity.

## Overview

You play as a civil process server working evening shifts in rural Hollis County, delivering court documents to addresses across the area. With each shift, you receive a docket of addresses to locate and serve. Your tools are your car, a flashlight, and your judgment. If an address feels wrong or unsafe, there is no combat—you mark it, leave, or head back.

The first shift features five distinct properties across the county (Correll residence, Harrow Lodge, Vale House, Bell residence, and Morrow House). Encounters require reading the environment—some involve fleeing back to your vehicle, while others (like Harrow Lodge) require turning away and standing completely still.

The project focuses on slow-burn, environmental unease rather than jump scares, relying on heavy fog, rural lighting, surface-aware footstep audio, and diegetic sound design.

## How to Run in Unity

- **Engine Version:** Unity 6 (`6000.6.0f1`)
- **Pipeline:** Universal Render Pipeline (URP)

### Steps:
1. Open **Unity Hub**.
2. Click **Add** -> **Add project from disk** and select this repository folder (`ServiceProject`).
3. Open the project using Unity `6000.6.0f1`.
4. In the Project window, open `Assets/Scenes/HollisCounty.unity`.
5. Press **Play** to start the shift.

## Controls

| Key | Action |
| --- | --- |
| **WASD** | Walk / Drive (S reverses) |
| **Mouse** | Look around (first-person on foot and inside vehicle) |
| **E** | Exit / enter vehicle, interact, file report at depot |
| **R** | Leave copy of documents at marked delivery table |
| **U** | Mark property unable to serve |
| **F** | Toggle flashlight (on foot) |
| **Space** | Ignition key |
| **Shift** | Sprint on foot / brake in car |
| **Tab** | Check docket clipboard (inside vehicle) |
| **M** | Check county map (inside vehicle) |
| **Esc** | Close paper / pause menu |

## Project Structure

- `Assets/Scenes/HollisCounty.unity`: Main game scene containing county layout, road network, and property destinations.
- `Assets/Scripts/`: Core gameplay scripts (player controller, vehicle mechanics, docket tracking, surface audio detection, encounter logic).
- `Assets/ServiceArt/`: Custom materials, lighting setups, environment meshes, and authored scene elements.
- `Assets/Editor/`: Editor build, cockpit modeling, and scene expansion tools.
- `docs/`: Design documents and shift specifications.

## Credits & Sourced Assets

Environment models use the *Flooded Grounds* pack by Sandro T. Character models use *Creep Horror Creature* by AC Game Assets. Audio recordings are sourced from human field recordings via Freesound and Nox Sound Essentials under CC0 and CC BY 4.0 licenses. Typography uses *Courier Prime* and *Barlow* (SIL Open Font License). Full itemized source links and licensing details are listed in `ASSET-CREDITS.txt` and `Assets/Resources/Audio/V5/sources.json`.
