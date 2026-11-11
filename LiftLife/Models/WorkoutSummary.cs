namespace LiftLife.Models;

/// <summary>
/// Result shown on the Workout Summary screen (4.5) after finishing a session.
/// Not a database table — it's built from the saved session.
/// </summary>
public class WorkoutSummary
{
    public string RoutineName { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; } = DateTime.Now;
    public TimeSpan Duration { get; set; }
    public double TotalVolumeKg { get; set; }
    public int SetsCompleted { get; set; }
    public List<ExerciseResult> Results { get; set; } = new();
    public TrainingDayChange Macros { get; set; } = new();

    public string DateText => CompletedAt.ToString("dddd, dd MMM yyyy • h:mm tt");
    public string DurationText => Duration.TotalHours >= 1
        ? Duration.ToString(@"h\:mm\:ss")
        : Duration.ToString(@"mm\:ss");
    public string VolumeText => $"{TotalVolumeKg:N0} kg";
    public string SetsText => SetsCompleted.ToString();
    public string ExercisesText => Results.Count.ToString();
    public int NewPrCount => Results.Count(r => r.IsNewPr);
    public string PrText => NewPrCount == 0 ? "No new PRs this time" : $"{NewPrCount} new personal record{(NewPrCount == 1 ? "" : "s")}!";
}

/// <summary>One exercise's result in the summary.</summary>
public class ExerciseResult
{
    public string Name { get; set; } = string.Empty;
    public int SetsDone { get; set; }
    public double BestWeightKg { get; set; }
    public int BestReps { get; set; }
    public bool IsNewPr { get; set; }

    public string DetailText => $"{SetsDone} set{(SetsDone == 1 ? "" : "s")} • best {BestWeightKg:0.#} kg × {BestReps}";
}

/// <summary>
/// How finishing a workout changed today's nutrition targets (the "dynamic macro trigger").
/// </summary>
public class TrainingDayChange
{
    public bool WasAlreadyTrainingDay { get; set; }
    public int CaloriesBefore { get; set; }
    public int CaloriesAfter { get; set; }
    public double CarbsBefore { get; set; }
    public double CarbsAfter { get; set; }

    public string HeadlineText => WasAlreadyTrainingDay
        ? "Training Day already active"
        : "Training Day activated";

    public string CaloriesText => WasAlreadyTrainingDay
        ? $"Calories target: {CaloriesAfter:N0} kcal"
        : $"Calories: {CaloriesBefore:N0} → {CaloriesAfter:N0} kcal";

    public string CarbsText => WasAlreadyTrainingDay
        ? $"Carbs target: {CarbsAfter:0} g"
        : $"Carbs: {CarbsBefore:0} → {CarbsAfter:0} g";
}
