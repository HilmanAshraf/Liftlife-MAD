using LiftLife.Models;

namespace LiftLife.Data;

/// <summary>
/// Higher-level data operations used by the pages. Wraps <see cref="LiftLifeDatabase"/>
/// and keeps SQL / query logic out of the UI.
/// </summary>
public class LiftLifeRepository
{
    private readonly LiftLifeDatabase _db;

    public LiftLifeRepository(LiftLifeDatabase db) => _db = db;

    // ---------------- Stations ----------------

    public async Task<List<Station>> GetStationsAsync(string? category = null)
    {
        var conn = await _db.GetConnectionAsync();
        var query = conn.Table<Station>();
        if (!string.IsNullOrWhiteSpace(category) && category != "All")
            query = query.Where(s => s.Category == category);
        return await query.OrderBy(s => s.Name).ToListAsync();
    }

    public async Task<List<string>> GetStationCategoriesAsync()
    {
        var conn = await _db.GetConnectionAsync();
        var all = await conn.Table<Station>().ToListAsync();
        var cats = all.Select(s => s.Category).Distinct().OrderBy(c => c).ToList();
        cats.Insert(0, "All");
        return cats;
    }

    public async Task UpdateStationStatusAsync(int stationId, StationStatus status, string? note, string reportedBy)
    {
        var conn = await _db.GetConnectionAsync();
        var station = await conn.FindAsync<Station>(stationId);
        if (station is null) return;
        station.Status = status;
        station.StatusNote = note;
        station.ReportedBy = reportedBy;
        station.LastUpdated = DateTime.Now;
        await conn.UpdateAsync(station);
    }

    /// <summary>
    /// Rough crowd-density estimate: share of stations currently busy, as a percentage.
    /// </summary>
    public async Task<(int percent, string label, int busyCount)> GetCrowdDensityAsync()
    {
        var conn = await _db.GetConnectionAsync();
        var all = await conn.Table<Station>().ToListAsync();
        if (all.Count == 0) return (0, "QUIET", 0);
        var busy = all.Count(s => s.Status == StationStatus.Busy);
        var percent = (int)Math.Round(busy * 100.0 / all.Count);
        var label = percent switch
        {
            >= 70 => "BUSY",
            >= 40 => "MODERATE",
            _ => "QUIET"
        };
        return (percent, label, busy);
    }

    // ---------------- Exercises & swap engine ----------------

