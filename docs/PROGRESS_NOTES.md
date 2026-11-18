# LiftLife — Explainer Notes (screen by screen)

**Student:** Mohamed Hilman Ashraf Bin Mohamed Ashraf (BSW01085755)
**Course:** CSNB544 Mobile Application Development
**App:** LiftLife — Community Gym Hub & Routine Tracker
**Framework:** .NET MAUI (C# + XAML) • **Database:** SQLite (local, offline-first)
**Primary platform:** iOS (tested on the iPhone simulator and my iPhone 14 Pro)

> These notes follow the app page by page, using the numbers from my Section A screens map.
> For each screen: what it shows, which files make it work, and the questions I expect.
> How AI was used to build the app is explained in `docs/AI_USE.md`.

---

## Contents
- [Setup: why .NET MAUI + VS Code on a Mac](#setup-why-net-maui--vs-code-on-a-mac)
- [How the app is structured](#how-the-app-is-structured)
- [Data layer (SQLite)](#data-layer-sqlite)
- [1.0 Splash screen, icon and navigation](#10-splash-screen-icon-and-navigation)
- [2.1 Community Station Hub](#21-community-station-hub)
- [3.1 Report Status](#31-report-status)
- [Station Log (maintenance history + queue)](#station-log-maintenance-history--queue)
- [2.2 Workout Routine Hub](#22-workout-routine-hub)
- [4.1 Routine Builder](#41-routine-builder)
- [4.2 Active Workout Session](#42-active-workout-session)
- [4.3 Quick Swap](#43-quick-swap)
- [4.4 Form Guide](#44-form-guide)
- [4.5 Workout Summary](#45-workout-summary)
- [2.3 Daily Nutrition Hub](#23-daily-nutrition-hub)
- [5.1 / 5.2 Add Food: Search and Quick Add](#51--52-add-food-search-and-quick-add)
- [6.2 Macro Targets](#62-macro-targets)
- [6.1 About LiftLife (2.4 Profile & Settings)](#61-about-liftlife-24-profile--settings)
- [Images and credits](#images-and-credits)
- [Running on the simulator and my iPhone](#running-on-the-simulator-and-my-iphone)
- [Changes from my M2 design](#changes-from-my-m2-design)
- [Possible future work](#possible-future-work)

---

## Setup: why .NET MAUI + VS Code on a Mac

The brief asks for .NET MAUI with Visual Studio. Microsoft retired **Visual Studio for Mac** on
**31 August 2024**. Its official replacement on a Mac is **Visual Studio Code with the C# Dev Kit and
.NET MAUI extensions**, which is what this project uses.

- .NET MAUI on .NET 10, built with the .NET SDK and Xcode
- SQLite through the `sqlite-net-pcl` library
- iOS is the main target because I use an iPhone. Android and Mac Catalyst are still in the project.
- Source control: Git, pushed to `https://github.com/HilmanAshraf/Liftlife-MAD`. `bin/`, `obj/` and
  the database file are excluded in `.gitignore`.

Two build fixes worth knowing:
- The iOS workload expected an exact Xcode version and my Mac had a newer minor version.
  `<ValidateXcodeVersion>false</ValidateXcodeVersion>` in `LiftLife.csproj` skips that strict check.
- The logo SVGs first used `<text>` for the "L". MAUI's image tool can't render text, so the "L" was
  redrawn as vector paths.

---

## How the app is structured

- **`App.xaml` / `App.xaml.cs`** — app entry point. Loads the colours and styles, forces dark mode
  (`UserAppTheme="Dark"`), and opens the splash page first.
- **`AppShell.xaml`** — the bottom tab bar: Community, Workouts, Nutrition, About.
- **`Views/`** — the screens. Each is a `.xaml` layout plus a `.xaml.cs` code-behind.
- **`ViewModels/`** — each screen's data and logic (MVVM).
- **`Models/`** — C# classes for the data. Most are SQLite tables.
- **`Data/`** — `LiftLifeDatabase` (connection, tables, seed data) and `LiftLifeRepository` (all queries).
- **`Services/`** — small helpers (rest alert, images, number input, time labels, member name).
- **`Converters/`** — XAML value converters (station status → badge colour).
- **`Resources/`** — styles, colours, fonts, app icon, splash, images.
- **`MauiProgram.cs`** — startup and dependency injection.

### MVVM (how every feature screen works)
- **View** (`Views/XxxPage.xaml`) — the layout. It shows data with `{Binding ...}`.
- **ViewModel** (`ViewModels/XxxViewModel.cs`) — holds the data and logic. It inherits
  `BaseViewModel`, which implements `INotifyPropertyChanged`. `SetProperty` raises `PropertyChanged`
  only when a value really changes, so the screen updates by itself. Lists use
  `ObservableCollection`, so adding or removing an item updates the list.
- **Code-behind** (`Views/XxxPage.xaml.cs`) — handles things that need the page: alerts, prompts and
  navigation. Then it calls the ViewModel.

The database and repository are registered as **singletons** in `MauiProgram.cs` (one shared copy).
ViewModels are **transient** (a fresh one per screen). `Services/ServiceHelper.cs` fetches them.

### Styles
- **`Resources/Styles/Colors.xaml`** — lime accent `#E9F558` (from the logo), near-black background
  `#0B0B0F`, dark cards, status colours (green free, red busy, grey out of order), macro colours.
- **`Resources/Styles/LiftLifeStyles.xaml`** — shared styles: `Card`, `ScreenTitle`, `CardTitle`,
  `PrimaryButton`, `DarkButton`, `OutlineButton`, `FieldBox`, text styles.
- `MauiProgram.cs` removes the iOS native text-field border so the rounded input boxes don't get a
  double border.

---

## Data layer (SQLite)

### Tables (`Models/`, each marked `[Table("...")]`)
| Table | Model | What it holds |
|---|---|---|
| `stations` | `Station` | gym machine, category, status, note, reported by/at, `ClaimedByMe` |
| `station_reports` | `StationReport` | every status change (the maintenance log) |
| `station_queue` | `StationQueueEntry` | members waiting for a busy machine |
| `exercises` | `Exercise` | exercise catalogue with **muscle group** and **equipment** |
| `routines` / `routine_exercises` | `Routine`, `RoutineExercise` | saved routines and their exercises (sets, reps, rest, setup note, order) |
| `workout_sessions` / `set_logs` | `WorkoutSession`, `SetLog` | finished workouts and each set (kg × reps) |
| `foods` / `meal_logs` | `FoodItem`, `MealLog` | food list and foods logged per date and meal |
| `user_settings` | `UserSettings` | macro targets, rest-day factors, member name, last training date |

`Enums.cs` holds `StationStatus`, `DayType` and `MealCategory`.

### `Data/LiftLifeDatabase.cs`
- Opens `liftlife.db3` in `FileSystem.AppDataDirectory` (the app's private storage).
- `GetConnectionAsync` creates the connection the first time it's needed, runs `CreateTableAsync<T>()`
  for every model (this also adds new columns to older databases), then seeds sample data.
- A `SemaphoreSlim` lock makes sure two screens loading at once can't create or seed the database twice.
- Seed methods check first, so sample data is only added once: exercises, 7 stations, station report
  history, queues, two routines (Push Day A, Pull Day B), past workouts with real sets, foods, meals
  for today and yesterday. `SeedLocalFoodsAsync` adds any food name that's missing, so new foods also
  reach older databases.
- `ResetAsync` closes the connection and deletes the file. The next query rebuilds everything.

### `Data/LiftLifeRepository.cs`
All data operations, so pages never write SQL themselves. Each screen section below names the methods it uses.

### Likely questions
- **Where is data stored?** In the SQLite file `liftlife.db3` in the app's private storage. It works
  offline and survives restarts.
- **Why a repository?** All data logic is in one place, pages stay simple, and it's easy to explain.
- **How can you prove it's saved?** Open the file in DB Browser for SQLite and show the rows, or show the
  live row counts on the About page.

---

## 1.0 Splash screen, icon and navigation

**What it shows:** the lime "L" squircle logo, "LiftLife", "Community Gym Hub" and the slogan
"Smart Community & Digital Innovation". After about 2 seconds it opens the tabbed app.

**Files**
- **`Resources/AppIcon/appicon.svg` + `appiconfg.svg`** — the app icon (dark background, lime "L").
  MAUI's Resizetizer makes every icon size iOS needs.
- **`Resources/Splash/splash.svg`** — the launch image iOS shows first. Same logo as the icon.
- **`Resources/Images/logo.svg`** — the same logo, shown on the in-app splash page.
- **`Views/SplashPage.xaml` / `.cs`** — the splash screen. `OnAppearing` waits with
  `await Task.Delay(2000)`, then sets `Application.Current.Windows[0].Page = new AppShell()`.
- **`AppShell.xaml`** — a `TabBar` with four `ShellContent` tabs and the icons in
  `Resources/Images/tab_*.svg`. The selected tab is lime.

**Navigation between screens:** sub-screens open with `Navigation.PushAsync` (full pages such as
Routine Builder and Station Log) or `PushModalAsync` (sheets such as Report Status, Quick Swap,
Form Guide and Add Food).

**Likely questions**
- **How does the splash move on by itself?** A 2-second `Task.Delay` in `OnAppearing`, then the window's
  page is replaced with `AppShell`.
- **Why a dark theme?** High contrast for quick glances in a gym, and it matches the lime logo.

---

## 2.1 Community Station Hub

**What it shows**
- Top bar: "LiftLife" and "📍 UNITEN Gymnasium".
- **Gym density banner** on the UNITEN Gymnasium photo, e.g. "QUIET (33%) • 2 of 6 stations in use •
  3 members in queues", with a progress bar. Density = busy ÷ working stations (broken ones excluded).
  A dark `LinearGradientBrush` over the photo keeps the text readable.
- **Filter chips:** All / Benches / Cables / Machines / Squat Racks.
- **Station cards:** name, who reported it and when, a coloured status badge.
- The main button depends on the status:
  - FREE → **Claim Station** (asks for your member name the first time)
  - claimed by you → **I'm Done • Release**
  - BUSY → **Station Busy? Swap** and **Join Queue** / **Leave Queue**
  - OUT OF ORDER → **Find Alternative**
- **Update Status** opens the report sheet. Tapping the top of a card opens the Station Log.
- **Pull down to refresh.**
- A Busy report older than 45 minutes shows **BUSY?** and "may be free now", so old reports don't block people.

**Machine queues**
- A busy card shows "👥 2 waiting • about 24 min if you join". Joining shows "You're #3, about 24 min".
- When the machine is free, the card shows "👥 Next up: Imran". If you claim it while someone else is
  next, the app asks you to confirm first. Claiming removes you from that queue.
- Out of Order clears the queue. Entries expire after 2 hours.

**Files**
- **`ViewModels/CommunityViewModel.cs`** — loads and filters stations, works out density, claim and
  release, `ToggleQueueAsync`. Also contains `CategoryChip` (one filter pill with `IsSelected`).
- **`Views/CommunityPage.xaml` / `.cs`** — the layout, plus the dialogs and navigation.
- **`Models/Station.cs`** — display helpers: `StatusLabel`, `DetailLine`, `PrimaryActionText`, `IsStale`
  (`StaleAfterMinutes = 45`), queue text (`MinutesPerMember = 8`).
- **`Models/StationQueueEntry.cs`** — station, member, join time, `IsMe` (`ExpireAfterMinutes = 120`).
- **`Converters/StatusConverters.cs`** — status → badge colour and text colour.
- **`Services/MemberNamePrompt.cs`** — asks for your name once and saves it in `UserSettings.MemberName`.
- **Repository:** `GetStationsAsync` (with category filter), density calculation,
  `UpdateStationStatusAsync`, `GetAlternativesForStationAsync`, `JoinQueueAsync`, `LeaveQueueAsync`,
  `GetQueueAsync`, `GetWaitingCountAsync`.

**The swap engine (key idea)**
Every exercise has a `MuscleGroup` and an `Equipment` type. For a busy station, the repository finds other
exercises for the **same muscle group** that use **different equipment**, then checks the live station
statuses. If a station for that equipment is free, the option is tagged "Free in Gym". So the suggestion
trains the same muscle and uses equipment that's actually available. It all runs locally in SQLite.

**Likely questions**
- **How does the badge change colour?** `BackgroundColor` is bound to `Status` through
  `StatusToColorConverter`, which returns green, red or grey.
- **How does the filter work?** Tapping a chip calls `SelectCategoryAsync`. A `DataTrigger` turns the
  selected chip lime, and the stations reload with `WHERE Category = ?`.
- **How is the queue order decided?** First come, first served: sorted by `JoinedAt`, position = index + 1.
- **How is the wait estimated?** People ahead of you × 8 minutes.
- **How does Release know it's my station?** `ClaimedByMe` is set only when you claim. Any other update sets it back to false.
- **Is the data shared between phones?** Not yet. It's an offline prototype on one device. The report
  structure (who, what, when) is ready to sync to an online database later.

---

## 3.1 Report Status

**What it shows:** close (✕), the station name, three options (Available / Busy / Out of Order, the
selected one gets a lime border), a note field and **Submit**.

**Files:** `Views/ReportStatusPage.xaml` / `.cs`. Saving calls `UpdateStationStatusAsync`.

**Rules**
- **Out of Order needs a note** (e.g. "Pulley cable snapped"). Submitting without one shows an alert.
- Saving updates the station row, **adds a row to `station_reports`**, clears the queue if the machine is
  broken, gives a haptic tap and refreshes the hub.

**Likely question:** **What happens when I submit?** `UpdateStationStatusAsync` updates the station (status,
note, reported by, time) and inserts a report row in the same method. The hub then reloads from SQLite.

---

## Station Log (maintenance history + queue)

Added after M2 to complete the "Maintenance Log" feature from my M1 proposal.

**What it shows:** current status, "3 reports in the last 30 days • 2 fault reports", **Update Status**,
the queue in order with join times plus Join/Leave, and the report history (newest first, coloured dots).
Example from the sample data: Squat Rack #1 was busy, then reported broken by Aina, then confirmed by Staff.

**Files:** `Views/StationDetailPage`, `ViewModels/StationDetailViewModel.cs`, `Models/StationReport.cs`.

**Likely question:** **Where is the maintenance log stored?** In `station_reports`. Every status change
(report, claim, release) adds a row, so the history is kept instead of overwritten.

---

## 2.2 Workout Routine Hub

**What it shows**
- "Workout Routines" and a **+ New** button.
- **Last session card**: "Yesterday: Pull Day B • 5,420 kg logged" (from `workout_sessions`).
- **Routine cards**: name, muscle focus, "5 exercises • Estimated: 50 mins", **START WORKOUT SESSION** and **Edit**.
- **Swipe left to delete** a routine (SwipeView), with a confirmation dialog. Pull to refresh.

**Files:** `ViewModels/WorkoutsViewModel.cs`, `Views/WorkoutsPage.xaml` / `.cs`,
`WorkoutSession.HistoryText`.

**Likely question:** **Why does the list update after saving a routine?** Coming back runs the hub's
`OnAppearing`, which reloads routines from SQLite.

---

## 4.1 Routine Builder

**What it shows:** routine name, muscle focus and estimated time. Each exercise card has its number,
name, muscle • equipment, **↑ move up**, **✕ remove**, editable **Sets / Reps / Rest** and a **setup
note** (e.g. "seat height 4"). **+ Add Exercise** is a two-step picker (muscle group, then exercise).
**Save** is in the top bar and at the bottom.

**Files:** `ViewModels/RoutineBuilderViewModel.cs`, `Views/RoutineBuilderPage.xaml` / `.cs`.
Repository: `GetRoutineAsync`, `ReplaceRoutineExercisesAsync`.

**Validation before saving:** name required, time 5–240 min, at least one exercise, sets 1–10,
reps 1–50, rest 0–600 s.

**Likely questions**
- **How are edits saved?** The Entries are two-way bound to the exercise objects. Nothing is written
  until **Save**, which validates first.
- **How is the order stored?** Each `routine_exercises` row has an `OrderIndex`. On save, the routine's
  old rows are deleted and the list is inserted again in its current order.

---

## 4.2 Active Workout Session

**Flow:** Workouts → **START WORKOUT SESSION** → this screen → **Finish** → Workout Summary.

**What it shows**
- **Cancel** / **Finish** bar.
- Two large stat boxes: **WORKOUT TIME** (lime, 32 pt bold) and **TOTAL VOLUME**.
- A progress bar and the exercise card: "1/5: Barbell Bench Press", **(?) Form Guide**,
  **Station Busy? Swap**, and a **⏱ Rest 1:30** button.
- Set table: SET / PREV / KG / REPS / DONE. PREV comes from the last time you did the exercise. KG is
  pre-filled from it and REPS from the routine target. **+ Add Set** copies the last row.
- **Previous / Next**, or **swipe the card left/right** to change exercise.
- A floating **rest bar** with **−15s / +15s / Skip**. When rest ends it turns green and says
  "Rest over — next set!".

**How it works (`ViewModels/ActiveWorkoutViewModel.cs`)**
- `SessionExercise` is one exercise with its sets. `SessionSet` is one row.
- Loading reads the routine's exercises and, for each, the sets from the last session that included it
  (`GetPreviousSetsAsync`).
- **One timer:** an `IDispatcherTimer` ticking every second drives the workout clock and the rest countdown.
- **Ticking DONE** checks weight (0–500 kg) and reps (1–100), locks the row, adds to total volume and
  starts the rest countdown for that exercise.
- **Rest controls:** `AdjustRestTime` (±15 s; reaching zero ends the rest) and `SkipRest`.
- **Custom rest per exercise:** the ⏱ button offers 30 s–5 min or a typed time ("75", "1:45", "2m",
  parsed by `ParseRestInput`, 10–600 s). `SetRestForCurrentAsync` saves it to that exercise in the
  routine (`UpdateRoutineExerciseRestAsync`), so the next session remembers it.
- **Rest alert:** at zero, `Services/RestAlert.cs` plays **three 0.45 s buzzes at full intensity** with
  iOS **Core Haptics** (`CHHapticEngine`). It works in silent mode. Other platforms fall back to
  `Vibration`. The "Rest over" bar stays for 3 seconds (`RestDoneHoldSeconds`).
- **Swap:** sets already ticked stay logged under the original exercise. Only the remaining sets move to
  the substitute, and the clock keeps running.
- **Finish** (`FinishAsync`): warns about unticked sets, checks for **personal records** against older
  history, saves the session and all ticked sets, and calls `ActivateTrainingDayAsync`.
- **Cancel** asks first, and nothing is saved.
- `DeviceDisplay.KeepScreenOn` stops the phone sleeping during a workout.

**Files:** `ViewModels/ActiveWorkoutViewModel.cs`, `Views/ActiveWorkoutPage.xaml` / `.cs`,
`Services/RestAlert.cs`. Repository: `GetPreviousSetsAsync`, `GetPersonalRecordAsync`,
`UpdateRoutineExerciseRestAsync`, `ActivateTrainingDayAsync`.

**Likely questions**
- **How does the rest timer work?** One 1-second `IDispatcherTimer`. Each tick updates the clock and,
  if resting, counts down. At zero it calls `RestAlert`.
- **Why Core Haptics instead of `Vibration.Vibrate`?** On my iPhone, `Vibration.Vibrate` was one light
  system buzz that's easy to miss and can be muted. Core Haptics lets me design a stronger pattern and
  isn't silenced by the mute switch.
- **Does the alert work with the app closed?** No. The timer runs inside the app, so it only fires while
  LiftLife is open.
- **Where does PREV come from?** `set_logs`: the sets from the most recent session that had this exercise.
- **How is a PR detected?** Before saving, the best set (heaviest, then most reps) is compared with the
  best set ever logged for that exercise. Higher weight, or the same weight with more reps, is a PR.

---

## 4.3 Quick Swap

**What it shows:** "Target: Chest (Same Mechanics)", substitute exercises with **your PR** and a
**Free in Gym** tag (best option pre-selected), a **scope switch** (off = this session only, on = update
the routine permanently) and **Confirm Exercise Swap**.

**Files:** `ViewModels/QuickSwapViewModel.cs`, `Views/QuickSwapPage`. Repository: `GetSwapOptionsAsync`,
`UpdateRoutineExerciseAsync`.

**Likely question:** **What does "update routine permanently" do?** It changes the `ExerciseId` in that
routine's `routine_exercises` row, so the routine starts with the substitute next time.

---

## 4.4 Form Guide

**What it shows**
- A **photo for the exercise's muscle group** (chest, back, legs, shoulders, biceps, triceps) with the
  muscle and equipment label over it.
- A **looping tempo animation**: a lime bar moves down for 2 s, pauses, then up for 1 s, counting reps.
- The exercise's **key form cues** as a checklist, **equipment safety checks**, and your setup note.

**Files:** `Views/FormGuidePage.xaml` / `.cs`, `Services/MuscleImages.cs`.

**Likely questions**
- **How is the photo chosen?** `MuscleImages.For(exercise.MuscleGroup)` looks up the muscle group in a
  dictionary and returns the file name. Unknown groups fall back to the gym photo.
- **How is the animation done?** `LoopTempoAsync` calls `TempoBar.TranslateToAsync` (MAUI animation API)
  in a loop, and `CancelAnimations` stops it when the page closes.

---

## 4.5 Workout Summary

**What it shows:** total volume, duration, sets and exercises, a **Training Day** card showing how the
targets changed (e.g. Calories 2,082 → 2,450 kcal, Carbs 196 → 280 g), and each exercise with a
**NEW PR** badge when you beat your record. **DONE** goes back to the hub, which now says "Today: …".

**Files:** `Models/WorkoutSummary.cs`, `Views/WorkoutSummaryPage`.

**Likely question:** **Why does Back not return to the finished workout?** The summary replaces the session
page (`Navigation.RemovePage`), so Back goes to the Routine Hub.

---

## 2.3 Daily Nutrition Hub

**The adaptive logic (key idea)**
- A date is a **Training Day** if a workout was completed on it (`IsTrainingDayAsync` checks
  `workout_sessions`). Otherwise it's a **Rest Day**.
- `UserSettings.TargetsFor(isTrainingDay)` in `Models/Nutrition.cs`:
  - Training day → 2,450 kcal, protein 185 g, carbs 280 g, fat 65 g
  - Rest day → calories × 0.85 and carbs × 0.70 (2,082 kcal, 196 g carbs). Protein and fat stay the same.
- Finishing a workout flips today's badge to **⚡ TRAINING DAY ACTIVE** and raises the targets. No manual switch.

**What it shows**
- **‹ Today ›** date switcher (no future dates). Swiping the summary card also changes day.
- Day badge (lime = Training, blue = Rest) and **Edit targets ›**.
- Calorie meter and "kcal remaining" (red when over).
- Protein / Carbs / Fat bars.
- Meal cards (Breakfast, Lunch, Dinner, Snacks). **Tap** a food to change the amount, **swipe left** to
  delete, **+ Add Food** to open the Add Food sheet for that meal.

**Files:** `ViewModels/NutritionViewModel.cs` (with `MacroProgress` and `MealSection`),
`Views/NutritionPage`. Repository: `IsTrainingDayAsync`, `UpdateMealQuantityAsync`.

**Likely questions**
- **How does the app know it's a training day?** It checks SQLite for a workout finished on that date.
- **Why only cut calories and carbs on rest days?** Carbs fuel training. Protein and fat stay the same to
  support recovery.

---

## 5.1 / 5.2 Add Food: Search and Quick Add

In M2 these were two screens (Food Search, Custom Food Creator). In the app they're **two tabs on one
sheet**, which is quicker to use.

**🔍 Search Foods (5.1):** meal chips, **search as you type** over the local food list, tap to select
(lime border), a servings **Stepper** (0.5–20) with a live kcal/macro preview, **Log Food**. If nothing
matches, a **Quick Add it** button fills in the name you searched.

**✏️ Quick Add (5.2):** type the food name, calories per serving, serving (e.g. "1 plate"), optional
macros, and servings eaten, then **Log Food**. **Save to My Foods** (on by default) also adds it to the
food list. Switch it off for a one-off meal. It also shows whether the macros add up (4/4/9 kcal per gram).

The food list includes **17 Malaysian campus foods** (nasi goreng, chicken rice, roti canai, teh tarik,
milo ais, char kuey teow…).

**Files:** `ViewModels/FoodLogViewModel.cs` (`IsQuickAdd`), `ViewModels/CustomFoodViewModel.cs`
(`LogAsync`), `Views/FoodLogPage`, `Services/NumberInput.cs` (reads "80,5" too).
Repository: `LogFoodAsync`, `LogQuickFoodAsync`, `FoodNameExistsAsync`.

**Likely questions**
- **What's stored when I log food?** A `meal_logs` row: date, meal, food, servings, serving text, and
  calories/macros already multiplied by servings (`MealLog.From`). Editing the food later doesn't change past days.
- **What happens to a one-off Quick Add?** It's stored only in `meal_logs` with `FoodId = 0`, so it counts
  for that day but doesn't clutter the food list.
- **How is the macro check done?** protein × 4 + carbs × 4 + fat × 9. If that's more than 20% away from
  the calories typed in, a warning shows.

---

## 6.2 Macro Targets

**What it shows:** baseline calories and macros, rest-day calorie % and carb % steppers, a macro sanity
check, a live **preview of both day types**, **Save** (with validation) and **Reset to Defaults**.
Opened from Nutrition (**Edit targets ›**) or the About page.

**Files:** `ViewModels/TargetSetupViewModel.cs`, `Views/TargetSetupPage`, `Models/Nutrition.cs`.

---

## 6.1 About LiftLife (2.4 Profile & Settings)

**What it shows**
- **Release card:** logo, name, tagline, **version from the `AppInfo` API** ("Version 1.0.0 (Build 1)"),
  course theme, and **device info from `DeviceInfo`** ("Running on iOS • Phone").
- **Developer details:** name, student ID, programme, university, course.
- **Application overview** and key features.
- **Local storage:** the SQLite file name and size, and **live row counts per table** (`GetStorageStatsAsync`).
- **Profile & settings:** member name (Change), **Macro Targets ›**.
- **Credits:** Pexels photos and the UNITEN Gymnasium photo.
- **Reset Demo Data:** deletes the database (after confirming) and recreates the sample data.

**Files:** `Views/AboutPage`, `ViewModels/AboutViewModel.cs`.

**Likely questions**
- **How is the version shown?** `AppInfo.Current.VersionString` and `BuildString`, which come from
  `ApplicationDisplayVersion` and `ApplicationVersion` in `LiftLife.csproj`.
- **What does Reset Demo Data do?** `ResetAsync` closes the connection and deletes `liftlife.db3`. The next
  query recreates the tables and seed data.

---

## Images and credits

- **6 Pexels photos** in `Resources/Images/photo_{chest,back,legs,shoulders,biceps,triceps}.jpg`, cropped to
  16:9 and compressed. Used in the Form Guide. Pexels Licence allows free use in apps.
- **`photo_uniten_gym.jpg`** — the UNITEN Gymnasium, © Universiti Tenaga Nasional (UNITEN). Used on the
  Community density banner.
- `LiftLife.csproj` has `<MauiImage Update="Resources\Images\photo_*.jpg" Resize="false" />` so MAUI copies
  the photos as they are.
- Full sources: `docs/MEDIA_CREDITS.md`. Credits also on the About page.

---

## Running on the simulator and my iPhone

- **`./run-sim.sh`** — builds, reinstalls and reopens the app on the iOS simulator, keeping its data.
  A small change takes about 10 seconds.
- **`./run-iphone.sh`** — finds the connected iPhone (`xcrun devicectl list devices`), builds for the
  device (`-p:RuntimeIdentifier=ios-arm64 -p:CodesignKey="Apple Development"`), installs and launches it.
- **Signing:** a free Apple ID in Xcode created an "Apple Development" certificate and a provisioning
  profile for `com.hilmanashraf.liftlife` that includes my phone. The .NET build uses the same certificate.
  Signing settings are passed on the command line, not saved in the `.csproj`, because the certificate
  name contains my email.
- **Free profiles expire after 7 days.** To renew: open the Xcode test project, pick the iPhone, press Run
  once, then `./run-iphone.sh`.
- The phone has its own database, separate from the simulator. Developer Mode must stay on.

**Why test on the phone:** the simulator can't play haptics or keep the screen on. These device APIs only
really work on the iPhone.

---

## Changes from my M2 design

| M2 design | In the app | Why |
|---|---|---|
| White wireframes | Dark theme with lime accent | Easier to read in a gym, matches the logo |
| "📍 Campus Gym" | "📍 UNITEN Gymnasium" + real gym photo | Made it specific to UNITEN |
| Looping MP4 form videos | Muscle-group photo + tempo animation + form cues | Free video sources didn't fit the licence or app size; photos and the animation show the same guidance |
| Room/SQLite | SQLite only | Room is an Android library. MAUI uses SQLite through `sqlite-net-pcl` |
| Separate Custom Food Creator (5.2) | Quick Add tab on the Add Food sheet | Fewer steps, and one-off meals don't fill the food list |
| — | Station Log, machine queues | Completes the maintenance-log feature and handles busy machines |
| — | Rest −15s / +15s / Skip, custom rest per exercise, Core Haptics alert | From testing on my iPhone |

---

## Possible future work
- Sync stations, reports and queues through an online database (e.g. Firebase) so all members share live status
- A notification when rest ends while the app is in the background
- Real exercise videos in the Form Guide
- Charts of volume and nutrition over time
