# Service

First-person horror game with free-roam driving/walking and no combat, built in Unity.

## About the Game

You play as a civil process server working night shifts in rural Hollis County. Your job is to drive out and deliver court documents to people who usually don't want to receive them. Every shift gives you a docket of addresses to locate and serve using your car, a flashlight, and your own judgment. If a place feels wrong, you can't fight back; you just leave.

The horror is focused on slow burn and atmosphere rather than cheap jump scares. In this build (v5), the first shift covers five locations across the county:
- Correll residence (cabin delivery)
- Harrow Lodge (interior delivery; when you hear breathing, turn away and stand completely still for 8 seconds)
- Vale House (upstairs study delivery with an escape back to the car)
- Bell residence (brick house interior delivery)
- Morrow House (manor with an upstairs gallery delivery)

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
| Mouse | Look around |
| E | Get in/out of car, knock, interact, file shift report at depot |
| R | Leave documents on the delivery table |
| U | Mark address as unable to serve |
| F | Flashlight (on foot) |
| Space | Start car ignition |
| Shift | Sprint on foot / brake while driving |
| Tab | Open docket clipboard (in car) |
| M | Open county map (in car) |
| Esc | Pause menu / close papers |

## Project Layout

- Assets/Scenes/HollisCounty.unity: Main scene with the road network and county properties.
- Assets/Scripts/: Gameplay scripts (player, car controller, surface footsteps, audio, and encounter logic).
- Assets/ServiceArt/: Shaders, materials, meshes, and scene assets.
- Assets/Editor/: Build tools and scene layout utilities.
- docs/: Design notes and shift specs.

## Asset Credits

- Environment: Flooded Grounds by Sandro T (Unity Asset Store).
- Monster model: Creep Horror Creature by AC Game Assets.
- Audio: Human field recordings and sound effects from Freesound and Nox Sound Essentials (CC0 / CC BY 4.0).
- Fonts: Courier Prime and Barlow (SIL Open Font License).
- Full links and source details are in ASSET-CREDITS.txt and Assets/Resources/Audio/V5/sources.json.
