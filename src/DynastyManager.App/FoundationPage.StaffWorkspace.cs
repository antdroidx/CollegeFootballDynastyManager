using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private async Task<string?> ShowStaffMarketListAsync(Team team, StaffRole role,
        IReadOnlyList<StaffMarketCandidate> candidates, string[] choices)
    {
        var completion = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        string? selection = null;
        var page = new ContentPage { AutomationId = "staff-market" };
        var close = new Button { Text = "Cancel", HorizontalOptions = LayoutOptions.Start };
        close.Clicked += async (_, _) => await Navigation.PopModalAsync();
        var search = new SearchBar { Placeholder = "Find a coach", FontSize = 12 };
        var results = new ContentView();
        void Render()
        {
            results.Content = DataTable([("Coach", 170), ("OVR", 50), ("Fit", 50), ("Age", 45), ("Style", 130), ("Source", 160), ("Action", 95)],
                candidates.Select((candidate, index) => (candidate, index))
                    .Where(item => string.IsNullOrWhiteSpace(search.Text) || item.candidate.Profile.FullName.Contains(search.Text.Trim(), StringComparison.OrdinalIgnoreCase))
                    .Select(item => {
                        var candidate = item.candidate;
                        var choose = new Button { Text = "Review hire", FontSize = 11, MinimumHeightRequest = 44 };
                        choose.Clicked += async (_, _) => {
                            selection = choices[item.index];
                            await Navigation.PopModalAsync();
                        };
                        return new View[] { CoachLink(candidate.Profile), RatingCell(StaffMarketService.GetProjectedOverall(candidate)),
                            TableText(_currentDynasty is null ? "—" : StaffMarketService.GetCandidateFitScore(_currentDynasty, team, candidate).ToString()),
                            TableText(candidate.Profile.Age.ToString()), TableText(candidate.StyleLabel),
                            TableText(candidate.SourceTeamName ?? "Free agent"), choose };
                    }));
        }
        search.TextChanged += (_, _) => Render();
        Render();
        page.Content = new Grid { Padding = 14, RowSpacing = 10,
            RowDefinitions = new() { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star) } };
        var layout = (Grid)page.Content;
        layout.Add(close, 0, 0);
        layout.Add(SectionTitle(FormatStaffRole(role) + " candidates", "Compare ratings and fit, open a coach profile, then review a hire. Hiring still requires confirmation."), 0, 1);
        layout.Add(search, 0, 2); layout.Add(new ScrollView { Content = results }, 0, 3);
        page.Disappearing += (_, _) => Dispatcher.Dispatch(() => {
            if (!Navigation.ModalStack.Contains(page)) completion.TrySetResult(selection);
        });
        await Navigation.PushModalAsync(page);
        return await completion.Task;
    }
}
