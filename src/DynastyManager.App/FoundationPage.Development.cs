using DynastyManager.Core.Simulation;
using Microsoft.Maui.Controls;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private View BuildCoachDevelopmentPanel()
    {
        var panel = new VerticalStackLayout { Spacing = 12, AutomationId = "coach-development" };
        if (_currentDynasty is not { } state) return panel;
        panel.Children.Add(SectionTitle("Player development", "Training, morale and availability · your coaching staff's impact"));
        var injuries = new Button { Text = "Injuries & availability", MinimumHeightRequest = 44 };
        injuries.Clicked += (_, _) => ShowSection(AppSection.Injuries);
        var talk = new Button { Text = "Team talk · morale", AutomationId = "season-coach-talk", MinimumHeightRequest = 44,
            IsEnabled = !state.CoachTalks.Any(t => t.TeamName == state.UserTeamName && t.SeasonYear == state.SeasonYear && t.Week == state.Week) };
        talk.Clicked += async (_, _) => await ShowSeasonCoachTalkAsync();
        panel.Children.Add(new Microsoft.Maui.Controls.FlexLayout { Direction = Microsoft.Maui.Layouts.FlexDirection.Row,
            Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap, Children = { talk, injuries } });
        var latest = GetLatestUserDevelopmentRecords();
        panel.Children.Add(new Label { Text = latest.Count > 0
            ? $"Latest training · {FormatSigned(latest.Average(r => r.OverallChange))} OVR average"
            : "Training changes will appear after the next development period.", TextColor = DynastyTheme.Muted, FontSize = 13 });
        panel.Children.Add(DataTable([("Player", 155), ("Pos", 45), ("Year", 60), ("OVR", 50), ("Δ", 50), ("Morale", 60), ("Availability", 115)],
            state.ActiveRoster.Where(p => p.TeamName == state.UserTeamName).OrderBy(p => p.Position).ThenByDescending(p => p.OverallRating).Select(player => new View[] {
                PlayerLink(player), TableText(OffensiveLineService.Label(player)), TableText(FormatClassYear(player.ClassYear, player.IsRedshirted || player.HasRedshirted)),
                RatingCell(player.OverallRating), TableText(FormatSigned(GameMoraleService.SeasonDelta(state, player))), MoraleIcon(player),
                TableText(player.CurrentInjury is null ? "Available" : $"Injured · {player.CurrentInjury.WeeksRemaining}w")
            })));
        return panel;
    }
}
