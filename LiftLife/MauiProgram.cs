using LiftLife.Data;
using LiftLife.Views;
using Microsoft.Extensions.Logging;

namespace LiftLife;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// ---- Dependency injection ----
		// Database is a singleton so the SQLite connection is shared app-wide.
		builder.Services.AddSingleton<LiftLifeDatabase>();
		builder.Services.AddSingleton<LiftLifeRepository>();

		// Pages
		builder.Services.AddTransient<CommunityPage>();
		builder.Services.AddTransient<WorkoutsPage>();
		builder.Services.AddTransient<NutritionPage>();
		builder.Services.AddTransient<AboutPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
