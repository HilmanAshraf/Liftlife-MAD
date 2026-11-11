using SQLite;

namespace LiftLife.Models;

/// <summary>
/// A food item in the local food catalogue (per 1 serving). Backs the food search
/// and custom food creator (screens 5.1 / 5.2).
/// </summary>
[Table("foods")]
public class FoodItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Serving description, e.g. "100 g", "1 cup", "1 scoop".</summary>
    public string Serving { get; set; } = "1 serving";

    public int Calories { get; set; }
    public double ProteinG { get; set; }
    public double CarbsG { get; set; }
    public double FatG { get; set; }

    /// <summary>True for user-created custom foods (vs. seeded defaults).</summary>
    public bool IsCustom { get; set; }

    [Ignore]
    public string MacroLine => $"{Calories} kcal • P{ProteinG:0} C{CarbsG:0} F{FatG:0}";
}

/// <summary>
/// A food logged against a given date and meal category. Quantity multiplies the
/// food's per-serving macros.
/// </summary>
[Table("meal_logs")]
public class MealLog
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public string DateKey { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");

    public MealCategory Category { get; set; }

    public int FoodId { get; set; }
    public string FoodName { get; set; } = string.Empty;

    public double Quantity { get; set; } = 1;

    // Snapshot of macros at log time (so later edits to the food don't change history).
    public int Calories { get; set; }
    public double ProteinG { get; set; }
    public double CarbsG { get; set; }
    public double FatG { get; set; }
}

/// <summary>
/// Single-row user profile holding baseline macro targets and today's day type.
/// Training-day targets are the baseline; rest-day targets reduce calories/carbs.
/// Backs the Target Macro Setup (6.2) and the Daily Nutrition Hub (4.0).
/// </summary>
[Table("user_settings")]
public class UserSettings
{
    [PrimaryKey]
    public int Id { get; set; } = 1; // always a single row

    // Baseline (training day) targets
    public int BaseCalories { get; set; } = 2450;
    public double BaseProteinG { get; set; } = 185;
    public double BaseCarbsG { get; set; } = 280;
    public double BaseFatG { get; set; } = 65;

    /// <summary>On rest days, calories are scaled by this factor (e.g. 0.85 = -15%).</summary>
    public double RestDayCalorieFactor { get; set; } = 0.85;

    /// <summary>On rest days, carbs are scaled by this factor.</summary>
    public double RestDayCarbFactor { get; set; } = 0.70;

    public DayType TodayType { get; set; } = DayType.Rest;

    /// <summary>Date (yyyy-MM-dd) of the last completed workout. Used to auto-switch to Training Day.</summary>
    public string? LastTrainingDate { get; set; }

    [Ignore]
    public bool IsTrainingToday => LastTrainingDate == DateTime.Today.ToString("yyyy-MM-dd");

    [Ignore]
    public int RestCalories => (int)Math.Round(BaseCalories * RestDayCalorieFactor);

    [Ignore]
    public double RestCarbsG => Math.Round(BaseCarbsG * RestDayCarbFactor);

    // ----- Computed targets for the active day (not stored) -----

    [Ignore]
    public int TargetCalories => TodayType == DayType.Training
        ? BaseCalories
        : (int)Math.Round(BaseCalories * RestDayCalorieFactor);

    [Ignore]
    public double TargetCarbsG => TodayType == DayType.Training
        ? BaseCarbsG
        : Math.Round(BaseCarbsG * RestDayCarbFactor);

    [Ignore]
    public double TargetProteinG => BaseProteinG; // protein stays high on rest days

    [Ignore]
    public double TargetFatG => BaseFatG;
}
