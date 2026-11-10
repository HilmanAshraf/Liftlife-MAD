using SQLite;

namespace LiftLife.Models;

/// <summary>
/// A completed (or in-progress) workout session. Stores the total volume lifted
/// so the Routine Hub can show history like "Yesterday: Pull Day B • 5,420 kg".
/// </summary>
[Table("workout_sessions")]
public class WorkoutSession
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int RoutineId { get; set; }

    public string RoutineName { get; set; } = string.Empty;

    public DateTime StartedAt { get; set; } = DateTime.Now;

    public DateTime? CompletedAt { get; set; }

    /// <summary>Total volume = sum of weight × reps across all completed sets (kg).</summary>
    public double TotalVolumeKg { get; set; }

    public bool IsCompleted { get; set; }

    /// <summary>History line for the Routine Hub, e.g. "Yesterday: Pull Day B • 5,420 kg logged".</summary>
    [Ignore]
    public string HistoryText
    {
        get
        {
            var date = (CompletedAt ?? StartedAt).Date;
            var day = date == DateTime.Today ? "Today"
                : date == DateTime.Today.AddDays(-1) ? "Yesterday"
                : date.ToString("dd MMM");
            return $"{day}: {RoutineName} • {TotalVolumeKg:N0} kg logged";
        }
    }
}

/// <summary>
/// A single logged set within a session: the exercise, set number, weight and reps.
/// </summary>
[Table("set_logs")]
public class SetLog
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int SessionId { get; set; }

    public int ExerciseId { get; set; }

    public string ExerciseName { get; set; } = string.Empty;

    public int SetNumber { get; set; }

    public double WeightKg { get; set; }

    public int Reps { get; set; }

    public bool IsDone { get; set; }

    [Ignore]
    public double Volume => WeightKg * Reps;
}
