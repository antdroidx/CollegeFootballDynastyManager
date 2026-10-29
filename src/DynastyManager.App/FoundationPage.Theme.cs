using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Dispatching;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private bool _systemThemePending;
    private bool _themeWaitScheduled;
    public Task RefreshSystemThemeAsync()
    {
        // Replacing the page during an awaited dynasty update would leave its
        // continuation updating the old page's state. Refresh after it commits.
        if (_isAdvancing || _advanceOverlay.IsVisible || _managementActionDepth > 0 || _openingSchool)
        {
            _systemThemePending = true;
            if (!_themeWaitScheduled)
            {
                _themeWaitScheduled = true;
                Dispatcher.StartTimer(TimeSpan.FromMilliseconds(100), () =>
                {
                    if (Window?.Page != this) { _themeWaitScheduled = false; return false; }
                    if (_isAdvancing || _advanceOverlay.IsVisible || _managementActionDepth > 0 || _openingSchool) return true;
                    _themeWaitScheduled = false;
                    _ = RefreshPendingThemeAsync();
                    return false;
                });
            }
            return Task.CompletedTask;
        }
        if (Navigation.ModalStack.Count > 0) { _systemThemePending = true; return Task.CompletedTask; }
        _systemThemePending = false;
        return Window is { } window ? RebuildThemeAsync(window, _activeSection) : Task.CompletedTask;
    }

    private async Task RefreshPendingThemeAsync()
    {
        try { await RefreshSystemThemeAsync(); }
        catch (Exception error) { System.Diagnostics.Trace.WriteLine("Deferred appearance refresh: " + error); }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        DynastyTheme.ApplyNativeAppearance();
        if (_systemThemePending) _ = RefreshPendingThemeAsync();
    }

    private async Task RebuildThemeAsync(Window hostWindow, AppSection section)
    {
            // Rebuild explicit-color views using the SAME live state, including unsaved changes.
            var next = new FoundationPage
            {
                _selectedRosterPlayer = _selectedRosterPlayer, _showDepthChartOrganizer = _showDepthChartOrganizer,
                _selectedRecruitId = _selectedRecruitId, _selectedRecruitSource = _selectedRecruitSource, _recruitingView = _recruitingView,
                _depthPositionCode = _depthPositionCode,
                _loaded = _loaded, _currentDynasty = _currentDynasty, _saveRepository = _saveRepository,
                _teamsByName = _teamsByName, _teamNames = _teamNames, _simulationProfiles = _simulationProfiles,
                _realCoachSeeds = _realCoachSeeds, _legacyRosterRows = _legacyRosterRows, _legacyTeams = _legacyTeams,
                _startingLibrary = _startingLibrary, _bundledStartingRoster = _bundledStartingRoster,
                _startingLibraryRepository = _startingLibraryRepository, _startingLibraryError = _startingLibraryError
            };
            foreach (var item in _managementTabs) next._managementTabs[item.Key] = item.Value;
            next.RefreshStartingChoices(SelectedStartingSetup?.Id, SelectedStartingRoster?.Id);
            next._generatePlayers.IsToggled=_generatePlayers.IsToggled;
            next.ConfigureLeague(_teamsByName.Values.ToArray());
            next._importStatus.Text = _importStatus.Text;
            next._homeStatus.Text = _homeStatus.Text;
            next._newDynastyButton.IsEnabled = _newDynastyButton.IsEnabled;
            foreach(var order in _tableOrders) next._tableOrders[order.Key]=order.Value;
            next._rosterTeamName=_rosterTeamName;
            next._rosterSearch.Text=_rosterSearch.Text;
            next._rosterPosition.SelectedIndex=_rosterPosition.SelectedIndex;
            next._rosterStatus.SelectedIndex=_rosterStatus.SelectedIndex;
            next._rosterSort.SelectedIndex=_rosterSort.SelectedIndex;
            next._recruitingSearch.Text=_recruitingSearch.Text;
            next._recruitingPositionFilter.SelectedIndex=_recruitingPositionFilter.SelectedIndex;
            next._recruitingStarFilter.SelectedIndex=_recruitingStarFilter.SelectedIndex;
            next._recruitingScoutedFilter.SelectedIndex=_recruitingScoutedFilter.SelectedIndex;
            next.SetDynastyControlsEnabled(_currentDynasty is not null);
            // ShowSection renders the requested view; keep the live-state transfer synchronous.
            next.ShowSection(section);
            next._sectionTrail.Clear();
            foreach (var previousSection in _sectionTrail.Reverse()) next._sectionTrail.Push(previousSection);
            hostWindow.Page = next;
            if (_saveRepository is not null) await next.RefreshSaveSlotsAsync();
            DynastyTheme.ApplyNativeAppearance();
    }

    private View BuildThemeSettings()
    {
        var picker = new Picker { Title = "Appearance", ItemsSource = new[] { "Dark", "Light", "System" },
            SelectedIndex = DynastyTheme.Preference == "System" ? 2 : DynastyTheme.Preference == "Light" ? 1 : 0, AutomationId = "theme-picker" };
        picker.SelectedIndexChanged += async (_, _) =>
        {
            if (picker.SelectedIndex < 0 || picker.SelectedItem?.ToString() == DynastyTheme.Preference) return;
            var hostWindow = Window;
            if (hostWindow is null) return;
            // The themed choice surface is already closed before this selection event.
            try
            {
            Preferences.Default.Set("app-theme", picker.SelectedItem?.ToString() ?? "System");
            var app = Application.Current!;
            app.Resources = DynastyTheme.CreateResources();
            ((App)app).ApplyThemePreference();
            await RebuildThemeAsync(hostWindow, AppSection.Settings);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                Console.WriteLine("Appearance change failed: " + ex);
                if (hostWindow.Page is { } presenter) await GameDialogs.AlertAsync(presenter, "Appearance change failed", ex.Message, "Close");
            }
        };
        return new VerticalStackLayout { Spacing = 4, Children = { new Label { Text = "Appearance" }, picker } };
    }
}
