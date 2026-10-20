using DynastyManager.Core.Simulation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private View RecruitIdentity(Guid id, string name, string title, string category)
    {
        var player = _currentDynasty?.ActiveRoster.FirstOrDefault(p => p.PlayerId == id)
            ?? _currentDynasty?.TransferPortalEntries.FirstOrDefault(p => p.Player.PlayerId == id)?.Player;
        var recruit = _currentDynasty?.HighSchoolRecruitingPool.FirstOrDefault(p => p.RecruitId == id);
        var generated = PlayerPhysicalService.Generate(id, player?.Position ?? recruit?.Position ?? DynastyManager.Core.Models.Position.QB);
        var height = player?.HeightInches ?? recruit?.HeightInches ?? 0;
        var weight = player?.WeightPounds ?? recruit?.WeightPounds ?? 0;
        var row = new Grid { ColumnSpacing = 10,
            ColumnDefinitions = new ColumnDefinitionCollection { new(76), new(GridLength.Star) } };
        row.Add(PersonPortrait.Create(id, name, Color.FromArgb("#31566C"), Color.FromArgb("#A9CEF3"), 76, appearance: player?.Appearance), 0, 0);
        row.Add(new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center, Children =
        {
            new Label { Text = category, FontSize = 9, FontAttributes = FontAttributes.Bold, TextColor = DynastyTheme.Accent },
            new Label { Text = title, FontSize = 16, FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.WordWrap },
            new Label { Text = PlayerPhysicalService.Format(height > 0 ? height : generated.Height, weight > 0 ? weight : generated.Weight), FontSize = 11, TextColor = DynastyTheme.Muted }
        } }, 1, 0);
        return row;
    }
}
