# LiftLife — Mock Q&A for the presentation

**How to use this:** read a question, answer it **out loud in your own words** before reading the
model answer, then open the file it points to and find the code. The rubric's top band for
"Code Understanding & AI Defense" (8–10 marks) needs you to explain the architecture and logic,
explain your AI prompt choices, and **make live changes on your own**. Part 4 is practice for that.

Files are under `LiftLife/` unless stated.

---

## Part 1 — Your 1-minute opening

> "LiftLife is a .NET MAUI app for shared campus gyms, built for UNITEN Gymnasium under the Smart
> Community theme. It solves three problems: not knowing which machines are free or broken, losing
> time when a machine is busy, and eating the same on training and rest days. Members report live
> machine status and queue for busy machines. When a station is busy, a swap engine suggests an
> exercise for the same muscle using equipment that's free. The workout tracker logs sets with a rest
> timer, and finishing a workout automatically switches the nutrition targets to a training day.
> Everything is stored offline in SQLite, using an MVVM structure with data binding."

---

## Part 2 — Questions by rubric criterion

### Icon, splash screen, About page

**Q: How does the splash screen work?**
Two steps. iOS first shows the launch image from `Resources/Splash/splash.svg`. Then
`App.CreateWindow` opens `Views/SplashPage`. In `SplashPage.OnAppearing`, `await Task.Delay(2000)`
waits 2 seconds, then sets `Application.Current.Windows[0].Page = new AppShell()`, which swaps the
whole window to the tabbed app.

**Q: Where does the icon come from?**
`Resources/AppIcon/appicon.svg` (background) and `appiconfg.svg` (the lime "L"). MAUI's build
tool (Resizetizer) turns them into every icon size iOS needs. They're referenced in
`LiftLife.csproj` as `<MauiIcon>`.

**Q: How does the About page know the version?**
`AboutViewModel.VersionText` uses the MAUI `AppInfo` API: `AppInfo.Current.VersionString` and
`BuildString`, which come from `ApplicationDisplayVersion` (1.0.0) and `ApplicationVersion` (1) in
the `.csproj`. The device line uses `DeviceInfo.Current`.

### Activities completion / navigation

**Q: Does the app follow your screens map?**
Yes. Splash → bottom tabs (Community, Workouts, Nutrition, About) → each tab's sub-screens: Report
Status, Routine Builder, Active Session, Quick Swap, Form Guide, Workout Summary, Add Food (Search +
Quick Add), Macro Targets. I added a few screens after testing: **Station Log** (maintenance
history + queue), **machine queues** and **Quick Add**. They're in my updated Section A screens map.

**Q: How is navigation built?**
`AppShell.xaml` holds a `TabBar` with four `ShellContent` tabs. Sub-screens are pushed with
`Navigation.PushAsync` (full pages, e.g. Routine Builder, Station Log) or `PushModalAsync` (sheets,
e.g. Report Status, Quick Swap, Form Guide, Add Food). After Finish, the summary replaces the session
page (`Navigation.RemovePage`), so Back goes to the Routine Hub, not the finished workout.

### Content / layout / design

**Q: Why a dark theme when your wireframes were white?**
Gym lighting and quick glances between sets. A dark background with a lime accent (the logo colour
from my splash wireframe) gives high contrast. The layouts follow the wireframes. All colours live
in `Resources/Styles/Colors.xaml`, and the shared styles (`Card`, `PrimaryButton`…) in
`LiftLifeStyles.xaml`, so every screen is consistent.

**Q: Where are the images from?**
Exercise photos are free Pexels photos (Pexels Licence). The gym photo is © UNITEN. Sources are in
`docs/MEDIA_CREDITS.md` and the About page. `Services/MuscleImages.For(muscleGroup)` picks the
Form Guide photo for each muscle group.

### Gestures and .NET MAUI APIs

**Q: Which gestures did you use?**
- **Tap** (`TapGestureRecognizer`): filter chips, DONE boxes, report options, station cards, date arrows, meal items
- **Swipe** (`SwipeGestureRecognizer`): change exercise in a session, change day in Nutrition
- **Swipe to delete** (`SwipeView`): routines, logged meals
- **Pull to refresh** (`RefreshView`): Community and Workouts

