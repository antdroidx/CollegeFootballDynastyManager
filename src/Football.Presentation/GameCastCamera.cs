namespace DynastyManager.App;

/// <summary>Shared college framing: apply identical zoom and bounded translation to turf and players.</summary>
public static class GameCastCamera
{
    public static void Apply(IEnumerable<View> layers, double width, double height, int mode, double x = .5, double y = .5)
    {
        if (width <= 0 || height <= 0) return;
        var zoom = mode == 0 ? 1 : mode == 1 ? 1.15 : 1.35;
        foreach (var layer in layers)
        {
            layer.Scale = zoom;
            layer.TranslationX = Math.Clamp((.5 - x) * width * zoom, -width * (zoom - 1) / 2, width * (zoom - 1) / 2);
            layer.TranslationY = Math.Clamp((.5 - y) * height * zoom, -height * (zoom - 1) / 2, height * (zoom - 1) / 2);
        }
    }
}

public static class GameCastScoreboard
{
    public static Grid Team(View badge, Label name, Label score, Label timeouts)
    {
        var grid = new Grid { Padding = new Thickness(9, 7), ColumnSpacing = 7,
            ColumnDefinitions = new() { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } };
        grid.Add(badge, 0, 0); grid.Add(new VerticalStackLayout { Spacing = 1, Children = { name, timeouts } }, 1, 0);
        grid.Add(score, 2, 0); return grid;
    }
    public static Grid Create(View away, View home)
    {
        var header = new Grid { BackgroundColor = DynastyTheme.Surface, ColumnSpacing = 1,
            ColumnDefinitions = new() { new(GridLength.Star), new(GridLength.Star) }, AutomationId = "game-day-score" };
        header.Add(away, 0, 0); header.Add(home, 1, 0); return header;
    }
}
