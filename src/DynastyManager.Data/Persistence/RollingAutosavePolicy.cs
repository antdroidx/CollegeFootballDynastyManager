namespace DynastyManager.Data.Persistence;

public static class RollingAutosavePolicy
{
    public static SaveKind GetSlotForWeek(int week)
    {
        if (week < 1)
            throw new ArgumentOutOfRangeException(nameof(week), "Week must be at least 1.");

        return ((week - 1) % 3) switch
        {
            0 => SaveKind.WeeklyAuto1,
            1 => SaveKind.WeeklyAuto2,
            _ => SaveKind.WeeklyAuto3
        };
    }

    public static string GetDisplayName(SaveKind kind) => kind switch
    {
        SaveKind.WeeklyAuto1 => "Weekly Auto 1",
        SaveKind.WeeklyAuto2 => "Weekly Auto 2",
        SaveKind.WeeklyAuto3 => "Weekly Auto 3",
        SaveKind.AutosaveCurrent => "Autosave Current",
        SaveKind.AutosavePreviousWeek => "Autosave Previous Week",
        SaveKind.StartOfSeason => "Start of Season",
        _ => "Manual"
    };
}
