using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

public static class DynastyTheme
{
    public static void ApplyNativeAppearance()
    {
#if ANDROID
        if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window is { } window)
        {
            var bars = AndroidX.Core.View.WindowCompat.GetInsetsController(window, window.DecorView);
            if (bars is null) return;
            bars.AppearanceLightStatusBars = IsLight;
            bars.AppearanceLightNavigationBars = IsLight;
        }
#endif
    }

    public static string Preference => Microsoft.Maui.Storage.Preferences.Default.Get("app-theme", "Dark");
    public static AppTheme RequestedTheme => Preference switch { "Light" => AppTheme.Light, "Dark" => AppTheme.Dark, _ => AppTheme.Unspecified };
    public static bool IsLight => Preference == "Light" || (Preference == "System" && Application.Current?.RequestedTheme == AppTheme.Light);
    public static Color Text => IsLight ? Color.FromArgb("#182638") : Color.FromArgb("#E6EBF1");
    public static Color Background => IsLight ? Color.FromArgb("#F2F5FA") : Color.FromArgb("#09121F");
    public static Color Surface => IsLight ? Colors.White : Color.FromArgb("#122233");
    public static Color Accent => IsLight ? Color.FromArgb("#245E99") : Color.FromArgb("#ADC9EF");
    public static Color Panel => IsLight ? Color.FromArgb("#E7EEF6") : Color.FromArgb("#192E43");
    public static Color Selected => IsLight ? Color.FromArgb("#E7E0F8") : Color.FromArgb("#38245F");
    public static Color Muted => IsLight ? Color.FromArgb("#526377") : Color.FromArgb("#9BA6B5");

    // Only UI literals pass through here; team colors and field graphics remain unchanged.
    public static Color Palette(string hex)
    {
        var original = Color.FromArgb(hex);
        if (!IsLight) return original;
        var r = original.Red; var g = original.Green; var b = original.Blue;
        // Navy/neutral UI surfaces, excluding grass and team brand values.
        if (Math.Max(r, Math.Max(g, b)) < .34f && b >= r && b >= g * .92f)
            return Color.FromRgba(.88f + r * .3f, .90f + g * .25f, .93f + b * .2f, original.Alpha);
        return original;
    }

    public static Color ReadableInk(Color? ink, VisualElement view)
    {
        if (ink is null) return Text;
        if (!IsLight) return ink;
        Color background = Background;
        for (Element? item = view; item is not null; item = item.Parent)
            if (item is VisualElement element && element.BackgroundColor is { Alpha: > .95f } color)
            { background = color; break; }
        var luminance = background.Red * .2126f + background.Green * .7152f + background.Blue * .0722f;
        if (luminance < .55f)
            return ink.Red * .2126f + ink.Green * .7152f + ink.Blue * .0722f < .35f
                ? Color.FromArgb("#E6EBF1") : ink;
        if (ink.Red > .65f && ink.Green > .65f && ink.Blue > .65f) return Text;
        if (ink.Red > .7f && ink.Green > .45f && ink.Blue < .65f) return Color.FromArgb("#765609");
        if (ink.Red > .7f && ink.Green < .4f) return Color.FromArgb("#A52828");
        if (ink.Green > .6f && ink.Red < .6f) return Color.FromArgb("#146B40");
        var inkLuminance = ink.Red * .2126f + ink.Green * .7152f + ink.Blue * .0722f;
        return inkLuminance > .55f ? Color.FromRgb(ink.Red * .55f, ink.Green * .55f, ink.Blue * .55f) : ink;
    }

    public static ResourceDictionary CreateResources() => new()
    {
        new Style(typeof(ContentPage)) { ApplyToDerivedTypes = true, Setters = { new Setter { Property = VisualElement.BackgroundColorProperty, Value = Background } } },
        new Style(typeof(Label)) { Setters = {
            new Setter { Property = Label.TextColorProperty, Value = Text },
            new Setter { Property = Label.FontSizeProperty, Value = 13.0 },
            new Setter { Property = Label.LineHeightProperty, Value = 1.1 } } },
        new Style(typeof(Button)) { Setters = {
            new Setter { Property = VisualElement.BackgroundColorProperty, Value = Palette("#263342") },
            new Setter { Property = Button.TextColorProperty, Value = Text },
            new Setter { Property = View.VerticalOptionsProperty, Value = LayoutOptions.Center },
            new Setter { Property = View.HorizontalOptionsProperty, Value = LayoutOptions.Start },
            new Setter { Property = Button.CornerRadiusProperty, Value = 8 },
            new Setter { Property = Button.FontSizeProperty, Value = 12.0 },
            new Setter { Property = Button.PaddingProperty, Value = new Thickness(10, 5) },
            new Setter { Property = VisualElement.MinimumHeightRequestProperty, Value = 44.0 } } },
        new Style(typeof(Border)) { Setters = {
            new Setter { Property = VisualElement.BackgroundColorProperty, Value = Surface },
            new Setter { Property = Border.StrokeProperty, Value = new SolidColorBrush(Colors.Transparent) },
            new Setter { Property = Border.StrokeThicknessProperty, Value = 0.0 },
            new Setter { Property = Border.StrokeShapeProperty, Value = new RoundRectangle { CornerRadius = 10 } },
            new Setter { Property = Border.PaddingProperty, Value = new Thickness(10) } } },
        new Style(typeof(Picker)) { Setters = {
            new Setter { Property = Picker.FontSizeProperty, Value = 13.0 },
            new Setter { Property = Picker.TextColorProperty, Value = Text },
            new Setter { Property = Picker.TitleColorProperty, Value = Muted },
            new Setter { Property = VisualElement.BackgroundColorProperty, Value = Palette("#202A36") },
            new Setter { Property = VisualElement.MinimumHeightRequestProperty, Value = 44.0 } } },
        new Style(typeof(Entry)) { Setters = {
            new Setter { Property = Entry.FontSizeProperty, Value = 13.0 },
            new Setter { Property = Entry.TextColorProperty, Value = Text },
            new Setter { Property = Entry.PlaceholderColorProperty, Value = Muted },
            new Setter { Property = VisualElement.BackgroundColorProperty, Value = Surface },
            new Setter { Property = VisualElement.MinimumHeightRequestProperty, Value = 44.0 } } },
        new Style(typeof(ProgressBar)) { Setters = { new Setter { Property = ProgressBar.ProgressColorProperty, Value = Accent } } },
        new Style(typeof(Switch)) { Setters = { new Setter { Property = Switch.OnColorProperty, Value = Accent } } }
    };
}
