namespace LiftLife.Views;

public partial class SplashPage : ContentPage
{
    public SplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Welcoming splash that auto-navigates to the main dashboard after ~2 seconds.
        await Task.Delay(2000);

        // Swap the app's root window content to the tabbed Shell.
        if (Application.Current?.Windows.Count > 0)
        {
            Application.Current.Windows[0].Page = new AppShell();
        }
    }
}