    public async Task<List<Exercise>> GetExercisesAsync()
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<Exercise>().OrderBy(e => e.MuscleGroup).ToListAsync();
    }

    /// <summary>
    /// Biomechanical substitution engine: given an exercise, return alternatives
    /// that train the same muscle group (excluding itself), each flagged with
    /// whether a matching station is currently free in the community gym.
    /// </summary>
    public async Task<List<ExerciseSwapOption>> GetSwapOptionsAsync(int exerciseId)
    {
        var conn = await _db.GetConnectionAsync();
        var source = await conn.FindAsync<Exercise>(exerciseId);
        if (source is null) return new();

        var alternatives = await conn.Table<Exercise>()
            .Where(e => e.MuscleGroup == source.MuscleGroup && e.Id != exerciseId)
            .ToListAsync();

        var stations = await conn.Table<Station>().ToListAsync();

        return alternatives.Select(alt =>
        {
            // Consider a substitute "available" if any station using the same
            // equipment for that muscle group is currently free.
            var free = stations.Any(s =>
                s.MuscleGroup == alt.MuscleGroup &&
                s.Status == StationStatus.Free &&
                s.Name.Contains(alt.Equipment, StringComparison.OrdinalIgnoreCase));

            return new ExerciseSwapOption
            {
                Exercise = alt,
                IsEquipmentFree = free || alt.Equipment is "Dumbbell" or "Machine"
            };
        }).ToList();
    }

    // ---------------- Routines ----------------

    public async Task<List<Routine>> GetRoutinesAsync()
    {
        var conn = await _db.GetConnectionAsync();
        var routines = await conn.Table<Routine>().OrderByDescending(r => r.CreatedAt).ToListAsync();
        foreach (var r in routines)
            r.ExerciseCount = await conn.Table<RoutineExercise>().Where(x => x.RoutineId == r.Id).CountAsync();
        return routines;
    }

    public async Task<List<RoutineExercise>> GetRoutineExercisesAsync(int routineId)
    {
        var conn = await _db.GetConnectionAsync();
        var items = await conn.Table<RoutineExercise>()
            .Where(x => x.RoutineId == routineId)
            .OrderBy(x => x.OrderIndex)
            .ToListAsync();

        // Hydrate display fields from the exercise catalogue.
        foreach (var item in items)
        {
            var ex = await conn.FindAsync<Exercise>(item.ExerciseId);
            if (ex is not null)
            {
                item.ExerciseName = ex.Name;
                item.MuscleGroup = ex.MuscleGroup;
                item.Equipment = ex.Equipment;
                item.FormCue = ex.FormCue;
            }
        }
        return items;
    }

    public async Task<int> SaveRoutineAsync(Routine routine)
    {
        var conn = await _db.GetConnectionAsync();
        if (routine.Id == 0)
        {
            await conn.InsertAsync(routine);
            return routine.Id;
        }
        await conn.UpdateAsync(routine);
        return routine.Id;
    }

    public async Task SaveRoutineExerciseAsync(RoutineExercise item)
    {
        var conn = await _db.GetConnectionAsync();
        if (item.Id == 0) await conn.InsertAsync(item);
        else await conn.UpdateAsync(item);
    }

    public async Task DeleteRoutineAsync(int routineId)
    {
        var conn = await _db.GetConnectionAsync();
        await conn.DeleteAsync<Routine>(routineId);
        var items = await conn.Table<RoutineExercise>().Where(x => x.RoutineId == routineId).ToListAsync();
        foreach (var i in items) await conn.DeleteAsync(i);
    }

    // ---------------- Workout sessions ----------------

    public async Task<int> StartSessionAsync(WorkoutSession session)
    {
        var conn = await _db.GetConnectionAsync();
        await conn.InsertAsync(session);
        return session.Id;
    }

    public async Task CompleteSessionAsync(WorkoutSession session, IEnumerable<SetLog> sets)
    {
        var conn = await _db.GetConnectionAsync();
        var done = sets.Where(s => s.IsDone).ToList();
        session.TotalVolumeKg = done.Sum(s => s.Volume);
        session.CompletedAt = DateTime.Now;
        session.IsCompleted = true;
        await conn.UpdateAsync(session);
        foreach (var s in done)
        {
            s.SessionId = session.Id;
            await conn.InsertAsync(s);
        }
    }

    public async Task<WorkoutSession?> GetLastCompletedSessionAsync()
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<WorkoutSession>()
            .Where(s => s.IsCompleted)
            .OrderByDescending(s => s.CompletedAt)
            .FirstOrDefaultAsync();
    }

    // ---------------- Nutrition ----------------

    public async Task<UserSettings> GetSettingsAsync()
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.FindAsync<UserSettings>(1) ?? new UserSettings();
    }

    public async Task SaveSettingsAsync(UserSettings settings)
    {
        var conn = await _db.GetConnectionAsync();
        settings.Id = 1;
        await conn.InsertOrReplaceAsync(settings);
    }

    public async Task<List<FoodItem>> SearchFoodsAsync(string? term)
    {
        var conn = await _db.GetConnectionAsync();
        var all = await conn.Table<FoodItem>().OrderBy(f => f.Name).ToListAsync();
        if (string.IsNullOrWhiteSpace(term)) return all;
        return all.Where(f => f.Name.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task AddCustomFoodAsync(FoodItem food)
    {
        var conn = await _db.GetConnectionAsync();
        food.IsCustom = true;
        await conn.InsertAsync(food);
    }

    public async Task<List<MealLog>> GetMealLogsAsync(DateTime date)
    {
        var conn = await _db.GetConnectionAsync();
        var key = date.ToString("yyyy-MM-dd");
        return await conn.Table<MealLog>().Where(m => m.DateKey == key).ToListAsync();
    }

    public async Task LogMealAsync(MealLog log)
    {
        var conn = await _db.GetConnectionAsync();
        await conn.InsertAsync(log);
    }

    public async Task DeleteMealLogAsync(int id)
    {
        var conn = await _db.GetConnectionAsync();
        await conn.DeleteAsync<MealLog>(id);
    }
}

/// <summary>A substitution option returned by the swap engine.</summary>
public class ExerciseSwapOption
{
    public Exercise Exercise { get; set; } = new();
    public bool IsEquipmentFree { get; set; }

    public string AvailabilityLabel => IsEquipmentFree ? "Free in Gym" : "Check availability";
}
