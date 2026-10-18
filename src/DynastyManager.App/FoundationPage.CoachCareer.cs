using System.Text;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Microsoft.Maui.Controls;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private Button CreateCoachCareerButton(StaffMember coach)
    {
        var button = new Button { Text = "Coach profile", FontSize = 12 };
        button.Clicked += async (_, _) =>
        {
            if (_currentDynasty is not null)
                await ShowCoachProfileAsync(coach);
        };
        return button;
    }

    private static async Task ShowCoachCareerAsync(DynastyState snapshot, Guid staffId, string name, Page presenter)
    {
        var summary = CoachCareerService.GetSummary(snapshot, staffId, name);
        var text = new StringBuilder();
        text.AppendLine($"Recorded head-coaching seasons: {summary.HeadCoachSeasons}");
        text.AppendLine($"Head coach: {summary.HeadCoachWins}-{summary.HeadCoachLosses} ({summary.HeadCoachWinPercentage:P1})");
        text.AppendLine($"HC honors / Conference titles {summary.ConferenceTitles} / CFP appearances {summary.PlayoffAppearances} / National titles {summary.NationalTitles}");
        text.AppendLine($"Bowl record: {summary.BowlWins}-{summary.BowlLosses}");
        text.AppendLine($"Other staff seasons: {summary.StaffSeasons}");
        var active = snapshot.UserHeadCoach?.StaffId == staffId ? snapshot.UserHeadCoach
            : snapshot.Staff.FirstOrDefault(coach => coach.StaffId == staffId);
        if (active is not null && CoachCareerService.GetCurrentRecord(snapshot, active) is { } current)
            text.AppendLine($"\n{snapshot.SeasonYear} in progress / {active.TeamName}: team record {current.Wins}-{current.Losses} (not included above)");
        text.AppendLine("\nSEASON HISTORY");
        if (summary.Seasons.Count == 0) text.AppendLine("No completed seasons recorded yet.");
        foreach (var season in summary.Seasons.OrderByDescending(item => item.SeasonYear))
        {
            var honors = new List<string>();
            if (season.NationalChampion) honors.Add("National champion");
            if (season.ConferenceChampion) honors.Add("Conference champion");
            if (season.PlayoffParticipant) honors.Add("CFP");
            if (season.WonBowl is bool won) honors.Add($"{season.BowlName ?? "Bowl"}: {(won ? "W" : "L")}");
            text.AppendLine($"{season.SeasonYear} / {season.TeamName} / {FormatStaffRole(season.Role)}");
            text.AppendLine($"{(season.Role == StaffRole.HeadCoach ? "Record" : "Team record on staff")} {season.Wins}-{season.Losses}" +
                (season.ConferenceWins + season.ConferenceLosses > 0 ? $" / Conf {season.ConferenceWins}-{season.ConferenceLosses}" : "") +
                (season.FinalRanking > 0 ? $" / Final rank #{season.FinalRanking}" : ""));
            if (honors.Count > 0) text.AppendLine(string.Join(" / ", honors));
            text.AppendLine();
        }
        text.Append("Records cover saved dynasty seasons. Staff-season team results are separate from head-coaching totals. Older CPU coaching history is unavailable before tracking began.");
        await presenter.DisplayAlertAsync(summary.CoachName, text.ToString(), "Close");
    }

    private View CreateCoachArchiveShortcut()
    {
        var button = new Button { Text = "Coach career archive" };
        button.Clicked += async (_, _) => await OpenCoachArchiveAsync();
        return button;
    }

    private async Task OpenCoachArchiveAsync()
    {
        if (_currentDynasty is null) return;
        var snapshot = CoachCareerService.RestoreUserHistory(_currentDynasty);
        var summaries = snapshot.CoachSeasonHistory.Select(item => item.StaffId).Distinct()
            .Select(id => CoachCareerService.GetSummary(snapshot, id)).OrderByDescending(item => item.HeadCoachWins)
            .ThenBy(item => item.CoachName).ToArray();
        var page = new ContentPage { Title = "Coach career archive", Padding = 16 };
        var search = new SearchBar { Placeholder = "Search coach or school", TextColor = Microsoft.Maui.Graphics.Colors.White,
            PlaceholderColor = DynastyTheme.Muted };
        var filter = new Picker { ItemsSource = new[] { "Head coaches", "All staff" }, SelectedIndex = 0 };
        var count = new Label { TextColor = DynastyTheme.Muted };
        var list = new CollectionView { SelectionMode = SelectionMode.None,
            EmptyView = new Label { Text = "No recorded careers match. Complete a season to begin the archive." } };
        list.ItemTemplate = new DataTemplate(() =>
        {
            var name = new Label { FontAttributes = FontAttributes.Bold };
            name.SetBinding(Label.TextProperty, nameof(CoachCareerSummary.CoachName));
            var record = new Label { FontSize = 12, TextColor = DynastyTheme.Muted };
            record.BindingContextChanged += (_, _) =>
            {
                if (record.BindingContext is CoachCareerSummary career)
                    record.Text = $"HC {career.HeadCoachWins}-{career.HeadCoachLosses} / {career.HeadCoachSeasons} HC seasons / {career.StaffSeasons} other staff seasons";
            };
            var detail = new Button { Text = "View career", FontSize = 12 };
            detail.Clicked += async (_, _) =>
            {
                if (detail.BindingContext is CoachCareerSummary career)
                    await ShowCoachCareerAsync(snapshot, career.StaffId, career.CoachName, page);
            };
            return new Border { Margin = new Thickness(0, 0, 0, 8), Content = new VerticalStackLayout
                { Spacing = 6, Children = { name, record, detail } } };
        });
        void Refresh()
        {
            var query = search.Text?.Trim() ?? "";
            var rows = summaries.Where(item => (filter.SelectedIndex == 1 || item.HeadCoachSeasons > 0) &&
                (query.Length == 0 || item.CoachName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    item.Seasons.Any(season => season.TeamName.Contains(query, StringComparison.OrdinalIgnoreCase)))).ToArray();
            list.ItemsSource = rows;
            count.Text = $"{rows.Length} recorded careers / completed seasons";
        }
        search.TextChanged += (_, _) => Refresh();
        filter.SelectedIndexChanged += (_, _) => Refresh();
        var close = new Button { Text = "Back to dynasty" };
        close.Clicked += async (_, _) => await Navigation.PopModalAsync();
        var layout = new Grid { RowSpacing = 10, RowDefinitions = new RowDefinitionCollection
            { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } };
        layout.Add(new Label { Text = "Coach career archive", FontSize = 18, FontAttributes = FontAttributes.Bold }, 0, 0);
        layout.Add(search, 0, 1); layout.Add(filter, 0, 2); layout.Add(count, 0, 3);
        layout.Add(list, 0, 4); layout.Add(close, 0, 5);
        page.Content = layout;
        Refresh();
        await Navigation.PushModalAsync(page);
    }
}
