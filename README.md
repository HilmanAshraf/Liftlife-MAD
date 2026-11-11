# LiftLife — Community Gym Hub & Routine Tracker

A .NET MAUI mobile application for the **Smart Community & Digital Innovation** theme
(CSNB544 Mobile Application Development).

LiftLife modernises the shared / community gym experience: live equipment status,
a smart exercise substitution engine for crowded gyms, a workout routine builder and
live session tracker, and adaptive daily nutrition that reacts to training vs. rest days.
All data is stored locally and offline using **SQLite**.

## Tech stack
- **Framework:** .NET MAUI (.NET 10), C# + XAML
- **Database:** SQLite (`sqlite-net-pcl`), local offline storage
- **Primary platform:** iOS (also builds for Android & Mac Catalyst)
- **Editor:** Visual Studio Code with C# Dev Kit + .NET MAUI extensions (official Mac toolchain)

## Core features
1. **Community station status & maintenance log** — live Free / Busy / Out of Order status,
   crowd density, claim and release stations, and a per-station report history
2. **"Crowded gym" swap engine** — same-muscle substitutes using equipment that's free
3. **Routine builder** — sets, reps, rest and machine setup notes per exercise
4. **Live workout tracker** — set logging with previous-session values, rest timer with vibration,
   personal-record detection and a workout summary
5. **Form guides** — looping tempo animation, form cues and equipment safety checks
6. **Adaptive nutrition** — training vs rest day targets switch automatically when a workout is
   finished; food search, logging and custom foods
7. **Local SQLite persistence** — everything works offline
8. **Splash, About and settings** — app/developer info, storage stats, member name, macro targets

## Screens
| Tab | Screens |
|---|---|
| Community | Station Hub · Report Status · Station Log |
| Workouts | Routine Hub · Routine Builder · Active Session · Quick Swap · Form Guide · Summary |
| Nutrition | Daily Nutrition · Add Food · Custom Food · Macro Targets |
| About | About LiftLife (profile, storage, settings) |

## Project layout
```
LiftLife/
  App.xaml(.cs)         App entry point + global theme (opens the splash screen)
  AppShell.xaml         Bottom tab navigation (Community / Workouts / Nutrition / About)
  Views/                Screens (XAML layout + code-behind for dialogs and navigation)
  ViewModels/           Screen data and logic (MVVM + data binding)
  Models/               SQLite tables and display models
  Data/                 LiftLifeDatabase (connection, tables, seed data) + LiftLifeRepository (queries)
  Services/             Small helpers (DI lookup, number input, time labels, member name)
  Converters/           XAML value converters (status → colour)
  Resources/            Styles, colours, fonts, app icon, splash, images
docs/PROGRESS_NOTES.md  Day-by-day development log with explanations
```

## Build & run on the iOS simulator
```bash
# one-time: point the toolchain at Xcode
sudo xcode-select -s /Applications/Xcode.app/Contents/Developer

# open the simulator window (Xcode 27 calls it DeviceHub) and boot an iPhone
open /Applications/Xcode.app/Contents/Applications/DeviceHub.app
xcrun simctl boot "iPhone 17"

# build, install and launch
cd LiftLife
dotnet build -f net10.0-ios
xcrun simctl install booted bin/Debug/net10.0-ios/iossimulator-arm64/LiftLife.app
xcrun simctl launch booted com.hilmanashraf.liftlife
```

To start again with the sample data, use **About → Reset Demo Data**, or uninstall first:
`xcrun simctl uninstall booted com.hilmanashraf.liftlife`.

See `docs/PROGRESS_NOTES.md` for a day-by-day development log and explanations.

## Developer
Mohamed Hilman Ashraf Bin Mohamed Ashraf — BSW01085755
Bachelor in Software Engineering (Hons), Universiti Tenaga Nasional (UNITEN)
