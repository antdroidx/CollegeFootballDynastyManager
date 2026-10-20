using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Import;
using DynastyManager.Data.Persistence;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private Button RosterImportButton()
    {
        var button = new Button { Text = "Import starting roster" };
        button.Clicked += async (_, _) => await OpenRosterImportAsync();
        return button;
    }

    private async Task OpenRosterImportAsync()
    {
        if (_startingLibraryError is not null) { await DisplayAlertAsync("Starting library", _startingLibraryError, "Close"); return; }
        var setups = AvailableStartingSetups();
        if (setups.Count == 0) { await DisplayAlertAsync("Starting rosters", "Add or restore a league setup in Starting rosters & league setups before importing.", "Close"); return; }
        if (_startingLibraryRepository is null) return;
        var selected = Math.Max(0, Array.FindIndex(setups.ToArray(), s => s.Id == SelectedStartingSetup?.Id));
        var league = new Picker { Title = "Default league setup", ItemsSource = setups.Select(s => s.Name).ToArray(), SelectedIndex = selected };
        var rosterName = new Entry { Placeholder = "Starting roster name", MaxLength = 80 };
        var makeDefault = new Switch { IsToggled = true };
        var page = new ContentPage { Title = "Roster Import", BackgroundColor = DynastyTheme.Background };
        var content = new Editor { Placeholder = "Team mapping, one per line: TGID=Exact league team name", HeightRequest = 160 };
        var status = new Label { TextColor = DynastyTheme.Muted };
        var sample = new VerticalStackLayout { Spacing = 4 };
        var choose = new Button { Text = "Choose PLAY-table CSV" };
        var check = new Button { Text = "Validate and preview", IsEnabled = false };
        var apply = new Button { Text = "Save starting roster", IsEnabled = false };
        var close = new Button { Text = "Close" };
        string? csv = null; RosterImportPreview? preview = null;
        void Invalidate() { preview = null; apply.IsEnabled = false; sample.Children.Clear(); }
        content.TextChanged += (_, _) => Invalidate();
        league.SelectedIndexChanged += (_, _) => Invalidate();
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
                rosterName.Text = Path.GetFileNameWithoutExtension(file.FileName);
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
                preview = Ncaa06RosterCsvImporter.Preview(new StringReader(csv), setups.SelectMany(s => s.Teams).Select(t => t.Name).Distinct(StringComparer.OrdinalIgnoreCase), mapping);
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
            if (preview is null || !preview.CanApply) return;
            if (string.IsNullOrWhiteSpace(rosterName.Text)) { status.Text = "Name this starting roster."; return; }
            if (_startingLibrary.Rosters.Any(r => r.Name.Equals(rosterName.Text.Trim(), StringComparison.OrdinalIgnoreCase))) { status.Text = "A roster with that name already exists. Choose a new name or remove the old pack."; return; }
            apply.IsEnabled = false;
            try
            {
                var roster = new StartingRosterPack { Name = rosterName.Text.Trim(), Players = preview.Players.ToArray() };
                var setup = setups[Math.Max(0, league.SelectedIndex)];
                if (makeDefault.IsToggled && !StartingLibraryService.IsCompatible(roster, setup.Teams)) throw new ArgumentException("This roster has no schools in the chosen default league setup. Choose another setup or turn off the default switch.");
                var defaults = _startingLibrary.DefaultRosterBySetup.ToDictionary();
                if (makeDefault.IsToggled) defaults[setup.Id] = roster.Id;
                await SaveStartingLibraryAsync(_startingLibrary with { Rosters = _startingLibrary.Rosters.Append(roster).ToArray(), DefaultRosterBySetup = defaults });
                RefreshStartingChoices(setup.Id, roster.Id);
                if (Navigation.ModalStack.LastOrDefault() == page) await Navigation.PopModalAsync();
            }
            catch (Exception ex) { status.Text = ex.Message; apply.IsEnabled = true; }
        };
        page.Content = new ScrollView { Content = new VerticalStackLayout { Padding = 14, Spacing = 10, Children =
        { close, SectionTitle("NCAA 06 NEXT roster import", "Import a PLAY CSV with decoded names and displayed 1–99 ratings. Required: TGID, PFNA, PLNA, PYEA (0=FR), PPOS, and all 17 NCAA rating fields. Overall is recalculated using POCI. Teams missing from a pack use default/generated players. Schools outside a chosen setup are ignored. Raw databases and PS2 saves need conversion first. Saving adds a reusable starting roster; existing dynasties are untouched."),
            new Label { Text = "Roster name" }, rosterName, new Label { Text = "Default league setup" }, league,
            new HorizontalStackLayout { Spacing = 8, Children = { makeDefault, new Label { Text = "Use by default for this league setup", VerticalOptions = LayoutOptions.Center } } },
            choose, content, check, status, sample, apply } } };
        await Navigation.PushModalAsync(page);
    }
}
