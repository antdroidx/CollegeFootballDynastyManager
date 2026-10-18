using DynastyManager.Data.Branding;
using DynastyManager.Data.Presentation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private static readonly BindableProperty TableValueProperty = BindableProperty.CreateAttached("TableValue", typeof(string), typeof(FoundationPage), null);
    private readonly Dictionary<string, (int Column, bool Descending)> _tableOrders = new();

    private static string? TableValue(View view) => view.GetValue(TableValueProperty) as string ?? view switch
    {
        Label label => label.Text,
        Button button => button.Text,
        Border { Content: View content } => TableValue(content),
        _ => null
    };

    private static Color BrandInk(Color color)
    {
        static double Linear(double value) => value <= .04045 ? value / 12.92 : Math.Pow((value + .055) / 1.055, 2.4);
        var luminance = .2126 * Linear(color.Red) + .7152 * Linear(color.Green) + .0722 * Linear(color.Blue);
        return luminance > .179 ? Colors.Black : Colors.White;
    }

    private static Label TableText(string text, bool bold = false, Color? color = null) => new()
    {
        Text = text, FontSize = 12, FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
        TextColor = color ?? Color.FromArgb("#E6EBF1"), VerticalOptions = LayoutOptions.Center,
        LineBreakMode = LineBreakMode.TailTruncation, Margin = new Thickness(8, 6)
    };

    private View SchoolCell(string name)
    {
        var brand = TeamBrandCatalog.Get(name);
        var primary = Color.FromArgb(brand.Primary);
        var abbreviation = _teamsByName.TryGetValue(name, out var team) ? team.Abbreviation : name[..Math.Min(4, name.Length)];
        var badge = new Border { BackgroundColor = primary, Stroke = Color.FromArgb(brand.Secondary),
            Padding = new Thickness(4, 4), StrokeThickness = 1, WidthRequest = 42,
            Content = new Label { Text = abbreviation, FontSize = 9, FontAttributes = FontAttributes.Bold,
                TextColor = BrandInk(primary), HorizontalTextAlignment = TextAlignment.Center } };
        var row = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(42), new(GridLength.Star) },
            ColumnSpacing = 4, Padding = new Thickness(6, 4) };
        row.Add(badge, 0, 0); row.Add(TableText(name, name == _currentDynasty?.UserTeamName), 1, 0);
        row.SetValue(TableValueProperty, name);
        var open = new TapGestureRecognizer();
        open.Tapped += (_, _) => OpenSchool(name);
        row.GestureRecognizers.Add(open);
        SemanticProperties.SetDescription(row, "View " + name);
        return row;
    }

    private static View SchoolBanner(string name, string subtitle)
    {
        var brand = TeamBrandCatalog.Get(name);
        var primary = Color.FromArgb(brand.Primary);
        var ink = BrandInk(primary);
        return new Border { BackgroundColor = primary, Stroke = Color.FromArgb(brand.Secondary),
            Padding = 0, Content = new VerticalStackLayout { Spacing = 0, Children =
            {
                new VerticalStackLayout { Padding = new Thickness(14, 10), Spacing = 3, Children =
                {
                    new Label { Text = name, FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = ink },
                    new Label { Text = subtitle, FontSize = 12, TextColor = ink }
                } },
                new BoxView { Color = Color.FromArgb(brand.Secondary), HeightRequest = 3 }
            } } };
    }

    private static View RatingCell(int rating)
    {
        var color = Color.FromArgb(rating >= 90 ? "#F4CC66" : rating >= 80 ? "#74D6AA" : rating >= 70 ? "#8EBEEB" : "#B6BFCD");
        return new Border { BackgroundColor = color.WithAlpha(.13f), StrokeThickness = 0,
            HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, Padding = new Thickness(7, 4),
            Content = new Label { Text = rating.ToString(), FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = color } };
    }

    private View DataTable((string Title, double Width)[] columns, IEnumerable<View[]> rows,
        Action<int, bool>? sortRequested = null, int initialSortColumn = -1, bool initialDescending = false)
    {
        var materialized = rows.ToArray();
        var key = string.Join("/", columns.Select(column => column.Title));
        var order = sortRequested is null && _tableOrders.TryGetValue(key, out var saved)
            ? saved : (Column: initialSortColumn, Descending: initialDescending);
        var grid = new Grid { RowSpacing = 0, ColumnSpacing = 0,
            WidthRequest = columns.Sum(column => column.Width), BackgroundColor = Colors.Transparent };
        foreach (var column in columns) grid.ColumnDefinitions.Add(new ColumnDefinition(column.Width));
        Button[] headers = [];
        headers = columns.Select((column, index) =>
        {
            var button = new Button { Text = column.Title, FontSize = 10, FontAttributes = FontAttributes.Bold,
                BackgroundColor = Colors.Transparent, TextColor = Color.FromArgb("#B8D8FA"),
                HorizontalOptions = LayoutOptions.Fill, VerticalOptions = LayoutOptions.Fill,
                Padding = new Thickness(5, 6), CornerRadius = 0, MinimumHeightRequest = 34,
                IsEnabled = column.Title is not ("Details" or "Stats" or "Profile") };
            SemanticProperties.SetDescription(button, "Sort " + column.Title);
            button.Clicked += (_, _) =>
            {
                order = (index, order.Column == index && !order.Descending);
                if (sortRequested is not null) { sortRequested(index, order.Descending); return; }
                _tableOrders[key] = order;
                Render();
            };
            return button;
        }).ToArray();
        void Render()
        {
            grid.Children.Clear(); grid.RowDefinitions.Clear();
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var heading = new BoxView { Color = Color.FromArgb("#253E59") };
            grid.Add(heading, 0, 0); Grid.SetColumnSpan(heading, columns.Length);
            for (var column = 0; column < headers.Length; column++)
            {
                headers[column].Text = columns[column].Title + (order.Column == column ? order.Descending ? " ↓" : " ↑" : "");
                headers[column].BackgroundColor = order.Column == column ? Color.FromArgb("#31597D") : Colors.Transparent;
                grid.Add(headers[column], column, 0);
            }
            var ordered = order.Column >= 0 && sortRequested is null
                ? TableOrdering.Sort(materialized, cells => TableValue(cells[order.Column]), order.Descending) : materialized;
            var index = 1;
            foreach (var cells in ordered)
            {
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                var user = cells.Any(cell => TableValue(cell) == _currentDynasty?.UserTeamName);
                var background = new BoxView { Color = Color.FromArgb(user ? "#203D50" : index % 2 == 0 ? "#1E2936" : "#17212D"), MinimumHeightRequest = 42 };
                grid.Add(background, 0, index); Grid.SetColumnSpan(background, columns.Length);
                for (var column = 0; column < cells.Length; column++) grid.Add(cells[column], column, index);
                index++;
            }
        }
        Render();
        return new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Always, Content = grid };
    }

    private static View SectionTitle(string title, string description) => new VerticalStackLayout { Spacing = 3, Children =
    {
        new Label { Text = title, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#A9CEF3") },
        new Label { Text = description, FontSize = 11, TextColor = DynastyTheme.Muted }
    } };

    private static View ResponsiveTableHeader(View summary, View controls)
    {
        var grid = new Grid { RowSpacing = 10,
            RowDefinitions = new RowDefinitionCollection { new(GridLength.Auto), new(GridLength.Auto) },
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(0) } };
        grid.Add(summary, 0, 0); grid.Add(controls, 0, 1);
        bool? previous = null;
        grid.SizeChanged += (_, _) =>
        {
            var wide = grid.Width >= 650;
            if (previous == wide) return;
            previous = wide;
            grid.ColumnDefinitions[1].Width = wide ? GridLength.Star : new GridLength(0);
            grid.ColumnSpacing = wide ? 14 : 0;
            grid.RowSpacing = wide ? 0 : 10;
            Grid.SetColumn(controls, wide ? 1 : 0);
            Grid.SetRow(controls, wide ? 0 : 1);
        };
        return grid;
    }
}
