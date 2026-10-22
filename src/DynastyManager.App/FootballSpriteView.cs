using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

/// <summary>Original palette-based pixel player. No bitmap sheets, downloads or simulation state.</summary>
internal sealed class FootballSpriteView : GraphicsView
{
    private readonly PlayerDrawable art;
    private int lastFrame = -1;
    public FootballSpriteView(Color primary, Color secondary, bool away, int skin, string label)
    {
        art = new(primary, secondary, away, skin) { Label = label };
        Drawable = art; InputTransparent = true;
        WidthRequest = HeightRequest = 28;
    }
    public void SetLabel(string label) { art.Label = label; Invalidate(); }
    public void Highlight(bool active) { art.Active = active; Invalidate(); }
    public void Animate(double progress, double seconds, double dx, bool blocking, bool passing)
    {
        var frame = (int)(progress * seconds * 8) % 4;
        var facing = Math.Abs(dx) > .00001 ? dx > 0 : art.Right;
        if (frame == lastFrame && facing == art.Right && blocking == art.Blocking && passing == art.Passing) return;
        lastFrame = frame; art.Frame = frame; art.Right = facing; art.Blocking = blocking; art.Passing = passing;
        Invalidate();
    }
    private sealed class PlayerDrawable(Color primary, Color secondary, bool away, int skin) : IDrawable
    {
        public string Label { get; set; } = "";
        public bool Active { get; set; }
        public bool Right { get; set; } = true;
        public bool Blocking { get; set; }
        public bool Passing { get; set; }
        public int Frame { get; set; }
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var scale = Math.Min(dirtyRect.Width, dirtyRect.Height) / 32f;
            canvas.SaveState();
            canvas.Translate(dirtyRect.X, dirtyRect.Y); canvas.Scale(scale, scale);
            var ink = Color.FromArgb("#111923");
            var skinColor = Color.FromArgb(new[] { "#E4B58D", "#C5895C", "#905C3B", "#603D2C" }[Math.Clamp(skin, 0, 3)]);
            var jersey = away ? Colors.White : primary;
            var pants = away ? Color.FromArgb("#E0E4E9") : secondary;
            canvas.FillColor = Color.FromRgba(0, 0, 0, .24); canvas.FillEllipse(5, 26, 22, 5);
            if (Active)
            {
                canvas.StrokeColor = Color.FromArgb("#FFDE59"); canvas.StrokeSize = 1.6f;
                canvas.DrawEllipse(3, 24, 26, 7);
            }
            void Pixel(Color color, int x, int y, int w, int h)
            { canvas.FillColor = color; canvas.FillRectangle(Right ? x : 32 - x - w, y, w, h); }
            // Cleats, knees and pants alternate without changing the player's field position.
            var stride = Blocking ? 0 : Frame switch { 0 => -2, 2 => 2, _ => 0 };
            Pixel(ink, 9 + stride, 24, 6, 5); Pixel(ink, 20 - stride, 23, 6, 5);
            Pixel(pants, 10 + stride, 21, 5, 5); Pixel(pants, 19 - stride, 20, 5, 5);
            Pixel(Colors.White, 10 + stride, 25, 3, 1); Pixel(Colors.White, 20 - stride, 24, 3, 1);
            Pixel(ink, 8, 11, 16, 11); Pixel(jersey, 9, 12, 14, 9);
            Pixel(primary, 9, 12, 3, 3); Pixel(primary, 21, 12, 3, 3);
            Pixel(secondary, 12, 20, 9, 2);
            if (Blocking)
            {
                Pixel(skinColor, 23, 12, 5, 3); Pixel(Colors.White, 27, 11, 3, 4);
                Pixel(skinColor, 22, 18, 5, 3); Pixel(Colors.White, 26, 17, 3, 4);
            }
            else if (Passing)
            {
                Pixel(skinColor, 6, 8, 4, 7); Pixel(Colors.White, 5, 7, 4, 3);
                Pixel(skinColor, 22, 15, 5, 3); Pixel(Colors.White, 26, 14, 3, 4);
            }
            else
            {
                Pixel(skinColor, 6, 14 + stride, 4, 6); Pixel(Colors.White, 5, 18 + stride, 4, 3);
                Pixel(skinColor, 22, 13 - stride, 4, 6); Pixel(Colors.White, 23, 17 - stride, 4, 3);
            }
            // Helmet silhouette, crown stripe, cheek and dark face opening.
            Pixel(ink, 11, 2, 11, 10); Pixel(ink, 9, 4, 15, 6);
            Pixel(primary, 11, 3, 10, 8); Pixel(primary, 10, 5, 13, 5);
            Pixel(secondary, 14, 3, 3, 7); Pixel(Colors.White, 11, 4, 2, 2);
            Pixel(skinColor, 20, 8, 3, 3); Pixel(ink, 21, 6, 3, 2);
            Pixel(Color.FromArgb("#C9D0D7"), 23, 8, 3, 1); Pixel(Color.FromArgb("#C9D0D7"), 25, 8, 1, 4);
            canvas.FontColor = away ? primary : Colors.White;
            canvas.FontSize = Label.Length > 2 ? 5.5f : 7;
            canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
            canvas.DrawString(Label, 10, 13, 13, 8, HorizontalAlignment.Center, VerticalAlignment.Center);
            canvas.RestoreState();
        }
    }
}
