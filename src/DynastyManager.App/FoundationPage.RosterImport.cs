using DynastyManager.Core.Models;
using DynastyManager.Data.Import;
using DynastyManager.Data.Persistence;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private Button RosterImportButton()
    {
        var button = new Button { Text = "Import NCAA 06 NEXT roster (CSV)" };
        button.Clicked += async (_, _) => await OpenRosterImportAsync();
        return button;
    }

    private async Task OpenRosterImportAsync()
    {
        if (_currentDynasty is null) return;
        if (_currentDynasty.Phase != SeasonPhase.Preseason || _currentDynasty.Week != 0 || _currentDynasty.Schedule.Any(g => g.HasPlayed))
        { await DisplayAlertAsync("Roster import", "Create a new dynasty and import before its first season starts.", "OK"); return; }
        var dynastyId = _currentDynasty.DynastyId;
        var page = new ContentPage { Title = "Roster Import", BackgroundColor = DynastyTheme.Background };
        var content = new Editor { Placeholder = "Team mapping, one per line: TGID=Exact league team name", HeightRequest = 160 };
        var status = new Label { TextColor = DynastyTheme.Muted };
        var sample = new VerticalStackLayout { Spacing = 4 };
        var choose = new Button { Text = "Choose PLAY-table CSV" };
        var check = new Button { Text = "Validate and preview", IsEnabled = false };
        var apply = new Button { Text = "Replace listed teams' rosters", IsEnabled = false };
        var close = new Button { Text = "Close" };
        string? csv = null; RosterImportPreview? preview = null;
        void Invalidate() { preview = null; apply.IsEnabled = false; sample.Children.Clear(); }
        content.TextChanged += (_, _) => Invalidate();
        close.Clicked += async (_, _) => await Navigation.PopModalAsync();
        choose.Clicked += async (_, _) =>
        {
            try
            {
                var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Select NCAA PLAY CSV export" });
                if (file is null) return;
                if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    throw new FormatException("Select a CSV export, rather than a raw database or memory-card save.");
                using var stream = await file.OpenReadAsync(); using var reader = new StreamReader(stream);
                var buffer = new char[8_000_001]; var length = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
                if (length > 8_000_000) throw new FormatException("The CSV is larger than the 8 MB import limit.");
                csv = new string(buffer, 0, length); Invalidate(); check.IsEnabled = true;
                status.Text = $"Selected {file.FileName}. Map numeric TGIDs below, then validate.";
            }
            catch (Exception ex) { status.Text = ex.Message; }
        };
        check.Clicked += (_, _) =>
        {
            if (csv is null) return;
            Invalidate();
            try
            {
                var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var line in (content.Text ?? "").Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                {
                    var pair = line.Split('=', 2, StringSplitOptions.TrimEntries);
                    if (pair.Length != 2 || !mapping.TryAdd(pair[0], pair[1])) throw new FormatException("Use one unique TGID=Team Name mapping per line.");
                }
                preview = Ncaa06RosterCsvImporter.Preview(new StringReader(csv), _teamsByName.Keys, mapping);
                status.Text = $"{preview.Players.Count} valid players · {preview.Players.Select(p => p.TeamName).Distinct().Count()} teams · {preview.Errors.Count} errors\n" +
                    string.Join("\n", preview.Errors.Take(12));
                foreach (var team in preview.Players.GroupBy(p => p.TeamName).OrderBy(g => g.Key))
                    sample.Children.Add(new Label { Text = $"{team.Key}: {team.Count()} players" });
                foreach (var p in preview.Players.Take(15)) sample.Children.Add(new Label { Text = $"{p.TeamName} · {p.FullName} · {p.Position} · OVR {p.OverallRating}" });
                apply.IsEnabled = preview.CanApply;
            }
            catch (Exception ex) { status.Text = ex.Message; }
        };
        apply.Clicked += async (_, _) =>
        {
            if (_currentDynasty?.DynastyId != dynastyId || preview is null || _isAdvancing) return;
            apply.IsEnabled = false;
            try
            {
                var imported = Ncaa06RosterCsvImporter.Apply(_currentDynasty, preview);
                _currentDynasty = imported; _cachedCurrentProfiles = null;
                await SaveCurrentAsync(SaveKind.AutosaveCurrent);
                RenderCurrentDynasty();
                await Navigation.PopModalAsync();
            }
            catch (Exception ex) { status.Text = ex.Message; }
        };
        page.Content = new ScrollView { Content = new VerticalStackLayout { Padding = 14, Spacing = 10, Children =
        { close, SectionTitle("NCAA 06 NEXT roster import", "Import a PLAY CSV with decoded names and displayed 1–99 ratings. Required: TGID, PFNA, PLNA, PYEA (0=FR), PPOS, and all 17 NCAA rating fields. Overall is recalculated using POCI. Unlisted teams retain their roster. Raw databases and PS2 saves need conversion first."),
            choose, content, check, status, sample, apply } } };
        await Navigation.PushModalAsync(page);
    }
}
