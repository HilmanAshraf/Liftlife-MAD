using LiftLife.Models;
using LiftLife.Services;
using LiftLife.ViewModels;

namespace LiftLife.Views;

/// <summary>Custom Food Creator (screen 5.2), opened from the Food Search sheet.</summary>
public partial class CustomFoodPage : ContentPage
{
    private readonly CustomFoodViewModel _vm;
    private readonly Func<FoodItem, Task> _onSaved;

    /// <param name="searchText">Pre-fills the name with what the member was searching for.</param>
    /// <param name="onSaved">Called with the new food so the search sheet can select it.</param>
    public CustomFoodPage(string? searchText, Func<FoodItem, Task> onSaved)
    {
        InitializeComponent();
        _onSaved = onSaved;
        _vm = ServiceHelper.Get<CustomFoodViewModel>();
        _vm.Prefill(searchText);
        BindingContext = _vm;
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var (food, error) = await _vm.SaveAsync();
        if (food is null)
        {
            await DisplayAlertAsync("Can't Save Yet", error, "OK");
            return;
        }

        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await _onSaved(food);
        await Navigation.PopModalAsync();
    }

    private async void OnCloseClicked(object? sender, EventArgs e) => await Navigation.PopModalAsync();
}
