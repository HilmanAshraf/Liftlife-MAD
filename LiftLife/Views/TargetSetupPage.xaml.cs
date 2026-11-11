using LiftLife.Services;
using LiftLife.ViewModels;

namespace LiftLife.Views;

/// <summary>Target Macro Setup (screen 6.2), opened from "Edit targets" on the Nutrition Hub.</summary>
public partial class TargetSetupPage : ContentPage
{
    private readonly TargetSetupViewModel _vm;
    private bool _loaded;

    public TargetSetupPage()
    {
        InitializeComponent();
        _vm = ServiceHelper.Get<TargetSetupViewModel>();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded) return;
        _loaded = true;
        await _vm.LoadAsync();
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var error = await _vm.SaveAsync();
        if (error is not null)
        {
            await DisplayAlertAsync("Check Your Targets", error, "OK");
            return;
        }

        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        await Navigation.PopAsync(); // the hub reloads with the new targets
    }

    private async void OnResetClicked(object? sender, EventArgs e)
    {
        var reset = await DisplayAlertAsync("Reset Targets",
            "Fill in the default targets? They're only saved when you tap Save.", "Reset", "Cancel");
        if (reset)
            _vm.ResetToDefaults();
    }
}
