using System.Globalization;
using System.Text.RegularExpressions;
namespace DynastyManager.App;

/// <summary>Shared compact sortable grid. Names remain interactive and numerical columns sort numerically.</summary>
public static class FootballDataGrid
{
    public static string Value(View view) => view switch
    {
        Label label => label.Text ?? "", Button button => button.Text ?? "",
        Border { Content: View child } => Value(child),
        Layout layout => string.Join(" ", layout.Children.OfType<View>().Select(Value).Where(v => v.Length > 0)),
        _ => ""
    };
    static int Compare(string a, string b)
    {
        static decimal? Number(string value)
        {
            value = value.Trim().TrimEnd('•').Trim().Replace("$", "").Replace(",", "").Replace("%", "");
            var match = Regex.Match(value, @"^([-+]?\d+(?:\.\d+)?)([mMkK]?)$");
            return match.Success && decimal.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var n)
                ? n * (match.Groups[2].Value.ToUpperInvariant() == "M" ? 1_000_000 : match.Groups[2].Value.ToUpperInvariant() == "K" ? 1000 : 1) : null;
        }
        var x = Number(a); var y = Number(b);
        return x.HasValue && y.HasValue ? x.Value.CompareTo(y.Value) : StringComparer.OrdinalIgnoreCase.Compare(a,b);
    }
    public static View Create((string Name, int Width)[] columns, IEnumerable<View[]> source)
    {
        var rows = source.ToArray(); var selected = -1; var descending = false; var page = 0; const int pageSize = 50;
        var previous = new Button { Text = "Previous", MinimumHeightRequest = 44 };
        var next = new Button { Text = "Next", MinimumHeightRequest = 44 };
        var count = new Label { TextColor = DynastyTheme.Muted, VerticalTextAlignment = TextAlignment.Center };
        var paging = new HorizontalStackLayout { Spacing = 8, IsVisible = rows.Length > pageSize, Children = { previous, count, next } };
        var grid = new Grid { ColumnSpacing = 0, RowSpacing = 0, MinimumWidthRequest = columns.Sum(c => c.Width) };
        foreach (var column in columns) grid.ColumnDefinitions.Add(new(column.Width));
        void Draw()
        {
            grid.Clear(); grid.RowDefinitions.Clear(); grid.RowDefinitions.Add(new(GridLength.Auto));
            for (var c = 0; c < columns.Length; c++)
            {
                var index = c; var header = new Button { Text = columns[c].Name + (selected == c ? descending ? " ↓" : " ↑" : ""), FontSize = 11, FontAttributes = FontAttributes.Bold, CornerRadius = 0, BorderWidth = 0, Padding = new Thickness(7,4), MinimumHeightRequest = 44, HorizontalOptions = LayoutOptions.Fill, BackgroundColor = selected == c ? DynastyTheme.Selected : DynastyTheme.Panel, TextColor = DynastyTheme.Text };
                SemanticProperties.SetDescription(header, "Sort " + columns[c].Name);
                header.Clicked += (_,_) => { descending = selected == index && !descending; selected = index; page = 0; Draw(); };
                grid.Add(header,c,0);
            }
            var sorted = selected < 0 ? rows : rows.OrderBy(row => Value(row[selected]), Comparer<string>.Create((a,b) => descending ? Compare(b,a) : Compare(a,b))).ToArray();
            var visible = sorted.Skip(page * pageSize).Take(pageSize).ToArray();
            previous.IsEnabled = page > 0; next.IsEnabled = (page + 1) * pageSize < rows.Length;
            count.Text = $"{page * pageSize + 1}–{Math.Min((page + 1) * pageSize, rows.Length)} of {rows.Length}";
            for (var r = 0; r < visible.Length; r++)
            {
                grid.RowDefinitions.Add(new(GridLength.Auto));
                var background = new BoxView { Color = r % 2 == 0 ? DynastyTheme.Surface : DynastyTheme.Background, MinimumHeightRequest = 44 };
                grid.Add(background,0,r+1); Grid.SetColumnSpan(background,columns.Length);
                for (var c = 0; c < visible[r].Length; c++)
                {
                    var cell = visible[r][c]; cell.VerticalOptions = LayoutOptions.Center; cell.Margin = new Thickness(7,3);
                    if (cell is Label label) { label.FontSize = 12; label.LineBreakMode = LineBreakMode.TailTruncation; }
                    if (cell is Button action)
                    {
                        action.BackgroundColor = DynastyTheme.Panel;
                        action.TextColor = DynastyTheme.Text;
                        action.BorderColor = DynastyTheme.ControlEdge;
                        action.BorderWidth = 1;
                        action.CornerRadius = 10;
                    }
                    grid.Add(cell,c,r+1);
                }
            }
        }
        previous.Clicked += (_,_) => { if (page > 0) { page--; Draw(); } };
        next.Clicked += (_,_) => { if ((page + 1) * pageSize < rows.Length) { page++; Draw(); } };
        Draw(); var scroll = new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = grid };
        var expand = Array.FindIndex(columns, c => c.Name is "Player" or "Team" or "Opponent"); if (expand < 0) expand = 0;
        scroll.SizeChanged += (_,_) =>
        {
            var extra = Math.Max(0, scroll.Width - columns.Sum(c => c.Width));
            grid.ColumnDefinitions[expand].Width = columns[expand].Width + extra;
        };
        return new VerticalStackLayout { Spacing = 6, Children = { paging, scroll } };
    }
}