**Q: Which MAUI APIs?**
- **Core Haptics / `Vibration`:** the rest-over alert (`Services/RestAlert.cs`)
- **`HapticFeedback`:** taps on Done, Claim and Save
- **`DeviceDisplay.KeepScreenOn`:** keeps the phone awake during a workout
- **`AppInfo` / `DeviceInfo`:** the About page
- **`FileSystem.AppDataDirectory`:** where the database file lives
- **Dispatcher timer** (`Dispatcher.CreateTimer`): the workout clock and rest countdown
- **Animation** (`TranslateToAsync`): the tempo bar in the Form Guide
- **Dialogs:** `DisplayAlertAsync`, `DisplayActionSheetAsync`, `DisplayPromptAsync`

**Q: Why Core Haptics for the rest alert instead of `Vibration.Vibrate`?**
On my iPhone, `Vibration.Vibrate` only gave a light tap that's easy to miss and can be muted.
Core Haptics (`CHHapticEngine`) lets me design a pattern (three 0.45-second buzzes at full intensity)
and it still works in silent mode. Other platforms fall back to `Vibration`.

### Storage and data binding

**Q: What storage did you use and why?**
Local SQLite through the `sqlite-net-pcl` library. The file is `liftlife.db3` in
`FileSystem.AppDataDirectory`. It works offline, which suits a gym with weak signal, and it's what the
brief allows for local storage.

**Q: What tables are there?**
`stations`, `station_reports` (maintenance log), `station_queue`, `exercises`, `routines`,
`routine_exercises`, `workout_sessions`, `set_logs`, `foods`, `meal_logs`, `user_settings`.
Each is a C# class in `Models/` marked with `[Table("...")]`.

**Q: How is the database created?**
`LiftLifeDatabase.GetConnectionAsync()` opens the connection the first time it's needed, calls
`CreateTableAsync<T>()` for every model (this also adds new columns to old databases), then runs the
seed methods. Each seed method checks first (e.g. "is the table empty?"), so sample data is only
added once.

**Q: How is the data organised in code?**
- **Models** (`Models/`) describe the data.
- **`LiftLifeDatabase`** handles the connection, tables and seed data.
- **`LiftLifeRepository`** holds every query, so pages never write SQL.
- **ViewModels** call the repository and expose data to the screens.
- **Views** only display and bind.

**Q: Explain data binding in your app.**
Every ViewModel inherits `BaseViewModel`, which implements `INotifyPropertyChanged`. Its
`SetProperty` method changes a value and raises `PropertyChanged`, so any XAML element bound to that
property (`Text="{Binding ElapsedText}"`) updates by itself. Lists use `ObservableCollection`, so
adding or removing items updates the screen. Entries use two-way binding, so typing updates the
ViewModel. I also set `x:DataType` on pages, which makes the bindings compiled and checked at build time.

**Q: What are the converters and triggers for?**
`Converters/StatusConverters.cs` turns a `StationStatus` into a badge colour (green, red or grey).
`DataTrigger`s change looks based on data, e.g. a filter chip turns lime when `IsSelected` is true,
and the rest bar turns green when `IsRestOver` is true.

**Q: Is your SQL safe from injection?**
Yes. The LINQ queries (`Table<T>().Where(...)`) are turned into parameterised SQL by sqlite-net, and
the few raw statements use `?` placeholders, e.g.
`ExecuteAsync("DELETE FROM station_queue WHERE StationId = ?", stationId)`. I never build SQL by
joining strings.

### Code understanding — logic questions

**Q: How does the swap engine work?** (`LiftLifeRepository.GetSwapOptionsAsync`)
It loads the current exercise, then finds every other exercise with the **same `MuscleGroup`**. For
each one, `IsEquipmentFree` checks the live stations: if a free station for that muscle uses that
equipment, it's "Free in Gym". Dumbbells and machines are assumed available, since gyms have several.
Free options are sorted first and the best one is pre-selected. *Honest limitation:* equipment is
matched by the station's name (e.g. "Cable" in "Cable Crossover #1"), which is a simplification.

**Q: How does crowd density work?** (`GetCrowdDensityAsync`)
Busy stations ÷ working stations (out-of-order ones are excluded), as a percentage. 70%+ = BUSY,
40%+ = MODERATE, otherwise QUIET. The banner also adds how many members are in queues.

