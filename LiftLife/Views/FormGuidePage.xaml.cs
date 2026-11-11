using LiftLife.ViewModels;

namespace LiftLife.Views;

/// <summary>
/// Form Guide sheet (screen 4.4): a looping tempo animation, the exercise's key form cues,
/// equipment-specific safety checks, and the member's own machine setup note.
/// </summary>
public partial class FormGuidePage : ContentPage
{
    private static readonly Dictionary<string, string[]> SafetyByEquipment = new()
    {
        ["Barbell"] = new[] { "Use collars on both sides of the bar.", "Set the safety pins or arms just below your lowest point." },
        ["Dumbbell"] = new[] { "Pick dumbbells up and put them down with a flat back.", "Return them to the rack so others can find them." },
        ["Machine"] = new[] { "Adjust the seat and pads before adding weight.", "Make sure the weight pin is fully inserted." },
        ["Cable"] = new[] { "Check the cable and clip are secure before each set.", "Report frayed cables in the Community tab." },
    };

    private bool _animating;

    public FormGuidePage(SessionExercise exercise)
    {
        InitializeComponent();

        TitleLabel.Text = exercise.Name;
        TagLabel.Text = exercise.TagLine;

        // Turn the stored cue sentence into bullet points.
        var cues = exercise.FormCue
            .Split(new[] { ',', '.', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(c => char.ToUpper(c[0]) + c[1..])
            .ToList();
        if (cues.Count == 0) cues.Add("Move through a full, controlled range of motion");
        foreach (var cue in cues) CueList.Add(Bullet(cue, "✓"));

        var safety = SafetyByEquipment.TryGetValue(exercise.Equipment, out var tips) ? tips.ToList() : new List<string>();
        safety.Add("Stop the set if you feel sharp pain.");
        foreach (var tip in safety) SafetyList.Add(Bullet(tip, "!"));

        if (exercise.HasSetupNote)
        {
            SetupCard.IsVisible = true;
            SetupLabel.Text = exercise.SetupNote;
        }
    }

    private static View Bullet(string text, string symbol) => new Grid
    {
        ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
        ColumnSpacing = 10,
        Children =
        {
            new Label { Text = symbol, TextColor = (Color)Application.Current!.Resources["Primary"], FontAttributes = FontAttributes.Bold },
            new Label { Text = text, TextColor = (Color)Application.Current.Resources["TextPrimary"], FontSize = 14 }.Column(1)
        }
    };

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoopTempoAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _animating = false;
        TempoBar.CancelAnimations();
    }

    /// <summary>Loops a 2s down / pause / 1s up animation to show the target rep tempo.</summary>
    private async Task LoopTempoAsync()
    {
        if (_animating) return;
        _animating = true;

        const double travel = 170 - 26 - 16; // track height - bar height - margins
        var rep = 0;

        while (_animating)
        {
            PhaseLabel.Text = "LOWER • 2s";
            await TempoBar.TranslateToAsync(0, travel, 2000, Easing.SinInOut);
            if (!_animating) break;

            PhaseLabel.Text = "PAUSE";
            await Task.Delay(500);
            if (!_animating) break;

            PhaseLabel.Text = "LIFT • 1s";
            await TempoBar.TranslateToAsync(0, 0, 1000, Easing.CubicOut);
            RepLabel.Text = $"Rep {++rep}";
            await Task.Delay(400);
        }
    }

    private async void OnCloseClicked(object? sender, EventArgs e) => await Navigation.PopModalAsync();
}

/// <summary>Tiny helper so Grid columns can be set inline when building views in code.</summary>
internal static class GridViewExtensions
{
    public static T Column<T>(this T view, int column) where T : BindableObject
    {
        Grid.SetColumn(view, column);
        return view;
    }
}
