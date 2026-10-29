using LiftLife.Views;

namespace LiftLife;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// Launch into the splash screen, which auto-navigates to the tabbed Shell.
		return new Window(new SplashPage());
	}
}
