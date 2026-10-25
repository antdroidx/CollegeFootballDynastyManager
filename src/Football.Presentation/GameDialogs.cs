using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

/// <summary>One accessible, theme-aware presentation for choices and confirmations throughout the app.</summary>
public static class GameDialogs
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private sealed class DialogPage : ContentPage
    {
        public Func<Task>? Cancel { get; set; }
        protected override bool OnBackButtonPressed() { if (Cancel is not null) _ = Cancel(); return true; }
    }

    public static async Task AlertAsync(Page owner, string title, string message, string close) =>
        _ = await PresentAsync(owner, title, message, close, null, null, -1);

    public static async Task<bool> AlertAsync(Page owner, string title, string message, string accept, string cancel) =>
        await PresentAsync(owner, title, message, cancel, accept, null, -1) == 1;

    public static async Task<string?> ActionSheetAsync(Page owner, string title, string? cancel, string? destructive, params string[] choices)
    {
        var all = destructive is null ? choices : choices.Prepend(destructive).ToArray();
        var result = await PresentAsync(owner, title, null, cancel ?? "Close", null, all, -1, destructive is not null);
        return result >= 0 && result < all.Length ? all[result] : cancel;
    }

    public static Task<int> ChooseIndexAsync(Page owner, string title, IReadOnlyList<string> choices, int selected) =>
        PresentAsync(owner, title, null, "Cancel", null, choices, selected);

    private static async Task<int> PresentAsync(Page owner, string title, string? message, string cancel, string? accept,
        IReadOnlyList<string>? choices, int selected, bool destructiveFirst = false)
    {
        await Gate.WaitAsync();
        try
        {
            var nav = owner.Navigation;
            var page = new DialogPage { BackgroundColor = Colors.Black.WithAlpha(.55f), AutomationId = "game-dialog" };
            var finished = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var closing = false;
            async Task Close(int value)
            {
                if (closing) return;
                closing = true;
                try { if (nav.ModalStack.LastOrDefault() == page) await nav.PopModalAsync(false); finished.TrySetResult(value); }
                catch (Exception error) { finished.TrySetException(error); }
            }
            page.Cancel = () => Close(-1);
            var titleLabel = new Label { Text = title, FontSize = 20, FontAttributes = FontAttributes.Bold,
                TextColor = DynastyTheme.Text, LineBreakMode = LineBreakMode.WordWrap, AutomationId = "game-dialog-title" };
            SemanticProperties.SetHeadingLevel(titleLabel, SemanticHeadingLevel.Level1);
            var body = new VerticalStackLayout { Spacing = 6 };
            if (!string.IsNullOrWhiteSpace(message)) body.Children.Add(new Label { Text = message, FontSize = 14,
                LineBreakMode = LineBreakMode.WordWrap, TextColor = DynastyTheme.Text });
            void RenderChoices(string query)
            {
                if (choices is null) return;
                body.Children.Clear();
                for (var index = 0; index < choices.Count; index++)
                {
                    if (!choices[index].Contains(query, StringComparison.OrdinalIgnoreCase)) continue;
                    var value = index;
                    var button = new Button { Text = choices[index], FontSize = 14, MinimumHeightRequest = 48, HeightRequest = 48,
                        HorizontalOptions = LayoutOptions.Fill, Padding = new Thickness(14, 10),
                        BackgroundColor = index == selected ? DynastyTheme.Selected : DynastyTheme.Panel,
                        TextColor = destructiveFirst && index == 0 ? DynastyTheme.Palette("#CE344C") : DynastyTheme.Text,
                        LineBreakMode = LineBreakMode.WordWrap, CornerRadius = 8, BorderWidth = index == selected ? 1 : 0, BorderColor = DynastyTheme.Accent };
                    SemanticProperties.SetDescription(button, choices[index] + (index == selected ? ", selected" : ""));
                    button.Clicked += async (_, _) => await Close(value);
                    body.Children.Add(button);
                }
                if (body.Children.Count == 0) body.Children.Add(new Label { Text = "No matching options", TextColor = DynastyTheme.Muted, Margin = new Thickness(8) });
            }
            if (choices is not null) RenderChoices("");
            var search = new SearchBar { Placeholder = "Find an option", TextColor = DynastyTheme.Text, PlaceholderColor = DynastyTheme.Muted,
                BackgroundColor = DynastyTheme.Panel, IsVisible = choices?.Count > 12, AutomationId = "game-dialog-search" };
            search.TextChanged += (_, e) => RenderChoices(e.NewTextValue ?? "");
            var dismiss = new Button { Text = cancel, MinimumHeightRequest = 44, CornerRadius = 8, FontSize = 14,
                BackgroundColor = DynastyTheme.Panel, TextColor = DynastyTheme.Text, HorizontalOptions = LayoutOptions.Fill, LineBreakMode = LineBreakMode.WordWrap };
            dismiss.Clicked += async (_, _) => await Close(-1);
            var actions = new Grid { ColumnSpacing = 8, ColumnDefinitions = new() { new(GridLength.Star) } };
            actions.Add(dismiss, 0, 0);
            if (accept is not null)
            {
                actions.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                var confirm = new Button { Text = accept, MinimumHeightRequest = 44, CornerRadius = 8, FontSize = 14,
                    BackgroundColor = DynastyTheme.Accent, TextColor = DynastyTheme.Background, HorizontalOptions = LayoutOptions.Fill, LineBreakMode = LineBreakMode.WordWrap };
                confirm.Clicked += async (_, _) => await Close(1);
                actions.Add(confirm, 1, 0);
            }
            var scroll = new ScrollView { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Default };
            var content = new Grid { Padding = 18, RowSpacing = 14, RowDefinitions = new() { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } };
            content.Add(titleLabel, 0, 0); content.Add(search, 0, 1); content.Add(scroll, 0, 2); content.Add(actions, 0, 3);
            var surface = new Border { Content = content, Padding = 0, BackgroundColor = DynastyTheme.Surface, Stroke = DynastyTheme.Panel,
                StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 16 }, HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center, MaximumWidthRequest = 560, AutomationId = "game-dialog-surface" };
            var root = new Grid { Padding = 16 }; root.Add(surface); page.Content = root;
            root.SizeChanged += (_, _) =>
            {
                if (root.Width <= 0 || root.Height <= 0) return;
                surface.WidthRequest = Math.Min(560, Math.Max(240, root.Width - 32));
                var available = Math.Max(120, root.Height - 32);
                // Keep actions reachable while long information or choice lists scroll inside the surface.
                var headingLines = Math.Max(1, (int)Math.Ceiling(title.Length / Math.Max(18, (surface.WidthRequest - 36) / 12)));
                var chrome = 170 + (headingLines - 1) * 26 + (search.IsVisible ? 56 : 0);
                surface.HeightRequest = Math.Min(available, chrome + (choices is not null ? choices.Count * 54 : Math.Max(60, (message?.Length ?? 0) / 38 * 22)));
            };
            page.Disappearing += (_, _) => page.Dispatcher.Dispatch(() =>
            {
                if (!closing && !nav.ModalStack.Contains(page)) finished.TrySetResult(-1);
            });
            await nav.PushModalAsync(page, false);
            return await finished.Task;
        }
        finally { Gate.Release(); }
    }
}
