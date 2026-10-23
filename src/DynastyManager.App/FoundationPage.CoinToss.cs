using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Branding;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;
using Microsoft.Maui.Graphics;
namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private async Task ShowCoinTossAsync(GameDaySession session, string controlledTeam, bool interactive)
    {
        var ready = new TaskCompletionSource();
        var page = new GameDayViewerPage { BackgroundColor = DynastyTheme.Background, BackHandler = () => true };
        var title = new Label { Text = "COIN TOSS", FontFamily = "monospace", FontSize = 20, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center };
        var coinText = new Label { Text = "★", FontSize = 28, TextColor = Color.FromArgb("#332500"), HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center };
        var coin = new Border { BackgroundColor = Color.FromArgb("#E8C979"), Stroke = Color.FromArgb("#94712D"), StrokeThickness = 3,
            StrokeShape = new RoundRectangle { CornerRadius = 60 }, WidthRequest = 64, HeightRequest = 64, Padding = 2,
            HorizontalOptions = LayoutOptions.Center, Content = coinText };
        var result = new Label { Text = session.Game.AwayTeamName + " calls the toss", FontSize = 16, HorizontalTextAlignment = TextAlignment.Center };
        var heads = new Button { Text = "Heads", AutomationId = "coin-heads" };
        var tails = new Button { Text = "Tails" };
        var receive = new Button { Text = "Receive" }; var defer = new Button { Text = "Defer" };
        var calls = new HorizontalStackLayout { Spacing = 12, HorizontalOptions = LayoutOptions.Center, Children = { heads, tails } };
        var choices = new HorizontalStackLayout { Spacing = 12, IsVisible = false, HorizontalOptions = LayoutOptions.Center, Children = { receive, defer } };
        var start = new Button { Text = "Start game", IsVisible = false, HorizontalOptions = LayoutOptions.Center };
        void Confirm(CoinTossResult toss)
        {
            CoinTossService.Apply(session, toss);
            result.Text = $"{toss.Winner} wins and {(toss.Deferred ? "defers" : "receives")}.\n{toss.ReceivingTeam} receives the opening kickoff.";
            choices.IsVisible = false; start.IsVisible = true;
        }
        async Task Flip(bool callHeads)
        {
            calls.IsVisible = false;
            var toss = CoinTossService.Toss(session.Game, callHeads);
            var animationDone = new TaskCompletionSource();
            new Animation(p => { coin.ScaleX = Math.Max(.05, Math.Abs(Math.Cos(p * Math.PI * 7))); coin.Rotation = Math.Sin(p * Math.PI) * 12; })
                .Commit(coin, "retro-coin", 16, 1100, Easing.Linear, (_, _) => animationDone.TrySetResult());
            await animationDone.Task;
            coin.ScaleX = 1; coin.Rotation = 0; coinText.Text = toss.Heads ? "H" : "T";
            if (interactive && toss.Winner == controlledTeam)
            {
                result.Text = $"{toss.Winner} wins! Receive now or defer to the second half?";
                choices.IsVisible = true;
                receive.Clicked += (_, _) => Confirm(toss with { ReceivingTeam = toss.Winner, Deferred = false });
                defer.Clicked += (_, _) => Confirm(toss);
            }
            else Confirm(toss);
        }
        heads.Clicked += async (_, _) => await Flip(true); tails.Clicked += async (_, _) => await Flip(false);
        start.Clicked += async (_, _) => { start.IsEnabled = false; await Navigation.PopModalAsync(); ready.TrySetResult(); };
        var home = TeamBrandCatalog.Get(session.Game.HomeTeamName);
        var field = new Grid { AutomationId = "coin-toss-field", BackgroundColor = Color.FromArgb("#245B38") };
        field.Add(new GraphicsView { Drawable = new GameFieldDrawable { CoinCeremony = true, YardLine = 50, Distance = 0,
            HomeColor = Color.FromArgb(home.Primary), HomeSecondaryColor = Color.FromArgb(home.Secondary),
            HomeInitial = TeamBrandCatalog.Initial(session.Game.HomeTeamName), HomeName = session.Game.HomeTeamName.ToUpperInvariant() } });
        var ceremony = new AbsoluteLayout { InputTransparent = true };
        var captainPositions = new List<(FootballSpriteView View, double X, int Slot)>();
        foreach (var (name, away, x) in new[] { (session.Game.HomeTeamName, false, .42), (session.Game.AwayTeamName, true, .58) })
        {
            var brand = TeamBrandCatalog.Get(name);
            for (var i = 0; i < 2; i++)
            {
                var captain = new FootballSpriteView(Color.FromArgb(brand.Primary), Color.FromArgb(brand.Secondary), away, i, "C") { WidthRequest = 46, HeightRequest = 46 };
                captain.HuddleFacing(away ? -1 : 1, 0);
                captainPositions.Add((captain, x, i));
                AbsoluteLayout.SetLayoutFlags(captain, AbsoluteLayoutFlags.PositionProportional);
                AbsoluteLayout.SetLayoutBounds(captain, new Rect(x, .25 + i * .12, 46, 46)); ceremony.Add(captain);
            }
        }
        var official = new GraphicsView { Drawable = new FootballOfficialDrawable(), WidthRequest = 32, HeightRequest = 48 };
        AbsoluteLayout.SetLayoutFlags(official, AbsoluteLayoutFlags.PositionProportional);
        AbsoluteLayout.SetLayoutBounds(official, new Rect(.5, .30, 32, 48)); ceremony.Add(official);
        field.Add(ceremony);
        var panel = new Border { BackgroundColor = DynastyTheme.Surface, StrokeThickness = 0, Margin = 10, Padding = 12,
            MaximumWidthRequest = 650, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.End,
            Content = new VerticalStackLayout { Spacing = 8, Children = { title,
                new Label { Text = $"{session.Game.AwayTeamName} at {session.Game.HomeTeamName} · Captains at midfield", HorizontalTextAlignment = TextAlignment.Center },
                coin, result, calls, choices, start } } };
        var panelBody = panel.Content;
        panel.Content = new ScrollView { Content = panelBody };
        field.SizeChanged += (_, _) =>
        {
            if (field.Width <= 0 || field.Height <= 0) return;
            var wide = field.Width > field.Height;
            panel.HorizontalOptions = wide ? LayoutOptions.End : LayoutOptions.Center;
            panel.WidthRequest = wide ? Math.Min(350, field.Width * .44) : -1;
            panel.MaximumHeightRequest = wide ? field.Height - 16 : field.Height * .48;
            foreach (var c in captainPositions)
                AbsoluteLayout.SetLayoutBounds(c.View, new Rect(wide ? c.X * .5 + .04 : c.X, wide ? .30 + c.Slot * .22 : .25 + c.Slot * .12, 46, 46));
            AbsoluteLayout.SetLayoutBounds(official, new Rect(wide ? .29 : .5, wide ? .4 : .30, 32, 48));
        };
        field.Add(panel); page.Content = field;
        await Navigation.PushModalAsync(page);
        if (!interactive || controlledTeam != session.Game.AwayTeamName) await Flip(true);
        await ready.Task;
    }
}
