using ProFootball.Core;
namespace ProFootball.App;

public sealed class TeamBadgeView : GraphicsView
{
    public TeamBadgeView(ProTeam team, int size = 44)
    {
        WidthRequest = Math.Max(44, size); HeightRequest = size; InputTransparent = true;
        Drawable = Mark(team); SemanticProperties.SetDescription(this, team.Abbreviation + " team badge");
    }
    static double Luminance(Color color)
    {
        static double Linear(double v) => v <= .04045 ? v / 12.92 : Math.Pow((v + .055) / 1.055, 2.4);
        return .2126 * Linear(color.Red) + .7152 * Linear(color.Green) + .0722 * Linear(color.Blue);
    }
    public static Color Ink(ProTeam team) => Luminance(Color.FromArgb(team.Primary)) > .179 ? Colors.Black : Colors.White;
    public static IDrawable Mark(ProTeam team, bool framed = true) => new BadgeArt(team, framed);
    sealed class BadgeArt(ProTeam team, bool framed) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF bounds)
        {
            canvas.SaveState(); var primary = Color.FromArgb(team.Primary); var secondary = Color.FromArgb(team.Secondary);
            var pad = framed ? 2f : 0f; var x = bounds.X + pad; var y = bounds.Y + pad; var width = bounds.Width - pad * 2; var height = bounds.Height - pad * 2;
            if (framed)
            {
                canvas.FillColor = primary; canvas.FillRoundedRectangle(x, y, width, height, height * .25f);
                canvas.StrokeColor = secondary; canvas.StrokeSize = Math.Max(1.5f, height * .035f); canvas.DrawRoundedRectangle(x, y, width, height, height * .25f);
            }
            var a = Luminance(primary); var b = Luminance(secondary); var contrast = (Math.Max(a, b) + .05) / (Math.Min(a, b) + .05);
            canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold; canvas.FontColor = contrast >= 4.5 ? secondary : Ink(team);
            canvas.FontSize = Math.Min(height * .43f, width / Math.Max(2, team.Abbreviation.Length) * 1.1f);
            canvas.DrawString(team.Abbreviation, x, y, width, height, HorizontalAlignment.Center, VerticalAlignment.Center);
            canvas.RestoreState();
        }
    }
}
