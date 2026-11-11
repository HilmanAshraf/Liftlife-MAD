# LiftLife — Development Progress & Explainer Notes

**Student:** Mohamed Hilman Ashraf Bin Mohamed Ashraf (BSW01085755)
**Course:** CSNB544 Mobile Application Development
**App:** LiftLife — Community Gym Hub & Routine Tracker
**Framework:** .NET MAUI (C# + XAML)
**Database:** SQLite (local, offline-first)
**Primary platform:** iOS (developed and tested on iPhone / Mac)

> This document is my working log. For each development day I record what I built,
> which files I touched, and *why* — so I can explain the app and answer questions
> about how it works.

---

## Why .NET MAUI + VS Code on a Mac

The exam brief asks for .NET MAUI, built with Visual Studio. Visual Studio **for Mac**
was officially retired by Microsoft on **31 August 2024**. Microsoft's own replacement
for Mac developers is **Visual Studio Code with the C# Dev Kit + .NET MAUI extensions**,
which is the current official Mac toolchain. That is what this project uses.

- Framework: .NET MAUI on .NET 10
- Editor: VS Code with C# Dev Kit and .NET MAUI extensions
- Database: SQLite via the `sqlite-net-pcl` library (local file storage on device)

---

## How the app is structured (the big picture)

A .NET MAUI app has one shared C# codebase that compiles to native iOS (and Android).
The key pieces:

- **`App.xaml` / `App.xaml.cs`** — the application entry point. It sets the global theme
  and decides which page shows first (the splash screen).
- **`AppShell.xaml`** — defines the app's navigation. LiftLife uses a **bottom tab bar**
  with four tabs: Community, Workouts, Nutrition, About.
- **`Views/`** — the screens (pages). Each screen is a `.xaml` file (the layout) plus a
  `.xaml.cs` file (the code behind it).
- **`Models/`** — plain C# classes that describe our data (a Station, a Routine, a Food item…).
  *(Added Day 2.)*
- **`Data/`** — the SQLite database layer that saves and loads those models.
- **`Resources/Styles/`** — colors and reusable styles (our dark + lime theme).
- **`Resources/AppIcon` & `Resources/Splash`** — the custom app icon and splash image.
- **`MauiProgram.cs`** — app startup configuration and dependency injection (wiring services
  and pages together).

**Data binding note:** XAML pages bind to data from the models/services, so when the
data changes the screen updates. This satisfies the "data entry + data manipulation +
save/retrieve from database" requirements.

---

## DAY 1 — Project foundation, theme, navigation, splash

**Goal:** Get a running skeleton: the solution, the brand look, the navigation shell,
the splash screen, and the SQLite plumbing.

### Files created / changed and what they do

- **`LiftLife.csproj`** — the project definition. I set:
  - the app name, app id (`com.hilmanashraf.liftlife`), and version `1.0.0`
  - the custom icon + splash references
  - added the SQLite packages (`sqlite-net-pcl`, `SQLitePCLRaw.bundle_green`)
  - made **iOS the primary build target** (Android/Mac Catalyst stay available)

- **`App.xaml`** — merges the color + style dictionaries and forces the app into
  **Dark mode** (`UserAppTheme="Dark"`), because LiftLife is a dark-themed app.

- **`App.xaml.cs`** — on startup the window opens the **SplashPage** first.

- **`Resources/Styles/Colors.xaml`** — the LiftLife brand palette:
  - lime/yellow accent `#E9F558` (from the "L" logo)
  - near-black background `#0B0B0F`, dark card surfaces
  - status colors: green = free, red = busy, grey = out of order

- **`Resources/Styles/LiftLifeStyles.xaml`** — reusable styles so every screen looks
  consistent: `Card`, `ScreenTitle`, `CardTitle`, `PrimaryButton` (lime), `DarkButton`,
  `OutlineButton`, text styles, entry style.

- **`Views/SplashPage.xaml` / `.cs`** — the welcoming splash screen:
  - shows the lime "L" squircle logo, "LiftLife", "Community Gym Hub", and the slogan
    "Smart Community & Digital Innovation"
  - a background timer waits ~2 seconds, then automatically navigates to the main
    tabbed dashboard (this is the required "splash auto-navigates to home" behaviour).

- **`AppShell.xaml`** — the bottom navigation bar with the 4 tabs. Styled so the selected
  tab is lime and unselected tabs are grey.

- **`Views/CommunityPage`, `WorkoutsPage`, `NutritionPage`, `AboutPage`** — the four tab
  screens. On Day 1 these are headers + placeholders; each gets fully built on its day.

- **`Data/LiftLifeDatabase.cs`** — the SQLite connection service. It opens a database file
  named `liftlife.db3` in the app's private data folder. Tables get registered on Day 2.

- **`MauiProgram.cs`** — registers the database as a shared singleton and registers the
  pages for dependency injection.

- **`Resources/AppIcon/appicon.svg` + `appiconfg.svg`** — the custom app icon: lime "L"
  squircle on a dark background.

- **`Resources/Splash/splash.svg`** — the splash image used at OS launch. Uses the exact
  same lime squircle + bold "L" as the app icon, scaled to 80%.
- **`Resources/Images/logo.svg`** — the same logo as a normal image, shown on the in-app
  `SplashPage`. So the icon, the iOS launch screen and the splash page all match.

- **`Resources/Images/tab_*.svg`** — the four bottom-tab icons.

### Platform decision & source control (end of Day 1)
- **Target platform set to iOS** — I use an iPhone, and iOS is the natural fit for a Mac
  dev setup. Android and Mac Catalyst are still available as secondary build targets, but
  iOS is primary.
- **Source control:** the whole project is in Git and pushed to GitHub at
  `https://github.com/HilmanAshraf/Liftlife-MAD.git` (branch `main`). Build artifacts
  (`bin/`, `obj/`) and the runtime SQLite file are excluded via `.gitignore`.
- **One-time local setup to build iOS** (run once in Terminal):
  `sudo xcode-select -s /Applications/Xcode.app/Contents/Developer`

### What I can demo after Day 1
The app launches → splash screen with the logo → after 2 seconds it lands on the
bottom-tab dashboard, and I can tap between Community / Workouts / Nutrition / About.

### Likely questions & my answers
- **Q: Why is Visual Studio for Mac not used?** It was retired by Microsoft in Aug 2024;
  VS Code with the .NET MAUI extension is the official Mac replacement.
- **Q: Where is data stored?** In a local SQLite file on the device — fully offline.
- **Q: How does the splash auto-navigate?** `SplashPage.OnAppearing` runs a 2-second
  delay then swaps the window's page to `AppShell` (the tabs).
- **Q: Why dark theme?** It matches the Figma design and gives high contrast for gym use.

---

## DAY 2 — Data layer: models, SQLite tables, repository, seed data

**Goal:** Build the complete offline data foundation so every feature screen has
something real to read from and write to. This is the "Storage + data binding"
backbone the rubric rewards.

### What I built and why

**Models (`Models/`)** — plain C# classes, each marked as a SQLite `[Table]`:
- **`Station`** — a gym machine with a live status (Free / Busy / Out of Order), who
  reported it, and when. Has helper properties (`StatusLabel`, `RelativeUpdated` like
  "2m ago") so the UI stays simple.
- **`Exercise`** — the master exercise catalogue. Each has a **muscle group** and
  **equipment** type — these two fields are what make the swap engine work.
- **`Routine`** + **`RoutineExercise`** — a saved workout split and the exercises in it
  (with target sets/reps/rest and personal setup notes).
- **`WorkoutSession`** + **`SetLog`** — a logged workout and each individual set
  (weight × reps). Session stores total volume for history.
- **`FoodItem`** + **`MealLog`** — the food catalogue and foods logged per day/meal.
- **`UserSettings`** — one row holding baseline macro targets and today's day type.
  It **computes** training-day vs rest-day targets automatically (rest day = fewer
  calories and carbs). This is the "adaptive nutrition" logic.
- **`Enums.cs`** — `StationStatus`, `DayType`, `MealCategory`.

**`Data/LiftLifeDatabase.cs`** — opens the SQLite file, creates all tables, and
**seeds default data on first launch**: ~20 exercises across muscle groups, 7 gym
stations, 10 common foods, default macro targets, and one sample "Push Day A" routine.
Seeding checks if a table is empty first, so it only happens once.

**`Data/LiftLifeRepository.cs`** — all the data operations the screens call, so pages
never touch SQL directly. Highlights:
- station listing + category filter + status update + crowd-density calculation
- **the swap engine** (`GetSwapOptionsAsync`): given an exercise, it finds other
  exercises that train the **same muscle group**, and flags each with whether matching
  equipment is currently free in the gym. This is the core "crowded gym" innovation.
- routine CRUD, workout session start/complete (auto-sums volume), nutrition logging.

**`MauiProgram.cs`** — registered the database and repository for dependency injection
so any page can request them.

### How the swap engine works (likely a key question)
Every exercise has a `MuscleGroup`. When a station is busy, I ask the repository for
all other exercises with the *same* muscle group. I then cross-check the live station
statuses: if an alternative's equipment type has a free station, it's marked
"Free in Gym". So the suggestion is both **biomechanically valid** (same muscle) and
**practical** (equipment actually available). All of this runs locally against SQLite —
no internet needed.

### iOS build confirmed working
The project now builds successfully for iOS with Xcode 27.1. Two environment notes:
- The installed .NET for iOS workload expects Xcode 27.0 exactly; the Mac has 27.1
  (same major band, newer minor). I set `ValidateXcodeVersion=false` in the csproj to
  skip the strict equality check — a supported flag for this exact situation.
- The logo SVGs originally used `<text>` for the "L"; the image rasterizer couldn't
  process text, so I redrew the "L" as vector paths. Icon + splash now build cleanly.

To see it live, install the iOS Simulator runtime once: `xcodebuild -downloadPlatform iOS`.

**Confirmed running on the iPhone 17 simulator (iOS 27.1):** splash screen shows the
lime "L" logo, auto-navigates after ~2s to the dashboard, and the bottom tab bar
(Community / Workouts / Nutrition / About) works with the custom tab icons and lime
selected-tab highlight. Screenshot: `docs/screenshot_dashboard.png`.

To build & run on the simulator from the terminal:
```bash
# open the simulator window (Xcode 27 renamed Simulator.app to DeviceHub.app)
open /Applications/Xcode.app/Contents/Applications/DeviceHub.app

# boot a simulator (one-time per session)
xcrun simctl boot "iPhone 17"
cd LiftLife
dotnet build -t:Run -f net10.0-ios -p:_DeviceName=:v2:udid=<SIMULATOR_UDID>
```

### Likely questions & my answers
- **Q: Where does data persist?** A local SQLite database file (`liftlife.db3`) in the
  app's private storage. It survives app restarts and works fully offline.
- **Q: How is the database created?** On first launch `CreateTableAsync` builds each
  table from the model classes, then seed methods insert starter data.
- **Q: Why a repository instead of querying in the page?** Keeps data logic in one place,
  makes pages simpler, and is easy to explain/test.
- **Q: What makes rest-day vs training-day targets change?** `UserSettings` stores a
  baseline; computed properties scale calories/carbs down on rest days. Protein stays high.

---

## How the screens are organised (MVVM pattern, from Day 3 onwards)

Each feature screen has three parts:
- **View** (`Views/XxxPage.xaml`) — the layout. Uses `{Binding ...}` to show data.
- **ViewModel** (`ViewModels/XxxViewModel.cs`) — holds the screen's data and logic. It
  inherits `BaseViewModel`, which implements `INotifyPropertyChanged`, so when a value
  changes the screen updates automatically. Lists use `ObservableCollection`, so adding or
  removing an item updates the list on screen.
- **Code-behind** (`Views/XxxPage.xaml.cs`) — handles taps that need dialogs or
  navigation (alerts, opening another page), then calls the ViewModel.

The ViewModels get the repository through **dependency injection** (registered in
`MauiProgram.cs`). `Services/ServiceHelper.cs` is a small helper that fetches them.

---

## DAY 3 — Community Station Hub + Report Status modal

**Goal:** The home screen where members see and report live gym equipment status.

### Files and what they do
- **`ViewModels/BaseViewModel.cs`** — base class for data binding (`SetProperty` raises
  `PropertyChanged` only when a value really changes).
- **`ViewModels/CommunityViewModel.cs`** — loads stations from SQLite, filters by category,
  works out the crowd-density banner, and claims stations. Also contains `CategoryChip`
  (one filter pill, with `IsSelected` driving the lime highlight).
- **`Views/CommunityPage.xaml` / `.cs`** — the Community Station Hub (screen 2.0):
  - **Top bar**: "LiftLife" + "📍 Campus Gym"
  - **Crowd density banner**: e.g. "GYM DENSITY: QUIET (33%) — 2 of 6 working stations in
    use", with a progress bar. Calculated as busy ÷ working stations (broken ones excluded).
  - **Filter chips**: All / Benches / Cables / Machines / Squat Racks (tap gesture).
  - **Station cards**: name, who reported it and when, coloured status badge.
  - **Main button changes with status**: FREE → *Claim Station* (marks it busy by "You");
    BUSY → *Station Busy? Swap*; OUT OF ORDER → *Find Alternative*. Swap runs the
    swap engine and lists same-muscle exercises using different equipment, with
    "Free in Gym" tags, then shows the form cue for the one you pick.
  - **Update Status** button opens the report modal.
  - **Pull down to refresh** (RefreshView gesture).
- **`Views/ReportStatusPage.xaml` / `.cs`** — Report Status modal (screen 2.1): close (✕),
  station name, three tappable options (Available / Busy / Out of Order, the selected one
  gets a lime border), a note field, and **Submit**. Validation: Out of Order requires a
  note. Saving writes to SQLite, gives a haptic tap, and refreshes the hub.
- **`Converters/StatusConverters.cs`** — value converters that turn a `StationStatus` into
  the badge colour (green / red / grey) and its text colour.
- **`Models/Station.cs`** — added `DetailLine` and `PrimaryActionText` display helpers.
- **`Data/LiftLifeRepository.cs`** — added `GetAlternativesForStationAsync` (swap from the
  hub) and improved the density calculation.

### Likely questions & my answers
- **Q: How does the badge change colour?** The badge's `BackgroundColor` is bound to the
  station's `Status` through `StatusToColorConverter`, which returns green, red or grey.
- **Q: How does the filter work?** Tapping a chip calls `SelectCategoryAsync`, which marks
  that chip selected (a `DataTrigger` turns it lime) and reloads stations with a SQL
  `WHERE Category = ?` filter.
- **Q: What happens when I submit a report?** `UpdateStationStatusAsync` updates the row
  (status, note, reported by, time) in SQLite, then the hub reloads from the database.
- **Q: Which gestures / MAUI APIs are used?** Tap gestures (chips, report options),
  pull-to-refresh, haptic feedback (`HapticFeedback`), modal navigation, alerts/action sheets.

---

## DAY 4 — Workout Routine Hub + Routine Builder

**Goal:** Let members build and manage their own workout routines.

### Files and what they do
- **`ViewModels/WorkoutsViewModel.cs`** — loads routines (with exercise counts) and the most
  recent completed session; deletes routines.
- **`Views/WorkoutsPage.xaml` / `.cs`** — Workout Routine Hub (screen 3.0):
  - Title "Workout Routines" + **+ New** button
  - **Last session card**: "Yesterday: Pull Day B • 5,420 kg logged" (from `workout_sessions`)
  - **Routine cards**: name, muscle focus, "5 exercises • Estimated: 50 mins",
    **START WORKOUT SESSION** and **Edit** buttons
  - **Swipe left to delete** (SwipeView gesture) with a confirmation dialog
  - Pull to refresh
  - *Start Workout Session* opens the live session tracker (built on Day 5).
- **`ViewModels/RoutineBuilderViewModel.cs`** — create/edit logic: name, focus, time,
  add / remove / reorder exercises, and **validation** before saving (name required, time
  5–240 min, at least one exercise, sets 1–10, reps 1–50, rest 0–600 s).
- **`Views/RoutineBuilderPage.xaml` / `.cs`** — Routine Builder (screen 4.1):
  - routine name, muscle focus, estimated time fields
  - each exercise card: number, name, muscle • equipment, **↑ move up**, **✕ remove**,
    editable **Sets / Reps / Rest** and a **setup note** (e.g. "seat height 4")
  - **+ Add Exercise**: a two-step picker (choose muscle group, then exercise)
  - **Save** (top bar or bottom button) → saves to SQLite and returns to the hub
- **`Data/LiftLifeRepository.cs`** — added `GetRoutineAsync` and
  `ReplaceRoutineExercisesAsync` (saves the exercise list in its current order).
- **`Data/LiftLifeDatabase.cs`** — seeds a second routine (**Pull Day B**) and one completed
  past session so the history card has data on first launch.
- **`Models`** — `RoutineExercise.Position` (1, 2, 3…) and `WorkoutSession.HistoryText`.
- **`MauiProgram.cs`** — registered the new ViewModels, and removed iOS's built-in text-field
  border so the custom rounded input boxes look clean.

### Likely questions & my answers
- **Q: How are edits in the text boxes saved?** The Entries are two-way bound to the
  exercise objects (`TargetSets`, `TargetReps`...). Nothing touches the database until
  **Save**, which validates everything first.
- **Q: How is exercise order stored?** Each `routine_exercises` row has an `OrderIndex`. On
  save, the old rows for that routine are deleted and the list is re-inserted in its current
  order — simple and always correct.
- **Q: Why does the list refresh after saving?** Going back triggers the hub's `OnAppearing`,
  which reloads routines from SQLite.

---

---

## DAY 5 — Active Workout Session, Quick Swap, Form Guide, Workout Summary

**Goal:** The live training experience — log sets in the gym, rest between them, swap
exercises when a station is busy, and see results that feed into nutrition.

### Flow
Workouts tab → **START WORKOUT SESSION** → Active Session (3.1)
→ *Station Busy? Swap* opens Quick Swap (4.3) / *(?) Form Guide* opens Form Guide (4.4)
→ **Finish** → Workout Summary (4.5) → **Done** → back to Workouts (history now says "Today: …").

### Files and what they do
- **`ViewModels/ActiveWorkoutViewModel.cs`** — the session "engine":
  - `SessionExercise` (one exercise with its own sets) and `SessionSet` (one row:
    SET / PREV / KG / REPS / DONE).
  - **Loading:** reads the routine's exercises, and for each one the sets from the **last time
    you did it** (`GetPreviousSetsAsync`). That fills the PREV column and pre-fills the weight;
    reps are pre-filled from the routine target.
  - **Timer:** one `IDispatcherTimer` ticking every second drives both the elapsed clock and
    the **rest countdown**.
  - **Ticking DONE** validates the weight (0–500 kg) and reps (1–100), locks the row, adds to
    the volume total, and starts the rest timer for that exercise's rest time. When rest hits
    zero the phone **vibrates** (`Vibration` API) and gives a **haptic** pulse.
  - **+ Add Set** copies the last row. **+15s / Skip** adjust the rest.
  - **Swap:** replaces the current exercise. Sets already ticked stay logged under the
    original exercise; only the remaining sets move over, and the clock keeps running.
  - **Finish:** checks for **personal records** (compared with older history, before saving),
    saves the session + all ticked sets to SQLite, then triggers the training-day macros.
- **`Views/ActiveWorkoutPage.xaml` / `.cs`** — screen 3.1: Cancel / Finish bar, clock and live
  volume, progress bar, the exercise card, set table, + Add Set, Previous / Next, and a
  **floating rest-timer pill**. Gestures: tap the DONE box, **swipe the card left/right** to
  change exercise. Uses `DeviceDisplay.KeepScreenOn` so the phone doesn't sleep mid-workout.
  Cancel asks before discarding; Finish warns about unticked sets.
- **`ViewModels/QuickSwapViewModel.cs` + `Views/QuickSwapPage`** — screen 4.3: "Target: Chest
  (Same Mechanics)" tag, substitutes from the swap engine with **your PR** and a **Free in Gym**
  tag (best option pre-selected), a **scope switch** (off = this session only, on = update the
  routine permanently), and **Confirm Exercise Swap**.
- **`Views/FormGuidePage`** — screen 4.4: a **looping tempo animation** (lime bar moves down
  2s, pauses, up 1s, counting reps — MAUI `TranslateToAsync` animation), the exercise's
  **key form cues** as a checklist, **equipment-specific safety checks**, and your setup note.
- **`Models/WorkoutSummary.cs` + `Views/WorkoutSummaryPage`** — screen 4.5: total volume,
  duration, sets, exercises, a **Training Day** card showing how the targets changed
  (e.g. Calories 2,082 → 2,450 kcal, Carbs 196 → 280 g), and each exercise with a **NEW PR**
  badge when you beat your record.
- **`Data/LiftLifeRepository.cs`** — added `GetPreviousSetsAsync`, `GetPersonalRecordAsync`,
  `UpdateRoutineExerciseAsync` (permanent swap) and `ActivateTrainingDayAsync`.
- **`Models/Nutrition.cs`** — `UserSettings` now remembers `LastTrainingDate`, so the app knows
  today is a training day.
- **`Data/LiftLifeDatabase.cs`** — seeds two past Push Day A sessions with real sets, so PREV
  values and PRs show on first launch.
- **`Resources/Styles/LiftLifeStyles.xaml`** — the `FieldBox` input style moved here so all
  screens share it.

### Likely questions & my answers
- **Q: How does the rest timer work?** A single 1-second `IDispatcherTimer`. Each tick updates
  the clock; if resting, it counts down. At zero it calls `Vibration.Default.Vibrate` and
  `HapticFeedback`, shows "Rest over", then hides the pill after 3 seconds.
- **Q: Where does PREV come from?** The `set_logs` table: the sets from the most recent session
  that included this exercise.
- **Q: How is a PR detected?** Before saving, the best set (heaviest, then most reps) is compared
  with the best set ever logged for that exercise. Higher weight, or same weight with more reps,
  is a new PR.
- **Q: What does "update routine permanently" do?** It changes the `ExerciseId` in that
  routine's `routine_exercises` row, so next time the routine starts with the substitute.
- **Q: How do workouts affect nutrition?** Finishing saves today's date as `LastTrainingDate`
  and sets the day to Training, which raises calories and carbs back to the full targets.
  The Nutrition tab (Day 6) reads these.
- **Q: What if I cancel?** Nothing is saved — the session is only written to the database on Finish.

---

## Still to come
- **Day 6** — Daily Nutrition Hub, food search/log, custom foods, training vs rest day macros
- **Day 7** — About page, final polish, testing, report
