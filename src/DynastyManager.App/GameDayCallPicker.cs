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
        BackgroundColor = DynastyTheme.Surface;
        Padding = 10;
        MaximumWidthRequest = 980;
        HorizontalOptions = LayoutOptions.Fill;
        var formation = new Picker { Title = "Formation", FontSize = 12, TextColor = DynastyTheme.Text };
        formation.Items.Add("All formations");
        foreach (var name in choices.Select(p => p.Formation).Distinct()) formation.Items.Add(name);
        formation.SelectedIndex = 0;
        var cancel = new Button { Text = "Close playbook", FontSize = 11, Padding = new Thickness(8, 3), MinimumHeightRequest = 32 };
        cancel.Clicked += (_, _) => close();
        var header = new Grid { ColumnSpacing = 10, ColumnDefinitions =
            new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star), new(GridLength.Auto) } };
        header.Add(new Label { Text = title, FontSize = 17, FontAttributes = FontAttributes.Bold,
            TextColor = DynastyTheme.Text, VerticalTextAlignment = TextAlignment.Center }, 0, 0);
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
                var card = Card(play.Name, play.Formation, play.Type, play.Art, () => select(play.Id), false);
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

    internal static Border Card(string name, string formation, string type, IDrawable? art, Action select, bool compact, bool portrait = false)
    {
        var diagram = new GraphicsView { Drawable = art, IsVisible = art is not null,
            HeightRequest = compact ? portrait ? 58 : 68 : 100, BackgroundColor = Color.FromArgb("#102A24"), InputTransparent = true };
        var info = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center, Children =
        {
            new Label { Text = name, FontSize = 12, FontAttributes = FontAttributes.Bold, MaxLines = 2, TextColor = DynastyTheme.Text },
            new Label { Text = type, FontSize = 9, MaxLines = 2, TextColor = DynastyTheme.IsLight ? Color.FromArgb("#765609") : Color.FromArgb("#E8C979") },
            new Label { Text = formation, FontSize = 9, MaxLines = 2, TextColor = DynastyTheme.Muted }
        } };
        View content;
        if (compact && !portrait)
        {
            var row = new Grid { ColumnSpacing = 6, ColumnDefinitions =
                new ColumnDefinitionCollection { new(new GridLength(1.1, GridUnitType.Star)), new(GridLength.Star) } };
            row.Add(diagram, 0, 0); row.Add(info, 1, 0); content = row;
        }
        else content = new VerticalStackLayout { Spacing = 4, Children = { diagram, info } };
        var card = new Border { Padding = 6, StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 7 }, BackgroundColor = DynastyTheme.Palette("#213442"), Content = content };
        SemanticProperties.SetDescription(card, $"Select {name}, {type}, {formation}");
        var tap = new TapGestureRecognizer(); tap.Tapped += (_, _) => select(); card.GestureRecognizers.Add(tap);
        return card;
    }
}
