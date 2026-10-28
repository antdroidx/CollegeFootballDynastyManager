using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

public static class DynastyTheme
{
    // Each Android host runs in its own process; college keeps the default palette.
    public static bool ProfessionalPalette { get; set; }

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
    public static Color Text => ProfessionalPalette ? (IsLight ? Color.FromArgb("#172B40") : Color.FromArgb("#E7EFF7")) : IsLight ? Color.FromArgb("#182638") : Color.FromArgb("#E6EBF1");
    public static Color Background => ProfessionalPalette ? (IsLight ? Color.FromArgb("#EFF3F8") : Color.FromArgb("#0B1625")) : IsLight ? Color.FromArgb("#F2F5FA") : Color.FromArgb("#09121F");
    public static Color Surface => ProfessionalPalette ? (IsLight ? Colors.White : Color.FromArgb("#132439")) : IsLight ? Colors.White : Color.FromArgb("#122233");
    public static Color Accent => ProfessionalPalette ? (IsLight ? Color.FromArgb("#006B70") : Color.FromArgb("#77D6D0")) : IsLight ? Color.FromArgb("#245E99") : Color.FromArgb("#ADC9EF");
    public static Color Panel => ProfessionalPalette ? (IsLight ? Color.FromArgb("#DCE6EF") : Color.FromArgb("#20374C")) : IsLight ? Color.FromArgb("#E7EEF6") : Color.FromArgb("#192E43");
    public static Color Selected => ProfessionalPalette ? (IsLight ? Color.FromArgb("#C6E9E7") : Color.FromArgb("#164B51")) : IsLight ? Color.FromArgb("#E7E0F8") : Color.FromArgb("#38245F");
    public static Color Muted => IsLight ? Color.FromArgb("#526377") : Color.FromArgb("#9BA6B5");

    // Only UI literals pass through here; team colors and field graphics remain unchanged.
    public static Color Palette(string hex)
    {
        var original = Color.FromArgb(hex);
        if (ProfessionalPalette && hex.ToUpperInvariant() is "#263342" or "#202A36" or "#1C2B3B" or "#152536") return Panel;
        if (!IsLight) return original;
        var r = original.Red; var g = original.Green; var b = original.Blue;
        // Navy/neutral UI surfaces, excluding grass and team brand values.
        if (Math.Max(r, Math.Max(g, b)) < .34f && b >= r && b >= g * .92f)
            return Color.FromRgba(.88f + r * .3f, .90f + g * .25f, .93f + b * .2f, original.Alpha);
        return original;
    }

    public static Color ReadableInk(Color? ink, VisualElement view)
    {
        ink ??= Text;
        Color background = Background;
        for (Element? item = view; item is not null; item = item.Parent)
            if (item is VisualElement element && element.BackgroundColor is { Alpha: > .95f } color)
            { background = color; break; }
        var (r,g,b) = ColorContrast.Ensure(ink.Red,ink.Green,ink.Blue,background.Red,background.Green,background.Blue);
        return Color.FromRgb(r,g,b);
    }

    public static void ConfigureContrast()
    {
        Microsoft.Maui.Handlers.LabelHandler.Mapper.AppendToMapping("ThemeContrast", (_, view) =>
        { if (view is Label label) label.Loaded += (_, _) => label.TextColor = ReadableInk(label.TextColor,label); });
        Microsoft.Maui.Handlers.ButtonHandler.Mapper.AppendToMapping("ThemeContrast", (_, view) =>
        { if (view is Button button) button.Loaded += (_, _) => button.TextColor = ReadableInk(button.TextColor,button); });
        Microsoft.Maui.Handlers.SearchBarHandler.Mapper.AppendToMapping("ThemeContrast", (_, view) =>
        { if (view is SearchBar search) search.Loaded += (_, _) => search.TextColor = ReadableInk(search.TextColor,search); });
    }

    public static Color ControlEdge => IsLight ? Color.FromArgb("#65798C") : Color.FromArgb("#6E879A");
    static VisualStateGroupList ButtonStates() => new() {
        new VisualStateGroup { Name="CommonStates", States={
            new VisualState { Name="Normal" },
            new VisualState { Name="Disabled", Setters={
                new Setter { Property=VisualElement.BackgroundColorProperty, Value=IsLight?Color.FromArgb("#DFE4EA"):Color.FromArgb("#293645") },
                new Setter { Property=Button.TextColorProperty, Value=IsLight?Color.FromArgb("#4A5664"):Color.FromArgb("#B4BFCA") },
                new Setter { Property=Button.BorderColorProperty, Value=IsLight?Color.FromArgb("#A0ADB9"):Color.FromArgb("#536273") } } },
            new VisualState { Name="Pressed", Setters={new Setter { Property=VisualElement.OpacityProperty, Value=.8 }} }
        } }
    };

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
            new Setter { Property = Button.BorderColorProperty, Value = ControlEdge },
            new Setter { Property = Button.BorderWidthProperty, Value = 1.0 },
            new Setter { Property = VisualStateManager.VisualStateGroupsProperty, Value = ButtonStates() },
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
