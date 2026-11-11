using LiftLife.Data;
using LiftLife.Models;
using LiftLife.Services;

namespace LiftLife.ViewModels;

/// <summary>
/// ViewModel for the Custom Food Creator (screen 5.2): manual calorie and macro entry
/// for foods that aren't in the catalogue (e.g. a campus cafeteria meal).
/// </summary>
public class CustomFoodViewModel : BaseViewModel
{
    private readonly LiftLifeRepository _repo;
    private string _name = string.Empty;
    private string _serving = "1 serving";
    private string _calories = string.Empty;
    private string _protein = string.Empty;
    private string _carbs = string.Empty;
    private string _fat = string.Empty;

    public CustomFoodViewModel(LiftLifeRepository repo) => _repo = repo;

    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Serving { get => _serving; set => SetProperty(ref _serving, value); }

    public string Calories
    {
        get => _calories;
        set { if (SetProperty(ref _calories, value)) RefreshEstimate(); }
    }

    public string Protein
    {
        get => _protein;
        set { if (SetProperty(ref _protein, value)) RefreshEstimate(); }
    }

    public string Carbs
    {
        get => _carbs;
        set { if (SetProperty(ref _carbs, value)) RefreshEstimate(); }
    }

    public string Fat
    {
        get => _fat;
        set { if (SetProperty(ref _fat, value)) RefreshEstimate(); }
    }

    /// <summary>Calories implied by the macros: 4 kcal/g protein and carbs, 9 kcal/g fat.</summary>
    private double MacroCalories =>
        (NumberInput.ToDouble(Protein) ?? 0) * 4 +
        (NumberInput.ToDouble(Carbs) ?? 0) * 4 +
        (NumberInput.ToDouble(Fat) ?? 0) * 9;

    /// <summary>True when the typed calories are far from what the macros add up to.</summary>
    public bool IsMismatch
    {
        get
        {
            var calories = NumberInput.ToInt(Calories);
            var estimate = MacroCalories;
            return calories is > 0 && estimate > 0 && Math.Abs(calories.Value - estimate) / calories.Value > 0.2;
        }
    }

    public string EstimateText
    {
        get
        {
            var estimate = MacroCalories;
            if (estimate <= 0) return "Tip: protein and carbs are 4 kcal per gram, fat is 9 kcal per gram.";
            return IsMismatch
                ? $"Heads up: these macros add up to about {estimate:0} kcal. Double-check the label."
                : $"Macros add up to about {estimate:0} kcal ✓";
        }
    }

    /// <summary>Starts the form with whatever the member was searching for.</summary>
    public void Prefill(string? name)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = char.ToUpper(name.Trim()[0]) + name.Trim()[1..];
    }

    /// <returns>(saved food, null) on success, or (null, error message).</returns>
    public async Task<(FoodItem? food, string? error)> SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return (null, "Please enter a food name.");

        if (string.IsNullOrWhiteSpace(Serving))
            return (null, "Please describe the serving size (e.g. 1 bowl, 100 g).");

        var calories = NumberInput.ToInt(Calories);
        if (calories is null or < 0 or > 3000)
            return (null, "Calories must be a whole number between 0 and 3000.");

        var protein = NumberInput.ToDouble(string.IsNullOrWhiteSpace(Protein) ? "0" : Protein);
        var carbs = NumberInput.ToDouble(string.IsNullOrWhiteSpace(Carbs) ? "0" : Carbs);
        var fat = NumberInput.ToDouble(string.IsNullOrWhiteSpace(Fat) ? "0" : Fat);
        if (protein is null or < 0 or > 300 || carbs is null or < 0 or > 500 || fat is null or < 0 or > 300)
            return (null, "Macros must be numbers: protein 0–300 g, carbs 0–500 g, fat 0–300 g.");

        if (await _repo.FoodNameExistsAsync(Name))
            return (null, $"\"{Name.Trim()}\" is already in your food list.");

        var food = new FoodItem
        {
            Name = Name.Trim(),
            Serving = Serving.Trim(),
            Calories = calories.Value,
            ProteinG = protein.Value,
            CarbsG = carbs.Value,
            FatG = fat.Value
        };
        await _repo.AddCustomFoodAsync(food);
        return (food, null);
    }

    private void RefreshEstimate()
    {
        OnPropertyChanged(nameof(EstimateText));
        OnPropertyChanged(nameof(IsMismatch));
    }
}
