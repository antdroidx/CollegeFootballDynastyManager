using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace DynastyManager.App;

public sealed class App : Application
{
    public App()
    {
        Resources = DynastyTheme.CreateResources();
        UserAppTheme = DynastyTheme.RequestedTheme;
        RequestedThemeChanged += async (_, _) =>
        {
            if (DynastyTheme.Preference != "System") return;
            Resources = DynastyTheme.CreateResources();
            if (Windows.FirstOrDefault()?.Page is FoundationPage page)
                await page.RefreshSystemThemeAsync();
        };
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new FoundationPage());
}
