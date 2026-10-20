using DynastyManager.Core.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

/// <summary>Lightweight, offline illustrated headshots. No image cache or network required.</summary>
internal sealed class PersonPortrait(PortraitAppearance face, Color primary, Color secondary,
    bool staff, int age) : IDrawable
{
    private static readonly string[] Skin = ["#F0C9AB", "#DCAB84", "#C58D64", "#AB714C", "#8E583B", "#70432F", "#503125"];
    private static readonly string[] Hair = ["#201C1C", "#3B2924", "#60412C", "#9F773F", "#BB955B", "#6C3528"];

    public static GraphicsView Create(Guid id, string name, Color primary, Color secondary,
        double size = 76, bool staff = false, int age = 20)
    {
        var view = new GraphicsView { WidthRequest = size, HeightRequest = size,
            HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center,
            InputTransparent = true, Drawable = new PersonPortrait(PortraitAppearance.ForIdentity(id), primary, secondary, staff, age) };
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
        canvas.FillColor = Color.FromArgb("#24394B");
        canvas.FillRoundedRectangle(0, 0, 100, 100, 16);
        canvas.FillColor = secondary.WithAlpha(.16f);
        canvas.FillEllipse(45, -12, 80, 80);
        // Team jersey/polo, shoulders and collar.
        canvas.FillColor = primary;
        canvas.FillRoundedRectangle(7, 75, 86, 25, 20);
        var skin = Color.FromArgb(Skin[face.Skin]);
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
        var hair = Color.FromArgb(staff && age >= 55 ? "#93928B" : Hair[face.HairColor]);
        canvas.FillColor = hair;
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
        canvas.StrokeColor = hair; canvas.StrokeSize = 1.8f;
        canvas.DrawLine(35, 41 + face.Brow, 43, 40); canvas.DrawLine(57, 40, 65, 41 + face.Brow);
        canvas.FillColor = Color.FromArgb("#211E20");
        canvas.FillEllipse(37, 45, 4, 4); canvas.FillEllipse(59, 45, 4, 4);
        canvas.StrokeColor = Color.FromArgb("#714331"); canvas.StrokeSize = 1.3f;
        canvas.DrawLine(46, 61, 55, 61 + face.Detail % 2);
        if (face.FacialHair > 0)
        {
            canvas.StrokeColor = hair.WithAlpha(.7f); canvas.StrokeSize = face.FacialHair == 3 ? 5 : 2;
            canvas.DrawLine(43, 66, 57, 66);
            if (face.FacialHair >= 2)
            {
                canvas.DrawLine(33, 57, 39, 66); canvas.DrawLine(67, 57, 61, 66);
            }
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
