using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Branding;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private static View IdentityHeader(Guid id, string name, string school, string subtitle, bool staff = false, int age = 20, NcaaPlayerAppearance? appearance = null)
    {
        var brand = TeamBrandCatalog.Get(school);
        var primary = Color.FromArgb(brand.Primary);
        var row = new Grid { ColumnSpacing = 14,
            ColumnDefinitions = new ColumnDefinitionCollection { new(108), new(GridLength.Star) } };
        row.Add(PersonPortrait.Create(id, name, primary, Color.FromArgb(brand.Secondary), 108, staff, age, appearance), 0, 0);
        row.Add(new VerticalStackLayout { Spacing = 5, VerticalOptions = LayoutOptions.Center, Children =
        {
            new Label { Text = staff ? "COACH PROFILE" : "PLAYER PROFILE", FontSize = 10,
                FontAttributes = FontAttributes.Bold, TextColor = DynastyTheme.Accent },
            new Label { Text = name, FontSize = 21, FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.WordWrap },
            new Label { Text = subtitle, FontSize = 12, TextColor = Colors.White },
            new Label { Text = school, FontSize = 12, TextColor = DynastyTheme.Muted }
        } }, 1, 0);
        return new Border { Content = row, Padding = 14, BackgroundColor = Color.FromArgb("#203342") };
    }

    private View PlayerLink(DynastyPlayer player) => PersonLink(player.PlayerId, player.FullName, player.TeamName,
        () => ShowPlayerProfileAsync(player.PlayerId), appearance: player.Appearance);

    private View PlayerLink(Guid id, string name) => PersonLink(id, name,
        _currentDynasty?.ActiveRoster.FirstOrDefault(player => player.PlayerId == id)?.TeamName ?? "",
        () => ShowPlayerProfileAsync(id), appearance: _currentDynasty?.ActiveRoster.FirstOrDefault(player => player.PlayerId == id)?.Appearance
            ?? _currentDynasty?.PlayerCareerHistory.FirstOrDefault(player => player.PlayerId == id)?.Appearance);

    private View CoachLink(StaffMember coach) => PersonLink(coach.StaffId, coach.FullName, coach.TeamName,
        () => ShowCoachProfileAsync(coach), true, coach.Age, coach.Appearance);

    private static View PersonLink(Guid id, string name, string school, Func<Task> open, bool staff = false, int age = 20, NcaaPlayerAppearance? appearance = null)
    {
        var brand = TeamBrandCatalog.Get(school);
        var row = new Grid { ColumnSpacing = 6, Padding = new Thickness(5, 3), MinimumHeightRequest = 48,
            ColumnDefinitions = new ColumnDefinitionCollection { new(36), new(GridLength.Star) } };
        row.Add(PersonPortrait.Create(id, name, Color.FromArgb(brand.Primary), Color.FromArgb(brand.Secondary), 36, staff, age, appearance), 0, 0);
        // A real button retains keyboard, screen-reader and touch behavior inside sortable tables.
        var button = new Button { Text = name, FontSize = 12,
            TextColor = Color.FromArgb(staff ? "#D6C4F2" : "#BDE5F5"), BackgroundColor = Colors.Transparent,
            HorizontalOptions = LayoutOptions.Fill, Padding = new Thickness(0, 4), MinimumHeightRequest = 44 };
        SemanticProperties.SetDescription(button, (staff ? "View coach " : "View player ") + name);
        button.Clicked += async (_, _) => await open();
        row.Add(button, 1, 0);
        row.SetValue(TableValueProperty, name);
        return row;
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
            Position = career?.Position ?? latest!.Position, OriginalRecruitingStars = career?.OriginalRecruitingStars ?? latest?.OriginalRecruitingStars,
            RecruitingStarsEstimated = career?.OriginalRecruitingStars is not null ? career.RecruitingStarsEstimated : latest?.RecruitingStarsEstimated ?? false,
            HeightInches = career?.HeightInches ?? latest?.HeightInches ?? 0, WeightPounds = career?.WeightPounds ?? latest?.WeightPounds ?? 0,
            JerseyNumber = career?.JerseyNumber ?? latest?.JerseyNumber, Appearance = career?.Appearance ?? latest?.Appearance
        };
        if (active is not null) player = PlayerRatingService.EnsureInitialized(state, player);
        var close = new Button { Text = "‹ Back" };
        close.Clicked += async (_, _) => await Navigation.PopModalAsync();
        View PlayerHeader() => IdentityHeader(player.PlayerId, player.FullName, player.TeamName, $"{(player.JerseyNumber is int number ? $"#{number} · " : "")}{player.Position} · {(active is null ? "Archived player" : FormatClassYear(player.ClassYear, player.IsRedshirted || player.HasRedshirted))}", appearance: player.Appearance);
        var portraitHeader = new ContentView { Content = PlayerHeader() };
        var editPortrait = new Button { Text = "Edit portrait", FontSize = 11 };
        editPortrait.Clicked += async (_, _) =>
        {
            var next = await EditPortraitAsync(player.PlayerId, player.FullName, player.TeamName, player.Appearance);
            if (next is null) return;
            player = player with { Appearance = next }; portraitHeader.Content = PlayerHeader(); RenderCurrentDynasty();
        };
        var content = new VerticalStackLayout { Padding = 14, Spacing = 12, Children =
        {
            close, portraitHeader, editPortrait,
            new Label { Text = PlayerPhysicalService.Format(player.HeightInches, player.WeightPounds), FontSize = 13, TextColor = DynastyTheme.Accent },
            new Label { Text = FormatOriginalRecruitingStars(player), TextColor = Color.FromArgb("#E8C979") }
        } };
        if (active is not null)
        {
            player = PlayerRatingService.EnsureInitialized(state, player);
            content.Children.Add(SectionTitle("Player attributes", $"OVR {player.OverallRating} · Potential {FormatPotentialStars(player.PotentialRating)} · Depth {player.DepthChartOrder}"));
            content.Children.Add(AttributeGroup("Character traits", "Decision-making and team influence",
                ("Awareness", player.AwarenessRating), ("Leadership", player.Skills!.Leadership),
                ("Discipline", player.Skills.Discipline)));
            content.Children.Add(AttributeGroup("Physical traits", "Athletic ability and availability",
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
        var honors = state.PlayerAwardHistory.Where(a => a.PlayerId == id).OrderByDescending(a => a.SeasonYear).ThenBy(a => a.IsPreseason).ToArray();
        if (honors.Length > 0)
        {
            content.Children.Add(SectionTitle("Honors & awards", "Permanent season selections"));
            foreach (var honor in honors) content.Children.Add(new Label { Text = $"{honor.SeasonYear} · {honor.AwardName}{(honor.ConferenceName is null ? "" : " · " + honor.ConferenceName)}", FontSize = 12, TextColor = Color.FromArgb("#E8C979") });
        }
        var gameAwards = PlayerOfGameService.All(state).Where(a => a.PlayerId == id).OrderByDescending(a => a.SeasonYear).ThenByDescending(a => a.Week).ToArray();
        if (gameAwards.Length > 0)
        {
            content.Children.Add(SectionTitle("Player of the Game", $"{gameAwards.Length} game award{(gameAwards.Length == 1 ? "" : "s")}"));
            foreach (var a in gameAwards) content.Children.Add(new Label { Text = $"{a.SeasonYear} · Week {a.Week} · {a.TeamName}\n{a.Summary}", FontSize = 12, TextColor = Color.FromArgb("#E8C979") });
        }
        var morale = GameMoraleService.SeasonMorale(state, player.TeamName);
        content.Children.Add(new Label { Text = $"Team morale: {GameMoraleService.Label(morale)} ({morale}/100) · modest training effect", FontSize = 11, TextColor = DynastyTheme.Muted });
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
        var tiles = new Grid { ColumnSpacing = 8, RowSpacing = 8 };
        int previousColumns = 0;
        void Arrange(int columns)
        {
            if (columns == previousColumns) return;
            previousColumns = columns;
            tiles.Children.Clear(); tiles.ColumnDefinitions.Clear(); tiles.RowDefinitions.Clear();
            for (var i = 0; i < columns; i++) tiles.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            for (var i = 0; i < attributes.Length; i++)
            {
                if (i % columns == 0) tiles.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                var attribute = attributes[i];
                var row = new Grid { ColumnSpacing = 6,
                    ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) } };
                row.Add(new Label { Text = attribute.Name, FontSize = 12, VerticalTextAlignment = TextAlignment.Center }, 0, 0);
                row.Add(RatingCell(attribute.Rating), 1, 0);
                tiles.Add(new Border { Padding = new Thickness(10, 7), BackgroundColor = Color.FromArgb("#1D2C38"), Content = row }, i % columns, i / columns);
            }
        }
        Arrange(2);
        tiles.SizeChanged += (_, _) => Arrange(tiles.Width >= 780 ? 4 : tiles.Width >= 540 ? 3 : tiles.Width < 300 ? 1 : 2);
        return new VerticalStackLayout { Spacing = 8, Children = { SectionTitle(title, subtitle), tiles } };
    }

    private async Task ShowCoachProfileAsync(StaffMember coach)
    {
        if (_currentDynasty is null) return;
        var state = CoachCareerService.RestoreUserHistory(_currentDynasty);
        var summary = CoachCareerService.GetSummary(state, coach.StaffId, coach.FullName);
        var close = new Button { Text = "‹ Back" }; close.Clicked += async (_, _) => await Navigation.PopModalAsync();
        View CoachHeader() => IdentityHeader(coach.StaffId, coach.FullName, coach.TeamName, FormatStaffRole(coach.Role), true, coach.Age, coach.Appearance);
        var portraitHeader = new ContentView { Content = CoachHeader() };
        var editPortrait = new Button { Text = "Edit portrait", FontSize = 11 };
        editPortrait.Clicked += async (_, _) =>
        {
            var next = await EditPortraitAsync(coach.StaffId, coach.FullName, coach.TeamName, coach.Appearance, true, coach.Age);
            if (next is null) return;
            coach = coach with { Appearance = next }; portraitHeader.Content = CoachHeader(); RenderCurrentDynasty();
        };
        var content = new VerticalStackLayout { Padding = 14, Spacing = 12, Children =
        {
            close, portraitHeader, editPortrait,
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
        [("Role", 160), ("Coach", 210), ("OVR", 55), ("Age", 45), ("Recruit", 65), ("Develop", 70)],
        (_currentDynasty is null ? Enumerable.Empty<StaffMember>() : Enum.GetValues<StaffRole>()
            .Select(role => StaffManagementService.GetStaff(_currentDynasty, school, role)).OfType<StaffMember>()).OrderBy(coach => coach.Role).Select(coach => new View[]
        {
            TableText(FormatStaffRole(coach.Role)), CoachLink(coach), RatingCell(coach.OverallRating), TableText(coach.Age.ToString()),
            TableText(coach.Recruiting.ToString()), TableText(coach.PlayerDevelopment.ToString())
        }));
}
