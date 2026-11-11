using System.Collections.ObjectModel;
using LiftLife.Data;
using LiftLife.Models;

namespace LiftLife.ViewModels;

/// <summary>One macro progress bar (Protein, Carbs or Fat).</summary>
public class MacroProgress : BaseViewModel
{
    private string _text = string.Empty;
    private double _progress;
    private bool _isOver;

    public string Name { get; init; } = string.Empty;

    public string Text
    {
        get => _text;
        private set => SetProperty(ref _text, value);
    }

    /// <summary>0.0 to 1.0 (capped, so the bar never overflows).</summary>
    public double Progress
    {
        get => _progress;
        private set => SetProperty(ref _progress, value);
    }

    public bool IsOver
    {
        get => _isOver;
        private set => SetProperty(ref _isOver, value);
    }

    public void Update(double consumed, double target)
    {
        Text = $"{consumed:0}g / {target:0}g";
        Progress = target <= 0 ? 0 : Math.Min(1, consumed / target);
        IsOver = consumed > target;
    }
}

/// <summary>A meal card (Breakfast, Lunch, Dinner, Snacks) with its logged foods.</summary>
public class MealSection : BaseViewModel
{
    private string _totalText = "0 kcal";
    private bool _isEmpty = true;

    public MealSection(MealCategory category, string icon)
    {
        Category = category;
        Header = $"{icon}  {category}";
    }

    public MealCategory Category { get; }
    public string Header { get; }
    public ObservableCollection<MealLog> Items { get; } = new();

    public string TotalText
    {
        get => _totalText;
        private set => SetProperty(ref _totalText, value);
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetProperty(ref _isEmpty, value);
    }

    public void Update(IEnumerable<MealLog> logs)
    {
        Items.Clear();
        foreach (var log in logs)
            Items.Add(log);

        TotalText = $"{Items.Sum(i => i.Calories):N0} kcal";
        IsEmpty = Items.Count == 0;
    }
}

/// <summary>
/// ViewModel for the Daily Nutrition Hub (screen 4.0). For the selected date it works out
/// whether it's a Training or Rest day, the adaptive targets, and what's been eaten.
/// </summary>
public class NutritionViewModel : BaseViewModel
{
    private readonly LiftLifeRepository _repo;
    private DateTime _date = DateTime.Today;
    private bool _isTraining;
    private string _dayHint = string.Empty;
    private string _caloriesText = string.Empty;
    private string _remainingText = string.Empty;
    private double _caloriesProgress;
    private bool _isOverCalories;

    public NutritionViewModel(LiftLifeRepository repo)
    {
        _repo = repo;
        Sections = new ObservableCollection<MealSection>
        {
            new(MealCategory.Breakfast, "🍳"),
            new(MealCategory.Lunch, "🍱"),
            new(MealCategory.Dinner, "🍽️"),
            new(MealCategory.Snacks, "🍌"),
        };
    }

    public ObservableCollection<MealSection> Sections { get; }

    public MacroProgress Protein { get; } = new() { Name = "Protein" };
    public MacroProgress Carbs { get; } = new() { Name = "Carbs" };
    public MacroProgress Fat { get; } = new() { Name = "Fat" };

    public DateTime SelectedDate => _date;

    /// <summary>Short label for the date switcher: "Today", "Yesterday" or "9 Nov".</summary>
    public string DateText =>
        _date == DateTime.Today ? "Today"
        : _date == DateTime.Today.AddDays(-1) ? "Yesterday"
        : _date.ToString("d MMM");

    public string FullDateText => _date.ToString("dddd, d MMMM yyyy");

    /// <summary>You can't log food for future days.</summary>
    public bool CanGoNext => _date < DateTime.Today;

    public bool IsTraining
    {
        get => _isTraining;
        private set
        {
            if (SetProperty(ref _isTraining, value))
                OnPropertyChanged(nameof(DayBadgeText));
        }
    }

    public string DayBadgeText => IsTraining ? "⚡ TRAINING DAY ACTIVE" : "🌙 REST DAY";

    public string DayHint
    {
        get => _dayHint;
        private set => SetProperty(ref _dayHint, value);
    }

    public string CaloriesText
    {
        get => _caloriesText;
        private set => SetProperty(ref _caloriesText, value);
    }

    public string RemainingText
    {
        get => _remainingText;
        private set => SetProperty(ref _remainingText, value);
    }

    public double CaloriesProgress
    {
        get => _caloriesProgress;
        private set => SetProperty(ref _caloriesProgress, value);
    }

    public bool IsOverCalories
    {
        get => _isOverCalories;
        private set => SetProperty(ref _isOverCalories, value);
    }

    /// <summary>Recalculates everything for the selected date from SQLite.</summary>
    public async Task LoadAsync()
    {
        var settings = await _repo.GetSettingsAsync();
        IsTraining = await _repo.IsTrainingDayAsync(_date);
        var target = settings.TargetsFor(IsTraining);

        var logs = await _repo.GetMealLogsAsync(_date);
        var calories = logs.Sum(l => l.Calories);

        CaloriesText = $"Calories: {calories:N0} / {target.Calories:N0} kcal";
        CaloriesProgress = target.Calories <= 0 ? 0 : Math.Min(1, (double)calories / target.Calories);
        IsOverCalories = calories > target.Calories;
        RemainingText = IsOverCalories
            ? $"{calories - target.Calories:N0} kcal over today's target"
            : $"{target.Calories - calories:N0} kcal remaining";

        Protein.Update(logs.Sum(l => l.ProteinG), target.ProteinG);
        Carbs.Update(logs.Sum(l => l.CarbsG), target.CarbsG);
        Fat.Update(logs.Sum(l => l.FatG), target.FatG);

        var calorieCut = (int)Math.Round((1 - settings.RestDayCalorieFactor) * 100);
        var carbCut = (int)Math.Round((1 - settings.RestDayCarbFactor) * 100);
        DayHint = IsTraining
            ? "Workout logged — full calories and carbs to fuel recovery."
            : $"No workout logged — calories −{calorieCut}% and carbs −{carbCut}%. Finish a workout to switch to Training Day.";

        foreach (var section in Sections)
            section.Update(logs.Where(l => l.Category == section.Category));
    }

    /// <summary>Moves the selected date by a number of days (never into the future).</summary>
    public async Task ChangeDayAsync(int days)
    {
        var next = _date.AddDays(days);
        if (next > DateTime.Today) return;

        _date = next;
        OnPropertyChanged(nameof(DateText));
        OnPropertyChanged(nameof(FullDateText));
        OnPropertyChanged(nameof(CanGoNext));
        await LoadAsync();
    }

    public async Task DeleteAsync(MealLog log)
    {
        await _repo.DeleteMealLogAsync(log.Id);
        await LoadAsync();
    }

    public async Task UpdateQuantityAsync(MealLog log, double quantity)
    {
        await _repo.UpdateMealQuantityAsync(log, quantity);
        await LoadAsync();
    }
}
