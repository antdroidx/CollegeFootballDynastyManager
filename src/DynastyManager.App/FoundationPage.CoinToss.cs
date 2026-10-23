using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private async Task ShowCoinTossAsync(GameDaySession session, string controlledTeam, bool interactive)
    {
        var ready = new TaskCompletionSource();
        var page = new GameDayViewerPage { BackgroundColor = DynastyTheme.Background, BackHandler = () => true };
        var title = new Label { Text = "COIN TOSS", FontFamily = "monospace", FontSize = 26, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center };
        var coinText = new Label { Text = "★", FontSize = 42, TextColor = Color.FromArgb("#332500"), HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center };
        var coin = new Border { BackgroundColor = Color.FromArgb("#E8C979"), Stroke = Color.FromArgb("#94712D"), StrokeThickness = 3,
            StrokeShape = new RoundRectangle { CornerRadius = 60 }, WidthRequest = 120, HeightRequest = 120, Padding = 2,
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
        page.Content = new ScrollView { Content = new VerticalStackLayout { Spacing = 18, Padding = 24, VerticalOptions = LayoutOptions.Center,
            Children = { title, new Label { Text = $"{session.Game.AwayTeamName} at {session.Game.HomeTeamName}", HorizontalTextAlignment = TextAlignment.Center }, coin, result, calls, choices, start } } };
        await Navigation.PushModalAsync(page);
        if (!interactive || controlledTeam != session.Game.AwayTeamName) await Flip(true);
        await ready.Task;
    }
}
