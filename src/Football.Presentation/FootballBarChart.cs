namespace DynastyManager.App;

/// <summary>Shared accessible chart: every bar retains its exact label and numeric value.</summary>
public static class FootballBarChart
{
    public static View Create(IEnumerable<(string Label, double Value)> source, string format = "0", string suffix = "")
    {
        var items = source.ToArray(); var max = Math.Max(1, items.Select(x => Math.Abs(x.Value)).DefaultIfEmpty(1).Max());
        var rows = new VerticalStackLayout { Spacing = 8 };
        if (items.Length == 0) rows.Add(new Label { Text = "No recorded data yet.", TextColor = DynastyTheme.Muted });
        foreach (var item in items)
        {
            var label = new Label { Text = item.Label + " · " + item.Value.ToString(format) + suffix, FontSize = 12, TextColor = DynastyTheme.Text };
            var track = new Grid { HeightRequest = 10, BackgroundColor = DynastyTheme.Panel };
            track.ColumnDefinitions.Add(new(new GridLength(Math.Max(.001, Math.Abs(item.Value) / max), GridUnitType.Star)));
            track.ColumnDefinitions.Add(new(new GridLength(Math.Max(.001, 1 - Math.Abs(item.Value) / max), GridUnitType.Star)));
            track.Add(new BoxView { Color = item.Value < 0 ? Colors.IndianRed : DynastyTheme.Accent });
            SemanticProperties.SetDescription(track, label.Text); rows.Add(label); rows.Add(track);
        }
        return rows;
    }
}
