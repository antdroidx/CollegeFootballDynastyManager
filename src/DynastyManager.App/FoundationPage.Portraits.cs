using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private static View RecruitIdentity(Guid id, string name, string title, string category)
    {
        var row = new Grid { ColumnSpacing = 10,
            ColumnDefinitions = new ColumnDefinitionCollection { new(54), new(GridLength.Star) } };
        row.Add(PersonPortrait.Create(id, name, Color.FromArgb("#31566C"), Color.FromArgb("#A9CEF3"), 54), 0, 0);
        row.Add(new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center, Children =
        {
            new Label { Text = category, FontSize = 9, FontAttributes = FontAttributes.Bold, TextColor = DynastyTheme.Accent },
            new Label { Text = title, FontSize = 16, FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.WordWrap }
        } }, 1, 0);
        return row;
    }
}
