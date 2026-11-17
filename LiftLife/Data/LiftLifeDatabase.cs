using LiftLife.Models;
using SQLite;

namespace LiftLife.Data;

/// <summary>
/// Central SQLite data layer for LiftLife. Uses sqlite-net-pcl with an async
/// connection stored in the platform's app-data directory so all routines,
/// logs, equipment reports and nutrition entries persist offline.
/// </summary>
public class LiftLifeDatabase
{
    private SQLiteAsyncConnection? _connection;

    // Makes sure only one caller creates/seeds the database at a time (and not during a reset).
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public const string DatabaseFilename = "liftlife.db3";

    public static string DatabasePath =>
        Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename);

    private const SQLiteOpenFlags Flags =
        SQLiteOpenFlags.ReadWrite |
        SQLiteOpenFlags.Create |
        SQLiteOpenFlags.SharedCache;

    /// <summary>
    /// Lazily opens the connection, creates tables on first use, and seeds
    /// default data the first time the app runs.
    /// </summary>
    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
            return _connection;

        await _initLock.WaitAsync();
        try
        {
            if (_connection is not null)
                return _connection; // another caller finished setting up while we waited

            var conn = new SQLiteAsyncConnection(DatabasePath, Flags);

            // CreateTableAsync also adds any new columns to existing tables.
            await conn.CreateTableAsync<Station>();
            await conn.CreateTableAsync<StationReport>();
        await conn.CreateTableAsync<StationQueueEntry>();
            await conn.CreateTableAsync<Exercise>();
            await conn.CreateTableAsync<Routine>();
            await conn.CreateTableAsync<RoutineExercise>();
            await conn.CreateTableAsync<WorkoutSession>();
            await conn.CreateTableAsync<SetLog>();
            await conn.CreateTableAsync<FoodItem>();
            await conn.CreateTableAsync<MealLog>();
            await conn.CreateTableAsync<UserSettings>();

            await SeedAsync(conn);

            _connection = conn;
            return conn;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <summary>
    /// Deletes the database file so the app starts again with fresh sample data.
    /// Used by "Reset Demo Data" on the About page.
    /// </summary>
    public async Task ResetAsync()
    {
        await _initLock.WaitAsync();
        try
        {
            if (_connection is not null)
            {
                await _connection.CloseAsync();
                _connection = null;
            }

            if (File.Exists(DatabasePath))
                File.Delete(DatabasePath);
        }
        finally
        {
            _initLock.Release();
        }
    }

    // ---------------------------------------------------------------
    //  Seeding — only runs once (checks whether tables are empty)
    // ---------------------------------------------------------------
    private static async Task SeedAsync(SQLiteAsyncConnection db)
    {
        await SeedExercisesAsync(db);
        await SeedStationsAsync(db);
        await SeedStationReportsAsync(db);
        await SeedQueuesAsync(db);
        await SeedFoodsAsync(db);
        await SeedLocalFoodsAsync(db);
        await SeedSettingsAsync(db);
        await SeedSampleRoutineAsync(db);
        await SeedWorkoutHistoryAsync(db);
        await SeedMealsAsync(db);
    }

    /// <summary>
    /// Maintenance-log history: a few past reports for some stations, then one row matching
    /// each station's current status, so every station has a believable log.
    /// </summary>
    private static async Task SeedStationReportsAsync(SQLiteAsyncConnection db)
    {
        if (await db.Table<StationReport>().CountAsync() > 0) return;

        var now = DateTime.Now;
        var history = new Dictionary<string, (double minutesAgo, StationStatus status, string? note, string by)[]>
        {
            ["Flat Barbell Bench #1"] = new[]
            {
                (180.0, StationStatus.Free, (string?)null, "Sara"),
                (95.0, StationStatus.Busy, "Occupied", "Imran"),
                (52.0, StationStatus.Free, null, "Imran"),
            },
            ["Squat Rack #1"] = new[]
            {
                (1500.0, StationStatus.Busy, (string?)"Occupied", "Daniel"),
                (190.0, StationStatus.OutOfOrder, "Safety pin bent", "Aina"),
            },
            ["Cable Crossover #1"] = new[]
            {
                (7200.0, StationStatus.OutOfOrder, (string?)"Pulley cable frayed", "Alex"),
                (4300.0, StationStatus.Free, "Cable replaced by staff", "Staff"),
            },
            ["Lat Pulldown #1"] = new[]
            {
                (240.0, StationStatus.Free, (string?)null, "Sara"),
            },
        };

        foreach (var station in await db.Table<Station>().ToListAsync())
        {
            if (history.TryGetValue(station.Name, out var past))
            {
                foreach (var (minutesAgo, status, note, by) in past)
                {
                    await db.InsertAsync(new StationReport
                    {
                        StationId = station.Id,
                        Status = status,
                        Note = note,
                        ReportedBy = by,
                        ReportedAt = now.AddMinutes(-minutesAgo)
                    });
                }
            }

            // The current status is always the latest entry in the log.
            await db.InsertAsync(new StationReport
            {
                StationId = station.Id,
                Status = station.Status,
                Note = station.StatusNote,
                ReportedBy = station.ReportedBy ?? "Community",
                ReportedAt = station.LastUpdated
            });
        }
    }

    /// <summary>
    /// A couple of members already waiting on busy stations, so the queue feature can be
    /// shown on first launch. Only busy stations someone else is using get a queue.
    /// </summary>
    private static async Task SeedQueuesAsync(SQLiteAsyncConnection db)
    {
        if (await db.Table<StationQueueEntry>().CountAsync() > 0) return;

        var now = DateTime.Now;
        var plan = new Dictionary<string, (string name, double minutesAgo)[]>
        {
            ["Flat Barbell Bench #1"] = new[] { ("Imran", 4.0), ("Sara", 1.0) },
            ["Lat Pulldown #1"] = new[] { ("Daniel", 3.0) },
        };

        foreach (var station in await db.Table<Station>().ToListAsync())
        {
            if (station.Status != StationStatus.Busy || station.ClaimedByMe) continue;
            if (!plan.TryGetValue(station.Name, out var members)) continue;

            foreach (var (name, minutesAgo) in members)
            {
                await db.InsertAsync(new StationQueueEntry
                {
                    StationId = station.Id,
                    MemberName = name,
                    JoinedAt = now.AddMinutes(-minutesAgo)
                });
            }
        }
    }

    /// <summary>
    /// Sample meals for today (a rest day) and yesterday (a training day: Pull Day B),
    /// so the Nutrition tab shows both day types on first launch.
    /// </summary>
    private static async Task SeedMealsAsync(SQLiteAsyncConnection db)
    {
        if (await db.Table<MealLog>().CountAsync() > 0) return;

        var today = DateTime.Today;
        var yesterday = today.AddDays(-1);

        var plan = new (DateTime date, MealCategory meal, string food, double qty)[]
        {
            (today, MealCategory.Breakfast, "Rolled Oats", 1),
            (today, MealCategory.Breakfast, "Banana", 1),
            (today, MealCategory.Breakfast, "Whey Protein", 1),
            (today, MealCategory.Lunch, "Mixed Nasi Lemak", 1),
            (today, MealCategory.Lunch, "Chicken Breast (grilled)", 1),

            (yesterday, MealCategory.Breakfast, "Whole Egg", 3),
            (yesterday, MealCategory.Breakfast, "Rolled Oats", 1.5),
            (yesterday, MealCategory.Lunch, "Chicken Breast (grilled)", 2),
            (yesterday, MealCategory.Lunch, "White Rice (cooked)", 2.5),
            (yesterday, MealCategory.Dinner, "Salmon (cooked)", 1.5),
            (yesterday, MealCategory.Dinner, "White Rice (cooked)", 2),
            (yesterday, MealCategory.Snacks, "Greek Yogurt (plain)", 1),
            (yesterday, MealCategory.Snacks, "Whey Protein", 1),
            (yesterday, MealCategory.Snacks, "Banana", 1),
        };

        foreach (var (date, meal, foodName, qty) in plan)
        {
            var food = await db.Table<FoodItem>().Where(f => f.Name == foodName).FirstOrDefaultAsync();
            if (food is not null)
                await db.InsertAsync(MealLog.From(food, qty, meal, date));
        }
    }

    /// <summary>
    /// Two past Push Day A sessions with logged sets, so the live tracker can show
    /// PREV values and the swap sheet can show personal records on first launch.
    /// </summary>
    private static async Task SeedWorkoutHistoryAsync(SQLiteAsyncConnection db)
    {
        if (await db.Table<SetLog>().CountAsync() > 0) return;

        var push = await db.Table<Routine>().Where(r => r.Name == "Push Day A").FirstOrDefaultAsync();
        if (push is null) return;

        // A week ago the bench was busy, so the member swapped to dumbbells.
        await SeedSessionAsync(db, push, DateTime.Today.AddDays(-7).AddHours(17), 52, new[]
        {
            ("Dumbbell Flat Bench Press", new[] { (28.0, 8), (28.0, 8), (26.0, 9) }),
            ("Incline Dumbbell Press",    new[] { (22.0, 10), (22.0, 10), (22.0, 9) }),
            ("Machine Shoulder Press",    new[] { (35.0, 10), (35.0, 10), (35.0, 10) }),
            ("Cable Lateral Raise",       new[] { (7.5, 15), (7.5, 15), (7.5, 14) }),
            ("Triceps Rope Pushdown",     new[] { (22.5, 12), (22.5, 12), (22.5, 12) }),
        });

        await SeedSessionAsync(db, push, DateTime.Today.AddDays(-3).AddHours(18), 49, new[]
        {
            ("Barbell Bench Press",    new[] { (75.0, 10), (80.0, 6), (80.0, 5) }),
            ("Incline Dumbbell Press", new[] { (24.0, 10), (24.0, 10), (24.0, 9) }),
            ("Machine Shoulder Press", new[] { (40.0, 10), (40.0, 10), (40.0, 9) }),
            ("Cable Lateral Raise",    new[] { (7.5, 15), (7.5, 15), (7.5, 15) }),
            ("Triceps Rope Pushdown",  new[] { (25.0, 12), (25.0, 12), (25.0, 11) }),
        });
    }

    private static async Task SeedSessionAsync(SQLiteAsyncConnection db, Routine routine, DateTime start, int minutes,
        (string exercise, (double weight, int reps)[] sets)[] plan)
    {
        var session = new WorkoutSession
        {
            RoutineId = routine.Id,
            RoutineName = routine.Name,
            StartedAt = start,
            CompletedAt = start.AddMinutes(minutes),
            IsCompleted = true
        };
        await db.InsertAsync(session);

        double volume = 0;
        foreach (var (exerciseName, sets) in plan)
        {
            var exercise = await db.Table<Exercise>().Where(e => e.Name == exerciseName).FirstOrDefaultAsync();
            if (exercise is null) continue;

            for (var i = 0; i < sets.Length; i++)
            {
                var (weight, reps) = sets[i];
                await db.InsertAsync(new SetLog
                {
                    SessionId = session.Id,
                    ExerciseId = exercise.Id,
                    ExerciseName = exercise.Name,
                    SetNumber = i + 1,
                    WeightKg = weight,
                    Reps = reps,
                    IsDone = true
                });
                volume += weight * reps;
            }
        }

        session.TotalVolumeKg = volume;
        await db.UpdateAsync(session);
    }

    private static async Task SeedExercisesAsync(SQLiteAsyncConnection db)
    {
        if (await db.Table<Exercise>().CountAsync() > 0) return;

        var exercises = new List<Exercise>
        {
            // Chest
            new() { Name = "Barbell Bench Press",   MuscleGroup = "Chest", Equipment = "Barbell",  FormCue = "Retract shoulder blades, bar to mid-chest, elbows ~45°.", DefaultRestSeconds = 120 },
            new() { Name = "Dumbbell Flat Bench Press", MuscleGroup = "Chest", Equipment = "Dumbbell", FormCue = "Lower under control, keep wrists stacked over elbows.", DefaultRestSeconds = 90 },
            new() { Name = "Machine Chest Press",    MuscleGroup = "Chest", Equipment = "Machine",  FormCue = "Align handles with mid-chest, press without locking hard.", DefaultRestSeconds = 75 },
            new() { Name = "Incline Dumbbell Press", MuscleGroup = "Chest", Equipment = "Dumbbell", FormCue = "Bench ~30°, drive up and slightly inward.", DefaultRestSeconds = 90 },
            new() { Name = "Cable Chest Fly",        MuscleGroup = "Chest", Equipment = "Cable",    FormCue = "Soft elbows, squeeze at the midline.", DefaultRestSeconds = 60 },

            // Back
            new() { Name = "Lat Pulldown",           MuscleGroup = "Back", Equipment = "Cable",    FormCue = "Chest up, pull to upper chest, drive elbows down.", DefaultRestSeconds = 90 },
            new() { Name = "Seated Cable Row",       MuscleGroup = "Back", Equipment = "Cable",    FormCue = "Neutral spine, pull to navel, squeeze shoulder blades.", DefaultRestSeconds = 90 },
            new() { Name = "Dumbbell Row",           MuscleGroup = "Back", Equipment = "Dumbbell", FormCue = "Flat back, row to hip, no torso rotation.", DefaultRestSeconds = 75 },
            new() { Name = "Machine Row",            MuscleGroup = "Back", Equipment = "Machine",  FormCue = "Chest on pad, lead with elbows.", DefaultRestSeconds = 75 },

            // Legs
            new() { Name = "Barbell Back Squat",     MuscleGroup = "Legs", Equipment = "Barbell",  FormCue = "Brace core, break at hips and knees, depth to parallel.", DefaultRestSeconds = 150 },
            new() { Name = "Leg Press",              MuscleGroup = "Legs", Equipment = "Machine",  FormCue = "Feet shoulder-width, don't lock knees at top.", DefaultRestSeconds = 120 },
            new() { Name = "Goblet Squat",           MuscleGroup = "Legs", Equipment = "Dumbbell", FormCue = "Elbows inside knees, upright torso.", DefaultRestSeconds = 90 },
            new() { Name = "Leg Extension",          MuscleGroup = "Legs", Equipment = "Machine",  FormCue = "Pause at top, control the negative.", DefaultRestSeconds = 60 },

            // Shoulders
            new() { Name = "Overhead Barbell Press", MuscleGroup = "Shoulders", Equipment = "Barbell",  FormCue = "Glutes tight, bar travels straight over mid-foot.", DefaultRestSeconds = 120 },
            new() { Name = "Dumbbell Shoulder Press",MuscleGroup = "Shoulders", Equipment = "Dumbbell", FormCue = "Press slightly inward, avoid flaring low back.", DefaultRestSeconds = 90 },
            new() { Name = "Machine Shoulder Press", MuscleGroup = "Shoulders", Equipment = "Machine",  FormCue = "Set seat so handles start at shoulder height.", DefaultRestSeconds = 75 },
            new() { Name = "Cable Lateral Raise",    MuscleGroup = "Shoulders", Equipment = "Cable",    FormCue = "Lead with elbows, raise to shoulder line.", DefaultRestSeconds = 60 },

            // Arms
            new() { Name = "Barbell Curl",           MuscleGroup = "Biceps", Equipment = "Barbell",  FormCue = "Elbows pinned, no swinging.", DefaultRestSeconds = 60 },
            new() { Name = "Dumbbell Curl",          MuscleGroup = "Biceps", Equipment = "Dumbbell", FormCue = "Supinate as you lift, control down.", DefaultRestSeconds = 60 },
            new() { Name = "Triceps Rope Pushdown",  MuscleGroup = "Triceps", Equipment = "Cable",   FormCue = "Elbows locked to sides, spread rope at bottom.", DefaultRestSeconds = 60 },
            new() { Name = "Overhead Dumbbell Extension", MuscleGroup = "Triceps", Equipment = "Dumbbell", FormCue = "Keep elbows pointed forward, full stretch.", DefaultRestSeconds = 60 },
        };

        await db.InsertAllAsync(exercises);
    }

    private static async Task SeedStationsAsync(SQLiteAsyncConnection db)
    {
        if (await db.Table<Station>().CountAsync() > 0) return;

        var now = DateTime.Now;
        var stations = new List<Station>
        {
            new() { Name = "Flat Barbell Bench #1", Category = "Benches", MuscleGroup = "Chest", Status = StationStatus.Busy, StatusNote = "Occupied", ReportedBy = "Alex", LastUpdated = now.AddMinutes(-2) },
            new() { Name = "Incline Dumbbell Bench #2", Category = "Benches", MuscleGroup = "Chest", Status = StationStatus.Free, ReportedBy = "Sara", LastUpdated = now.AddMinutes(-5) },
            new() { Name = "Cable Crossover #1", Category = "Cables", MuscleGroup = "Chest", Status = StationStatus.Free, LastUpdated = now.AddMinutes(-8) },
            new() { Name = "Lat Pulldown #1", Category = "Cables", MuscleGroup = "Back", Status = StationStatus.Busy, StatusNote = "In use", ReportedBy = "Imran", LastUpdated = now.AddMinutes(-1) },
            new() { Name = "Squat Rack #1", Category = "Squat Racks", MuscleGroup = "Legs", Status = StationStatus.OutOfOrder, StatusNote = "Safety pin bent", ReportedBy = "Staff", LastUpdated = now.AddHours(-2) },
            new() { Name = "Squat Rack #2", Category = "Squat Racks", MuscleGroup = "Legs", Status = StationStatus.Free, LastUpdated = now.AddMinutes(-12) },
            new() { Name = "Leg Press", Category = "Machines", MuscleGroup = "Legs", Status = StationStatus.Free, LastUpdated = now.AddMinutes(-20) },
        };

        await db.InsertAllAsync(stations);
    }

    private static async Task SeedFoodsAsync(SQLiteAsyncConnection db)
    {
        if (await db.Table<FoodItem>().CountAsync() > 0) return;

        var foods = new List<FoodItem>
        {
            new() { Name = "Chicken Breast (grilled)", Serving = "100 g", Calories = 165, ProteinG = 31, CarbsG = 0, FatG = 3.6 },
            new() { Name = "White Rice (cooked)",       Serving = "100 g", Calories = 130, ProteinG = 2.7, CarbsG = 28, FatG = 0.3 },
            new() { Name = "Whole Egg",                 Serving = "1 egg (50 g)", Calories = 78, ProteinG = 6.3, CarbsG = 0.6, FatG = 5.3 },
            new() { Name = "Rolled Oats",               Serving = "40 g", Calories = 150, ProteinG = 5, CarbsG = 27, FatG = 3 },
            new() { Name = "Banana",                    Serving = "1 medium", Calories = 105, ProteinG = 1.3, CarbsG = 27, FatG = 0.4 },
            new() { Name = "Whey Protein",              Serving = "1 scoop (30 g)", Calories = 120, ProteinG = 24, CarbsG = 3, FatG = 1.5 },
            new() { Name = "Greek Yogurt (plain)",      Serving = "150 g", Calories = 90, ProteinG = 15, CarbsG = 6, FatG = 0.6 },
            new() { Name = "Peanut Butter",             Serving = "1 tbsp (16 g)", Calories = 94, ProteinG = 4, CarbsG = 3, FatG = 8 },
            new() { Name = "Salmon (cooked)",           Serving = "100 g", Calories = 208, ProteinG = 20, CarbsG = 0, FatG = 13 },
            new() { Name = "Mixed Nasi Lemak",          Serving = "1 plate", Calories = 494, ProteinG = 13, CarbsG = 80, FatG = 14 },
        };

        await db.InsertAllAsync(foods);
    }

    /// <summary>
    /// Common Malaysian campus / mamak foods (typical serving, approximate values).
    /// Each one is only added if a food with that name isn't there yet, so this also
    /// tops up databases created by older versions of the app.
    /// </summary>
    private static async Task SeedLocalFoodsAsync(SQLiteAsyncConnection db)
    {
        var local = new List<FoodItem>
        {
            new() { Name = "Nasi Goreng Kampung",      Serving = "1 plate", Calories = 600, ProteinG = 18, CarbsG = 80, FatG = 22 },
            new() { Name = "Nasi Ayam (Chicken Rice)", Serving = "1 plate", Calories = 600, ProteinG = 30, CarbsG = 75, FatG = 18 },
            new() { Name = "Nasi Ayam Penyet",         Serving = "1 plate", Calories = 780, ProteinG = 40, CarbsG = 80, FatG = 33 },
            new() { Name = "Mee Goreng Mamak",         Serving = "1 plate", Calories = 660, ProteinG = 20, CarbsG = 85, FatG = 26 },
            new() { Name = "Char Kuey Teow",           Serving = "1 plate", Calories = 740, ProteinG = 20, CarbsG = 76, FatG = 38 },
            new() { Name = "Roti Canai",               Serving = "1 piece", Calories = 300, ProteinG = 6,  CarbsG = 40, FatG = 13 },
            new() { Name = "Roti Telur",               Serving = "1 piece", Calories = 360, ProteinG = 11, CarbsG = 40, FatG = 17 },
            new() { Name = "Satay Ayam",               Serving = "5 sticks", Calories = 300, ProteinG = 25, CarbsG = 10, FatG = 18 },
            new() { Name = "Telur Mata (Fried Egg)",   Serving = "1 egg", Calories = 90, ProteinG = 6, CarbsG = 0.4, FatG = 7 },
            new() { Name = "Teh Tarik",                Serving = "1 glass", Calories = 150, ProteinG = 4, CarbsG = 24, FatG = 4 },
            new() { Name = "Milo Ais",                 Serving = "1 glass", Calories = 190, ProteinG = 5, CarbsG = 32, FatG = 5 },
            new() { Name = "Brown Rice (cooked)",      Serving = "100 g", Calories = 112, ProteinG = 2.6, CarbsG = 24, FatG = 0.9 },
            new() { Name = "Sweet Potato (boiled)",    Serving = "150 g", Calories = 130, ProteinG = 2.5, CarbsG = 30, FatG = 0.2 },
            new() { Name = "Tofu",                     Serving = "100 g", Calories = 76, ProteinG = 8, CarbsG = 2, FatG = 4.8 },
            new() { Name = "Full Cream Milk",          Serving = "250 ml", Calories = 155, ProteinG = 8, CarbsG = 12, FatG = 8 },
            new() { Name = "Protein Bar",              Serving = "1 bar", Calories = 210, ProteinG = 20, CarbsG = 22, FatG = 7 },
            new() { Name = "Apple",                    Serving = "1 medium", Calories = 95, ProteinG = 0.5, CarbsG = 25, FatG = 0.3 },
        };

        var existing = (await db.Table<FoodItem>().ToListAsync())
            .Select(f => f.Name.Trim().ToLowerInvariant())
            .ToHashSet();

        var missing = local.Where(f => !existing.Contains(f.Name.ToLowerInvariant())).ToList();
        if (missing.Count > 0)
            await db.InsertAllAsync(missing);
    }

    private static async Task SeedSettingsAsync(SQLiteAsyncConnection db)
    {
        if (await db.Table<UserSettings>().CountAsync() > 0) return;
        await db.InsertAsync(new UserSettings());
    }

    private static async Task SeedSampleRoutineAsync(SQLiteAsyncConnection db)
    {
        // Each sample routine is only added if a routine with that name doesn't exist yet.
        await SeedRoutineAsync(db, "Push Day A", "Chest / Shoulders / Triceps", 50, new[]
        {
            ("Barbell Bench Press", 3, 8, 120),
            ("Incline Dumbbell Press", 3, 10, 90),
            ("Machine Shoulder Press", 3, 10, 75),
            ("Cable Lateral Raise", 3, 15, 60),
            ("Triceps Rope Pushdown", 3, 12, 60),
        });

        await SeedRoutineAsync(db, "Pull Day B", "Back / Biceps", 45, new[]
        {
            ("Lat Pulldown", 3, 10, 90),
            ("Seated Cable Row", 3, 10, 90),
            ("Dumbbell Row", 3, 10, 75),
            ("Barbell Curl", 3, 12, 60),
        });

        // One completed past session so the history card has data on first launch.
        if (await db.Table<WorkoutSession>().CountAsync() == 0)
        {
            var pull = await db.Table<Routine>().Where(r => r.Name == "Pull Day B").FirstOrDefaultAsync();
            var yesterday = DateTime.Today.AddDays(-1).AddHours(18);
            await db.InsertAsync(new WorkoutSession
            {
                RoutineId = pull?.Id ?? 0,
                RoutineName = "Pull Day B",
                StartedAt = yesterday,
                CompletedAt = yesterday.AddMinutes(47),
                TotalVolumeKg = 5420,
                IsCompleted = true
            });
        }
    }

    private static async Task SeedRoutineAsync(SQLiteAsyncConnection db, string name, string focus, int minutes,
        (string exercise, int sets, int reps, int rest)[] plan)
    {
        if (await db.Table<Routine>().Where(r => r.Name == name).CountAsync() > 0) return;

        var routine = new Routine { Name = name, Focus = focus, EstimatedMinutes = minutes };
        await db.InsertAsync(routine);

        for (var i = 0; i < plan.Length; i++)
        {
            var (exerciseName, sets, reps, rest) = plan[i];
            var exercise = await db.Table<Exercise>().Where(e => e.Name == exerciseName).FirstOrDefaultAsync();
            if (exercise is null) continue;

            await db.InsertAsync(new RoutineExercise
            {
                RoutineId = routine.Id,
                ExerciseId = exercise.Id,
                OrderIndex = i,
                TargetSets = sets,
                TargetReps = reps,
                RestSeconds = rest
            });
        }
    }
}
