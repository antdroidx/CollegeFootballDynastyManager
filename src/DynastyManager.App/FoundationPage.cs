using DynastyManager.Data.Import;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace DynastyManager.App;

public sealed class FoundationPage : ContentPage
{
    private readonly Label _status;
    private bool _loaded;

    public FoundationPage()
    {
        Title = "Dynasty Manager";

        _status = new Label
        {
            Text = "Validating legacy CSV data…",
            FontSize = 17
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(24),
                Spacing = 14,
                Children =
                {
                    new Label
                    {
                        Text = "College Football Dynasty Manager",
                        FontSize = 28,
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label
                    {
                        Text = "C# / .NET MAUI migration build",
                        FontSize = 18
                    },
                    new Label
                    {
                        Text = "Phase 2: reading the original Career Edition CSV data with the new C# models."
                    },
                    _status,
                    new Label
                    {
                        Text = "Modern defensive positions: DE, DT, OLB, MLB, CB, FS, SS"
                    },
                    new Label
                    {
                        Text = "Legacy DL/LB/S rows are converted deterministically for migration. New CSV files can specify the exact modern position."
                    }
                }
            }
        };

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        if (_loaded)
            return;

        _loaded = true;

        try
        {
            var universeCsv = await ReadAssetAsync("legacy_universe.csv");
            var rosterCsv = await ReadAssetAsync("legacy_roster.csv");
            var coachCsv = await ReadAssetAsync("legacy_coach.csv");

            var universe = LegacyUniverseCsvImporter.Import(universeCsv);
            var roster = LegacyRosterCsvImporter.Import(rosterCsv);
            var coaches = LegacyCoachCsvImporter.Import(coachCsv);

            _status.Text =
                $"Legacy import successful!\n\n" +
                $"Conferences: {universe.Conferences.Count:N0}\n" +
                $"Teams: {universe.Teams.Count:N0}\n" +
                $"Players: {roster.Players.Count:N0}\n" +
                $"Coaches: {coaches.Count:N0}\n" +
                $"DL/LB/S conversions: {roster.Warnings.Count:N0}";
        }
        catch (Exception ex)
        {
            _status.Text = $"Legacy CSV import failed:\n{ex.Message}";
        }
    }

    private static async Task<string> ReadAssetAsync(string fileName)
    {
        await using var stream = await FileSystem.Current.OpenAppPackageFileAsync(fileName);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
