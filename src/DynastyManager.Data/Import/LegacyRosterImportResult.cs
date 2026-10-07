namespace DynastyManager.Data.Import;

public sealed record LegacyRosterImportResult(
    IReadOnlyList<ImportedPlayerRow> Players,
    IReadOnlyList<ImportWarning> Warnings);
