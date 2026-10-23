using Microsoft.Maui.Layouts;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private readonly ContentView _rosterBanner = new();
    private readonly ContentView _rosterDetail = new() { IsVisible = false };
    private readonly Entry _rosterSearch = new() { Placeholder = "Find a player" };
    private readonly Picker _rosterPosition = new() { Title = "Position", ItemsSource = new[] { "All positions" }.Concat(Enum.GetNames<Position>()).Concat(new[] { "OT", "OG", "OC" }).ToArray(), SelectedIndex = 0 };
    private readonly Picker _rosterStatus = new() { Title = "Role", ItemsSource = new[] { "All players", "Starters", "Second team", "Depth", "Redshirts", "Injured", "Walk-ons" }, SelectedIndex = 0 };
    private readonly Picker _rosterSort = new() { Title = "Sort roster", ItemsSource = new[] { "Depth chart", "Overall rating", "Potential", "Player name" }, SelectedIndex = 0 };
    private readonly Button _depthChartOrganizerButton = new() { Text = "Organize depth chart", AutomationId = "depth-chart-open", BackgroundColor = DynastyTheme.Palette("#315D59") };
    private readonly VerticalStackLayout _depthChartOrganizer = new() { Spacing = 10, IsVisible = false };
    private bool _showDepthChartOrganizer;
    private Guid? _selectedRosterPlayer;
    private IReadOnlyDictionary<Guid, DynastyPlayer> _effectiveDepthPlayers = new Dictionary<Guid, DynastyPlayer>();

    private View BuildOrganizedRosterSection()
    {
        ConfigureDepthChartControls();
        _rosterSearch.TextChanged += (_, _) => RenderCurrentDynasty();
        _rosterPosition.SelectedIndexChanged += (_, _) => RenderCurrentDynasty();
        _rosterStatus.SelectedIndexChanged += (_, _) => RenderCurrentDynasty();
        _rosterSort.SelectedIndexChanged += (_, _) =>
        {
            foreach (var key in _tableOrders.Keys.Where(key => key.StartsWith("Pos/Player/", StringComparison.Ordinal)).ToArray()) _tableOrders.Remove(key);
            RenderCurrentDynasty();
        };
        var filters = new Grid { ColumnSpacing = 8, RowSpacing = 8,
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(90), new(95) },
            RowDefinitions = new RowDefinitionCollection { new(GridLength.Auto) } };
        filters.Add(_rosterSearch, 0, 0); filters.Add(_rosterPosition, 1, 0);
        filters.Add(_rosterStatus, 2, 0);
        _rosterPosition.FontSize = _rosterStatus.FontSize = 11;
        _rosterSearch.FontSize = 12;
        var stats = new Button { Text = "Player statistics", TextColor = DynastyTheme.Accent, BackgroundColor = Colors.Transparent };
        stats.Clicked += (_, _) => ShowSection(AppSection.Stats);
        _depthChartOrganizerButton.Clicked += (_, _) =>
        {
            _showDepthChartOrganizer = !_showDepthChartOrganizer;
            RenderCurrentDynasty();
        };
        return new VerticalStackLayout { Spacing = 10, Children =
        {
            ResponsiveTableHeader(
                new VerticalStackLayout { Spacing = 8, Children = { _rosterBanner, _offseasonRosterStatus } },
                new VerticalStackLayout { Spacing = 8, Children = { filters,
                    new FlexLayout { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap, Children = { _rosterSort, _depthChartOrganizerButton, stats } } } }),
            _depthChartOrganizer, BuildRosterWorkspace()
        } };
    }

    private void RenderRosterGrid(IReadOnlyList<DynastyPlayer> roster)
    {
        if (_currentDynasty is null) return;
        _rosterSearch.IsVisible = _rosterPosition.IsVisible = _rosterStatus.IsVisible = _rosterSort.IsVisible = !_showDepthChartOrganizer;
        _rosterBanner.Content = SchoolBanner(_currentDynasty.UserTeamName,
            $"{_currentDynasty.SeasonYear} roster · {roster.Count}/{DynastyRosterRules.MaximumRosterSize} players · {roster.Count(p => p.IsRedshirted)} redshirts · {roster.Count(p => p.IsWalkOn)} walk-ons");
        var warnings = RosterManagementService.GetPositionWarnings(_currentDynasty, _currentDynasty.UserTeamName);
        _offseasonRosterStatus.Text = warnings.Count == 0 ? "Depth chart ready. Open a player to manage depth, redshirts and career history."
            : "Depth needs: " + string.Join(" · ", warnings);
        RenderDepthChartOrganizer(roster);
        if (_showDepthChartOrganizer) { _rosterDetail.IsVisible = false; return; }
        _effectiveDepthPlayers = DepthChartService.ProjectRoster(_currentDynasty.ActiveRoster, _currentDynasty.DepthChartAssignments).ToDictionary(p => p.PlayerId);
        var development = GetLatestUserDevelopmentRecords().ToDictionary(record => record.PlayerId);
        var selected = roster.FirstOrDefault(player => player.PlayerId == _selectedRosterPlayer);
        _rosterDetail.IsVisible = selected is not null;
        _rosterDetail.Content = selected is null ? null : CreatePlayerProfileView(selected.PlayerId,
            () => { _selectedRosterPlayer=null; RenderCurrentDynasty(); });

        ArrangeRosterWorkspace();
        var players = roster.Where(p => (_rosterPosition.SelectedIndex <= 0 || (p.Position.ToString() == (string)_rosterPosition.SelectedItem || OffensiveLineService.Label(p) == (string)_rosterPosition.SelectedItem))
            && MatchesRosterStatus(p)
            && (string.IsNullOrWhiteSpace(_rosterSearch.Text) || p.FullName.Contains(_rosterSearch.Text.Trim(), StringComparison.OrdinalIgnoreCase)));
        players = _rosterSort.SelectedIndex switch
        {
            1 => players.OrderByDescending(p => p.OverallRating).ThenBy(p => p.FullName),
            2 => players.OrderByDescending(p => p.PotentialRating).ThenBy(p => p.FullName),
            3 => players.OrderBy(p => p.FullName),
            _ => players.OrderBy(p => p.Position).ThenBy(p => p.IsRedshirted).ThenBy(p => p.DepthChartOrder).ThenByDescending(p => p.OverallRating)
        };
        var rows = new List<View[]>();
        foreach (var player in players)
        {
            var details = new Button { Text = "Open", FontSize = 11, Margin = new Thickness(4, 0) };
            SemanticProperties.SetDescription(details, "Open " + player.FullName);
            details.Clicked += async (_, _) =>
            {
                if (Width < 1000) { await ShowPlayerProfileAsync(player.PlayerId); return; }
                _selectedRosterPlayer = player.PlayerId;
                RenderCurrentDynasty();
                if (_sectionViews[AppSection.Roster] is ScrollView scroll) await scroll.ScrollToAsync(0, 0, false);
            };
            var status = player.CurrentInjury is not null ? $"Injured {player.CurrentInjury.WeeksRemaining}w"
                : player.IsRedshirted ? "Redshirt" : player.IsWalkOn ? "Walk-on" : EffectiveRosterStarter(player) ? "Starter" : "Depth";
            var playerDevelopment = development.GetValueOrDefault(player.PlayerId);
            var seasonDelta = GameMoraleService.SeasonDelta(_currentDynasty, player);
            var morale = GameMoraleService.PlayerMorale(_currentDynasty, player);
            rows.Add([TableText(OffensiveLineService.Label(player)), PlayerLink(player),
                TableText(FormatClassYear(player.ClassYear, player.IsRedshirted || player.HasRedshirted)),
                RatingCell(player.OverallRating),
                TableText(FormatSigned(seasonDelta), true, GetChangeColor(seasonDelta)),
                MoraleIcon(player),
                details, TableText(player.IsRedshirted ? "RS" : DisplayDepthOrder(player).ToString()),
                TableText(FormatPotentialStars(player.PotentialRating), color: DynastyTheme.Palette("#E9C76C")),
                TableText(player.OriginalRecruitingStars is int stars ? $"{(player.RecruitingStarsEstimated ? "~" : "")}{stars}★" : "—"),
                TableText(status, color: player.CurrentInjury is not null ? NegativeChangeColor : player.IsRedshirted ? DynastyTheme.Palette("#D6A3EB") : DynastyTheme.Muted),
                HeightCell(player.HeightInches), TableText(player.WeightPounds > 0 ? player.WeightPounds.ToString() : "—")]);
        }
        _transferPortalList.Children.Add(SectionTitle($"Depth chart · {rows.Count} players", "Δ = change from season start · Morale responds to form and injury · swipe for more columns"));
        if (rows.Count == 0) _transferPortalList.Children.Add(new Label { Text = "No players match these filters." });
        else _transferPortalList.Children.Add(DataTable([("Pos", 36), ("Player", 148), ("Year", 40), ("OVR", 44), ("Δ", 36), ("Morale", 50), ("Details", 56),
            ("Depth", 54), ("Potential", 75), ("HS", 45), ("Status", 105), ("Height", 64), ("Wt lb", 60)], rows, configureColumns: true));
    }

    private bool MatchesRosterStatus(DynastyPlayer player) => _rosterStatus.SelectedIndex switch
    {
        1 => EffectiveRosterStarter(player),
        2 => !player.IsRedshirted && EffectiveDepthPlayer(player).DepthChartOrder == DynastyRosterRules.StarterPositionCounts.GetValueOrDefault(player.Position, 1) + 1,
        3 => !player.IsRedshirted && !EffectiveRosterStarter(player),
        4 => player.IsRedshirted,
        5 => player.CurrentInjury is not null,
        6 => player.IsWalkOn,
        _ => true
    };

    private Button CloseRosterDetailButton()
    {
        var close = new Button { Text = "Close player", BackgroundColor = Colors.Transparent, TextColor = DynastyTheme.Accent };
        close.Clicked += (_, _) => { _selectedRosterPlayer = null; RenderCurrentDynasty(); };
        return close;
    }
    private DynastyPlayer EffectiveDepthPlayer(DynastyPlayer player) => _effectiveDepthPlayers.GetValueOrDefault(player.PlayerId) ?? player;
    private bool EffectiveRosterStarter(DynastyPlayer player) => RosterManagementService.IsStarter(EffectiveDepthPlayer(player));
    private int DisplayDepthOrder(DynastyPlayer player) => _currentDynasty is not null && _currentDynasty.DepthChartAssignments.Any(a => a.TeamName == player.TeamName)
        ? DepthChartService.AtPosition(_currentDynasty, player.TeamName, DepthChartService.AssignedCode(_currentDynasty, player)).Select((p, i) => (p.PlayerId, Rank: i + 1)).First(p => p.PlayerId == player.PlayerId).Rank
        : player.Position == Position.OL && _currentDynasty is not null
        ? _currentDynasty.ActiveRoster.Where(p => p.TeamName == player.TeamName && !p.IsRedshirted &&
            OffensiveLineService.Label(p) == OffensiveLineService.Label(player)).OrderBy(p => p.DepthChartOrder)
            .Select((p, index) => (p.PlayerId, Order: index + 1)).FirstOrDefault(p => p.PlayerId == player.PlayerId).Order
        : player.DepthChartOrder;

}
