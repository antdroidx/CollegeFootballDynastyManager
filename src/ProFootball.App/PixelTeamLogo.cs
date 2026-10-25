using ProFootball.Core;
namespace ProFootball.App;
// Original files stay unchanged. The Android renderer samples a 24px grid for crisp retro pixels.
public sealed class PixelTeamLogo : GraphicsView
{
    static readonly Dictionary<string, PixelArt> Cache = [];
    public PixelTeamLogo(ProTeam team, int size = 44)
    { WidthRequest = HeightRequest = size; InputTransparent = true; Loaded += async (_, _) => { Drawable = await Art(team); Invalidate(); }; }
    public static async Task<PixelArt> Art(ProTeam team)
    {
        if (team.LogoFile is null) return new([], 0);
        if (Cache.TryGetValue(team.LogoFile, out var existing)) return existing;
        using var input = await FileSystem.OpenAppPackageFileAsync(team.LogoFile);
#if ANDROID
        using var original = await Android.Graphics.BitmapFactory.DecodeStreamAsync(input);
        if (original is null) return new([], 0);
        using var small = Android.Graphics.Bitmap.CreateScaledBitmap(original, 24, 24, false)!;
        var pixels = new int[24 * 24]; small.GetPixels(pixels, 0, 24, 0, 0, 24, 24);
        var art = new PixelArt(pixels, 24); Cache[team.LogoFile] = art; return art;
#else
        return new([],0);
#endif
    }
    public sealed class PixelArt(int[] pixels, int side) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF bounds)
        {
            if (side == 0) return; var size = Math.Min(bounds.Width, bounds.Height) / side; var left = bounds.X + (bounds.Width - size * side) / 2; var top = bounds.Y + (bounds.Height - size * side) / 2;
            for (var y = 0; y < side; y++) for (var x = 0; x < side; x++) { var c = pixels[y * side + x]; var alpha = (c >> 24) & 255; if (alpha < 50) continue; canvas.FillColor = Color.FromRgba((c >> 16) & 255, (c >> 8) & 255, c & 255, alpha); canvas.FillRectangle(left + x * size, top + y * size, size + .1f, size + .1f); }
        }
    }
}
