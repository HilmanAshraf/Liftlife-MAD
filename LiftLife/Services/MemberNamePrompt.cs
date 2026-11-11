using LiftLife.Data;

namespace LiftLife.Services;

/// <summary>
/// Asks the member for a display name the first time they report or claim a station,
/// so reports read "by Hilman" instead of "by You". The name is saved in SQLite.
/// </summary>
public static class MemberNamePrompt
{
    /// <summary>Returns the saved name, or asks for one. Null if the member cancels.</summary>
    public static async Task<string?> EnsureAsync(Page page, LiftLifeRepository repo)
    {
        var current = await repo.GetMemberNameAsync();
        return string.IsNullOrWhiteSpace(current) ? await AskAsync(page, repo, null) : current;
    }

    /// <summary>Shows the name prompt, validates it and saves it. Null if cancelled or invalid.</summary>
    public static async Task<string?> AskAsync(Page page, LiftLifeRepository repo, string? initial)
    {
        var input = await page.DisplayPromptAsync(
            "Your Member Name",
            "Shown on your station reports so other members know who updated them.",
            accept: "Save",
            cancel: "Cancel",
            placeholder: "e.g. Hilman",
            maxLength: 20,
            keyboard: Keyboard.Text,
            initialValue: initial ?? string.Empty);

        if (input is null) return null; // cancelled

        var name = input.Trim();
        if (name.Length < 2)
        {
            await page.DisplayAlertAsync("Name Too Short", "Please enter at least 2 characters.", "OK");
            return null;
        }

        await repo.SetMemberNameAsync(name);
        return name;
    }
}
