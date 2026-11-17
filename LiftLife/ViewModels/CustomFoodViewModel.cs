using LiftLife.Data;
using LiftLife.Models;
using LiftLife.Services;

namespace LiftLife.ViewModels;

/// <summary>
/// Quick Add form on the Add Food sheet (screen 5.2): the member types any food, its calories
/// and (optionally) macros, and logs it straight away. Useful for cafeteria or home-cooked meals
/// that aren't in the food list. Can also save the food to "My Foods" for next time.
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
    private double _servings = 1;
    private bool _saveToMyFoods = true;

    public CustomFoodViewModel(LiftLifeRepository repo) => _repo = repo;

    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Serving
    {
        get => _serving;
        set
        {
            if (SetProperty(ref _serving, value))
                OnPropertyChanged(nameof(ServingsText)); // "1.5 × 1 plate" follows the serving text
        }
    }

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

    /// <summary>How many servings were eaten (Stepper, 0.5–20).</summary>
    public double Servings
    {
        get => _servings;
        set
        {
            if (SetProperty(ref _servings, value))
                OnPropertyChanged(nameof(ServingsText));
        }
    }

    public string ServingsText => $"{Servings:0.#} × {(string.IsNullOrWhiteSpace(Serving) ? "serving" : Serving.Trim())}";

    /// <summary>On = also add it to the food list so it can be searched next time.</summary>
    public bool SaveToMyFoods { get => _saveToMyFoods; set => SetProperty(ref _saveToMyFoods, value); }

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
            if (estimate <= 0) return "Macros are optional. Protein and carbs are 4 kcal per gram, fat is 9 kcal per gram.";
            return IsMismatch
                ? $"Heads up: these macros add up to about {estimate:0} kcal. Double-check the numbers."
                : $"Macros add up to about {estimate:0} kcal ✓";
        }
    }

    /// <summary>Starts the form with whatever the member was searching for.</summary>
    public void Prefill(string? name)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = char.ToUpper(name.Trim()[0]) + name.Trim()[1..];
    }

    /// <summary>Checks the form and logs the food to the chosen meal and day.</summary>
    /// <returns>An error message, or null if logged.</returns>
    public async Task<string?> LogAsync(MealCategory meal, DateTime date)
    {
        var (food, error) = Validate();
        if (food is null) return error;

        if (Servings is < 0.5 or > 20)
            return "Choose between 0.5 and 20 servings.";

        await _repo.LogQuickFoodAsync(food, Servings, meal, date, SaveToMyFoods);
        return null;
    }

    /// <returns>(food, null) if the form is valid, or (null, error message).</returns>
    private (FoodItem? food, string? error) Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return (null, "Please enter what you ate.");

        var calories = NumberInput.ToInt(Calories);
        if (calories is null or < 0 or > 3000)
            return (null, "Calories must be a whole number between 0 and 3000 per serving.");

        var protein = NumberInput.ToDouble(string.IsNullOrWhiteSpace(Protein) ? "0" : Protein);
        var carbs = NumberInput.ToDouble(string.IsNullOrWhiteSpace(Carbs) ? "0" : Carbs);
        var fat = NumberInput.ToDouble(string.IsNullOrWhiteSpace(Fat) ? "0" : Fat);
        if (protein is null or < 0 or > 300 || carbs is null or < 0 or > 500 || fat is null or < 0 or > 300)
            return (null, "Macros must be numbers: protein 0–300 g, carbs 0–500 g, fat 0–300 g.");

        return (new FoodItem
        {
            Name = Name.Trim(),
            Serving = string.IsNullOrWhiteSpace(Serving) ? "1 serving" : Serving.Trim(),
            Calories = calories.Value,
            ProteinG = protein.Value,
            CarbsG = carbs.Value,
            FatG = fat.Value
        }, null);
    }

    private void RefreshEstimate()
    {
        OnPropertyChanged(nameof(EstimateText));
        OnPropertyChanged(nameof(IsMismatch));
    }
}
