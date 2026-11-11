using LiftLife.Models;

namespace LiftLife.Views;

/// <summary>Workout Summary (screen 4.5), shown after a session is saved.</summary>
public partial class WorkoutSummaryPage : ContentPage
{
    public WorkoutSummaryPage(WorkoutSummary summary)
    {
        InitializeComponent();
        BindingContext = summary;
    }

    // Back to the Routine Hub, which reloads and shows this session as "Today: ...".
    private async void OnDoneClicked(object? sender, EventArgs e) => await Navigation.PopToRootAsync();
}
