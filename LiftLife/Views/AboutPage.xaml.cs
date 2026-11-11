using LiftLife.Data;
using LiftLife.Services;
using LiftLife.ViewModels;

namespace LiftLife.Views;

/// <summary>About LiftLife (screens 5.0 / 6.1): app, developer, storage and profile.</summary>
public partial class AboutPage : ContentPage
{
    private readonly AboutViewModel _vm;
    private readonly LiftLifeRepository _repo;

    public AboutPage()
    {
        InitializeComponent();
        _vm = ServiceHelper.Get<AboutViewModel>();
        _repo = ServiceHelper.Get<LiftLifeRepository>();
        BindingContext = _vm;
    }

    // Reloads each time, so the record counts reflect what was just logged elsewhere.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    private async void OnChangeNameClicked(object? sender, EventArgs e)
    {
        var current = await _vm.GetMemberNameAsync();
        if (await MemberNamePrompt.AskAsync(this, _repo, current) is not null)
            await _vm.LoadAsync();
    }

    private async void OnTargetsClicked(object? sender, EventArgs e) =>
        await Navigation.PushAsync(new TargetSetupPage());

    private async void OnResetClicked(object? sender, EventArgs e)
    {
        var reset = await DisplayAlertAsync("Reset Demo Data?",
            "This deletes all your reports, routines, workouts, foods and meals, and restores the sample data. It can't be undone.",
            "Reset", "Cancel");
        if (!reset) return;

        await _vm.ResetAsync();
        await DisplayAlertAsync("Data Reset", "LiftLife is back to its sample data.", "OK");
    }
}
