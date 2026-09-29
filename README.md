# Service

First-person horror game with free-roam driving/walking and no combat, built in Unity.

## About the Game

You play as a civil process server working night shifts in rural Hollis County. Your job is to drive out and deliver court documents to people who usually don't want to receive them. Every shift gives you a docket of addresses to locate and serve using your car, a flashlight, and your judgment. If a place feels wrong, you can't fight back; you just leave.

The horror is focused on slow burn and atmosphere rather than cheap jump scares. In this build (v16), the first shift covers five locations across the county:
- Correll residence (friendly delivery; knock at the front door, resident response, seated German Shepherd)
- Harrow Lodge (interior delivery; read the entrance notice for room directions; when breathing begins, turn away and stand completely still for 8 seconds; avoid sustained eye contact)
- Vale House (upstairs study delivery with inward-swinging door and pursuit escape back to the car)
- Bell residence (friendly delivery at front entrance, resident response, followed by return-path ambush and delayed vehicle ignition escape)
- Morrow House (manor with an upstairs gallery delivery, entrance door interaction, accelerated pursuit, and roadwork hurdles)

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
| E | Read notice / knock / enter or exit car, interact, file report at depot |
| R | Leave documents on the delivery table |
| U | Mark address as unable to serve |
| F | Flashlight (on foot) |
| Space | Jump on foot (disabled on stairs) / turn ignition key (in car) |
| Ctrl | Hold to crouch (on foot) |
| V / B | Radio power / cycle through 3 stations (in car) |
| Shift | Sprint on foot / brake while driving |
| Q / E | Hold while sprinting to look behind over shoulder |
| Tab | Open docket clipboard (in car) |
| M | Open route map (in car, follows curved roads and return lane) |
| Esc | Pause menu / close papers |

## Project Layout

- Assets/Scenes/HollisCounty.unity: Main scene with the road network, return lane, and county properties.
- Assets/Scripts/: Gameplay scripts (player movement, first-person hands, crouch/jump, car controller, surface footsteps, storm effects, radio, route map, audio, and encounter logic).
- Assets/ServiceArt/: Shaders, materials, meshes, scanned woodland assets, and scene assets.
- Assets/Editor/: Build tools, doorway inspection, and scene layout utilities.

## Asset Credits

- Environment: Flooded Grounds by Sandro T, Conifers [BOTD], Rocks and Boulders 2 (Unity Asset Store), Poly Haven CC0 scanned woodland assets (ground textures, timber, stumps, ferns, leather), and Kenney City Kit Roads (CC0).
- Monster models: Creep Horror Creature by AC Game Assets, and Demon Horror Creature with Weapon.
- First-person arms: PSX First Person Arms by Drillimpact (CC0).
- Resident NPCs: Sophia and Nathan by Renderpeople (royalty-free game use).
- Domestic dog: German Shepherd by Pawel Walasiewicz (BlenderKit royalty-free).
- Vehicle cabin: Ford Crown Victoria interior by Tyble (CC BY 3.0 fan art, noncommercial).
- Music: "This House", "The Descent", "George Street Shuffle", "Local Forecast - Elevator", and "Jazz Brunch" by Kevin MacLeod (incompetech.com), licensed under CC BY 4.0.
- Audio: Human field recordings and sound effects from Freesound, Nox Sound Essentials, and ViRiX Dreamcore (CC0 / CC BY 4.0 / CC BY 3.0).
- Fonts: Courier Prime and Barlow (SIL Open Font License).
- Full links and source details are in ASSET-CREDITS.txt, Assets/Resources/Audio/V5/sources.json, Assets/Resources/Audio/V9/sources.json, Assets/Resources/Audio/Radio/, Assets/Resources/Audio/V13/sources.json, and Assets/ServiceArt/ScannedWoodland/*/source.json.

