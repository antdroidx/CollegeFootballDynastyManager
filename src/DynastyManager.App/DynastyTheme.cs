using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

public static class DynastyTheme
{
    public static readonly Color Background = Color.FromArgb("#0B1220");
    public static readonly Color Surface = Color.FromArgb("#142137");
    public static readonly Color Accent = Color.FromArgb("#14B8A6");
    public static readonly Color Muted = Color.FromArgb("#A9B9CE");

    public static ResourceDictionary CreateResources() => new()
    {
        new Style(typeof(ContentPage)) { Setters = { new Setter { Property = VisualElement.BackgroundColorProperty, Value = Background } } },
        new Style(typeof(Label)) { Setters = {
            new Setter { Property = Label.TextColorProperty, Value = Color.FromArgb("#EDF4FC") },
            new Setter { Property = Label.FontSizeProperty, Value = 14.0 },
            new Setter { Property = Label.LineHeightProperty, Value = 1.15 } } },
        new Style(typeof(Button)) { Setters = {
            new Setter { Property = VisualElement.BackgroundColorProperty, Value = Color.FromArgb("#254363") },
            new Setter { Property = Button.TextColorProperty, Value = Colors.White },
            new Setter { Property = Button.CornerRadiusProperty, Value = 12 },
            new Setter { Property = Button.FontSizeProperty, Value = 13.0 },
            new Setter { Property = Button.PaddingProperty, Value = new Thickness(14, 10) },
            new Setter { Property = VisualElement.MinimumHeightRequestProperty, Value = 48.0 } } },
        new Style(typeof(Border)) { Setters = {
            new Setter { Property = VisualElement.BackgroundColorProperty, Value = Surface },
            new Setter { Property = Border.StrokeProperty, Value = new SolidColorBrush(Color.FromArgb("#2A3C55")) },
            new Setter { Property = Border.StrokeThicknessProperty, Value = 1.0 },
            new Setter { Property = Border.StrokeShapeProperty, Value = new RoundRectangle { CornerRadius = 16 } },
            new Setter { Property = Border.PaddingProperty, Value = new Thickness(14) } } },
        new Style(typeof(Picker)) { Setters = {
            new Setter { Property = Picker.TextColorProperty, Value = Colors.White },
            new Setter { Property = Picker.TitleColorProperty, Value = Muted },
            new Setter { Property = VisualElement.BackgroundColorProperty, Value = Color.FromArgb("#1C304B") },
            new Setter { Property = VisualElement.MinimumHeightRequestProperty, Value = 48.0 } } },
        new Style(typeof(Entry)) { Setters = {
            new Setter { Property = Entry.TextColorProperty, Value = Colors.White },
            new Setter { Property = Entry.PlaceholderColorProperty, Value = Muted },
            new Setter { Property = VisualElement.BackgroundColorProperty, Value = Surface },
            new Setter { Property = VisualElement.MinimumHeightRequestProperty, Value = 48.0 } } },
        new Style(typeof(ProgressBar)) { Setters = { new Setter { Property = ProgressBar.ProgressColorProperty, Value = Accent } } },
        new Style(typeof(Switch)) { Setters = { new Setter { Property = Switch.OnColorProperty, Value = Accent } } }
    };
}
