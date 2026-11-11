using LiftLife.Models;
using LiftLife.Services;
using LiftLife.ViewModels;

namespace LiftLife.Views;

/// <summary>Daily Nutrition Hub (screen 4.0).</summary>
public partial class NutritionPage : ContentPage
{
    private readonly NutritionViewModel _vm;

    public NutritionPage()
    {
        InitializeComponent();
        _vm = ServiceHelper.Get<NutritionViewModel>();
        BindingContext = _vm;
    }

    // Reloads every time the tab is shown, so finishing a workout updates the day type.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    private async void OnPreviousDayTapped(object? sender, TappedEventArgs e) => await _vm.ChangeDayAsync(-1);
    private async void OnNextDayTapped(object? sender, TappedEventArgs e) => await _vm.ChangeDayAsync(1);
    private async void OnSwipedRight(object? sender, SwipedEventArgs e) => await _vm.ChangeDayAsync(-1);
    private async void OnSwipedLeft(object? sender, SwipedEventArgs e) => await _vm.ChangeDayAsync(1);

    private async void OnEditTargetsTapped(object? sender, TappedEventArgs e) =>
        await Navigation.PushAsync(new TargetSetupPage());

    private async void OnAddFoodClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is MealSection section)
            await Navigation.PushModalAsync(new FoodLogPage(section.Category, _vm.SelectedDate, _vm.LoadAsync));
    }

    /// <summary>Tap a logged food to change how many servings you had.</summary>
    private async void OnItemTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not MealLog log)
            return;

        var input = await DisplayPromptAsync(
            "Change Amount",
            $"Servings of {log.FoodName} ({log.Serving})",
            accept: "Save",
            cancel: "Cancel",
            initialValue: NumberInput.Format(log.Quantity),
            maxLength: 5,
            keyboard: Keyboard.Numeric);

        if (input is null) return; // cancelled

        var quantity = NumberInput.ToDouble(input);
        if (quantity is null or < 0.25 or > 20)
        {
            await DisplayAlertAsync("Check the Amount", "Enter between 0.25 and 20 servings.", "OK");
            return;
        }

        await _vm.UpdateQuantityAsync(log, quantity.Value);
    }

    private async void OnDeleteInvoked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not MealLog log)
            return;

        var confirm = await DisplayAlertAsync("Remove Food", $"Remove {log.FoodName} from this day?", "Remove", "Cancel");
        if (confirm)
            await _vm.DeleteAsync(log);
    }
}
