using DynastyManager.Core.Simulation;
using DynastyManager.Data.Branding;
using DynastyManager.Data.Presentation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private static readonly BindableProperty TableValueProperty = BindableProperty.CreateAttached("TableValue", typeof(string), typeof(FoundationPage), null);
    private readonly Dictionary<string, (int Column, bool Descending)> _tableOrders = new();

    private static bool TextColumn(string title) => title is "Player" or "Prospect" or "Coach" or "School" or "Team" or "Opponent"
        or "Role" or "Style" or "Source" or "Availability" or "Status" or "Origin" or "Competition" or "Interest / status"
        or "Honors / postseason" or "Bowl";

    private static string? TableValue(View view) => view.GetValue(TableValueProperty) as string ?? view switch
    {
        Label label => label.Text,
        Button button => button.Text,
        Border { Content: View content } => TableValue(content),
        _ => null
    };

    private static View HeightCell(int inches)
    {
        var label = TableText(inches > 0 ? $"{inches / 12}′{inches % 12}″" : "—");
        label.SetValue(TableValueProperty, inches > 0 ? inches.ToString() : null);
        return label;
    }

    private static Color BrandInk(Color color)
    {
        static double Linear(double value) => value <= .04045 ? value / 12.92 : Math.Pow((value + .055) / 1.055, 2.4);
        var luminance = .2126 * Linear(color.Red) + .7152 * Linear(color.Green) + .0722 * Linear(color.Blue);
        return luminance > .179 ? Colors.Black : Colors.White;
    }

    private static Label TableText(string text, bool bold = false, Color? color = null) => new()
    {
        Text = text, FontSize = 11, FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
        TextColor = color ?? DynastyTheme.Palette("#E6EBF1"), VerticalOptions = LayoutOptions.Center,
        LineBreakMode = LineBreakMode.TailTruncation, Margin = new Thickness(7, 4)
    };

    private View SchoolCell(string name, bool includeRank = true)
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
        row.Add(badge, 0, 0); row.Add(TableText(_currentDynasty is null || !includeRank ? name : CollegePollService.RankedName(name, BuildCurrentNationalRankings()), name == _currentDynasty?.UserTeamName), 1, 0);
        row.SetValue(TableValueProperty, name);
        var open = new TapGestureRecognizer();
        open.Tapped += (_, _) => OpenSchool(name);
        row.GestureRecognizers.Add(open);
        SemanticProperties.SetDescription(row, "View " + name);
        return row;
    }

    private View SchoolBanner(string name, string subtitle)
    {
        var brand=TeamBrandCatalog.Get(name);var primary=Color.FromArgb(brand.Primary);var ink=BrandInk(primary);
        var initial=_teamsByName.TryGetValue(name,out var team)?team.Abbreviation[..1]:name[..1];
        var identity=new Grid { ColumnSpacing=10, ColumnDefinitions=new() {new(42),new(GridLength.Star)} };
        identity.Add(new Border { WidthRequest=40,HeightRequest=44,Padding=0,BackgroundColor=primary,
            Stroke=Color.FromArgb(brand.Secondary),StrokeThickness=1,
            Content=new Label {Text=initial,FontSize=30,FontAttributes=FontAttributes.Bold,TextColor=Color.FromArgb(brand.Secondary),
                HorizontalTextAlignment=TextAlignment.Center,VerticalTextAlignment=TextAlignment.Center} },0,0);
        identity.Add(new VerticalStackLayout {Spacing=3,VerticalOptions=LayoutOptions.Center,Children={
            new Label {Text=_currentDynasty is null?name:CollegePollService.RankedName(name,BuildCurrentNationalRankings()),
                FontSize=15,FontAttributes=FontAttributes.Bold,TextColor=ink},
            new Label {Text=subtitle,FontSize=11,TextColor=ink} } },1,0);
        return new Border {BackgroundColor=primary,Padding=new Thickness(12,10),Content=identity};
    }

    private static View RatingCell(int rating)
    {
        var color = Color.FromArgb(DynastyTheme.IsLight
            ? rating >= 90 ? "#7B5B00" : rating >= 80 ? "#116540" : rating >= 70 ? "#225D9A" : "#526377"
            : rating >= 90 ? "#F4CC66" : rating >= 80 ? "#74D6AA" : rating >= 70 ? "#8EBEEB" : "#B6BFCD");
        return new Border { BackgroundColor = color.WithAlpha(.13f), StrokeThickness = 0,
            HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, Padding = new Thickness(7, 4),
            Content = new Label { Text = rating.ToString(), FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = color } };
    }

    private View DataTable((string Title, double Width)[] columns, IEnumerable<View[]> rows,
        Action<int, bool>? sortRequested = null, int initialSortColumn = -1, bool initialDescending = false, bool configureColumns = false)
    {
        var materialized = rows.ToArray();
        bool allColumns = !configureColumns || Microsoft.Maui.Storage.Preferences.Default.Get("columns-" + string.Join("/", columns.Select(c=>c.Title)), false);
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
                BackgroundColor = Colors.Transparent, TextColor = DynastyTheme.Palette("#B8D8FA"),
                HorizontalOptions = TextColumn(column.Title) ? LayoutOptions.Start : LayoutOptions.Fill, VerticalOptions = LayoutOptions.Fill,
                Padding = new Thickness(5, 6), CornerRadius = 0, MinimumHeightRequest = 34,
                IsEnabled = column.Title is not ("Details" or "Stats" or "Profile") };
            SemanticProperties.SetDescription(button, "Sort " + column.Title);
            button.Clicked += (_, _) =>
            {
                order = (index, order.Column == index && !order.Descending);
                if (sortRequested is not null) { sortRequested(index, order.Descending); return; }
                _tableOrders[key] = order;
                Render();
                // Column visibility is reapplied by the table layout after sorting.
                foreach(var cell in grid.Children.OfType<View>()) if(cell is not BoxView) cell.IsVisible=allColumns || Grid.GetColumn(cell)<7;
            };
            return button;
        }).ToArray();
        void Render()
        {
            grid.Children.Clear(); grid.RowDefinitions.Clear();
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var heading = new BoxView { Color = DynastyTheme.Panel };
            grid.Add(heading, 0, 0); Grid.SetColumnSpan(heading, columns.Length);
            for (var column = 0; column < headers.Length; column++)
            {
                headers[column].Text = columns[column].Title + (order.Column == column ? order.Descending ? " ↓" : " ↑" : "");
                headers[column].BackgroundColor = order.Column == column ? DynastyTheme.Palette("#31597D") : Colors.Transparent;
                grid.Add(headers[column], column, 0);
            }
            var ordered = order.Column >= 0 && sortRequested is null
                ? TableOrdering.Sort(materialized, cells => TableValue(cells[order.Column]), order.Descending) : materialized;
            var index = 1;
            foreach (var cells in ordered)
            {
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                var user = cells.Any(cell => TableValue(cell) == _currentDynasty?.UserTeamName);
                var background = new BoxView { Color = user ? DynastyTheme.Selected : index % 2 == 0 ? DynastyTheme.Panel : DynastyTheme.Surface, MinimumHeightRequest = 44 };
                grid.Add(background, 0, index); Grid.SetColumnSpan(background, columns.Length);
                for (var column = 0; column < cells.Length; column++)
                {
                    if (cells[column] is Label label)
                        label.HorizontalTextAlignment = TextColumn(columns[column].Title) ? TextAlignment.Start : TextAlignment.Center;
                    grid.Add(cells[column], column, index);
                }
                index++;
            }
        }
        Render();
        var scroll = new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Default,
            Content = grid
        };
        var naturalWidth = columns.Sum(column => column.Width);
        void ApplyColumns()
        {
            naturalWidth = columns.Where((c,i)=>allColumns || i<7).Sum(c=>c.Width);
            for(var i=0;i<columns.Length;i++) grid.ColumnDefinitions[i].Width = allColumns || i<7 ? columns[i].Width : 0;
            foreach(var cell in grid.Children.OfType<View>())
                if(cell is not BoxView) cell.IsVisible=allColumns || Grid.GetColumn(cell)<7;
            grid.WidthRequest=naturalWidth;
        }
        ApplyColumns();
        scroll.SizeChanged += (_, _) =>
        {
            var width = Math.Max(naturalWidth, scroll.Width);
            if (Math.Abs(grid.WidthRequest - width) < 1) return;
            grid.WidthRequest = width;
            var extra = (width - naturalWidth) / (allColumns ? columns.Length : Math.Min(7, columns.Length));
            for (var column = 0; column < columns.Length; column++)
                grid.ColumnDefinitions[column].Width = allColumns || column < 7 ? columns[column].Width + extra : 0;
        };
        if (!configureColumns) return scroll;
        var columnsToggle = new Button { Text=allColumns ? "Columns: All" : "Columns: Essential", FontSize=11,
            BackgroundColor=Colors.Transparent, TextColor=DynastyTheme.Accent };
        columnsToggle.Clicked += (_,_) => { allColumns=!allColumns;
            Microsoft.Maui.Storage.Preferences.Default.Set("columns-" + string.Join("/", columns.Select(c=>c.Title)),allColumns);
            columnsToggle.Text=allColumns ? "Columns: All" : "Columns: Essential"; ApplyColumns(); };
        return new VerticalStackLayout { Spacing=3, Children={columnsToggle,scroll} };
    }

    private static View SectionTitle(string title, string description) => new VerticalStackLayout { Spacing = 3, Children =
    {
        new Label { Text = title, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = DynastyTheme.Palette("#A9CEF3") },
        new Label { Text = description, IsVisible=!string.IsNullOrWhiteSpace(description), FontSize = 11, TextColor = DynastyTheme.Muted }
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
