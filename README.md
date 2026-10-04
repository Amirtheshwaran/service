# Service

First-person horror game with free-roam driving/walking and no combat, built in Unity.

## About the Game

You play as a civil process server working night shifts in rural Hollis County. Your job is to drive out and deliver court documents to people who usually don't want to receive them. Every shift gives you a docket of addresses to locate and serve using your car, a flashlight, and your judgment. If a place feels wrong, you can't fight back; you just leave.

The horror is focused on slow burn and atmosphere rather than cheap jump scares. In this build (v22), the story unfolds across three night shifts:
- Night 1 (October 1st, 1998): Ordinary civil service work across five rural properties. Subtle atmospheric omens, doorstep resident interactions, an unsettling figure along the tree line, and a corridor encounter at Vale House.
- Night 2 (October 4th, 1998): Escalated tension and wrongness. Harrow Lodge watcher encounter (turn away and stand completely still), Bell residence note and pursuit escape back to the car, and Morrow House pursuit.
- Night 3 (October 9th, 1998): Severe fog and unmapped route past the county road closure, leading to the unsurveyed final parcel.

## How to Play / Run in Unity

Built with Unity 6 (6000.6.0f1) using URP.

1. Open Unity Hub.
2. Click Add -> Add project from disk, and select this folder (ServiceProject).
3. Open it with Unity 6000.6.0f1.
4. In the Project tab, open Assets/Scenes/HollisCounty.unity.
5. Hit Play.

## Controls

| Key | Action |
| --- | --- |
| WASD | Walk / Drive (S reverses car) |
| Mouse | Look around (including inside cockpit) |
| E | Read notice / knock / enter or exit car, close door, interact, file report |
| R | Leave documents on the delivery table |
| U | Mark address as unable to serve |
| F | Flashlight (on foot) |
| Space | Jump on foot (disabled on stairs) / turn ignition key (in car) |
| Ctrl | Hold to crouch (on foot) |
| V / B | Radio power / cycle through 3 stations (in car) |
| Shift | Forward sprint (with breath depletion) / brake while driving |
| Tab | Open docket clipboard (in car, while stopped) |
| Esc | Pause menu / close papers or held note |

## Gameplay & Features

- **Note Transcripts & Legibility Plates**: Inspecting a handwritten doorstep note displays a typed transcript with signature attribution underneath. Interaction prompts sit on shaded backdrop plates with targeting reticles to maintain legibility in direct flashlight beams.
- **Door Closing & Reopening**: Players can pull house entrance doors shut from the outside doorstep after entry, or reopen them prior to completing deliveries.
- **Road Boundary Corridor & Pull-offs**: Soft boundary guidance keeps the vehicle within drivable road corridors and mailbox pull-offs without dead stops, supporting tight three-point turns.
- **Forward Sprint Pacing & Stamina**: Forward-only sprint mechanics with stamina depletion and audible breathing audio cues (approx. 6 seconds calm, 10 seconds in pursuit), replacing unlimited sprint.
- **Resident & Creature Behavior**: Rex charges barking down Correll driveway before accompanying the player to the steps; brief upstairs hall encounter at Vale House on night one.
- **Camcorder Aesthetic**: VHS/camcorder visual filter with adjustable grain, fringing, vignette, and gamma-space color grading.
- **First-Person Hands & Gestures**: Overhand flashlight grip, dedicated gestures for knocking, handing over papers, placing documents on desks, and pushing doors, plus dynamic steering wheel hands with crossing poses.
- **Typewriter Time Cards & Stamps**: Fears to Fathom style typed date and time opening cards, corner arrival stamps, and rewind sequence cards.
- **Audio & Pursuit Dread**: Adaptive pursuit tension score ("Anxiety" and "Penumbra" by Kevin MacLeod), rising drones, panic breathing audio, tall-grass foley, and VHS tape-damage glitch effects.
- **Wayfinding**: Reflective blue rural mailboxes with 911 numbers and family names at each driveway, dynamic on-screen route guidance, and handwritten docket clipboard.
- **Settings Menu**: Press `Esc` while playing to access configuration options for video, audio, controls, mouse look sensitivity, camera filter intensity, and brightness.
- **Route Saves**: Completed delivery shifts save automatically upon filing your report at the county depot.

## Project Layout

- Assets/Scenes/HollisCounty.unity: Main scene with the road network, rural properties, and county depot.
- Assets/Scripts/: Gameplay scripts (player locomotion, interaction gestures, car physics, route guidance, dialog/notes, VHS post-processing, and horror encounter logic).
- Assets/ServiceArt/: Shaders, materials, meshes, scanned woodland assets, furniture, mailboxes, and scene assets.
- Assets/Editor/: Build tools, doorway inspection, layout toolkits, and test runners.

## Asset Credits

- Environment & Props: Flooded Grounds by Sandro T, Conifers [BOTD], Rocks and Boulders 2 (Unity Asset Store), Poly Haven CC0 scanned woodland assets and furniture, and Kenney City Kit Roads (CC0).
- Monster models: Creep Horror Creature by AC Game Assets, and Demon Horror Creature with Weapon.
- First-person arms: PSX First Person Arms by Drillimpact (CC0).
- Resident NPCs: BELAZ elderly man and vrimen resident model (CC BY 4.0), with animations from Unity Starter Assets.
- Domestic dog: German Shepherd by RetroStyle Games (itch.io).
- Props: Blue rural mailbox by Rylae Shylna (CC BY 4.0), electrical powerline poles by tiedtke (CC BY 4.0), and fuzzy dice by jediscoob (CC BY 4.0).
- Vehicle cabin: Ford Crown Victoria interior by Tyble (CC BY 3.0 fan art, noncommercial).
- Music: "Anxiety", "Penumbra", "This House", "The Descent", "George Street Shuffle", "Local Forecast - Elevator", and "Jazz Brunch" by Kevin MacLeod (incompetech.com), licensed under CC BY 4.0.
- Audio: Human field recordings and sound effects from Freesound, Nox Sound Essentials, and ViRiX Dreamcore (CC0 / CC BY 4.0 / CC BY 3.0).
- Fonts: Courier Prime, Barlow, VT323, and handwritten fonts (SIL Open Font License).
- Full links and source details are in ASSET-CREDITS.txt.
