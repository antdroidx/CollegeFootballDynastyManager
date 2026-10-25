namespace DynastyManager.App;

/// <summary>The college responsive attribute tiles, shared by both applications.</summary>
public static class PlayerAttributeViews
{
    public static View Group(string title, string subtitle, (string Name, int Rating)[] attributes, Func<string, string, View> SectionTitle)
    {
        var tiles = new Grid { ColumnSpacing = 8, RowSpacing = 8 };
        int previousColumns = 0;
        void Arrange(int columns)
        {
            if (columns == previousColumns) return;
            previousColumns = columns;
            tiles.Children.Clear(); tiles.ColumnDefinitions.Clear(); tiles.RowDefinitions.Clear();
            for (var i = 0; i < columns; i++) tiles.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            for (var i = 0; i < attributes.Length; i++)
            {
                if (i % columns == 0) tiles.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                var attribute = attributes[i];
                var row = new Grid { ColumnSpacing = 6,
                    ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) } };
                row.Add(new Label { Text = attribute.Name, FontSize = 12, VerticalTextAlignment = TextAlignment.Center }, 0, 0);
                row.Add(new Label { Text=attribute.Rating.ToString(), FontSize=12, FontAttributes=FontAttributes.Bold,
                    TextColor=DynastyTheme.IsLight ? Color.FromArgb("#166841") : Color.FromArgb("#70D4AD"), VerticalTextAlignment=TextAlignment.Center },1,0);
                tiles.Add(new VerticalStackLayout { Spacing=5, Padding=new Thickness(4,5), Children={row,
                    new ProgressBar { Progress=Math.Clamp(attribute.Rating/99.0,0,1), HeightRequest=4,
                        ProgressColor=attribute.Rating>=85 ? Color.FromArgb("#38B888") : attribute.Rating>=70 ? Color.FromArgb("#599BDB") : Color.FromArgb("#B49C61") } } },i%columns,i/columns);
            }
        }
        Arrange(2);
        tiles.SizeChanged += (_, _) => Arrange(tiles.Width >= 780 ? 4 : tiles.Width >= 540 ? 3 : tiles.Width < 300 ? 1 : 2);
        return new VerticalStackLayout { Spacing = 8, Children = { SectionTitle(title, subtitle), tiles } };
    }

}
