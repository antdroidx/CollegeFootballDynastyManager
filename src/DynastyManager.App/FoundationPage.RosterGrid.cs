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
    private readonly Picker _rosterPosition = new() { Title = "Position", ItemsSource = new[] { "All positions" }.Concat(Enum.GetNames<Position>()).ToArray(), SelectedIndex = 0 };
    private readonly Picker _rosterSort = new() { Title = "Sort roster", ItemsSource = new[] { "Depth chart", "Overall rating", "Potential", "Player name" }, SelectedIndex = 0 };
    private Guid? _selectedRosterPlayer;

    private View BuildOrganizedRosterSection()
    {
        _rosterSearch.TextChanged += (_, _) => RenderCurrentDynasty();
        _rosterPosition.SelectedIndexChanged += (_, _) => RenderCurrentDynasty();
        _rosterSort.SelectedIndexChanged += (_, _) => RenderCurrentDynasty();
        var filters = new Grid { ColumnSpacing = 8, ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(120) } };
        filters.Add(_rosterSearch, 0, 0); filters.Add(_rosterPosition, 1, 0);
        var stats = new Button { Text = "Player statistics", TextColor = DynastyTheme.Accent, BackgroundColor = Colors.Transparent };
        stats.Clicked += (_, _) => ShowSection(AppSection.Stats);
        return new VerticalStackLayout { Spacing = 10, Children =
        {
            ResponsiveTableHeader(
                new VerticalStackLayout { Spacing = 8, Children = { _rosterBanner, _offseasonRosterStatus } },
                new VerticalStackLayout { Spacing = 8, Children = { filters,
                    new HorizontalStackLayout { Spacing = 8, Children = { _rosterSort, stats } } } }),
            _rosterDetail, _transferPortalList
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
        var development = GetLatestUserDevelopmentRecords().ToDictionary(record => record.PlayerId);
        var selected = roster.FirstOrDefault(player => player.PlayerId == _selectedRosterPlayer);
        _rosterDetail.IsVisible = selected is not null;
        _rosterDetail.Content = selected is null ? null : CreateRosterPlayerDetail(selected,
            roster.Count(p => p.Position == selected.Position && !p.IsRedshirted), development.GetValueOrDefault(selected.PlayerId));

        var players = roster.Where(p => (_rosterPosition.SelectedIndex <= 0 || p.Position.ToString() == (string)_rosterPosition.SelectedItem)
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
            rows.Add([TableText(player.Position.ToString()), TableText(player.FullName, RosterManagementService.IsStarter(player)),
                RatingCell(player.OverallRating), details, TableText(player.IsRedshirted ? "RS" : player.DepthChartOrder.ToString()),
                TableText($"Y{player.ClassYear}"), TableText(FormatPotentialStars(player.PotentialRating), color: Color.FromArgb("#E9C76C")),
                TableText(player.OriginalRecruitingStars is int stars ? $"{stars}★" : "—"),
                TableText(status, color: player.CurrentInjury is not null ? NegativeChangeColor : player.IsRedshirted ? Color.FromArgb("#D6A3EB") : DynastyTheme.Muted)]);
        }
        _transferPortalList.Children.Add(SectionTitle($"Depth chart · {rows.Count} players", "Aligned columns · swipe horizontally for year, potential, HS stars and status"));
        if (rows.Count == 0) _transferPortalList.Children.Add(new Label { Text = "No players match these filters." });
        else _transferPortalList.Children.Add(DataTable([("Pos", 45), ("Player", 172), ("OVR", 54), ("Details", 64),
            ("Depth", 54), ("Year", 50), ("Potential", 75), ("HS", 45), ("Status", 105)], rows));
    }

    private Button CloseRosterDetailButton()
    {
        var close = new Button { Text = "Close player", BackgroundColor = Colors.Transparent, TextColor = DynastyTheme.Accent };
        close.Clicked += (_, _) => { _selectedRosterPlayer = null; RenderCurrentDynasty(); };
        return close;
    }
}
