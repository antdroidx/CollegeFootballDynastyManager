using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace DynastyManager.App;

public sealed class App : Application
{
    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new FoundationPage());
}
