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
    private readonly Button _depthChartOrganizerButton = new() { Text = "Organize depth chart", BackgroundColor = DynastyTheme.Palette("#315D59") };
    private readonly VerticalStackLayout _depthChartOrganizer = new() { Spacing = 10, IsVisible = false };
    private bool _showDepthChartOrganizer;
    private Guid? _selectedRosterPlayer;

    private View BuildOrganizedRosterSection()
    {
        _rosterSearch.TextChanged += (_, _) => RenderCurrentDynasty();
        _rosterPosition.SelectedIndexChanged += (_, _) => RenderCurrentDynasty();
        _rosterStatus.SelectedIndexChanged += (_, _) => RenderCurrentDynasty();
        _rosterSort.SelectedIndexChanged += (_, _) =>
        {
            foreach (var key in _tableOrders.Keys.Where(key => key.StartsWith("Pos/Player/OVR/", StringComparison.Ordinal)).ToArray()) _tableOrders.Remove(key);
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
            _depthChartOrganizer, _rosterDetail, _transferPortalList
        } };
    }

    private void RenderRosterGrid(IReadOnlyList<DynastyPlayer> roster)
    {
        if (_currentDynasty is null) return;
        _rosterBanner.Content = SchoolBanner(_currentDynasty.UserTeamName,
            $"{_currentDynasty.SeasonYear} roster · {roster.Count}/{DynastyRosterRules.MaximumRosterSize} players · {roster.Count(p => p.IsRedshirted)} redshirts · {roster.Count(p => p.IsWalkOn)} walk-ons");
        var warnings = RosterManagementService.GetPositionWarnings(_currentDynasty, _currentDynasty.UserTeamName);
        _offseasonRosterStatus.Text = warnings.Count == 0 ? "Depth chart ready. Open a player to manage depth, redshirts and career history."
            : "Depth needs: " + string.Join(" · ", warnings);
        RenderDepthChartOrganizer(roster);
        var development = GetLatestUserDevelopmentRecords().ToDictionary(record => record.PlayerId);
        var selected = roster.FirstOrDefault(player => player.PlayerId == _selectedRosterPlayer);
        _rosterDetail.IsVisible = selected is not null;
        _rosterDetail.Content = selected is null ? null : CreateRosterPlayerDetail(selected,
            roster.Count(p => p.Position == selected.Position && !p.IsRedshirted), development.GetValueOrDefault(selected.PlayerId));

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
                _selectedRosterPlayer = player.PlayerId;
                RenderCurrentDynasty();
                if (_sectionViews[AppSection.Roster] is ScrollView scroll) await scroll.ScrollToAsync(0, 0, false);
            };
            var status = player.CurrentInjury is not null ? $"Injured {player.CurrentInjury.WeeksRemaining}w"
                : player.IsRedshirted ? "Redshirt" : player.IsWalkOn ? "Walk-on" : RosterManagementService.IsStarter(player) ? "Starter" : "Depth";
            var playerDevelopment = development.GetValueOrDefault(player.PlayerId);
            var seasonDelta = GameMoraleService.SeasonDelta(_currentDynasty, player);
            var morale = GameMoraleService.PlayerMorale(_currentDynasty, player);
            rows.Add([TableText(OffensiveLineService.Label(player)), PlayerLink(player),
                RatingCell(player.OverallRating),
                TableText(FormatSigned(seasonDelta), true, GetChangeColor(seasonDelta)),
                MoraleIcon(player),
                details, TableText(player.IsRedshirted ? "RS" : DisplayDepthOrder(player).ToString()),
                TableText(FormatClassYear(player.ClassYear, player.IsRedshirted || player.HasRedshirted)), TableText(FormatPotentialStars(player.PotentialRating), color: DynastyTheme.Palette("#E9C76C")),
                TableText(player.OriginalRecruitingStars is int stars ? $"{(player.RecruitingStarsEstimated ? "~" : "")}{stars}★" : "—"),
                TableText(status, color: player.CurrentInjury is not null ? NegativeChangeColor : player.IsRedshirted ? DynastyTheme.Palette("#D6A3EB") : DynastyTheme.Muted),
                HeightCell(player.HeightInches), TableText(player.WeightPounds > 0 ? player.WeightPounds.ToString() : "—")]);
        }
        _transferPortalList.Children.Add(SectionTitle($"Depth chart · {rows.Count} players", "Δ = change from season start · Morale responds to form and injury · swipe for more columns"));
        if (rows.Count == 0) _transferPortalList.Children.Add(new Label { Text = "No players match these filters." });
        else _transferPortalList.Children.Add(DataTable([("Pos", 45), ("Player", 172), ("OVR", 54), ("Δ", 45), ("Morale", 60), ("Details", 64),
            ("Depth", 54), ("Year", 50), ("Potential", 75), ("HS", 45), ("Status", 105), ("Height", 64), ("Wt lb", 60)], rows));
    }

    private bool MatchesRosterStatus(DynastyPlayer player) => _rosterStatus.SelectedIndex switch
    {
        1 => RosterManagementService.IsStarter(player),
        2 => !player.IsRedshirted && player.DepthChartOrder == DynastyRosterRules.StarterPositionCounts.GetValueOrDefault(player.Position, 1) + 1,
        3 => !player.IsRedshirted && !RosterManagementService.IsStarter(player),
        4 => player.IsRedshirted,
        5 => player.CurrentInjury is not null,
        6 => player.IsWalkOn,
        _ => true
    };

    private void RenderDepthChartOrganizer(IReadOnlyList<DynastyPlayer> roster)
    {
        _depthChartOrganizer.Children.Clear();
        _depthChartOrganizer.IsVisible = _showDepthChartOrganizer;
        _depthChartOrganizerButton.Text = _showDepthChartOrganizer ? "Close depth chart" : "Organize depth chart";
        if (!_showDepthChartOrganizer || _currentDynasty is null) return;

        _depthChartOrganizer.Children.Add(SectionTitle("Depth chart organizer",
            "Drag a player onto another player at the same position, or use the arrow buttons. Changes apply immediately."));
        foreach (var group in roster.Where(player => !player.IsRedshirted)
                     .GroupBy(player => OffensiveLineService.Label(player)).OrderBy(group => group.Key))
        {
            var players = group.OrderBy(player => player.DepthChartOrder).ToArray();
            _depthChartOrganizer.Children.Add(new Label
            {
                Text = $"{group.Key} · {(group.Key is "OT" or "OG" ? 2 : group.Key == "OC" ? 1 : DynastyRosterRules.StarterPositionCounts.GetValueOrDefault(Enum.Parse<Position>(group.Key), 1))} starter(s)",
                FontAttributes = FontAttributes.Bold, TextColor = DynastyTheme.Accent
            });
            foreach (var player in players)
            {
                var up = new Button { Text = "↑", FontSize = 12, Padding = new Thickness(10, 4), IsEnabled = Array.IndexOf(players, player) > 0 };
                var down = new Button { Text = "↓", FontSize = 12, Padding = new Thickness(10, 4), IsEnabled = Array.IndexOf(players, player) < players.Length - 1 };
                up.Clicked += (_, _) => MoveDepthPlayer(player.PlayerId, -1);
                down.Clicked += (_, _) => MoveDepthPlayer(player.PlayerId, 1);
                var row = new Grid { Padding = new Thickness(8, 5), ColumnSpacing = 8,
                    ColumnDefinitions = new ColumnDefinitionCollection { new(32), new(38), new(GridLength.Star), new(42), new(55), new(GridLength.Auto), new(GridLength.Auto) } };
                var handle = new Label { Text = "☰", FontSize = 20, VerticalTextAlignment = TextAlignment.Center,
                    TextColor = DynastyTheme.Accent };
                SemanticProperties.SetDescription(handle, $"Drag {player.FullName}");
                row.Add(handle, 0, 0);
                row.Add(new Label { Text = (Array.IndexOf(players, player) + 1).ToString(), FontAttributes = FontAttributes.Bold,
                    VerticalTextAlignment = TextAlignment.Center }, 1, 0);
                row.Add(new Label { Text = player.FullName, VerticalTextAlignment = TextAlignment.Center }, 2, 0);
                row.Add(new Label { Text = FormatClassYear(player.ClassYear, player.IsRedshirted || player.HasRedshirted), FontSize = 11, TextColor = DynastyTheme.Muted,
                    VerticalTextAlignment = TextAlignment.Center }, 3, 0);
                row.Add(RatingCell(player.OverallRating), 4, 0); row.Add(up, 5, 0); row.Add(down, 6, 0);
                var card = new Border { BackgroundColor = DynastyTheme.Palette(RosterManagementService.IsStarter(player) ? "#244B45" : "#243542"),
                    Stroke = DynastyTheme.Palette("#527185"), Content = row };
                var drag = new DragGestureRecognizer { CanDrag = true };
                drag.DragStarting += (_, args) => args.Data.Properties["DepthPlayerId"] = player.PlayerId.ToString();
                var drop = new DropGestureRecognizer { AllowDrop = true };
                drop.Drop += (_, args) =>
                {
                    if (_currentDynasty is null || !args.Data.Properties.TryGetValue("DepthPlayerId", out var value) ||
                        !Guid.TryParse(value?.ToString(), out var sourceId)) return;
                    var source = _currentDynasty.ActiveRoster.FirstOrDefault(candidate => candidate.PlayerId == sourceId);
                    if (source is null || OffensiveLineService.Label(source) != OffensiveLineService.Label(player)) return;
                    _currentDynasty = RosterManagementService.MovePlayerToOrder(_currentDynasty,
                        _currentDynasty.UserTeamName, sourceId, player.DepthChartOrder);
                    RenderCurrentDynasty();
                };
                card.GestureRecognizers.Add(drag); card.GestureRecognizers.Add(drop);
                _depthChartOrganizer.Children.Add(card);
            }
        }
    }

    private void MoveDepthPlayer(Guid playerId, int direction)
    {
        if (_currentDynasty is null) return;
        _currentDynasty = RosterManagementService.MovePlayer(_currentDynasty,
            _currentDynasty.UserTeamName, playerId, direction);
        RenderCurrentDynasty();
    }

    private Button CloseRosterDetailButton()
    {
        var close = new Button { Text = "Close player", BackgroundColor = Colors.Transparent, TextColor = DynastyTheme.Accent };
        close.Clicked += (_, _) => { _selectedRosterPlayer = null; RenderCurrentDynasty(); };
        return close;
    }
    private int DisplayDepthOrder(DynastyPlayer player) => player.Position == Position.OL && _currentDynasty is not null
        ? _currentDynasty.ActiveRoster.Where(p => p.TeamName == player.TeamName && !p.IsRedshirted &&
            OffensiveLineService.Label(p) == OffensiveLineService.Label(player)).OrderBy(p => p.DepthChartOrder)
            .Select((p, index) => (p.PlayerId, Order: index + 1)).FirstOrDefault(p => p.PlayerId == player.PlayerId).Order
        : player.DepthChartOrder;

}
