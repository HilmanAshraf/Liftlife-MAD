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

    /// <summary>Second line in the food search list, e.g. "100 g • P31 C0 F4".</summary>
    [Ignore]
    public string ServingLine => $"{Serving} • P{ProteinG:0} C{CarbsG:0} F{FatG:0}";

    [Ignore]
    public string CaloriesText => $"{Calories} kcal";
}

/// <summary>Calorie and macro targets for one day.</summary>
public record MacroTargets(int Calories, double ProteinG, double CarbsG, double FatG);

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

    /// <summary>Serving description copied from the food, e.g. "100 g".</summary>
    public string Serving { get; set; } = string.Empty;

    // Totals for this entry (food macros × quantity), copied at log time
    // so the history stays the same even if the food is changed later.
    public int Calories { get; set; }
    public double ProteinG { get; set; }
    public double CarbsG { get; set; }
    public double FatG { get; set; }

    /// <summary>Builds a log entry for a food, multiplying its macros by the quantity.</summary>
    public static MealLog From(FoodItem food, double quantity, MealCategory category, DateTime date) => new()
    {
        DateKey = date.ToString("yyyy-MM-dd"),
        Category = category,
        FoodId = food.Id,
        FoodName = food.Name,
        Serving = food.Serving,
        Quantity = quantity,
        Calories = (int)Math.Round(food.Calories * quantity),
        ProteinG = Math.Round(food.ProteinG * quantity, 1),
        CarbsG = Math.Round(food.CarbsG * quantity, 1),
        FatG = Math.Round(food.FatG * quantity, 1)
    };

    /// <summary>e.g. "1.5 × 100 g • P46 C0 F5".</summary>
    [Ignore]
    public string DetailText =>
        $"{Quantity:0.##} × {(string.IsNullOrWhiteSpace(Serving) ? "serving" : Serving)} • P{ProteinG:0} C{CarbsG:0} F{FatG:0}";

    [Ignore]
    public string CaloriesText => $"{Calories} kcal";
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

    /// <summary>
    /// Adaptive targets: a training day uses the full baseline; a rest day lowers calories
    /// and carbs. Protein and fat stay the same to protect muscle recovery.
    /// </summary>
    public MacroTargets TargetsFor(bool isTrainingDay) => isTrainingDay
        ? new MacroTargets(BaseCalories, BaseProteinG, BaseCarbsG, BaseFatG)
        : new MacroTargets(RestCalories, BaseProteinG, RestCarbsG, BaseFatG);
}
