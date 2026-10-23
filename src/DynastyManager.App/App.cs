using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace DynastyManager.App;

public sealed class App : Application
{
    private bool _applyingTheme;
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
        RequestedThemeChanged += async (_, _) =>
        {
            if (_applyingTheme || DynastyTheme.Preference != "System") return;
            Resources = DynastyTheme.CreateResources();
            if (Windows.FirstOrDefault()?.Page is FoundationPage page)
                try { await page.RefreshSystemThemeAsync(); }
                catch(Exception error) { System.Diagnostics.Trace.WriteLine("System appearance refresh: "+error); }
        };
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new FoundationPage());
}
