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

- **`Resources/Splash/splash.svg`** — the splash image used at OS launch.

- **`Resources/Images/tab_*.svg`** — the four bottom-tab icons.

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

## DAY 2 — (planned) Data layer: models, repository, seed data
*To be filled in when completed.*

## DAY 3 — (planned) Community Station Hub + Report Status
## DAY 4 — (planned) Workout Routine Hub + Routine Builder
## DAY 5 — (planned) Active Workout Session + Quick Swap engine + rest timer
## DAY 6 — (planned) Daily Nutrition Hub + food logging + macro logic
## DAY 7 — (planned) About page, icon/splash polish, testing, final report
