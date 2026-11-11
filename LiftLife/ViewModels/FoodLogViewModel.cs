using System.Collections.ObjectModel;
using LiftLife.Data;
using LiftLife.Models;

namespace LiftLife.ViewModels;

/// <summary>
/// ViewModel for the Food Search &amp; Log sheet (screen 5.1): search the local food
/// catalogue, pick a meal and number of servings, and log it to SQLite.
/// </summary>
public class FoodLogViewModel : BaseViewModel
{
    private readonly LiftLifeRepository _repo;
    private DateTime _date = DateTime.Today;
    private string _searchText = string.Empty;
    private FoodItem? _selectedFood;
    private double _quantity = 1;

    public FoodLogViewModel(LiftLifeRepository repo) => _repo = repo;

    public ObservableCollection<FoodItem> Foods { get; } = new();

    /// <summary>Meal chips (reuses the filter-chip class from the Community Hub).</summary>
    public ObservableCollection<CategoryChip> Meals { get; } = new();

    public string Subtitle => _date == DateTime.Today ? "Logging for today" : $"Logging for {_date:ddd, d MMM}";

    /// <summary>Searches as you type.</summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                _ = SearchAsync();
        }
    }

    public FoodItem? SelectedFood
    {
        get => _selectedFood;
        set
        {
            if (SetProperty(ref _selectedFood, value))
                RefreshPreview();
        }
    }

    public double Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
                RefreshPreview();
        }
    }

    public string SelectedName => SelectedFood?.Name ?? "Select a food";

    public string QuantityText => $"{Quantity:0.#} × {SelectedFood?.Serving ?? "serving"}";

    /// <summary>Live preview of what will be logged (food macros × servings).</summary>
    public string PreviewText => SelectedFood is null
        ? "Tap a food in the list, then choose how many servings."
        : $"{SelectedFood.Calories * Quantity:0} kcal • P{SelectedFood.ProteinG * Quantity:0} C{SelectedFood.CarbsG * Quantity:0} F{SelectedFood.FatG * Quantity:0}";

    public MealCategory SelectedMeal =>
        Enum.TryParse<MealCategory>(Meals.FirstOrDefault(m => m.IsSelected)?.Name, out var meal) ? meal : MealCategory.Snacks;

    public async Task InitAsync(MealCategory meal, DateTime date)
    {
        _date = date;
        OnPropertyChanged(nameof(Subtitle));

        Meals.Clear();
        foreach (var category in Enum.GetValues<MealCategory>())
            Meals.Add(new CategoryChip { Name = category.ToString(), IsSelected = category == meal });

        await SearchAsync();
    }

    public void SelectMeal(CategoryChip chip)
    {
        foreach (var m in Meals)
            m.IsSelected = m == chip;
    }

    public async Task SearchAsync()
    {
        var term = SearchText;
        var results = await _repo.SearchFoodsAsync(term);
        if (term != SearchText) return; // a newer search has started

        Foods.Clear();
        foreach (var food in results)
            Foods.Add(food);

        // Keep the selection highlighted if it's still in the list.
        if (SelectedFood is not null)
            SelectedFood = Foods.FirstOrDefault(f => f.Id == SelectedFood.Id) ?? SelectedFood;
    }

    /// <summary>After creating a custom food: clear the search and select the new food.</summary>
    public async Task SelectNewFoodAsync(FoodItem food)
    {
        _searchText = string.Empty;
        OnPropertyChanged(nameof(SearchText));
        await SearchAsync();
        SelectedFood = Foods.FirstOrDefault(f => f.Id == food.Id);
        Quantity = 1;
    }

    /// <returns>An error message, or null if logged.</returns>
    public async Task<string?> LogAsync()
    {
        if (SelectedFood is null)
            return "Tap a food in the list first.";

        if (Quantity is < 0.5 or > 20)
            return "Choose between 0.5 and 20 servings.";

        await _repo.LogFoodAsync(SelectedFood, Quantity, SelectedMeal, _date);
        return null;
    }

    private void RefreshPreview()
    {
        OnPropertyChanged(nameof(SelectedName));
        OnPropertyChanged(nameof(QuantityText));
        OnPropertyChanged(nameof(PreviewText));
    }
}
