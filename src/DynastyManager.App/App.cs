using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace DynastyManager.App;

public sealed class App : Application
{
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var content = new VerticalStackLayout
        {
            Padding = new Thickness(24),
            Spacing = 14,
            Children =
            {
                new Label
                {
                    Text = "College Football Dynasty Manager",
                    FontSize = 28,
                    FontAttributes = FontAttributes.Bold
                },
                new Label
                {
                    Text = "C# / .NET MAUI foundation build",
                    FontSize = 18
                },
                new Label
                {
                    Text = "Phase 1 is running. The legacy Java game remains untouched while the new cross-platform core is built alongside it."
                },
                new Label
                {
                    Text = "Initial model: QB, RB, WR, TE, OL, DE, DT, OLB, MLB, CB, FS, SS, K"
                },
                new Label
                {
                    Text = "Next: CSV universe import, dynasty persistence, and simulation port."
                }
            }
        };

        return new Window(new ContentPage
        {
            Title = "Dynasty Manager",
            Content = new ScrollView { Content = content }
        });
    }
}