**Q: How does the queue work?** (`Models/StationQueueEntry.cs`, repository "Station queues" section)
Each entry stores the station, member name, join time, and `IsMe` for this phone's member. The order
is first come, first served (sorted by `JoinedAt`), and your position is index + 1. Wait estimate =
people ahead × 8 minutes (`Station.MinutesPerMember`). Claiming removes you from the queue. An
out-of-order report clears it. Entries expire after 120 minutes (`ExpireAfterMinutes`). If someone
else is next and you try to claim, the app asks you to confirm first.

**Q: How do training and rest days work?** (`IsTrainingDayAsync`, `UserSettings.TargetsFor`)
A date is a Training Day if a workout was **completed** on it (checked in `workout_sessions`).
`TargetsFor(true)` gives the full 2,450 kcal and 280 g carbs. A rest day multiplies calories by 0.85
and carbs by 0.70. Protein and fat stay the same to protect recovery.

**Q: How is a personal record detected?** (`ActiveWorkoutViewModel.FinishAsync`)
**Before** saving, for each exercise I take the best set (heaviest, then most reps) and compare it with
`GetPersonalRecordAsync`, the best set ever logged. Higher weight, or the same weight with more reps,
is a new PR. It has to happen before saving, otherwise it would compare against itself.

**Q: Where does the PREV column come from?** (`GetPreviousSetsAsync`)
`set_logs` rows for that exercise from the most recent session that included it. They also pre-fill
the weight box.

**Q: How does the rest timer work?**
One `IDispatcherTimer` ticks every second. `Tick()` updates the workout clock, and if resting, counts
`_restRemaining` down. At 0 it calls `RestAlert.Play()`, shows "Rest over" in green for 3 seconds
(`RestDoneHoldSeconds`), then hides the bar. −15s / +15s call `AdjustRestTime(±15)`, and going to 0
ends the rest.

**Q: How does the custom rest time get saved?** (`SetRestForCurrentAsync`, `ParseRestInput`)
`ParseRestInput` reads "75", "75s", "1:45" or "2m" as seconds. `SetRestForCurrentAsync` checks
10–600 seconds, updates the exercise, and saves it to `routine_exercises.RestSeconds` with
`UpdateRoutineExerciseRestAsync`, so the next session remembers it.

**Q: What's the difference between Quick Add with "Save to My Foods" on and off?**
On: the food is inserted into `foods` (marked `IsCustom`), then logged, so it can be searched later.
Off: only a `meal_logs` row is written (with `FoodId = 0`), so it counts for that day without
cluttering the food list. Either way the log stores the calories already multiplied by servings, so
changing a food later doesn't change past days.

**Q: Why does `meal_logs` copy the calories instead of linking to the food?**
So history stays correct. If a food's values are edited or the food is deleted, past days still show
what was actually eaten.

**Q: Why delete and re-insert routine exercises when saving a routine?** (`ReplaceRoutineExercisesAsync`)
It's the simplest way to guarantee the saved order matches the screen after adds, removes and
reorders. Each row gets a fresh `OrderIndex` 0, 1, 2…

**Q: What is dependency injection doing here?** (`MauiProgram.cs`)
I register `LiftLifeDatabase` and `LiftLifeRepository` as **singletons** (one shared connection for
the whole app) and the ViewModels as **transient** (a fresh one each time a page opens). Pages get
them through `ServiceHelper.Get<T>()`.

**Q: What does `async`/`await` do in your code?**
Database work runs without freezing the screen. `await` waits for the query to finish, then carries
on with the result. The UI stays responsive.

**Q: What's the `SemaphoreSlim` in `LiftLifeDatabase` for?**
Two screens can ask for the database at the same moment. The lock makes sure only one of them creates
and seeds it, and "Reset Demo Data" can't run in the middle of that.

### Tricky follow-ups (answer honestly)

**Q: Can another student's phone see your queue position?**
No. It's an offline-first prototype; everything is on one device. The data already stores who, what
and when, so the next step would be syncing `stations`, `station_reports` and `station_queue` through
Firebase (the online option the brief allows).

**Q: Why no login?**
It's one member on their own phone, so a member name is enough. Proper accounts only make sense with
an online database; Firebase Authentication would come with the sync above.

**Q: Does the rest alert work when the phone is locked?**
No. The timer runs inside the app, so it alerts while LiftLife is open on screen. A local
notification scheduled at the end of the rest would be the fix.

