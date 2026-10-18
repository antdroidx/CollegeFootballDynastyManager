using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

public static class DynastyTheme
{
    public static readonly Color Background = Color.FromArgb("#101318");
    public static readonly Color Surface = Color.FromArgb("#191F27");
    public static readonly Color Accent = Color.FromArgb("#89ADD2");
    public static readonly Color Muted = Color.FromArgb("#9BA6B5");

    public static ResourceDictionary CreateResources() => new()
    {
        new Style(typeof(ContentPage)) { ApplyToDerivedTypes = true, Setters = { new Setter { Property = VisualElement.BackgroundColorProperty, Value = Background } } },
        new Style(typeof(Label)) { Setters = {
            new Setter { Property = Label.TextColorProperty, Value = Color.FromArgb("#E6EBF1") },
            new Setter { Property = Label.FontSizeProperty, Value = 13.0 },
            new Setter { Property = Label.LineHeightProperty, Value = 1.1 } } },
        new Style(typeof(Button)) { Setters = {
            new Setter { Property = VisualElement.BackgroundColorProperty, Value = Color.FromArgb("#263342") },
            new Setter { Property = Button.TextColorProperty, Value = Colors.White },
            new Setter { Property = View.VerticalOptionsProperty, Value = LayoutOptions.Center },
            new Setter { Property = View.HorizontalOptionsProperty, Value = LayoutOptions.Start },
            new Setter { Property = Button.CornerRadiusProperty, Value = 5 },
            new Setter { Property = Button.FontSizeProperty, Value = 12.0 },
            new Setter { Property = Button.PaddingProperty, Value = new Thickness(10, 5) },
            new Setter { Property = VisualElement.MinimumHeightRequestProperty, Value = 36.0 } } },
        new Style(typeof(Border)) { Setters = {
            new Setter { Property = VisualElement.BackgroundColorProperty, Value = Surface },
            new Setter { Property = Border.StrokeProperty, Value = new SolidColorBrush(Colors.Transparent) },
            new Setter { Property = Border.StrokeThicknessProperty, Value = 0.0 },
            new Setter { Property = Border.StrokeShapeProperty, Value = new RoundRectangle { CornerRadius = 6 } },
            new Setter { Property = Border.PaddingProperty, Value = new Thickness(10) } } },
        new Style(typeof(Picker)) { Setters = {
            new Setter { Property = Picker.FontSizeProperty, Value = 13.0 },
            new Setter { Property = Picker.TextColorProperty, Value = Colors.White },
            new Setter { Property = Picker.TitleColorProperty, Value = Muted },
            new Setter { Property = VisualElement.BackgroundColorProperty, Value = Color.FromArgb("#202A36") },
            new Setter { Property = VisualElement.MinimumHeightRequestProperty, Value = 36.0 } } },
        new Style(typeof(Entry)) { Setters = {
            new Setter { Property = Entry.FontSizeProperty, Value = 13.0 },
            new Setter { Property = Entry.TextColorProperty, Value = Colors.White },
            new Setter { Property = Entry.PlaceholderColorProperty, Value = Muted },
            new Setter { Property = VisualElement.BackgroundColorProperty, Value = Surface },
            new Setter { Property = VisualElement.MinimumHeightRequestProperty, Value = 36.0 } } },
        new Style(typeof(ProgressBar)) { Setters = { new Setter { Property = ProgressBar.ProgressColorProperty, Value = Accent } } },
        new Style(typeof(Switch)) { Setters = { new Setter { Property = Switch.OnColorProperty, Value = Accent } } }
    };
}
