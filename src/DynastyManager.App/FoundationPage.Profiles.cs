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
            new Label { Text = subtitle, FontSize = 12, TextColor = DynastyTheme.Text },
            new Label { Text = school, FontSize = 12, TextColor = DynastyTheme.Muted }
        } }, 1, 0);
        return new Border { Content = row, Padding = 14, BackgroundColor = DynastyTheme.Palette("#203342") };
    }

    private View PlayerLink(DynastyPlayer player) => PersonLink(player.FullName, () => ShowPlayerProfileAsync(player.PlayerId));

    private View PlayerLink(Guid id, string name) => PersonLink(name, () => ShowPlayerProfileAsync(id));

    private View CoachLink(StaffMember coach) => PersonLink(coach.FullName, () => ShowCoachProfileAsync(coach), true);

    private static View PersonLink(string name, Func<Task> open, bool staff = false)
    {
        // A real button retains keyboard, screen-reader and touch behavior inside sortable tables.
        var displayName=staff || !name.Contains(' ') ? name : name[..1]+". "+name[(name.IndexOf(' ')+1)..];
        var button = new Button { Text = displayName, FontSize = 12,
            TextColor = DynastyTheme.IsLight ? Color.FromArgb(staff ? "#623794" : "#23587D") : Color.FromArgb(staff ? "#D6C4F2" : "#BDE5F5"), BackgroundColor = Colors.Transparent,
            HorizontalOptions = LayoutOptions.Start, Padding = new Thickness(6, 4), MinimumHeightRequest = 44 };
        SemanticProperties.SetDescription(button, (staff ? "View coach " : "View player ") + name);
        button.Clicked += async (_, _) => await open();
        button.SetValue(TableValueProperty, name);
        return button;
    }

    private async Task ShowPlayerProfileAsync(Guid id)
    {
        if (_activeSection == AppSection.Roster && DynastyManager.Data.Presentation.ManagementLayout.SplitProfile(_rosterWorkspace.Width))
        {
            _selectedRosterPlayer=id; RenderCurrentDynasty(); return;
        }
        var host=new ContentView();
        void RefreshProfile() => host.Content=CreatePlayerProfileView(id,()=>{ _=Navigation.PopModalAsync(); });
        RefreshProfile(); if(host.Content is null)return;
        var page=new ContentPage { AutomationId="player-profile",Content=new ScrollView {Content=host} };
        _refreshOpenPlayerProfile=RefreshProfile;
        page.Disappearing+=(_,_)=>Dispatcher.Dispatch(()=>{if(!Navigation.ModalStack.Contains(page))_refreshOpenPlayerProfile=null;});
        await Navigation.PushModalAsync(page);
    }

    private View? CreatePlayerProfileView(Guid id, Action closeProfile)
    {
        if (_currentDynasty is null) return null;
        var state = _currentDynasty;
        var visibleStats = VisiblePlayerStatistics();
        var active = state.ActiveRoster.FirstOrDefault(player => player.PlayerId == id);
        var career = state.PlayerCareerHistory.FirstOrDefault(player => player.PlayerId == id);
        var latest = visibleStats.Where(line => line.PlayerId == id).OrderByDescending(line => line.SeasonYear).FirstOrDefault();
        var departure = state.PlayerDepartureHistory.Concat(state.RecentPlayerDepartures).Where(p => p.PlayerId == id).OrderByDescending(p => p.SeasonYear).FirstOrDefault();
        if (active is null && career is null && latest is null && departure is null) return null;
        var player = active ?? new DynastyPlayer
        {
            PlayerId = id, FullName = career?.PlayerName ?? latest?.PlayerName ?? departure!.FullName, TeamName = latest?.TeamName ?? departure?.TeamName ?? "Recorded career",
            Position = career?.Position ?? latest?.Position ?? departure!.Position, OverallRating = departure?.OverallRating ?? 0, OriginalRecruitingStars = career?.OriginalRecruitingStars ?? latest?.OriginalRecruitingStars,
            RecruitingStarsEstimated = career?.OriginalRecruitingStars is not null ? career.RecruitingStarsEstimated : latest?.RecruitingStarsEstimated ?? false,
            HeightInches = career?.HeightInches ?? latest?.HeightInches ?? 0, WeightPounds = career?.WeightPounds ?? latest?.WeightPounds ?? 0,
            JerseyNumber = career?.JerseyNumber ?? latest?.JerseyNumber, Appearance = career?.Appearance ?? latest?.Appearance
        };
        if (active is not null) player = PlayerRatingService.EnsureInitialized(state, player);
        var close = new Button { Text = "‹ Back" };
        close.Clicked += (_, _) => closeProfile();
        View PlayerHeader() => IdentityHeader(player.PlayerId, player.FullName, player.TeamName, $"{(player.JerseyNumber is int number ? $"#{number} · " : "")}{OffensiveLineService.Label(player)} · {(active is null ? "Archived player" : FormatClassYear(player.ClassYear, player.IsRedshirted || player.HasRedshirted))}", appearance: player.Appearance);
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
            new Label { Text = FormatOriginalRecruitingStars(player), TextColor = DynastyTheme.Palette("#E8C979") }
        } };
        var hallEntry = state.HallOfFame.FirstOrDefault(e => e.PlayerId == id);
        if (hallEntry is not null) content.Children.Add(SectionTitle($"Hall of Fame · {hallEntry.InductionYear}", hallEntry.Citation));
        if (active is not null)
        {
            content.Children.Add(PlayerMoraleSummary(player));
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
                TextColor = player.CurrentInjury is null ? DynastyTheme.Palette("#83D6B6") : DynastyTheme.Palette("#E5A1B7"), FontSize = 12 });
        }
        else content.Children.Add(new Label { Text = "This is a recorded career. Current attributes are unavailable after departure.", FontSize = 12, TextColor = DynastyTheme.Muted });
        var honors = state.PlayerAwardHistory.Where(a => a.PlayerId == id).OrderByDescending(a => a.SeasonYear).ThenBy(a => a.IsPreseason).ToArray();
        if (honors.Length > 0)
        {
            content.Children.Add(SectionTitle("Honors & awards", "Permanent season selections"));
            foreach (var honor in honors) content.Children.Add(new Label { Text = $"{honor.SeasonYear} · {honor.AwardName}{(honor.ConferenceName is null ? "" : " · " + honor.ConferenceName)}", FontSize = 12, TextColor = DynastyTheme.Palette("#E8C979") });
        }
        var gameAwards = PlayerOfGameService.All(state).Where(a => a.PlayerId == id).OrderByDescending(a => a.SeasonYear).ThenByDescending(a => a.Week).ToArray();
        if (gameAwards.Length > 0)
        {
            content.Children.Add(SectionTitle("Player of the Game", $"{gameAwards.Length} game award{(gameAwards.Length == 1 ? "" : "s")}"));
            foreach (var a in gameAwards) content.Children.Add(new Label { Text = $"{a.SeasonYear} · Week {a.Week} · {a.TeamName}\n{a.Summary}", FontSize = 12, TextColor = DynastyTheme.Palette("#E8C979") });
        }
        var totals = visibleStats.Where(line => line.PlayerId == id).ToArray();
        content.Children.Add(SectionTitle("Career snapshot", $"{totals.Sum(line => line.Games)} recorded games · {totals.Sum(line => line.ScrimmageYards):N0} yards · {totals.Sum(line => line.Touchdowns)} TD"));
        var stats = new Button { Text = "Season & career statistics", BackgroundColor = DynastyTheme.Palette("#315D59") };
        stats.Clicked += async (_, _) => await ShowPlayerStatisticsAsync(player);
        content.Children.Add(stats);
        var school = new Button { Text = "View team roster & staff", IsEnabled = _teamsByName.ContainsKey(player.TeamName) };
        school.Clicked += (_, _) => OpenSchool(player.TeamName);
        content.Children.Add(school);
        return OrganizePlayerProfile(content, player, active is not null);

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
                row.Add(new Label { Text=attribute.Rating.ToString(), FontSize=12, FontAttributes=FontAttributes.Bold,
                    TextColor=DynastyTheme.IsLight ? Color.FromArgb("#166841") : Color.FromArgb("#70D4AD"), VerticalTextAlignment=TextAlignment.Center },1,0);
                tiles.Add(new VerticalStackLayout { Spacing=5, Padding=new Thickness(4,5), Children={row,
                    new ProgressBar { Progress=Math.Clamp(attribute.Rating/99.0,0,1), HeightRequest=4,
                        ProgressColor=attribute.Rating>=85 ? Color.FromArgb("#38B888") : attribute.Rating>=70 ? Color.FromArgb("#599BDB") : Color.FromArgb("#B49C61") } } },i%columns,i/columns);
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
        var overview = new VerticalStackLayout { Spacing = 10, Children = {
            SectionTitle("Coach profile", $"Age {coach.Age} · OVR {coach.OverallRating} · {coach.CareerYears} career years · {coach.ContractYearsRemaining} contract years"),
            editPortrait
        } };
        if (coach.FormerPlayerId is not null) overview.Add(new Label { Text = $"Former {coach.AlmaMater} player · Class of {coach.GraduationYear} · entered coaching after apprenticeship", TextColor = DynastyTheme.Accent });
        if (coach.SharedRoleCoaches.Count > 0) overview.Add(new Label { Text = "Shared coordinator role: " + string.Join(" · ", coach.SharedRoleCoaches.Select(c => $"{c.FullName} ({c.PublishedTitle})")), TextColor = DynastyTheme.Muted });
        if (coach.DataSeason is int dataSeason) overview.Add(new Label { Text = $"{dataSeason} staff · {coach.PublishedTitle} · age and coaching ratings are game estimates", TextColor = DynastyTheme.Muted });
        var traits = AttributeGroup("Coaching traits", "Leadership, strategy, development and program management",
            ("Leadership", coach.Leadership), ("Recruiting", coach.Recruiting), ("Player development", coach.PlayerDevelopment),
            ("Game management", coach.GameManagement), ("Scheme", coach.Scheme), ("Special teams", coach.SpecialTeams),
            ("Medical", coach.Medical), ("Conditioning", coach.Conditioning), ("Talent evaluation", coach.TalentEvaluation),
            ("Potential evaluation", coach.PotentialEvaluation), ("Regional knowledge", coach.RegionalKnowledge), ("Staff management", coach.StaffManagement));
        var history = new VerticalStackLayout { Spacing = 10, Children = {
            SectionTitle("Head-coaching career", $"{summary.HeadCoachWins}–{summary.HeadCoachLosses} · {summary.HeadCoachSeasons} seasons · {summary.ConferenceTitles} conference titles · {summary.NationalTitles} national titles")
        } };
        if (summary.Seasons.Count == 0) history.Add(new Label { Text = "No completed seasons recorded yet." });
        else history.Add(DataTable([("Year", 60), ("School", 195), ("Role", 160), ("W", 40), ("L", 40), ("Rank", 50)],
            summary.Seasons.OrderByDescending(season => season.SeasonYear).Select(season => new View[] {
                TableText(season.SeasonYear.ToString()), SchoolCell(season.TeamName), TableText(FormatStaffRole(season.Role)),
                TableText(season.Wins.ToString()), TableText(season.Losses.ToString()), TableText(season.FinalRanking > 0 ? $"#{season.FinalRanking}" : "—")
            })));
        history.Add(new Label { Text = "Career totals count recorded head-coaching seasons. Other staff roles retain team results in the season table.", FontSize = 11, TextColor = DynastyTheme.Muted });
        var school = new Button { Text = "View team roster & staff" };
        school.Clicked += (_, _) => OpenSchool(coach.TeamName);
        overview.Add(school);
        var content = new VerticalStackLayout { Padding = 14, Spacing = 12, Children = {
            close, ResponsivePanels(portraitHeader, ProfileTabs(("Overview", overview), ("Attributes", traits), ("Career history", history)))
        } };
        await Navigation.PushModalAsync(new ContentPage { AutomationId = "coach-profile", Content = new ScrollView { Content = content } });
    }

    private View TeamStaffTable(string school) => DataTable(
        [("Role", 160), ("Coach", 210), ("OVR", 55), ("Age", 45), ("Recruit", 65), ("Develop", 70)],
        (_currentDynasty is null ? Enumerable.Empty<StaffMember>() : Enum.GetValues<StaffRole>()
            .Select(role => StaffManagementService.GetStaff(_currentDynasty, school, role)).OfType<StaffMember>()).OrderBy(coach => coach.Role).Select(coach => new View[]
        {
            TableText(FormatStaffRole(coach.Role)), CoachLink(coach), RatingCell(coach.OverallRating), TableText(coach.DataSeason is null ? coach.Age.ToString() : $"~{coach.Age}"),
            TableText(coach.Recruiting.ToString()), TableText(coach.PlayerDevelopment.ToString())
        }));
}
