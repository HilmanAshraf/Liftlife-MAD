using LiftLife.Models;
using LiftLife.Services;
using LiftLife.ViewModels;

namespace LiftLife.Views;

/// <summary>Food Search &amp; Log sheet (screen 5.1), opened from "+ Add Food".</summary>
public partial class FoodLogPage : ContentPage
{
    private readonly FoodLogViewModel _vm;
    private readonly MealCategory _meal;
    private readonly DateTime _date;
    private readonly Func<Task> _onLogged;
    private bool _loaded;

    /// <param name="meal">Meal the sheet was opened from (can be changed with the chips).</param>
    /// <param name="date">Day being logged (the date shown on the Nutrition Hub).</param>
    /// <param name="onLogged">Called after logging so the hub refreshes its totals.</param>
    public FoodLogPage(MealCategory meal, DateTime date, Func<Task> onLogged)
    {
        InitializeComponent();
        _meal = meal;
        _date = date;
        _onLogged = onLogged;
        _vm = ServiceHelper.Get<FoodLogViewModel>();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded) return; // load once, so typed text isn't reset
        _loaded = true;
        await _vm.InitAsync(_meal, _date);
    }

    private void OnMealTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is CategoryChip chip)
            _vm.SelectMeal(chip);
    }

    private void OnSearchTabTapped(object? sender, EventArgs e) => _vm.IsQuickAdd = false;

    // Used by the tab, the "Quick Add it" button and the list footer.
    private void OnQuickAddTabTapped(object? sender, EventArgs e) => _vm.StartQuickAdd();

    private async void OnLogClicked(object? sender, EventArgs e)
    {
        var error = await _vm.LogAsync();
        if (error is not null)
        {
            await DisplayAlertAsync("Can't Log Yet", error, "OK");
            return;
        }

        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await _onLogged();
        await Navigation.PopModalAsync();
    }

    private async void OnCloseClicked(object? sender, EventArgs e) => await Navigation.PopModalAsync();
}
