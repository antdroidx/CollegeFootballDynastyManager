using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

// A field overlay, rather than another navigation screen: selection never advances a snap.
internal sealed record GameDayCallChoice(string Id, string Name, string Formation, string Type, IDrawable Art);

internal sealed class GameDayViewerPage : ContentPage
{
    public Func<bool>? BackHandler { get; set; }
    protected override bool OnBackButtonPressed() => BackHandler?.Invoke() == true || base.OnBackButtonPressed();
}

internal sealed class GameDayCallPicker : Border
{
    public GameDayCallPicker(string title, IReadOnlyList<GameDayCallChoice> choices, string selectedId,
        Action<string> select, Action close)
    {
        AutomationId = "game-day-call-picker";
        StrokeThickness = 1;
        Stroke = Color.FromArgb("#C9A85C");
        StrokeShape = new RoundRectangle { CornerRadius = 12 };
        BackgroundColor = Color.FromArgb("#F5101F2C");
        Padding = 10;
        MaximumWidthRequest = 980;
        HorizontalOptions = LayoutOptions.Fill;
        var formation = new Picker { Title = "Formation", FontSize = 12, TextColor = Colors.White };
        formation.Items.Add("All formations");
        foreach (var name in choices.Select(p => p.Formation).Distinct()) formation.Items.Add(name);
        formation.SelectedIndex = 0;
        var cancel = new Button { Text = "Close playbook", FontSize = 11, Padding = new Thickness(8, 3), MinimumHeightRequest = 32 };
        cancel.Clicked += (_, _) => close();
        var header = new Grid { ColumnSpacing = 10, ColumnDefinitions =
            new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star), new(GridLength.Auto) } };
        header.Add(new Label { Text = title, FontSize = 17, FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White, VerticalTextAlignment = TextAlignment.Center }, 0, 0);
        header.Add(formation, 1, 0); header.Add(cancel, 2, 0);
        var cards = new Grid { RowSpacing = 6, ColumnSpacing = 6 };
        var columns = 3;
        void Populate()
        {
            cards.Children.Clear(); cards.RowDefinitions.Clear(); cards.ColumnDefinitions.Clear();
            for (var c = 0; c < columns; c++) cards.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            var filtered = choices.Where(p => formation.SelectedIndex <= 0 || p.Formation == formation.SelectedItem?.ToString()).ToArray();
            for (var i = 0; i < filtered.Length; i++)
            {
                if (i % columns == 0) cards.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                var play = filtered[i];
                var card = Card(play.Name, play.Formation, play.Type, play.Art, () => select(play.Id), true);
                card.Stroke = Color.FromArgb("#E8C979"); card.StrokeThickness = play.Id == selectedId ? 2 : 0;
                cards.Add(card, i % columns, i / columns);
            }
        }
        formation.SelectedIndexChanged += (_, _) => Populate();
        SizeChanged += (_, _) => { var count = Width < 560 ? 2 : 3; if (count != columns) { columns = count; Populate(); } };
        Populate();
        var body = new Grid { RowSpacing = 6, RowDefinitions =
            new RowDefinitionCollection { new(GridLength.Auto), new(GridLength.Star) } };
        body.Add(header, 0, 0); body.Add(new ScrollView { Content = cards }, 0, 1);
        Content = body;
    }

    internal static Border Card(string name, string formation, string type, IDrawable? art, Action select, bool compact)
    {
        var card = new Border { Padding = 6, StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 7 }, BackgroundColor = Color.FromArgb("#213442"),
            Content = new VerticalStackLayout { Spacing = 2, Children =
            {
                new GraphicsView { Drawable = art, IsVisible = art is not null, HeightRequest = compact ? 42 : 100,
                    BackgroundColor = Color.FromArgb("#102A24"), InputTransparent = true },
                new Label { Text = name, FontSize = 12, FontAttributes = FontAttributes.Bold, MaxLines = 1, TextColor = Colors.White },
                new Label { Text = $"{type} · {formation}", FontSize = 9, MaxLines = 1, TextColor = Color.FromArgb("#E8C979") }
            } } };
        SemanticProperties.SetDescription(card, $"Select {name}, {type}, {formation}");
        var tap = new TapGestureRecognizer(); tap.Tapped += (_, _) => select(); card.GestureRecognizers.Add(tap);
        return card;
    }
}
