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

    /// <summary>
    /// Changes a station's live status AND adds an entry to its maintenance log.
    /// </summary>
    /// <param name="claimedByMe">True when this member is claiming the station for themselves.</param>
    public async Task UpdateStationStatusAsync(int stationId, StationStatus status, string? note, string reportedBy,
        bool claimedByMe = false)
    {
        var conn = await _db.GetConnectionAsync();
        var station = await conn.FindAsync<Station>(stationId);
        if (station is null) return;

        station.Status = status;
        station.StatusNote = note;
        station.ReportedBy = reportedBy;
        station.LastUpdated = DateTime.Now;
        station.ClaimedByMe = claimedByMe; // any other update ends my claim
        await conn.UpdateAsync(station);

        await conn.InsertAsync(new StationReport
        {
            StationId = stationId,
            Status = status,
            Note = note,
            ReportedBy = reportedBy,
            ReportedAt = station.LastUpdated
        });
    }

    public async Task<Station?> GetStationAsync(int stationId)
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.FindAsync<Station>(stationId);
    }

    /// <summary>A station's maintenance log, newest first.</summary>
    public async Task<List<StationReport>> GetStationReportsAsync(int stationId)
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<StationReport>()
            .Where(r => r.StationId == stationId)
            .OrderByDescending(r => r.ReportedAt)
            .ToListAsync();
    }

    // ---------------- Member profile ----------------

    public async Task<string?> GetMemberNameAsync() => (await GetSettingsAsync()).MemberName;

    public async Task SetMemberNameAsync(string name)
    {
        var settings = await GetSettingsAsync();
        settings.MemberName = name.Trim();
        await SaveSettingsAsync(settings);
    }

    // ---------------- Storage (About page) ----------------

    /// <summary>Row counts per table, shown on the About page.</summary>
    public async Task<List<(string label, int count)>> GetStorageStatsAsync()
    {
        var conn = await _db.GetConnectionAsync();
        return new List<(string, int)>
        {
            ("Gym stations", await conn.Table<Station>().CountAsync()),
            ("Station reports", await conn.Table<StationReport>().CountAsync()),
            ("Exercises", await conn.Table<Exercise>().CountAsync()),
            ("Routines", await conn.Table<Routine>().CountAsync()),
            ("Workouts completed", await conn.Table<WorkoutSession>().Where(s => s.IsCompleted).CountAsync()),
            ("Sets logged", await conn.Table<SetLog>().CountAsync()),
            ("Foods", await conn.Table<FoodItem>().CountAsync()),
            ("Meal entries", await conn.Table<MealLog>().CountAsync()),
        };
    }

    /// <summary>Deletes all data; the next query recreates the database with sample data.</summary>
    public async Task ResetAllDataAsync()
    {
        await _db.ResetAsync();
        await _db.GetConnectionAsync();
    }

    /// <summary>
    /// Crowd-density estimate: share of usable stations (not out of order) that are busy.
    /// </summary>
    public async Task<(int percent, string label, int busyCount, int usableCount)> GetCrowdDensityAsync()
    {
        var conn = await _db.GetConnectionAsync();
        var all = await conn.Table<Station>().ToListAsync();
        var usable = all.Where(s => s.Status != StationStatus.OutOfOrder).ToList();
        if (usable.Count == 0) return (0, "QUIET", 0, 0);
        var busy = usable.Count(s => s.Status == StationStatus.Busy);
        var percent = (int)Math.Round(busy * 100.0 / usable.Count);
        var label = percent switch
        {
            >= 70 => "BUSY",
            >= 40 => "MODERATE",
            _ => "QUIET"
        };
        return (percent, label, busy, usable.Count);
    }

    /// <summary>
    /// Swap engine entry point from the Community Hub: when a station is busy or broken,
    /// suggest exercises for the same muscle group that use different equipment.
    /// </summary>
    public async Task<List<ExerciseSwapOption>> GetAlternativesForStationAsync(Station station)
    {
        var conn = await _db.GetConnectionAsync();
        var alternatives = await conn.Table<Exercise>()
            .Where(e => e.MuscleGroup == station.MuscleGroup)
            .ToListAsync();
        var stations = await conn.Table<Station>().ToListAsync();

        return alternatives
            // Skip exercises that need the same equipment as the unavailable station.
            .Where(e => !station.Name.Contains(e.Equipment, StringComparison.OrdinalIgnoreCase))
            .Select(e => new ExerciseSwapOption { Exercise = e, IsEquipmentFree = IsEquipmentFree(e, stations) })
            .OrderByDescending(o => o.IsEquipmentFree)
            .ToList();
    }

    /// <summary>
    /// An alternative counts as available if a matching station for that muscle group is
    /// free, or if it uses dumbbells/machines (assumed to have several units in the gym).
    /// </summary>
    private static bool IsEquipmentFree(Exercise exercise, List<Station> stations)
    {
        var matchingFree = stations.Any(s =>
            s.MuscleGroup == exercise.MuscleGroup &&
            s.Status == StationStatus.Free &&
            s.Name.Contains(exercise.Equipment, StringComparison.OrdinalIgnoreCase));

        return matchingFree || exercise.Equipment is "Dumbbell" or "Machine";
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

        return alternatives
            .Select(alt => new ExerciseSwapOption { Exercise = alt, IsEquipmentFree = IsEquipmentFree(alt, stations) })
            .OrderByDescending(o => o.IsEquipmentFree)
            .ToList();
    }

    // ---------------- Routines ----------------

    public async Task<List<Routine>> GetRoutinesAsync()
    {
        var conn = await _db.GetConnectionAsync();
        var routines = await conn.Table<Routine>().OrderBy(r => r.Id).ToListAsync();
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

    public async Task<Routine?> GetRoutineAsync(int routineId)
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.FindAsync<Routine>(routineId);
    }

    /// <summary>
    /// Saves the routine's exercise list in its current order. Simplest correct approach:
    /// remove the old rows for this routine, then insert the new list with fresh order indexes.
    /// </summary>
    public async Task ReplaceRoutineExercisesAsync(int routineId, IList<RoutineExercise> items)
    {
        var conn = await _db.GetConnectionAsync();
        await conn.ExecuteAsync("DELETE FROM routine_exercises WHERE RoutineId = ?", routineId);

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            item.Id = 0;
            item.RoutineId = routineId;
            item.OrderIndex = i;
            await conn.InsertAsync(item);
        }
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

    /// <summary>
    /// The sets logged for an exercise in the most recent session that included it.
    /// Fills the PREV column in the live tracker.
    /// </summary>
    public async Task<List<SetLog>> GetPreviousSetsAsync(int exerciseId)
    {
        var conn = await _db.GetConnectionAsync();
        var logs = await conn.Table<SetLog>().Where(s => s.ExerciseId == exerciseId).ToListAsync();
        if (logs.Count == 0) return new();

        var lastSessionId = logs.Max(s => s.SessionId);
        return logs.Where(s => s.SessionId == lastSessionId).OrderBy(s => s.SetNumber).ToList();
    }

    /// <summary>Personal record = heaviest set ever logged (ties broken by more reps).</summary>
    public async Task<SetLog?> GetPersonalRecordAsync(int exerciseId)
    {
        var conn = await _db.GetConnectionAsync();
        var logs = await conn.Table<SetLog>().Where(s => s.ExerciseId == exerciseId).ToListAsync();
        return logs.OrderByDescending(s => s.WeightKg).ThenByDescending(s => s.Reps).FirstOrDefault();
    }

    /// <summary>Permanent swap: the routine will use the new exercise from now on.</summary>
    public async Task UpdateRoutineExerciseAsync(int routineExerciseId, int newExerciseId)
    {
        var conn = await _db.GetConnectionAsync();
        var row = await conn.FindAsync<RoutineExercise>(routineExerciseId);
        if (row is null) return;
        row.ExerciseId = newExerciseId;
        await conn.UpdateAsync(row);
    }

    /// <summary>
    /// Dynamic macro trigger: finishing a workout switches today to a Training Day,
    /// which raises the calorie and carb targets. Returns the before/after values.
    /// </summary>
    public async Task<TrainingDayChange> ActivateTrainingDayAsync()
    {
        var settings = await GetSettingsAsync();
        var already = settings.IsTrainingToday;

        var change = new TrainingDayChange
        {
            WasAlreadyTrainingDay = already,
            CaloriesBefore = already ? settings.BaseCalories : settings.RestCalories,
            CarbsBefore = already ? settings.BaseCarbsG : settings.RestCarbsG,
            CaloriesAfter = settings.BaseCalories,
            CarbsAfter = settings.BaseCarbsG
        };

        settings.TodayType = DayType.Training;
        settings.LastTrainingDate = DateTime.Today.ToString("yyyy-MM-dd");
        await SaveSettingsAsync(settings);
        return change;
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

    /// <summary>Logs a food against a meal and date (macros = food × quantity).</summary>
    public Task LogFoodAsync(FoodItem food, double quantity, MealCategory category, DateTime date) =>
        LogMealAsync(MealLog.From(food, quantity, category, date));

    /// <summary>Changes the amount of a logged food and recalculates its macros.</summary>
    public async Task UpdateMealQuantityAsync(MealLog log, double quantity)
    {
        var conn = await _db.GetConnectionAsync();
        var food = await conn.FindAsync<FoodItem>(log.FoodId);

        if (food is not null)
        {
            var updated = MealLog.From(food, quantity, log.Category, DateTime.ParseExact(log.DateKey, "yyyy-MM-dd", null));
            updated.Id = log.Id;
            await conn.UpdateAsync(updated);
            return;
        }

        // Food was removed from the catalogue: scale the stored totals instead.
        var factor = log.Quantity <= 0 ? 1 : quantity / log.Quantity;
        log.Calories = (int)Math.Round(log.Calories * factor);
        log.ProteinG = Math.Round(log.ProteinG * factor, 1);
        log.CarbsG = Math.Round(log.CarbsG * factor, 1);
        log.FatG = Math.Round(log.FatG * factor, 1);
        log.Quantity = quantity;
        await conn.UpdateAsync(log);
    }

    public async Task<bool> FoodNameExistsAsync(string name)
    {
        var conn = await _db.GetConnectionAsync();
        var all = await conn.Table<FoodItem>().ToListAsync();
        return all.Any(f => string.Equals(f.Name.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// A date counts as a Training Day if a workout was completed on it. This is what
    /// makes the nutrition targets switch automatically after a workout.
    /// </summary>
    public async Task<bool> IsTrainingDayAsync(DateTime date)
    {
        var conn = await _db.GetConnectionAsync();
        var start = date.Date;
        var end = start.AddDays(1);

        var sessions = await conn.Table<WorkoutSession>().Where(s => s.IsCompleted).ToListAsync();
        if (sessions.Any(s => s.CompletedAt >= start && s.CompletedAt < end))
            return true;

        var settings = await GetSettingsAsync();
        return settings.LastTrainingDate == start.ToString("yyyy-MM-dd");
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
