# Strike Zone

Strike Zone is a single-player ten-pin bowling prototype built with Unity 6.6.8f1, URP, the Input System, and TextMeshPro.

## Open and play

1. Open the `StrikeZone` folder in Unity Hub with Unity `6000.6.8f1`.
2. Open `Assets/Scenes/BowlingMenu.unity`.
3. Press **Play** and choose **PLAY**.

The Build Settings scene order is `BowlingMenu`, `BowlingGame`, `BowlingResults`, so a Windows player build opens at the menu.

## Controls

- **A / D** or **Left / Right**: aim
- **Space**: start the power meter; press again to lock power and throw
- **Esc**: pause or resume
- Use the mouse to click menu and pause buttons.

## Included systems

- Pure C# frame progression and ten-pin score calculation in `Assets/Scripts/Logic`.
- Rigidbody lane, ball, pins, gutter, pit, and once-per-roll pin counting.
- Contextual HUD with ten-frame scoreboard, power meter, pause/game-over overlays, and strike/spare/gutter banners.
- Menu, instructions, master-volume settings, and results with score, strike/spare/open counts, best frame, and read-only scoreboard.
- Smooth ball/deck camera framing and event-driven audio feedback. Short synthesized placeholder sounds play until optional audio clips are assigned in the Inspector.

## Editor tools

Unity menu **Strike Zone → Build Remaining Screens** regenerates the HUD and menu/results scenes and updates Build Settings. It expects TextMeshPro Essential Resources to be imported. **Strike Zone → Build Windows Player** creates `Builds/Windows/StrikeZone.exe`. **Strike Zone → Build Lane Prototype** is for creating the initial primitive lane; **Install Pin System**, **Install Throw System**, and **Install Game Flow** install their components on that prototype.

## Project notes

- Target platform: Windows PC.
- Player default resolution: 1920×1080, resizable window.
- Rendering: URP; primitives and built-in UI are used, with no Asset Store dependency.
- The final scene layout is serialized in `Assets/Scenes`; scene changes should be saved before sharing the project.
- EditMode scoring/frame tests and a standalone Windows build are still separate QA steps; neither is run by the screen builder.
