using DynastyManager.Core.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

/// <summary>Lightweight, offline illustrated headshots. No image cache or network required.</summary>
public sealed class PersonPortrait(PortraitAppearance face, Color primary, Color secondary,
    bool staff, int age, bool naturalAging) : IDrawable
{
    private static readonly string[] Hair = ["#201C1C", "#3B2924", "#60412C", "#9F773F", "#BB955B", "#6C3528", "#93928B", "#C8C9C5"];

    public static GraphicsView Create(Guid id, string name, Color primary, Color secondary,
        double size = 76, bool staff = false, int age = 20, NcaaPlayerAppearance? appearance = null)
    {
        var view = new GraphicsView { WidthRequest = size, HeightRequest = size,
            HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center,
            BackgroundColor = Colors.Transparent, InputTransparent = true, Drawable = new PersonPortrait(
                staff ? PortraitAppearance.ForStaff(id, age, appearance) : PortraitAppearance.ForPlayer(id, appearance),
                primary, secondary, staff, age, appearance?.HairColor.HasValue != true) };
        SemanticProperties.SetDescription(view, $"Illustrated portrait of {name}");
        return view;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.SaveState();
        var size = Math.Min(dirtyRect.Width, dirtyRect.Height);
        canvas.Translate(dirtyRect.X + (dirtyRect.Width - size) / 2, dirtyRect.Y + (dirtyRect.Height - size) / 2);
        canvas.Scale(size / 100, size / 100);
        canvas.ClipRectangle(0, 0, 100, 100);
        // Team jersey/polo, shoulders and collar.
        canvas.FillColor = primary;
        canvas.FillRoundedRectangle(7, 75, 86, 25, 20);
        var skin = Color.FromArgb(PortraitAppearance.SkinColor(face.Skin));
        canvas.FillColor = skin;
        canvas.FillRectangle(41, 65, 18, 17);
        canvas.FillColor = secondary;
        var collar = new PathF();
        collar.MoveTo(35, 77); collar.LineTo(50, 88); collar.LineTo(65, 77);
        collar.LineTo(61, 75); collar.LineTo(50, 82); collar.LineTo(39, 75); collar.Close();
        canvas.FillPath(collar);
        if (staff)
        {
            canvas.StrokeColor = secondary.WithAlpha(.6f); canvas.StrokeSize = 1.5f;
            canvas.DrawLine(50, 87, 50, 99);
        }
        var width = 43 + face.FaceShape * 2;
        var left = (100 - width) / 2f;
        canvas.FillColor = skin;
        canvas.FillEllipse(left - 4, 41, 9, 15); canvas.FillEllipse(left + width - 5, 41, 9, 15);
        canvas.FillRoundedRectangle(left, 22, width, 52, 20);
        canvas.FillColor = Color.FromArgb("#271B18").WithAlpha(.13f);
        canvas.FillEllipse(47, 49, 7, 10);
        var hair = Color.FromArgb(Hair[face.HairColor]);
        canvas.FillColor = hair;
        if (face.HairStyle is 10 or 11)
        {
            canvas.FillColor = hair;
            if (face.HairStyle == 10)
            {
                canvas.FillRoundedRectangle(left - 5, 32, 8, 40, 3);
                canvas.FillRoundedRectangle(left + width - 3, 32, 8, 40, 3);
            }
            else for (var i = 0; i < 3; i++)
            {
                canvas.FillRoundedRectangle(left - 6 + i * 3, 30, 3, 43 - i * 3, 1);
                canvas.FillRoundedRectangle(left + width + 3 - i * 3, 30, 3, 43 - i * 3, 1);
            }
        }
        if (face.HairStyle != 4)
        {
            canvas.FillRoundedRectangle(left - 1, 16, width + 2, face.HairStyle == 0 ? 18 : 24, 12);
            canvas.FillColor = skin;
            canvas.FillRoundedRectangle(left + 3, face.HairStyle == 1 ? 30 : 27, width - 6, 20, 9);
            canvas.FillColor = hair;
            if (face.HairStyle == 2) canvas.FillEllipse(left - 2, 14, width * .75f, 20);
            if (face.HairStyle == 3)
            {
                for (var i = 0; i < 6; i++) canvas.FillEllipse(left - 2 + i * (width / 6f), 14 + i % 2 * 3, 12, 13);
            }
        }
        if (face.HairStyle == 5)
        {
            canvas.StrokeColor = hair; canvas.StrokeSize = 2;
            for (var i = 0; i < 5; i++) canvas.DrawLine(left + 5 + i * 8, 20, left + 7 + i * 8, 35);
        }
        if (face.HairStyle == 6) { canvas.FillColor = hair; canvas.FillRectangle(left + 1, 15, width - 2, 14); }
        if (face.HairStyle == 7) { canvas.FillColor = hair.WithAlpha(.55f); canvas.FillRoundedRectangle(left, 26, 5, 19, 2); canvas.FillRoundedRectangle(left + width - 5, 26, 5, 19, 2); }
        if (face.HairStyle == 8) { canvas.FillColor = skin; canvas.FillEllipse(left + 7, 13, width - 14, 20); }
        canvas.StrokeColor = hair; canvas.StrokeSize = 1.8f;
        canvas.DrawLine(35, 41 + face.Brow, 43, 40); canvas.DrawLine(57, 40, 65, 41 + face.Brow);
        canvas.FillColor = Color.FromArgb("#211E20");
        canvas.FillEllipse(37, 45, 4, 4); canvas.FillEllipse(59, 45, 4, 4);
        canvas.StrokeColor = Color.FromArgb("#714331"); canvas.StrokeSize = 1.3f;
        canvas.DrawLine(46, 61, 55, 61 + face.Detail % 2);
        // Filled silhouettes read as facial hair at thumbnail sizes, without scar-like cheek strokes.
        if (face.FacialHair is 1 or 3)
        {
            canvas.FillColor = hair.WithAlpha(face.FacialHair == 1 ? .22f : .95f);
            var beard = new PathF();
            beard.MoveTo(left + 2, 51); beard.LineTo(38, 57); beard.LineTo(43, 55);
            beard.LineTo(57, 55); beard.LineTo(62, 57); beard.LineTo(left + width - 2, 51);
            beard.CurveTo(68, 70, 60, 77, 50, 77); beard.CurveTo(40, 77, 31, 70, left + 2, 51); beard.Close();
            canvas.FillPath(beard);
            canvas.FillColor = skin; canvas.FillRoundedRectangle(42, 59, 16, 5, 2);
        }
        else if (face.FacialHair == 2)
        {
            canvas.FillColor = hair;
            canvas.FillRoundedRectangle(42, 63, 16, 12, 5);
            canvas.FillRoundedRectangle(42, 56, 16, 3, 1);
            canvas.FillColor = skin; canvas.FillRoundedRectangle(45, 62, 10, 3, 1);
        }
        // Keep the mouth visible against beard and goatee fills.
        canvas.StrokeColor = Color.FromArgb("#714331"); canvas.StrokeSize = 1.3f;
        canvas.DrawLine(46, 61, 55, 61 + face.Detail % 2);
        if (staff && age >= 45)
        {
            if (naturalAging && age < 55 && face.HairStyle != 4)
            {
                canvas.StrokeColor = Color.FromArgb("#B4B4AE"); canvas.StrokeSize = 1.5f;
                canvas.DrawLine(left + 1, 31, left + 2, 39); canvas.DrawLine(left + width - 1, 31, left + width - 2, 39);
            }
            canvas.StrokeColor = Color.FromArgb("#503125").WithAlpha(age >= 60 ? .30f : .15f);
            canvas.StrokeSize = .8f;
            canvas.DrawLine(34, 51, 40, 52); canvas.DrawLine(60, 52, 66, 51);
            if (age >= 55) { canvas.DrawLine(41, 35, 59, 35); canvas.DrawLine(42, 37, 58, 37); }
        }
        if (staff && age >= 45 && face.Detail == 0)
        {
            canvas.StrokeColor = secondary; canvas.StrokeSize = 1.2f;
            canvas.DrawRoundedRectangle(32, 43, 14, 10, 3); canvas.DrawRoundedRectangle(54, 43, 14, 10, 3);
            canvas.DrawLine(46, 47, 54, 47);
        }
        canvas.RestoreState();
    }
}
