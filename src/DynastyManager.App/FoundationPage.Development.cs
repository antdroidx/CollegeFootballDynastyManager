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
        var talk = new Button { AutomationId = "season-coach-talk", MinimumHeightRequest = 44 };
        var talkStatus = new Label { FontSize = 12, TextColor = DynastyTheme.Muted, AutomationId = "season-coach-talk-status" };
        var openingTalk = false;
        void UpdateTalk()
        {
            var current = _currentDynasty ?? state;
            var used = current.CoachTalks.Any(t => t.TeamName == current.UserTeamName && t.SeasonYear == current.SeasonYear && t.Week == current.Week);
            talk.IsEnabled = !used && !openingTalk;
            var scope = current.UserCoachRole == DynastyManager.Core.Models.StaffRole.HeadCoach ? "Team" : UserCoachControlService.ControlsOffense(current) ? "Offense" : "Defense";
            talk.Text = openingTalk ? "Opening talk…" : used ? scope + " talk · used this week" : scope + " talk · morale";
            talk.BackgroundColor = used || openingTalk ? DynastyTheme.Panel : DynastyTheme.Accent;
            talk.TextColor = used || openingTalk ? DynastyTheme.Muted : DynastyTheme.Background;
            talkStatus.Text = used ? "Available after the next weekly advance. Current talk effects fade over three weeks."
                : "One team talk per week · reactions can be positive, neutral, or negative.";
            SemanticProperties.SetDescription(talk, talk.Text + ". " + talkStatus.Text);
        }
        talk.Clicked += async (_, _) =>
        {
            if (openingTalk || !talk.IsEnabled) return;
            openingTalk = true; UpdateTalk();
            try { await ShowSeasonCoachTalkAsync(); }
            finally { openingTalk = false; UpdateTalk(); }
        };
        UpdateTalk();
        panel.Children.Add(new ActionRow { Children = { talk, injuries } });
        panel.Children.Add(talkStatus);
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
