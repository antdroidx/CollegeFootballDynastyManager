using DynastyManager.Data.Branding;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
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

    private static View DataTable((string Title, double Width)[] columns, IEnumerable<View[]> rows)
    {
        var grid = new Grid { RowSpacing = 1, ColumnSpacing = 0,
            WidthRequest = columns.Sum(column => column.Width), BackgroundColor = Color.FromArgb("#2D3A4B") };
        foreach (var column in columns) grid.ColumnDefinitions.Add(new ColumnDefinition(column.Width));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        var heading = new BoxView { Color = Color.FromArgb("#253E59") };
        grid.Add(heading, 0, 0); Grid.SetColumnSpan(heading, columns.Length);
        for (var column = 0; column < columns.Length; column++)
        {
            var label = TableText(columns[column].Title.ToUpperInvariant(), true, Color.FromArgb("#B8D8FA"));
            label.FontSize = 10;
            label.Margin = new Thickness(8, 8);
            grid.Add(label, column, 0);
        }
        var index = 1;
        foreach (var cells in rows)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var background = new BoxView { Color = Color.FromArgb(index % 2 == 0 ? "#1E2936" : "#17212D"), MinimumHeightRequest = 42 };
            grid.Add(background, 0, index); Grid.SetColumnSpan(background, columns.Length);
            for (var column = 0; column < cells.Length; column++) grid.Add(cells[column], column, index);
            index++;
        }
        return new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Always,
            Content = grid };
    }

    private static View SectionTitle(string title, string description) => new VerticalStackLayout { Spacing = 3, Children =
    {
        new Label { Text = title, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#A9CEF3") },
        new Label { Text = description, FontSize = 11, TextColor = DynastyTheme.Muted }
    } };
}
