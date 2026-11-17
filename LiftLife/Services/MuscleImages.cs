namespace LiftLife.Services;

/// <summary>
/// Picks the photo for a muscle group (shown at the top of the Form Guide).
/// Photos are free-licence images from Pexels, stored in Resources/Images.
/// </summary>
public static class MuscleImages
{
    private static readonly Dictionary<string, string> Photos = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Chest"] = "photo_chest.jpg",
        ["Back"] = "photo_back.jpg",
        ["Legs"] = "photo_legs.jpg",
        ["Shoulders"] = "photo_shoulders.jpg",
        ["Biceps"] = "photo_biceps.jpg",
        ["Triceps"] = "photo_triceps.jpg",
    };

    /// <summary>The photo file for the muscle group, or the gym banner if there isn't one.</summary>
    public static string For(string muscleGroup) =>
        Photos.TryGetValue(muscleGroup, out var file) ? file : "photo_uniten_gym.jpg";
}
