using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Branding;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private static View IdentityHeader(string name, string school, string subtitle)
    {
        var brand = TeamBrandCatalog.Get(school);
        var primary = Color.FromArgb(brand.Primary);
        var initials = string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part => part[0]));
        var avatar = new Border { WidthRequest = 58, HeightRequest = 58, Padding = 0, BackgroundColor = primary,
            Stroke = Color.FromArgb(brand.Secondary), StrokeThickness = 2,
            Content = new Label { Text = initials, FontSize = 23, FontAttributes = FontAttributes.Bold,
                TextColor = BrandInk(primary), HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center } };
        SemanticProperties.SetDescription(avatar, "Initials avatar for " + name);
        var row = new Grid { ColumnSpacing = 12, ColumnDefinitions = new ColumnDefinitionCollection { new(58), new(GridLength.Star) } };
        row.Add(avatar, 0, 0);
        row.Add(new VerticalStackLayout { Spacing = 4, Children =
        {
            new Label { Text = name, FontSize = 18, FontAttributes = FontAttributes.Bold },
            new Label { Text = school + " · " + subtitle, FontSize = 12, TextColor = DynastyTheme.Muted },
            new Label { Text = "Initials avatar", FontSize = 10, TextColor = DynastyTheme.Muted }
        } }, 1, 0);
        return new Border { Content = row, BackgroundColor = Color.FromArgb("#233543") };
    }

    private Button PlayerLink(DynastyPlayer player) => PlayerLink(player.PlayerId, player.FullName);

    private Button PlayerLink(Guid id, string name)
    {
        var button = new Button { Text = name, FontSize = 12, TextColor = Color.FromArgb("#BDE5F5"),
            BackgroundColor = Colors.Transparent, HorizontalOptions = LayoutOptions.Fill, Padding = new Thickness(8, 4) };
        button.SetValue(TableValueProperty, name);
        SemanticProperties.SetDescription(button, "View player " + name);
        button.Clicked += async (_, _) => await ShowPlayerProfileAsync(id);
        return button;
    }

    private Button CoachLink(StaffMember coach)
    {
        var button = new Button { Text = coach.FullName, FontSize = 12, TextColor = Color.FromArgb("#D6C4F2"),
            BackgroundColor = Colors.Transparent, HorizontalOptions = LayoutOptions.Fill, Padding = new Thickness(8, 4) };
        button.SetValue(TableValueProperty, coach.FullName);
        SemanticProperties.SetDescription(button, "View coach " + coach.FullName);
        button.Clicked += async (_, _) => await ShowCoachProfileAsync(coach);
        return button;
    }

    private async Task ShowPlayerProfileAsync(Guid id)
    {
        if (_currentDynasty is null) return;
        var state = _currentDynasty;
        var visibleStats = VisiblePlayerStatistics();
        var active = state.ActiveRoster.FirstOrDefault(player => player.PlayerId == id);
        var career = state.PlayerCareerHistory.FirstOrDefault(player => player.PlayerId == id);
        var latest = visibleStats.Where(line => line.PlayerId == id).OrderByDescending(line => line.SeasonYear).FirstOrDefault();
        if (active is null && career is null && latest is null) return;
        var player = active ?? new DynastyPlayer
        {
            PlayerId = id, FullName = career?.PlayerName ?? latest!.PlayerName, TeamName = latest?.TeamName ?? "Recorded career",
            Position = career?.Position ?? latest!.Position, OriginalRecruitingStars = career?.OriginalRecruitingStars ?? latest?.OriginalRecruitingStars
        };
        var close = new Button { Text = "‹ Back" };
        close.Clicked += async (_, _) => await Navigation.PopModalAsync();
        var content = new VerticalStackLayout { Padding = 14, Spacing = 12, Children =
        {
            close, IdentityHeader(player.FullName, player.TeamName, $"{player.Position} · {(active is null ? "Archived player" : FormatClassYear(player.ClassYear, player.IsRedshirted || player.HasRedshirted))}"),
            new Label { Text = FormatOriginalRecruitingStars(player), TextColor = Color.FromArgb("#E8C979") }
        } };
        if (active is not null)
        {
            player = PlayerRatingService.EnsureInitialized(state, player);
            content.Children.Add(SectionTitle("Player attributes", $"OVR {player.OverallRating} · Potential {FormatPotentialStars(player.PotentialRating)} · Depth {player.DepthChartOrder}"));
            content.Children.Add(AttributeGroup("Character traits", "Decision-making and team influence",
                ("Awareness", player.AwarenessRating), ("Leadership", player.Skills!.Leadership),
                ("Discipline", player.Skills.Discipline)));
            content.Children.Add(AttributeGroup("Physical traits", "Athletic ability, endurance and availability",
                ("Speed", player.SpeedRating), ("Acceleration", player.Skills.Acceleration),
                ("Agility", player.AgilityRating), ("Strength", player.StrengthRating),
                ("Jumping", player.Skills.Jumping),
                ("Injury", player.InjuryRating)));
            content.Children.Add(AttributeGroup("Skill traits", "Position technique and football execution",
                ("Throw power", player.Skills.PassingPower),
                ("Throw accuracy", player.Skills.PassingAccuracy), ("Catching", player.Skills.Catching),
                ("Break tackle", player.Skills.BreakTackle), ("Ball carrying", player.Skills.BallCarrying),
                ("Tackling", player.Skills.Tackling), ("Run blocking", player.Skills.RunBlocking),
                ("Pass blocking", player.Skills.PassBlocking),
                ("Kicking power", player.Skills.KickPower),
                ("Kicking accuracy", player.Skills.KickAccuracy)));
            content.Children.Add(new Label { Text = "Higher ratings are better. Injury represents resistance to injury and wear.", FontSize = 11, TextColor = DynastyTheme.Muted });
            content.Children.Add(new Label { Text = player.CurrentInjury is null ? "Status: healthy" :
                $"Status: {player.CurrentInjury.Severity} {player.CurrentInjury.BodyArea} injury · {player.CurrentInjury.WeeksRemaining} weeks remaining",
                TextColor = player.CurrentInjury is null ? Color.FromArgb("#83D6B6") : Color.FromArgb("#E5A1B7"), FontSize = 12 });
        }
        else content.Children.Add(new Label { Text = "This is a recorded career. Current attributes are unavailable after departure.", FontSize = 12, TextColor = DynastyTheme.Muted });
        var totals = visibleStats.Where(line => line.PlayerId == id).ToArray();
        content.Children.Add(SectionTitle("Career snapshot", $"{totals.Sum(line => line.Games)} recorded games · {totals.Sum(line => line.ScrimmageYards):N0} yards · {totals.Sum(line => line.Touchdowns)} TD"));
        var stats = new Button { Text = "Season & career statistics", BackgroundColor = Color.FromArgb("#315D59") };
        stats.Clicked += async (_, _) => await ShowPlayerStatisticsAsync(player);
        content.Children.Add(stats);
        var school = new Button { Text = "View team roster & staff", IsEnabled = _teamsByName.ContainsKey(player.TeamName) };
        school.Clicked += async (_, _) => { await Navigation.PopModalAsync(); OpenSchool(player.TeamName); };
        content.Children.Add(school);
        await Navigation.PushModalAsync(new ContentPage { Content = new ScrollView { Content = content } });
    }

    private View AttributeGroup(string title, string subtitle, params (string Name, int Rating)[] attributes)
    {
        var rows = new Grid { ColumnSpacing = 12, RowSpacing = 6,
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(64) } };
        for (var index = 0; index < attributes.Length; index++)
        {
            rows.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            rows.Add(new Label { Text = attributes[index].Name, FontSize = 12,
                VerticalTextAlignment = TextAlignment.Center, Margin = new Thickness(4, 3) }, 0, index);
            rows.Add(RatingCell(attributes[index].Rating), 1, index);
        }
        return new VerticalStackLayout { Spacing = 6, Children = { SectionTitle(title, subtitle), rows } };
    }

    private async Task ShowCoachProfileAsync(StaffMember coach)
    {
        if (_currentDynasty is null) return;
        var state = CoachCareerService.RestoreUserHistory(_currentDynasty);
        var summary = CoachCareerService.GetSummary(state, coach.StaffId, coach.FullName);
        var close = new Button { Text = "‹ Back" }; close.Clicked += async (_, _) => await Navigation.PopModalAsync();
        var content = new VerticalStackLayout { Padding = 14, Spacing = 12, Children =
        {
            close, IdentityHeader(coach.FullName, coach.TeamName, FormatStaffRole(coach.Role)),
            SectionTitle("Coach profile", $"Age {coach.Age} · OVR {coach.OverallRating} · {coach.CareerYears} career years · {coach.ContractYearsRemaining} contract years"),
            AttributeGroup("Coaching traits", "Leadership, strategy, development and program management",
                ("Leadership", coach.Leadership), ("Recruiting", coach.Recruiting), ("Player development", coach.PlayerDevelopment),
                ("Game management", coach.GameManagement), ("Scheme", coach.Scheme), ("Special teams", coach.SpecialTeams),
                ("Medical", coach.Medical), ("Conditioning", coach.Conditioning), ("Talent evaluation", coach.TalentEvaluation),
                ("Potential evaluation", coach.PotentialEvaluation), ("Regional knowledge", coach.RegionalKnowledge), ("Staff management", coach.StaffManagement)),
            SectionTitle("Head-coaching career", $"{summary.HeadCoachWins}–{summary.HeadCoachLosses} · {summary.HeadCoachSeasons} seasons · {summary.ConferenceTitles} conference titles · {summary.NationalTitles} national titles")
        } };
        if (summary.Seasons.Count == 0) content.Children.Add(new Label { Text = "No completed seasons recorded yet." });
        else content.Children.Add(DataTable([("Year", 60), ("School", 195), ("Role", 160), ("W", 40), ("L", 40), ("Rank", 50)],
            summary.Seasons.OrderByDescending(season => season.SeasonYear).Select(season => new View[]
            {
                TableText(season.SeasonYear.ToString()), SchoolCell(season.TeamName), TableText(FormatStaffRole(season.Role)),
                TableText(season.Wins.ToString()), TableText(season.Losses.ToString()), TableText(season.FinalRanking > 0 ? $"#{season.FinalRanking}" : "—")
            })));
        content.Children.Add(new Label { Text = "Career totals count recorded head-coaching seasons. Other staff roles retain team results in the season table.", FontSize = 11, TextColor = DynastyTheme.Muted });
        var school = new Button { Text = "View team roster & staff" };
        school.Clicked += async (_, _) => { await Navigation.PopModalAsync(); OpenSchool(coach.TeamName); };
        content.Children.Add(school);
        await Navigation.PushModalAsync(new ContentPage { Content = new ScrollView { Content = content } });
    }

    private View TeamStaffTable(string school) => DataTable(
        [("Role", 160), ("Coach", 175), ("OVR", 55), ("Age", 45), ("Recruit", 65), ("Develop", 70)],
        (_currentDynasty is null ? Enumerable.Empty<StaffMember>() : Enum.GetValues<StaffRole>()
            .Select(role => StaffManagementService.GetStaff(_currentDynasty, school, role)).OfType<StaffMember>()).OrderBy(coach => coach.Role).Select(coach => new View[]
        {
            TableText(FormatStaffRole(coach.Role)), CoachLink(coach), RatingCell(coach.OverallRating), TableText(coach.Age.ToString()),
            TableText(coach.Recruiting.ToString()), TableText(coach.PlayerDevelopment.ToString())
        }));
}
