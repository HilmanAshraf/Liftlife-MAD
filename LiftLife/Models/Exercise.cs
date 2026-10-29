using SQLite;

namespace LiftLife.Models;

/// <summary>
/// A master exercise in the local catalogue. Used to build routines and to power
/// the biomechanical substitution engine (exercises sharing a MuscleGroup are
/// valid swaps for each other).
/// </summary>
[Table("exercises")]
public class Exercise
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Primary muscle group, e.g. Chest, Back, Legs, Shoulders.</summary>
    public string MuscleGroup { get; set; } = string.Empty;

    /// <summary>Equipment needed, e.g. Barbell, Dumbbell, Machine, Cable.</summary>
    public string Equipment { get; set; } = string.Empty;

    /// <summary>Short biomechanical cue / safety tip shown in the form guide.</summary>
    public string FormCue { get; set; } = string.Empty;

    /// <summary>Default rest in seconds suggested for this exercise.</summary>
    public int DefaultRestSeconds { get; set; } = 90;
}
