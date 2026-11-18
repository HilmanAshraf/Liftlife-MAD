# LiftLife — Demo video script (about 8–9 minutes)

**Story:** a UNITEN student arrives at the gym, checks which machines are free, queues for a busy
bench, trains Push Day A, and LiftLife adjusts their nutrition because they trained.
Each step shows something the rubric marks, noted in **[brackets]**.

---

## Before you record

1. **Reset the data** so the video starts with the sample data: About → **Reset Demo Data**. This
   also clears your member name, so the app asks for it on camera (that shows data entry).
2. Run `./run-sim.sh` so the latest build is on the simulator. Close **DB Browser** for now.
3. Turn on **Do Not Disturb** on the Mac so notifications don't pop up.
4. Make the simulator window big and put it next to nothing else (clean background).
5. **Recording:** press **Cmd + Shift + 5** → *Record Selected Portion* → draw around the simulator →
   **Options → Microphone: MacBook microphone** → Record. The mouse pointer shows where you tap.
   Stop with the ■ in the menu bar. (For the iPhone part, use iPhone Control Centre → Screen Recording.)
6. Speak slowly. Short pauses after each tap so viewers can see the result.

---

## The flow

### 0. Intro — 15 s **[App name, developer]**
> "Hi, I'm Mohamed Hilman Ashraf, BSW01085755. This is LiftLife, a .NET MAUI app for shared campus
> gyms, built for UNITEN Gymnasium under the Smart Community and Digital Innovation theme. All data
> is stored offline in SQLite."

### 1. Icon and splash — 20 s **[Icon, Splash screen]**
- Show the **home screen** with the LiftLife icon. Tap it.
- Splash: lime "L" logo, *Community Gym Hub*, slogan → moves to the dashboard after 2 seconds.
> "The custom icon and the splash screen use the same logo. The splash moves to the app automatically."

### 2. Community — about 2 min **[Activities, gestures, data entry, data manipulation]**
- **Density banner** (UNITEN photo): *QUIET (33%) • 2 of 6 stations in use • 3 members in queues.*
  > "Gym density is calculated from live station reports."
- Tap the **Benches** chip, then **All** **[tap gesture]**. Pull down to **refresh** **[pull-to-refresh]**.
- **Flat Barbell Bench #1** is BUSY with *2 waiting*. Tap **Join Queue** → enter your name when asked
  **[data entry]** → *You're #3, about 24 min*.
- Tap **Station Busy? Swap** → same-muscle alternatives with "Free in Gym" → pick one → form cue.
  > "Instead of waiting, the swap engine suggests an exercise for the same muscle using free equipment."
- Tap the **top of the bench card** → **Station Log**: the queue (Imran, Sara, you) and the
  maintenance history. Go back.
- **Squat Rack #1** (OUT OF ORDER): tap **Update Status** → choose **Out of Order**, clear the note,
  tap Submit → *"Add a note"* **[validation]** → type a note → Submit → card and log update.
- **Incline Dumbbell Bench #2** (FREE): **Claim Station** → card shows *In use by you* and
  **I'm Done • Release** → tap it → free again **[data manipulation]**.

### 3. Nutrition before training — 45 s **[Data entry, storage]**
- Badge: **🌙 REST DAY**, *Calories … / 2,082 kcal*.
  > "No workout today yet, so it's a rest day: calories are 15% lower and carbs 30% lower."
- **+ Add Food** on Dinner → **Search Foods**: type `roti` → pick **Roti Canai** → + servings → **Log Food**.
- **+ Add Food** again → **✏️ Quick Add**: `Nasi Kerabu`, `520` kcal, `1 plate`, macros 25/70/15 →
  shows *Macros add up to about 515 kcal ✓* → **Log Food**.
  > "Students can log any food, like cafeteria meals, not just the built-in list."
- Tap a logged item → change the amount. **Swipe left** on one → Delete **[swipe gesture]**.

