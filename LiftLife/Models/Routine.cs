using SQLite;

namespace LiftLife.Models;

/// <summary>
/// A saved workout routine / split (e.g. "Push Day A"). Backs the Workout
/// Routine Hub (screen 3.0) and Routine Builder (4.1).
/// </summary>
[Table("routines")]
public class Routine
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Muscle focus summary, e.g. "Chest / Shoulders / Triceps".</summary>
    public string Focus { get; set; } = string.Empty;

    /// <summary>Estimated completion time in minutes.</summary>
    public int EstimatedMinutes { get; set; } = 45;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Ignore]
    public int ExerciseCount { get; set; }

    [Ignore]
    public string SummaryLine => $"{ExerciseCount} exercises • Estimated: {EstimatedMinutes} mins";
}

/// <summary>
/// Join between a routine and an exercise, with the target set/rep scheme and
/// per-exercise setup notes. Ordered within the routine by <see cref="OrderIndex"/>.
/// </summary>
[Table("routine_exercises")]
public class RoutineExercise
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int RoutineId { get; set; }

    public int ExerciseId { get; set; }

    public int OrderIndex { get; set; }

    public int TargetSets { get; set; } = 3;

    public int TargetReps { get; set; } = 10;

    public int RestSeconds { get; set; } = 90;

    /// <summary>Personal machine setup note, e.g. "Seat height 4, pin 60kg".</summary>
    public string? SetupNote { get; set; }

    // Hydrated from the exercise catalogue for display (not stored).
    [Ignore] public string ExerciseName { get; set; } = string.Empty;
    [Ignore] public string MuscleGroup { get; set; } = string.Empty;
    [Ignore] public string Equipment { get; set; } = string.Empty;
    [Ignore] public string FormCue { get; set; } = string.Empty;
}
