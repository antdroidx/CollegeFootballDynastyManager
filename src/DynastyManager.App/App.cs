using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace DynastyManager.App;

public sealed class App : Application
{
    private bool _applyingTheme;
    private bool _systemThemeQueued;
    public void ApplyThemePreference()
    {
        _applyingTheme=true;
        try { UserAppTheme=DynastyTheme.RequestedTheme; }
        finally { _applyingTheme=false; }
    }
    public App()
    {
        Resources = DynastyTheme.CreateResources();
        UserAppTheme = DynastyTheme.RequestedTheme;
        RequestedThemeChanged += (_, _) =>
        {
            if (_applyingTheme || _systemThemeQueued || DynastyTheme.Preference != "System") return;
            _systemThemeQueued = true;
            // Finish Android's configuration callback before replacing native views.
            Dispatcher.StartTimer(TimeSpan.FromMilliseconds(100), () =>
            {
                _ = RefreshSystemAppearanceAsync();
                return false;
            });
        };
    }

    private async Task RefreshSystemAppearanceAsync()
    {
        try
        {
            if (DynastyTheme.Preference != "System") return;
            Resources = DynastyTheme.CreateResources();
            await Task.Delay(16);
            if (Windows.FirstOrDefault()?.Page is FoundationPage page)
                await page.RefreshSystemThemeAsync();
        }
        catch (Exception error) { System.Diagnostics.Trace.WriteLine("System appearance refresh: " + error); }
        finally { _systemThemeQueued = false; }
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new FoundationPage());
}
