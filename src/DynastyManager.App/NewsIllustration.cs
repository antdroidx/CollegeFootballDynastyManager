using DynastyManager.Data.Branding;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

/// <summary>Offline editorial artwork; never presented as a photograph of an actual event.</summary>
internal sealed class NewsIllustration(string school) : IDrawable
{
    public static View Create(string school, string category, double height)
    {
        var view = new GraphicsView { Drawable = new NewsIllustration(school), HeightRequest = height,
            HorizontalOptions = LayoutOptions.Fill, InputTransparent = true };
        SemanticProperties.SetDescription(view, school + " football stadium illustration for " + category);
        return view;
    }

    public void Draw(ICanvas canvas, RectF bounds)
    {
        var brand = TeamBrandCatalog.Get(school);
        canvas.FillColor = Color.FromArgb("#10233B");
        canvas.FillRectangle(bounds);
        // Stadium terraces, floodlights and a full-width field form a lightweight vector image.
        for (var row = 0; row < 4; row++)
        {
            canvas.FillColor = Color.FromArgb(row % 2 == 0 ? brand.Primary : "#31465A");
            canvas.FillRectangle(0, bounds.Height * (.24f + row * .065f), bounds.Width, bounds.Height * .05f);
        }
        canvas.StrokeColor = Color.FromArgb("#D6E5EB"); canvas.StrokeSize = 2;
        foreach (var x in new[] { bounds.Width * .1f, bounds.Width * .9f })
        {
            canvas.DrawLine(x, bounds.Height * .12f, x, bounds.Height * .53f);
            canvas.FillColor = Color.FromArgb("#F6E7AB");
            canvas.FillRoundedRectangle(x - 13, bounds.Height * .1f, 26, 5, 2);
        }
        canvas.FillColor = Color.FromArgb("#216044");
        canvas.FillRectangle(0, bounds.Height * .56f, bounds.Width, bounds.Height * .44f);
        canvas.StrokeColor = Colors.White.WithAlpha(.4f); canvas.StrokeSize = 1;
        for (var i = 1; i < 10; i++) canvas.DrawLine(bounds.Width * i / 10, bounds.Height * .58f, bounds.Width * i / 10, bounds.Height);
        canvas.FillColor = Color.FromArgb(brand.Primary);
        canvas.FillCircle(bounds.Width / 2, bounds.Height * .7f, bounds.Height * .24f);
        canvas.FontColor = Colors.White; canvas.FontSize = bounds.Height * .24f;
        canvas.DrawString(school.Length > 0 ? school[..1] : "•", bounds.Width / 2 - 35, bounds.Height * .55f, 70, bounds.Height * .3f,
            HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}