### 4. Workout — about 2.5 min **[Main features, MAUI APIs]**
- **Workouts** tab: routine cards and *Yesterday: Pull Day B • 5,420 kg logged*.
- Tap **Edit** on Push Day A briefly → show sets / reps / rest / setup note → back.
  (Optional: swipe a routine left to show Delete, then swipe back. Don't delete.)
- **START WORKOUT SESSION** on Push Day A:
  - Big **WORKOUT TIME** and **TOTAL VOLUME**. **PREV** shows last session (75kg × 10, 80kg × 6…).
  - Set 1: tick **DONE** → volume goes up, **rest bar** appears. Tap **−15s**, **+15s**, then **Skip**.
  - Tap **⏱ Rest** → **Custom…** → `0:10` → tick set 2 → let the 10 s run out: bar turns **green**,
    *Rest over* (the phone buzzes — see part 7).
    > "Rest time is per exercise and saved to the routine. When rest ends, the phone vibrates."
  - Set 3: change the weight to **82.5** (more than the previous best of 80) → tick DONE.
  - Tap **(?) Form Guide** → muscle photo, **tempo animation**, cues, safety checks → close.
  - Tap **Station Busy? Swap** → substitutes with **your PR** and the "update routine" switch →
    Confirm → card shows *Swapped from Barbell Bench Press*.
  - **Swipe the card left** to the next exercise, tick one set **[swipe gesture]**.
  - Tap **Finish** → warning about unticked sets → **Finish**.
- **Workout Summary:** volume, duration, sets, **NEW PR** on Barbell Bench Press, and
  **⚡ Training Day activated: Calories 2,082 → 2,450 kcal**. Tap **DONE** → hub now says *Today: Push Day A*.

### 5. Nutrition after training — 30 s **[Data manipulation / feedback]**
- Nutrition tab: **⚡ TRAINING DAY ACTIVE**, target now **2,450 kcal**, carbs 280 g.
  > "Finishing the workout switched today to a training day automatically."
- Tap **‹** to Yesterday (also a training day), then back. Tap **Edit targets ›** → show the preview → back.

### 6. About — 30 s **[About page]**
- Version **1.0.0 (Build 1)** and *Running on iOS…* (read with the MAUI AppInfo and DeviceInfo APIs).
- Developer details, overview, key features.
- **Local storage**: SQLite file size and the record counts (they've gone up during the demo).
- Member name, Macro Targets link, photo credits.

### 7. Proof it's saved in SQLite — 45 s **[Storage]**
- Open **DB Browser for SQLite** on the database (see `docs/PROGRESS_NOTES.md` or ask me for the
  command) → **Browse Data**:
  - `meal_logs`: the Roti Canai and Nasi Kerabu rows you just logged
  - `station_queue`: your entry with `IsMe = 1`
  - `workout_sessions` / `set_logs`: today's Push Day A with the 82.5 kg set
  > "Everything you saw is stored in this local SQLite database."
- Close DB Browser afterwards.

### 8. On a real iPhone — 20 s **[MAUI APIs on a device]**
- Short clip from your iPhone (screen recording): tick a set with a short rest and say
  *"you can feel three strong buzzes"* when it ends (vibration doesn't show on video, so say it).
  The screen also stays awake during the workout.

### 9. Close — 10 s
> "LiftLife helps community gym members share equipment, keep training when a machine is busy, and
> eat right for training and rest days. Thank you."

---

## Rubric checklist (tick while recording)

| Rubric item | Where in the video |
|---|---|
| Icon | 1 |
| Splash screen with logo | 1 |
| About page (app + developer) | 6 |
| Activities completion | 2–6 (every tab and sub-screen) |
| Navigation | tabs, back buttons, sheets throughout |
| Data entry | name prompt, report note, food, Quick Add, weights/reps, custom rest |
| Data manipulation / feedback | density, queue position, swap, PR, training-day switch |
| Gestures | tap, swipe (exercise card), swipe-to-delete (meal), pull-to-refresh |
| MAUI APIs | haptics/vibration, keep screen on, AppInfo/DeviceInfo, timer, animation |
| Storage | 7 (DB Browser) + About record counts |
| Images | UNITEN banner, Form Guide photo |
