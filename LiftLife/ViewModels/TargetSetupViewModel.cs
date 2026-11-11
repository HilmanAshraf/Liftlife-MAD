using LiftLife.Data;
using LiftLife.Models;
using LiftLife.Services;

namespace LiftLife.ViewModels;

/// <summary>
/// ViewModel for the Target Macro Setup (screen 6.2): the baseline (training day) targets
/// and how much calories and carbs drop on rest days, with a live preview of both days.
/// </summary>
public class TargetSetupViewModel : BaseViewModel
{
    private readonly LiftLifeRepository _repo;
    private UserSettings _settings = new();
    private string _calories = string.Empty;
    private string _protein = string.Empty;
    private string _carbs = string.Empty;
    private string _fat = string.Empty;
    private double _restCaloriePercent = 85;
    private double _restCarbPercent = 70;

    public TargetSetupViewModel(LiftLifeRepository repo) => _repo = repo;

    public string Calories { get => _calories; set { if (SetProperty(ref _calories, value)) RefreshPreview(); } }
    public string Protein { get => _protein; set { if (SetProperty(ref _protein, value)) RefreshPreview(); } }
    public string Carbs { get => _carbs; set { if (SetProperty(ref _carbs, value)) RefreshPreview(); } }
    public string Fat { get => _fat; set { if (SetProperty(ref _fat, value)) RefreshPreview(); } }

    /// <summary>Rest-day calories as a % of training-day calories (Stepper, 50–100).</summary>
    public double RestCaloriePercent
    {
        get => _restCaloriePercent;
        set { if (SetProperty(ref _restCaloriePercent, value)) RefreshPreview(); }
    }

    /// <summary>Rest-day carbs as a % of training-day carbs (Stepper, 30–100).</summary>
    public double RestCarbPercent
    {
        get => _restCarbPercent;
        set { if (SetProperty(ref _restCarbPercent, value)) RefreshPreview(); }
    }

    private int? CaloriesValue => NumberInput.ToInt(Calories);
    private double? ProteinValue => NumberInput.ToDouble(Protein);
    private double? CarbsValue => NumberInput.ToDouble(Carbs);
    private double? FatValue => NumberInput.ToDouble(Fat);

    public string RestCaloriesText => CaloriesValue is int c
        ? $"{RestCaloriePercent:0}% of training • {c * RestCaloriePercent / 100:N0} kcal"
        : $"{RestCaloriePercent:0}% of training";

    public string RestCarbsText => CarbsValue is double c
        ? $"{RestCarbPercent:0}% of training • {c * RestCarbPercent / 100:0} g"
        : $"{RestCarbPercent:0}% of training";

    public string TrainingPreview => CaloriesValue is int c && ProteinValue is double p && CarbsValue is double cb && FatValue is double f
        ? $"⚡ Training day: {c:N0} kcal • P{p:0} C{cb:0} F{f:0}"
        : "⚡ Training day: enter all four values";

    public string RestPreview => CaloriesValue is int c && ProteinValue is double p && CarbsValue is double cb && FatValue is double f
        ? $"🌙 Rest day: {Math.Round(c * RestCaloriePercent / 100):N0} kcal • P{p:0} C{Math.Round(cb * RestCarbPercent / 100):0} F{f:0}"
        : "🌙 Rest day: enter all four values";

    /// <summary>Calories implied by the macros (4/4/9 kcal per gram).</summary>
    private double MacroCalories => (ProteinValue ?? 0) * 4 + (CarbsValue ?? 0) * 4 + (FatValue ?? 0) * 9;

    public bool IsMacroMismatch =>
        CaloriesValue is > 0 && MacroCalories > 0 && Math.Abs(CaloriesValue.Value - MacroCalories) / CaloriesValue.Value > 0.1;

    public string MacroCheckText => MacroCalories <= 0
        ? "Protein and carbs are 4 kcal per gram, fat is 9 kcal per gram."
        : IsMacroMismatch
            ? $"Your macros add up to {MacroCalories:N0} kcal, more than 10% away from your calorie target."
            : $"Your macros add up to {MacroCalories:N0} kcal ✓";

    public async Task LoadAsync()
    {
        _settings = await _repo.GetSettingsAsync();
        Apply(_settings);
    }

    /// <summary>Fills the form with the original default targets (not saved until Save).</summary>
    public void ResetToDefaults() => Apply(new UserSettings());

    /// <returns>An error message, or null if saved.</returns>
    public async Task<string?> SaveAsync()
    {
        if (CaloriesValue is null or < 1000 or > 6000)
            return "Calories must be a whole number between 1000 and 6000.";
        if (ProteinValue is null or < 30 or > 400)
            return "Protein must be between 30 and 400 g.";
        if (CarbsValue is null or < 30 or > 800)
            return "Carbs must be between 30 and 800 g.";
        if (FatValue is null or < 20 or > 250)
            return "Fat must be between 20 and 250 g.";

        _settings.BaseCalories = CaloriesValue.Value;
        _settings.BaseProteinG = ProteinValue.Value;
        _settings.BaseCarbsG = CarbsValue.Value;
        _settings.BaseFatG = FatValue.Value;
        _settings.RestDayCalorieFactor = RestCaloriePercent / 100;
        _settings.RestDayCarbFactor = RestCarbPercent / 100;

        await _repo.SaveSettingsAsync(_settings);
        return null;
    }

    private void Apply(UserSettings s)
    {
        Calories = s.BaseCalories.ToString();
        Protein = NumberInput.Format(s.BaseProteinG);
        Carbs = NumberInput.Format(s.BaseCarbsG);
        Fat = NumberInput.Format(s.BaseFatG);
        RestCaloriePercent = Math.Round(s.RestDayCalorieFactor * 100);
        RestCarbPercent = Math.Round(s.RestDayCarbFactor * 100);
    }

    private void RefreshPreview()
    {
        OnPropertyChanged(nameof(RestCaloriesText));
        OnPropertyChanged(nameof(RestCarbsText));
        OnPropertyChanged(nameof(TrainingPreview));
        OnPropertyChanged(nameof(RestPreview));
        OnPropertyChanged(nameof(MacroCheckText));
        OnPropertyChanged(nameof(IsMacroMismatch));
    }
}