**Q: Why is `ValidateXcodeVersion` set to false in the .csproj?**
The installed .NET iOS workload expects Xcode 27.0 exactly, but my Mac had a newer version. That
check only compares major.minor version numbers. Turning it off lets the build use the newer Xcode
of the same series.

**Q: Why VS Code and not Visual Studio for Mac?**
Microsoft retired Visual Studio for Mac on 31 August 2024. Its official replacement is VS Code with
the C# Dev Kit and .NET MAUI extensions.

**Q: How did you get it on your iPhone without the App Store?**
Development signing with a free Apple ID: Xcode made a certificate and a 7-day provisioning profile
for my phone, and the .NET build signs with them (`run-iphone.sh`).

---

## Part 3 — AI use (the rubric marks "AI prompt choices")

The rubric expects you to explain how you used AI, so answer openly. If you claim you wrote it all and
then can't explain something, that scores far worse than a clear, honest answer.

**Q: Did you use AI?**
> "Yes. I used an AI coding assistant, Kiro, in the Kiro IDE (built on VS Code). I gave it my M1 proposal, M2 screens map and
> wireframes, and the exam requirements. I asked it to build the app in stages, one feature area at a
> time, and asked it to keep a notes file explaining each file so I could learn the code. After each
> stage I tested on the simulator and my iPhone and asked for changes."

