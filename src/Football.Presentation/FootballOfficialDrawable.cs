using Microsoft.Maui.Graphics;
namespace DynastyManager.App;

public sealed class FootballOfficialDrawable : IDrawable
{
    public void Draw(ICanvas canvas, RectF rect) => Paint(canvas, rect.Center.X, rect.Center.Y, Math.Min(rect.Width / 18, rect.Height / 28));
    public static void Paint(ICanvas c, float x, float y, float scale = 1)
    {
        c.SaveState(); c.Translate(x, y); c.Scale(scale, scale);
        c.FillColor = Color.FromArgb("#55000000"); c.FillEllipse(-8, 10, 16, 5);
        c.FillColor = Colors.Black; c.FillRectangle(-5, 3, 4, 10); c.FillRectangle(1, 3, 4, 10);
        c.FillColor = Colors.White; c.FillRectangle(-6, -6, 12, 10);
        c.FillColor = Colors.Black; for (var i = -5; i <= 4; i += 3) c.FillRectangle(i, -6, 1.5f, 10);
        c.FillColor = Color.FromArgb("#C99164"); c.FillRectangle(-4,-12,8,6); c.FillRectangle(-9,-4,3,8); c.FillRectangle(6,-4,3,8);
        c.FillColor = Colors.White; c.FillRectangle(-5,-14,10,3);
        c.RestoreState();
    }
}
