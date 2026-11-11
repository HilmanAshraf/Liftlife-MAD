using LiftLife.Data;
using LiftLife.ViewModels;
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

		// ViewModels
		builder.Services.AddTransient<CommunityViewModel>();
		builder.Services.AddTransient<WorkoutsViewModel>();
		builder.Services.AddTransient<RoutineBuilderViewModel>();
		builder.Services.AddTransient<ActiveWorkoutViewModel>();
		builder.Services.AddTransient<QuickSwapViewModel>();
		builder.Services.AddTransient<NutritionViewModel>();
		builder.Services.AddTransient<FoodLogViewModel>();
		builder.Services.AddTransient<CustomFoodViewModel>();
		builder.Services.AddTransient<TargetSetupViewModel>();
		builder.Services.AddTransient<StationDetailViewModel>();
		builder.Services.AddTransient<AboutViewModel>();

		// Pages
		builder.Services.AddTransient<CommunityPage>();
		builder.Services.AddTransient<WorkoutsPage>();
		builder.Services.AddTransient<NutritionPage>();
		builder.Services.AddTransient<AboutPage>();

		// Remove the native iOS text-field border so our own rounded boxes are the only border.
		Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoNativeBorder", (handler, view) =>
		{
#if IOS || MACCATALYST
			handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#endif
		});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