**Q: Give an example of a prompt you used and why you worded it that way.**
Use a real one, e.g.:
> "I asked for a changeable rest time, bigger timer and volume, a stronger vibration, food the user
> can type in themselves, and a queue for occupied machines. I listed them as separate points so each
> one could be checked on its own, and I described the problem I saw ('the rest timer doesn't really
> vibrate, just a slight haptic') rather than a solution, so it could pick the right API."

**Q: What did the AI get wrong, and how did you catch it?** (real examples from this project)
- The splash logo used SVG text, which the image tool couldn't render. The build failed, so it was
  redrawn as vector paths.
- A stray grey box appeared under the station list (a default `BoxView` style). I saw it in a
  screenshot, and it was replaced.
- Text fields had double borders on iOS. Fixed by removing the native border in `MauiProgram.cs`.
- On my real iPhone the rest vibration was too weak. I tested it myself and asked for a stronger one,
  which led to the Core Haptics version.
- The Quick Add servings line didn't update when the serving text changed. Found during testing and
  fixed by notifying `ServingsText`.

**Q: How do you know the code is correct?**
Every change was built, run on the simulator and on my phone, and for data features I checked the
SQLite file directly in DB Browser (e.g. that a Quick Add entry saved 1.5 × 520 = 780 kcal).

**Q: Could you change it without AI?** → do one of the Part 4 drills live.

---

## Part 4 — Live modification drills

Practise each one **without help**. After a change, save the file (**Cmd + S**) and run this from
the repo folder (about 10 seconds):
```bash
./run-sim.sh
```
(or `./run-iphone.sh` for the phone, about 30 seconds). Undo a change afterwards with
`git restore <file>`.

| # | Lecturer asks | File | What to change | Check |
|---|---|---|---|---|
| 1 | "Make the splash last 3 seconds" | `Views/SplashPage.xaml.cs` | `Task.Delay(2000)` → `3000` | Splash stays longer |
| 2 | "Change the accent colour" | `Resources/Styles/Colors.xaml` | `Primary` `#E9F558` → e.g. `#4CD964` | Buttons and chips change everywhere |
| 3 | "Change the location label" | `Views/CommunityPage.xaml` | `Text="📍 UNITEN Gymnasium"` | Top right of Community |
| 4 | "Estimate 10 minutes per person in the queue" | `Models/Station.cs` | `MinutesPerMember = 8` → `10` | "about 30 min if you join" |
| 5 | "Mark busy reports stale after 30 minutes" | `Models/Station.cs` | `StaleAfterMinutes = 45` → `30` | Older busy cards show BUSY? |
| 6 | "Make the rest buttons ±30 seconds" | `Views/ActiveWorkoutPage.xaml` + `.xaml.cs` | Button text `−15s`/`+15s` → `−30s`/`+30s`; `AdjustRestTime(15)` / `(-15)` → `30` / `-30` | Tap during rest |
| 7 | "Add a 6-minute rest option" | `Views/ActiveWorkoutPage.xaml.cs` | Add `("6:00", 360)` to `RestPresets` | Tap ⏱ Rest |
| 8 | "Add a new food" | `Data/LiftLifeDatabase.cs` | Add a line to the list in `SeedLocalFoodsAsync`, e.g. `new() { Name = "Nasi Kerabu", Serving = "1 plate", Calories = 520, ProteinG = 25, CarbsG = 70, FatG = 15 },` | Restart app, search it (this seed tops up every launch) |
| 9 | "Add a new gym station" | `Data/LiftLifeDatabase.cs` | Add a line in `SeedStationsAsync` | ⚠️ Only seeds an empty table, so use **About → Reset Demo Data** after |
| 10 | "Make rest days cut 20% calories" | Easiest: app → Nutrition → **Edit targets** → rest-day calories 80%. In code: `Models/Nutrition.cs` `RestDayCalorieFactor = 0.85` → `0.80` | ⚠️ The code default only affects new data, so **Reset Demo Data** after a code change |
| 11 | "Change the button text Join Queue" | `Models/Station.cs` | `QueueButtonText` strings | Busy station card |
| 12 | "Show 'Rest over' for 5 seconds" | `ViewModels/ActiveWorkoutViewModel.cs` | `RestDoneHoldSeconds = 3` → `5` | Let a rest finish |

**Why 9 and 10 need a reset:** seed data and default settings are only written when the database is
first created. The database already exists on the device, so the old values stay until it's recreated.
Knowing this is a strong answer in itself.

---

## Part 5 — "Walk me through what happens when…"

**…I open the app.**
`MauiProgram.CreateMauiApp` registers services → `App.CreateWindow` opens `SplashPage` → after 2 s
the window's page becomes `AppShell` (tabs) → `CommunityPage.OnAppearing` calls
`CommunityViewModel.LoadAsync` → the repository asks `GetConnectionAsync`, which creates tables and
seeds them on the very first run → stations are added to the `ObservableCollection` → the cards appear.

**…I tick DONE on a set.**
`OnDoneTapped` → `ToggleDone(set)` checks weight 0–500 and reps 1–100 → `set.IsDone = true` (the box
turns lime through a `DataTrigger`, inputs lock through `IsNotDone`) → `RefreshTotals` updates
`VolumeText` → `StartRest(RestSeconds)` sets `IsResting` → the rest bar appears (bound to
`IsResting`) → `Tick()` counts down every second → at 0, `RestAlert.Play()` buzzes.

**…I submit a station report.**
`ReportStatusPage.OnSubmitClicked` → Out of Order needs a note → `UpdateStationStatusAsync` updates
the `stations` row, inserts a `station_reports` row, and clears the queue if it's broken → the
callback reloads the Community list → the badge colour updates through the converter.

**…I finish a workout and open Nutrition.**
`FinishAsync` → finds PRs first → saves the session and ticked sets (`CompleteSessionAsync` sums the
volume) → `ActivateTrainingDayAsync` records today → Summary shows the calorie change → opening
Nutrition runs `LoadAsync` → `IsTrainingDayAsync(today)` is now true → `TargetsFor(true)` → the badge
shows ⚡ TRAINING DAY ACTIVE with 2,450 kcal.

---

## Part 6 — Quick facts

| Thing | Value | Where |
|---|---|---|
| Splash time | 2 s | `SplashPage.xaml.cs` |
| Database file | `liftlife.db3` in `FileSystem.AppDataDirectory` | `LiftLifeDatabase.cs` |
| Training-day targets | 2,450 kcal • P185 • C280 • F65 | `Models/Nutrition.cs` |
| Rest-day factors | calories × 0.85, carbs × 0.70 | `Models/Nutrition.cs` |
| Queue estimate | 8 min per person | `Station.MinutesPerMember` |
| Queue expiry | 120 min | `StationQueueEntry.ExpireAfterMinutes` |
| Stale busy report | 45 min | `Station.StaleAfterMinutes` |
| Custom rest range | 10–600 s | `ActiveWorkoutViewModel` |
| Rest alert | 3 × 0.45 s buzzes, full intensity | `Services/RestAlert.cs` |
| Version | 1.0.0 (build 1) | `LiftLife.csproj` |
| Framework | .NET MAUI on .NET 10, C# + XAML | `LiftLife.csproj` |
