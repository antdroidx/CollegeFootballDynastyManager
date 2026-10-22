using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace DynastyManager.App;

public sealed class App : Application
{
    public App()
    {
        Resources = DynastyTheme.CreateResources();
        UserAppTheme = DynastyTheme.IsLight ? AppTheme.Light : AppTheme.Dark;
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new FoundationPage());
}
