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
        if (await db.Table<Routine>().CountAsync() > 0) return;

        var push = new Routine { Name = "Push Day A", Focus = "Chest / Shoulders / Triceps", EstimatedMinutes = 50 };
        await db.InsertAsync(push);

        // Look up a few seeded exercises by name to attach to the routine.
        async Task<int> IdOf(string name) =>
            (await db.Table<Exercise>().Where(e => e.Name == name).FirstOrDefaultAsync())?.Id ?? 0;

        var items = new List<RoutineExercise>
        {
            new() { RoutineId = push.Id, ExerciseId = await IdOf("Barbell Bench Press"),     OrderIndex = 0, TargetSets = 3, TargetReps = 8,  RestSeconds = 120 },
            new() { RoutineId = push.Id, ExerciseId = await IdOf("Incline Dumbbell Press"),  OrderIndex = 1, TargetSets = 3, TargetReps = 10, RestSeconds = 90 },
            new() { RoutineId = push.Id, ExerciseId = await IdOf("Machine Shoulder Press"),  OrderIndex = 2, TargetSets = 3, TargetReps = 10, RestSeconds = 75 },
            new() { RoutineId = push.Id, ExerciseId = await IdOf("Cable Lateral Raise"),     OrderIndex = 3, TargetSets = 3, TargetReps = 15, RestSeconds = 60 },
            new() { RoutineId = push.Id, ExerciseId = await IdOf("Triceps Rope Pushdown"),   OrderIndex = 4, TargetSets = 3, TargetReps = 12, RestSeconds = 60 },
        };
        await db.InsertAllAsync(items);
    }
}
