namespace LiftLife.Services;

/// <summary>
/// Small helper to fetch registered services (repository, ViewModels) from the
/// dependency injection container that was set up in MauiProgram.cs.
/// </summary>
public static class ServiceHelper
{
    public static T Get<T>() where T : notnull =>
        IPlatformApplication.Current!.Services.GetRequiredService<T>();
}
