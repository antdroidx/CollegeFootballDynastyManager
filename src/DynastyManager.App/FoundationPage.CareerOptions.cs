using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Persistence;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private readonly Entry _newCoachName = new() { Placeholder = "Your coach name", Text = "Coach", MaxLength = 60, AutomationId = "new-coach-name" };
    private readonly ContentView _careerOptionsHost = new();
    private static readonly string[] CareerRoles = ["Head coach", "Offensive coordinator", "Defensive coordinator"];
    private readonly Picker _newCoachRole = new() { Title = "Coaching role", ItemsSource = CareerRoles,
        SelectedIndex = Math.Clamp(Preferences.Default.Get("default-coach-role", 0), 0, 2), AutomationId = "new-coach-role" };
    private readonly Switch _newCoachFiring = new() { IsToggled = Preferences.Default.Get("default-coach-firing", true), AutomationId = "new-coach-firing" };
    private static string VersionCaption => "Pre-release 0.20 · Build " + (AppInfo.Current.VersionString.Split('.').ElementAtOrDefault(2) ?? AppInfo.Current.BuildString);

    private View BuildNewCareerOptions() => new VerticalStackLayout { Spacing = 8, Children = {
        new Label { Text = "Your coaching career", FontAttributes = FontAttributes.Bold }, _newCoachName, _newCoachRole,
        new HorizontalStackLayout { Spacing = 8, Children = { _newCoachFiring, new Label { Text = "Coach can be fired", VerticalTextAlignment = TextAlignment.Center } } },
        new Label { Text = "Coordinators control recruiting, game plans and talks for their side. The staff handles the other side. Earn new coordinator or head-coach offers through your career.", FontSize = 12, TextColor = DynastyTheme.Muted }
    } };

    private View BuildCareerPreferences()
    {
        var name = new Entry { Text = _currentDynasty?.UserHeadCoach?.FullName ?? "Coach", Placeholder = "Your coach name", MaxLength = 60, AutomationId = "coach-name-setting" };
        var saveName = new Button { Text = "Save coach name", AutomationId = "save-coach-name" };
        saveName.Clicked += async (_, _) =>
        {
            if (_currentDynasty is null) return;
            saveName.IsEnabled = false;
            var beforeRename = _currentDynasty;
            try { _currentDynasty = UserCoachCareerService.Rename(_currentDynasty, name.Text ?? ""); await SaveCurrentAsync(SaveKind.AutosaveCurrent); RenderCurrentDynasty(); }
            catch (Exception error) { _currentDynasty = beforeRename; await GameDialogs.AlertAsync(this, "Coach name", error.Message, "Close"); }
            finally { saveName.IsEnabled = true; }
        };
        var firing = new Switch { IsToggled = _currentDynasty?.UserCoachFiringEnabled ?? true, AutomationId = "coach-firing-setting" };
        var saving = false;
        firing.Toggled += async (_, args) => {
            if (_currentDynasty is null || saving) return;
            var previous = _currentDynasty;
            saving = true; firing.IsEnabled = false;
            _currentDynasty = previous with { UserCoachFiringEnabled = args.Value };
            try { await SaveCurrentAsync(SaveKind.AutosaveCurrent); }
            catch (Exception error) {
                _currentDynasty = previous; firing.IsToggled = previous.UserCoachFiringEnabled;
                await GameDialogs.AlertAsync(this, "Could not save career option", error.Message, "Close");
            }
            finally { saving = false; firing.IsEnabled = true; }
        };
        return new VerticalStackLayout { Spacing = 8, Children = {
            name, saveName,
            new Label { Text = _currentDynasty is null ? "Career options" : UserCoachControlService.Label(_currentDynasty.UserCoachRole), FontAttributes = FontAttributes.Bold },
            new HorizontalStackLayout { Spacing = 8, Children = { firing, new Label { Text = "Coach can be fired", VerticalTextAlignment = TextAlignment.Center } } },
            new Label { Text = "Off: poor results still affect reputation and job security, but cannot end your job. Changing this does not undo a previous dismissal. Roles change through job offers.", FontSize = 12, TextColor = DynastyTheme.Muted },
            new Label { IsVisible = _currentDynasty?.UserCoachRole != StaffRole.HeadCoach, Text = "Coordinators: staff automatically recruits for the other side, reserving up to half of available weekly points even when your own assistance is off.", FontSize = 12, TextColor = DynastyTheme.Muted },
            new Label { Text = VersionCaption, FontSize = 12, TextColor = DynastyTheme.Muted }
        } };
    }

    private async Task ShowStartupOptionsAsync()
    {
        var choice = await GameDialogs.ActionSheetAsync(this, "Options", "Close", null, "Appearance", "Default coaching role", "Coach firing");
        if (choice == "Appearance")
        {
            var theme = await GameDialogs.ActionSheetAsync(this, "Appearance", "Cancel", null, "Dark", "Light", "System");
            if (theme is not ("Dark" or "Light" or "System")) return;
            Preferences.Default.Set("app-theme", theme);
            ((App)Application.Current!).ApplyThemePreference();
            Application.Current.Resources = DynastyTheme.CreateResources();
            if (Window is { } window) await RebuildThemeAsync(window, AppSection.Home);
        }
        else if (choice == "Default coaching role")
        {
            var role = await GameDialogs.ActionSheetAsync(this, "Default coaching role", "Cancel", null, CareerRoles);
            var index = Array.IndexOf(CareerRoles, role);
            if (index >= 0) { Preferences.Default.Set("default-coach-role", index); _newCoachRole.SelectedIndex = index; }
        }
        else if (choice == "Coach firing")
        {
            var value = await GameDialogs.ActionSheetAsync(this, "Coach firing for new careers", "Cancel", null, "On", "Off");
            if (value is "On" or "Off") { Preferences.Default.Set("default-coach-firing", value == "On"); _newCoachFiring.IsToggled = value == "On"; }
        }
    }
}
