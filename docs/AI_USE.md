# AI Use Statement — LiftLife

**Student:** Mohamed Hilman Ashraf Bin Mohamed Ashraf (BSW01085755)
**Course:** CSNB544 Mobile Application Development

## Tool
I used **Kiro**, an AI coding assistant, in the Kiro IDE (built on VS Code).

## What I did
- Wrote the **M1 proposal** (problem, objectives, features) and the **M2 design** (screens map and
  wireframes). These were the main input for the app.
- Chose the platform and setup: .NET MAUI with VS Code on my Mac, iOS as the main target because I use
  an iPhone, and SQLite for storage.
- Asked for the app **in stages**, one feature area at a time (foundation and data layer, Community,
  Workouts, Active Session, Nutrition, About), so I could test and understand each part before the next.
- **Tested every stage** on the iOS simulator and on my iPhone 14 Pro, and checked saved data in the
  SQLite file with DB Browser for SQLite.
- Gave feedback from my own testing that changed the app:
  - rest time I can change (−/+, skip, custom rest per exercise)
  - a bigger, bolder workout timer and total volume
  - a stronger rest alert, because the original vibration was only a slight tap on my phone
  - logging any food with its calories (Quick Add)
  - the UNITEN Gymnasium name, and a UNITEN photo of the gym that I supplied
  - queues for occupied machines
- Made the design decisions: photos instead of exercise videos (video sources were paid or didn't allow
  this use), crediting UNITEN and Pexels, and **not** adding a login (not needed for a local, single-device app).
- Set up Apple development signing so the app runs on my own iPhone.
- Recorded the demo video.

## What the AI did
- Generated most of the C# and XAML code from my prompts and design.
- Wrote the build and run scripts (`run-sim.sh`, `run-iphone.sh`).
- Helped fix toolchain problems (Xcode version check, SVG logo rendering, signing).
- Wrote documentation I asked for: the explainer notes (`docs/PROGRESS_NOTES.md`), media credits, and
  practice questions for the presentation.

## Example prompts and why I worded them that way
- *"make in ios version… i use an iphone"* — told it the real device I'd test on, so it would build for iOS.
- *"for database, use SQLite"* — the exam requires data storage, so I named the database up front.
- *"changeable rest time (reduce or add time more, skips and custom set rest time for each exercise)"* —
  I listed each change separately so I could check each one on its own.
- *"rest timer doesnt vibrate but rather just a slight haptic"* — I described the problem I felt on my
  phone instead of guessing a fix. That led to the Core Haptics alert.
- *"create a file of notes of what you actually do in each file"* — so I could learn the code and explain it.

## Problems found and fixed
- The logo used SVG text, which MAUI's image tool can't render. The build failed, so the "L" was redrawn as vector paths.
- A stray grey box showed under the station list (a default `BoxView` style). I spotted it on screen and it was replaced.
- Text fields had double borders on iOS. Fixed by removing the native border in `MauiProgram.cs`.
- The rest vibration was too weak on my real iPhone. I tested it and asked for a stronger one.
- The Quick Add servings line didn't update when the serving text changed. Found in testing and fixed.

## How I checked the work
Every change was built and run on the simulator and on my iPhone. For data features I opened
`liftlife.db3` in DB Browser to confirm the rows were saved correctly (for example, a Quick Add of
1.5 × 520 kcal saved as 780 kcal).

## How I learned the code
I used the explainer notes to go through the app screen by screen, practised answering questions, and
practised making small changes myself (for example the splash duration, rest buttons and queue estimate)
and rebuilding with `./run-sim.sh`.
