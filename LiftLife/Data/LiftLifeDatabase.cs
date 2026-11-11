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

        _connection = new SQLiteAsyncConnection(DatabasePath, Flags);

        await _connection.CreateTableAsync<Station>();
        await _connection.CreateTableAsync<Exercise>();
        await _connection.CreateTableAsync<Routine>();
        await _connection.CreateTableAsync<RoutineExercise>();
        await _connection.CreateTableAsync<WorkoutSession>();
        await _connection.CreateTableAsync<SetLog>();
        await _connection.CreateTableAsync<FoodItem>();
        await _connection.CreateTableAsync<MealLog>();
        await _connection.CreateTableAsync<UserSettings>();

        await SeedAsync(_connection);

        return _connection;
    }

    // ---------------------------------------------------------------
    //  Seeding — only runs once (checks whether tables are empty)
    // ---------------------------------------------------------------
    private static async Task SeedAsync(SQLiteAsyncConnection db)
    {
        await SeedExercisesAsync(db);
        await SeedStationsAsync(db);
        await SeedFoodsAsync(db);
        await SeedSettingsAsync(db);
        await SeedSampleRoutineAsync(db);
        await SeedWorkoutHistoryAsync(db);
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
