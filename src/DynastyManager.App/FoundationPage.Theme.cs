using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private bool _systemThemePending;
    public Task RefreshSystemThemeAsync()
    {
        if (Navigation.ModalStack.Count > 0) { _systemThemePending = true; return Task.CompletedTask; }
        return Window is { } window ? RebuildThemeAsync(window, _activeSection) : Task.CompletedTask;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_systemThemePending) { _systemThemePending = false; _ = RefreshSystemThemeAsync(); }
    }

    private async Task RebuildThemeAsync(Window hostWindow, AppSection section)
    {
            // Rebuild explicit-color views using the SAME live state, including unsaved changes.
            var next = new FoundationPage
            {
                _selectedRosterPlayer = _selectedRosterPlayer, _showDepthChartOrganizer = _showDepthChartOrganizer,
                _depthPositionCode = _depthPositionCode,
                _loaded = _loaded, _currentDynasty = _currentDynasty, _saveRepository = _saveRepository,
                _teamsByName = _teamsByName, _teamNames = _teamNames, _simulationProfiles = _simulationProfiles,
                _realCoachSeeds = _realCoachSeeds, _legacyRosterRows = _legacyRosterRows, _legacyTeams = _legacyTeams,
                _startingLibrary = _startingLibrary, _bundledStartingRoster = _bundledStartingRoster,
                _startingLibraryRepository = _startingLibraryRepository, _startingLibraryError = _startingLibraryError
            };
            next.RefreshStartingChoices(SelectedStartingSetup?.Id, SelectedStartingRoster?.Id);
            next.ConfigureLeague(_teamsByName.Values.ToArray());
            next._importStatus.Text = _importStatus.Text;
            next._homeStatus.Text = _homeStatus.Text;
            next._newDynastyButton.IsEnabled = _newDynastyButton.IsEnabled;
            next.SetDynastyControlsEnabled(_currentDynasty is not null);
            next.RenderCurrentDynasty();
            next.ShowSection(section);
            next._sectionTrail.Clear();
            foreach (var previousSection in _sectionTrail.Reverse()) next._sectionTrail.Push(previousSection);
            hostWindow.Page = next;
            if (_saveRepository is not null) await next.RefreshSaveSlotsAsync();
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
            // Let Android dismiss its native picker before replacing its owning page.
            await Task.Delay(150);
            try
            {
            Preferences.Default.Set("app-theme", picker.SelectedItem?.ToString() ?? "System");
            var app = Application.Current!;
            app.Resources = DynastyTheme.CreateResources();
            app.UserAppTheme = DynastyTheme.RequestedTheme;
            await RebuildThemeAsync(hostWindow, AppSection.Settings);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                Console.WriteLine("Appearance change failed: " + ex);
                await hostWindow.Page.DisplayAlertAsync("Appearance change failed", ex.Message, "Close");
            }
        };
        return new VerticalStackLayout { Spacing = 4, Children = { new Label { Text = "Appearance" }, picker } };
    }
}
