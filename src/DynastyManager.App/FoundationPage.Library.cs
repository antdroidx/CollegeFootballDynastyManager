using DynastyManager.Data.Persistence;
using DynastyManager.Core.Seasons;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private readonly Label _libraryStatus = new() { FontSize = 12, TextColor = DynastyTheme.Muted };
    private readonly Label _newDynastyStatus = new() { FontSize = 12, TextColor = DynastyTheme.Muted };

    private View BuildLibrarySection()
    {
        var create = new Button { Text = "New dynasty", BackgroundColor = DynastyTheme.Accent,
            TextColor = DynastyTheme.Background, HorizontalOptions = LayoutOptions.End };
        create.Clicked += (_, _) => ShowSection(AppSection.NewDynasty);
        var heading = new Grid { ColumnSpacing = 12,
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) } };
        heading.Add(new Label { Text = "Your dynasties", FontSize = 16, FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center }, 0, 0);
        heading.Add(create, 1, 0);
        return new VerticalStackLayout { Spacing = 12, MaximumWidthRequest = 1000, Children =
        {
            heading,
            new Label { Text = "Load a saved career or start a new one.", FontSize = 12, TextColor = DynastyTheme.Muted },
            new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap, Children = { StartingLibraryButton(), RosterImportButton() } },
            _saveList, _libraryStatus
        } };
    }

    private View BuildNewDynastySection()
    {
        _newDynastyButton.BackgroundColor = DynastyTheme.Accent;
        _newDynastyButton.TextColor = DynastyTheme.Background;
        return new VerticalStackLayout { Spacing = 10, MaximumWidthRequest = 560, Children =
        {
            new Label { Text = "Choose your league and school.", FontSize = 13, TextColor = DynastyTheme.Muted },
            new Label { Text = "Dynasty name", FontSize = 12, FontAttributes = FontAttributes.Bold }, _dynastyNameEntry,
            new Label { Text = "League structure", FontSize = 12, FontAttributes = FontAttributes.Bold }, _leaguePicker,
            new Label { Text = "Starting roster", FontSize = 12, FontAttributes = FontAttributes.Bold }, _startingRosterPicker,
            new Label { Text = "Import and manage reusable rosters and league setups from the main screen.", FontSize = 11, TextColor = DynastyTheme.Muted },
            new Label { Text = "Your school", FontSize = 12, FontAttributes = FontAttributes.Bold }, _teamPicker,
            new Label { Text = "Staff and recruiting preferences can be changed after starting.", FontSize = 11, TextColor = DynastyTheme.Muted },
            _newDynastyButton, _newDynastyStatus
        } };
    }

    private void UpdateShellContext()
    {
        var library = _activeSection is AppSection.Home or AppSection.NewDynasty;
        _navigationBar.IsVisible = !library && _currentDynasty is not null;
        _libraryButton.IsVisible = _activeSection != AppSection.Home;
        _libraryButton.Text = _activeSection == AppSection.NewDynasty ? "‹ Cancel" : "‹ Exit Dynasty";
        _headerContext.IsVisible = !library && _currentDynasty is not null;
        _settingsButton.IsVisible = !library && _currentDynasty is not null && _activeSection != AppSection.Settings;
        if (_currentDynasty is not null)
            _headerContext.Text = $"{_currentDynasty.UserTeamName} · {_currentDynasty.SeasonYear} · {SeasonProgression.PeriodLabel(_currentDynasty.Phase, _currentDynasty.Week)}";
        if (_currentDynasty is not null)
            _advanceWeekButton.Text = SeasonProgression.AdvanceLabel(_currentDynasty.Phase, _currentDynasty.Week);
    }

    private async Task ReturnToLibraryAsync()
    {
        if (_isAdvancing || _advanceOverlay.IsVisible) return;
        try
        {
            if (_activeSection != AppSection.NewDynasty && _currentDynasty is not null)
            {
                await ShowAdvanceProgressAsync("Saving dynasty...", .5);
                await SaveCurrentAsync(SaveKind.AutosaveCurrent);
            }
            ShowSection(AppSection.Home);
        }
        catch (Exception ex) { await GameDialogs.AlertAsync(this, "Could not save dynasty", ex.Message, "Close"); }
        finally { _advanceOverlay.IsVisible = false; }
    }

    protected override bool OnBackButtonPressed()
    {
        if (_advanceOverlay.IsVisible || _isAdvancing) return true;
        if (_navigationExpanded && _managementRoot is { Width: < 600 })
        { _navigationExpanded = false; ArrangeManagementShell(); return true; }
        if (_activeSection == AppSection.Recruiting && _selectedRecruitId is not null)
        {
            _selectedRecruitId=null;_selectedRecruitSource=null;
            _recruitingDetailHost.Content=null;_recruitingDetailHost.IsVisible=false;return true;
        }
        if (_activeSection == AppSection.Roster && _selectedRosterPlayer is not null)
        { _selectedRosterPlayer=null;RenderCurrentDynasty();return true; }
        if (_activeSection == AppSection.Home) return base.OnBackButtonPressed();
        if (_activeSection != AppSection.NewDynasty && RestorePreviousSection()) return true;
        _ = ReturnToLibraryAsync();
        return true;
    }

    private async Task OpenSaveVersionsAsync(IReadOnlyList<DynastySaveInfo> saves)
    {
        var ordered = saves.OrderByDescending(save => save.UpdatedUtc).ToArray();
        var labels = ordered.Select((save, index) => $"{index + 1}. {save.SeasonYear} / {FormatWeek(save)} / {RollingAutosavePolicy.GetDisplayName(save.Kind)} / {save.UpdatedUtc.ToLocalTime():g}").ToArray();
        var selected = await GameDialogs.ActionSheetAsync(this, "Saved versions", "Cancel", null, labels);
        var index = Array.IndexOf(labels, selected);
        if (index < 0) return;
        var action = await GameDialogs.ActionSheetAsync(this, labels[index], "Cancel", null, "Load save", "Delete save");
        if (action == "Load save") await LoadSaveAsync(ordered[index].SaveId);
        else if (action == "Delete save") await DeleteSaveAsync(ordered[index].SaveId);
    }
}
