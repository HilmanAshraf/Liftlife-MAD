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
1. Community gym station status & fault reporting
2. "Crowded gym" real-time equipment swap engine
3. Workout routine builder & customization
4. Live workout tracker with automated rest timer
5. Embedded exercise form guides
6. Adaptive daily nutrition & macro targets (training vs. rest day)
7. Local SQLite data persistence
8. Splash, About, and system pages

## Project layout
```
LiftLife/
  App.xaml(.cs)         App entry point + global theme
  AppShell.xaml         Bottom tab navigation (Community / Workouts / Nutrition / About)
  Views/                Screens (XAML + code-behind)
  Models/               Data classes (added Day 2)
  Data/                 SQLite database layer
  Resources/Styles/     Colors + reusable styles (dark + lime theme)
  Resources/AppIcon/    Custom app icon
  Resources/Splash/     Splash image
```

## Build & run (iOS)
```bash
# one-time: point the toolchain at Xcode
sudo xcode-select -s /Applications/Xcode.app/Contents/Developer

cd LiftLife
dotnet build -f net10.0-ios
```

See `docs/PROGRESS_NOTES.md` for a day-by-day development log and explanations.

## Developer
Mohamed Hilman Ashraf Bin Mohamed Ashraf — BSW01085755
Bachelor in Software Engineering (Hons), Universiti Tenaga Nasional (UNITEN)
