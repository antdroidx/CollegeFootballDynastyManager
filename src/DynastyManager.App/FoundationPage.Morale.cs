using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Microsoft.Maui.Graphics;
namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private static string MoraleMood(int value) => value >= 75 ? "Delighted" : value >= 60 ? "Happy" : value >= 45 ? "Content" : value >= 30 ? "Unhappy" : "Very unhappy";
    private View MoraleIcon(DynastyPlayer player)
    {
        var value = GameMoraleService.PlayerMorale(_currentDynasty!, player);
        var face = new GraphicsView { Drawable = new MoraleFace(value), WidthRequest = 28, HeightRequest = 28, VerticalOptions = LayoutOptions.Center };
        var description = $"Morale: {MoraleMood(value)} ({value}/100)";
        SemanticProperties.SetDescription(face, description); ToolTipProperties.SetText(face, description);
        face.SetValue(TableValueProperty, value.ToString()); return face;
    }
    private View PlayerMoraleSummary(DynastyPlayer player)
    {
        var value = GameMoraleService.PlayerMorale(_currentDynasty!, player);
        var row = new Grid { ColumnDefinitions = new() { new(36), new(GridLength.Star) }, ColumnSpacing = 8 };
        row.Add(MoraleIcon(player), 0, 0);
        row.Add(new Label { Text = $"Morale · {MoraleMood(value)} ({value}/100)\nSeason OVR Δ {FormatSigned(GameMoraleService.SeasonDelta(_currentDynasty!, player))} · automatic training", FontSize = 12, VerticalTextAlignment = TextAlignment.Center }, 1, 0);
        return new Border { StrokeThickness = 0, Padding = 10, Content = row, AutomationId = "player-morale-summary" };
    }
    private sealed class MoraleFace(int value) : IDrawable
    {
        public void Draw(ICanvas c, RectF r)
        {
            var x=r.Center.X;var y=r.Center.Y;var size=Math.Min(r.Width,r.Height)*.44f;
            c.FillColor=value>=60?Color.FromArgb("#64C981"):value>=45?Color.FromArgb("#E5C15B"):Color.FromArgb("#EA8B73");
            c.FillCircle(x,y,size);c.StrokeColor=Color.FromArgb("#27352E");c.StrokeSize=1.6f;c.FillColor=Color.FromArgb("#27352E");
            c.FillCircle(x-size*.32f,y-size*.22f,1.4f);c.FillCircle(x+size*.32f,y-size*.22f,1.4f);
            var p=new PathF();p.MoveTo(x-size*.42f,y+size*.25f);
            var bend=value>=60?size*.65f:value>=45?size*.25f:-size*.18f;
            p.QuadTo(x,y+bend,x+size*.42f,y+size*.25f);c.DrawPath(p);
        }
    }
}
