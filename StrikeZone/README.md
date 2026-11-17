# Strike Zone

Strike Zone is a single-player ten-pin bowling prototype built with Unity 6.6.8f1, URP, the Input System, and TextMeshPro.

## Open and play

1. Open the `StrikeZone` folder in Unity Hub with Unity `6000.6.8f1`.
2. Open `Assets/Scenes/BowlingMenu.unity`.
3. Press **Play** and choose **PLAY**.

The Build Settings scene order is `BowlingMenu`, `BowlingGame`, `BowlingResults`, so a Windows player build opens at the menu.

## Controls

- **A / D** or **Left / Right**: aim
- **Q / E**: add left or right hook to the next throw (hold while locking power)
- **Space**: start the power meter; press again near **70%** to lock power and throw
- **Space during a roll**: quick-return the ball after it reaches the pit or stops, skipping the remaining pin-settle wait
- **Esc**: pause or resume
- Use the mouse to click menu and pause buttons.

## Modes and feedback

- **Classic Game**: play a standard ten-frame game.
- **Strike Challenge**: earn at least three strikes in ten frames; progress stays visible in the gameplay HUD and the result screen reports whether you met the target.
- **2 Player Local**: players alternate after each frame on the same lane, keep separate ten-frame scorecards, and see the winner at the end.
- Every roll receives an **S / A / B / C shot rank** based on pins knocked down, power timing, and aim centering. The best rank is shown in the results.
- Results save local records between launches: Classic and Challenge high scores, plus Player 1 / Player 2 wins and draws.

## Included systems

- Pure C# frame progression and ten-pin score calculation in `Assets/Scripts/Logic`.
- Rigidbody lane, ball, pins, gutter, pit, and once-per-roll pin counting.
- Skill-based shot shaping: aim angle, a timing-based power meter, and a lateral hook impulse.
- Ten-frame strike challenge with live objective tracking and end-of-game success feedback.
- Per-roll skill rating based on pin conversion, power release, and aim centering.
- Contextual HUD with ten-frame scoreboard, power meter, pause/game-over overlays, and strike/spare/gutter banners.
- Menu, instructions, master-volume settings, and results with score, strike/spare/open counts, best frame, and read-only scoreboard.
- A main-menu local records panel for saved Classic / Challenge highs and 2-player wins / draws.
- Smooth ball/deck camera framing, animated shot-rank banners, strike / spare camera feedback, and layered synthesized bowling sounds. Optional Inspector audio clips can replace the generated cues.

## Editor tools

Unity menu **Strike Zone → Build Remaining Screens** regenerates the HUD and menu/results scenes and updates Build Settings. It expects TextMeshPro Essential Resources to be imported. **Strike Zone → Build Windows Player** creates `Builds/Windows/StrikeZone.exe`. **Strike Zone → Build Lane Prototype** is for creating the initial primitive lane; **Install Pin System**, **Install Throw System**, and **Install Game Flow** install their components on that prototype.

## Project notes

- Target platform: Windows PC.
- Player default resolution: 1920×1080, resizable window.
- Rendering: URP; primitives and built-in UI are used, with no Asset Store dependency.
- The final scene layout is serialized in `Assets/Scenes`; scene changes should be saved before sharing the project.
- EditMode scoring/frame tests and a standalone Windows build are still separate QA steps; neither is run by the screen builder.
