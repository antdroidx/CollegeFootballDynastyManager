using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private int _depthPositionCode;
    private readonly Picker _depthPositionPicker = new() { Title = "Depth chart position", AutomationId = "depth-position-picker",
        ItemsSource = DepthChartService.PositionNames.ToArray(), SelectedIndex = 0, FontSize = 15 };
    private readonly Entry _depthCandidateSearch = new() { Placeholder = "Search any player on your roster", AutomationId = "depth-candidate-search", FontSize = 13 };

    private void ConfigureDepthChartControls()
    {
        _depthPositionPicker.SelectedIndex = _depthPositionCode;
        _depthPositionPicker.SelectedIndexChanged += (_, _) =>
        {
            if (_depthPositionPicker.SelectedIndex < 0 || _depthPositionPicker.SelectedIndex == _depthPositionCode) return;
            _depthPositionCode = _depthPositionPicker.SelectedIndex;
            RenderCurrentDynasty();
        };
        _depthCandidateSearch.TextChanged += (_, _) => RenderCurrentDynasty();
    }

    private void RenderDepthChartOrganizer(IReadOnlyList<DynastyPlayer> roster)
    {
        // Reusable native controls must be detached before the next layout owns them.
        if (_depthPositionPicker.Parent is Layout oldPicker) oldPicker.Children.Remove(_depthPositionPicker);
        if (_depthCandidateSearch.Parent is Layout oldSearch) oldSearch.Children.Remove(_depthCandidateSearch);
        _depthChartOrganizer.Children.Clear();
        _depthChartOrganizer.IsVisible = _showDepthChartOrganizer;
        _depthChartOrganizerButton.Text = _showDepthChartOrganizer ? "Close depth chart" : "Organize depth chart";
        if (!_showDepthChartOrganizer || _currentDynasty is null) return;
        var state = _currentDynasty;
        var team = state.UserTeamName;
        var code = _depthPositionCode;
        var label = DepthChartService.PositionNames[code];
        var auto = new Button { Text = "✦ Auto Set", FontSize = 13, AutomationId = "depth-auto-set",
            BackgroundColor = DynastyTheme.Palette("#315D59"), TextColor = Colors.White, Padding = new Thickness(16, 10) };
        auto.Clicked += async (_, _) =>
        {
            var choice = await DisplayActionSheetAsync("Auto Set depth chart", "Cancel", null, "Whole team · roster, scheme & staff", $"Sort {label} depth only");
            if (_currentDynasty is null || choice is null || choice == "Cancel") return;
            _currentDynasty = DepthChartService.AutoSet(_currentDynasty, _currentDynasty.UserTeamName,
                choice.StartsWith("Sort", StringComparison.Ordinal) ? code : null);
            RenderCurrentDynasty();
        };
        var header = new Grid { ColumnSpacing = 12, ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) } };
        header.Add(new VerticalStackLayout { Spacing = 3, Children =
        {
            new Label { Text = "DEPTH CHART", FontSize = 12, CharacterSpacing = 1.5, TextColor = DynastyTheme.Accent },
            new Label { Text = "Build your starting lineup", FontSize = 21, FontAttributes = FontAttributes.Bold },
            new Label { Text = "Position OVR uses the calibrated POCI weights. Auto Set weighs scheme fit and staff expertise.", FontSize = 12, TextColor = DynastyTheme.Muted }
        } }, 0);
        header.Add(auto, 1);
        _depthChartOrganizer.Children.Add(header);
        var units = new FlexLayout { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap, AlignItems = FlexAlignItems.Center };
        foreach (var (name, first, last) in new[] { ("Offense", 0, 9), ("Defense", 10, 18), ("Special teams", 19, 20) })
        {
            var active = code >= first && code <= last;
            var button = new Button { Text = name, FontSize = 12, Padding = new Thickness(14, 7), Margin = new Thickness(0, 0, 6, 0),
                BackgroundColor = active ? DynastyTheme.Accent : DynastyTheme.Palette("#243542"), TextColor = active ? BrandInk(DynastyTheme.Accent) : DynastyTheme.Text };
            button.Clicked += (_, _) => _depthPositionPicker.SelectedIndex = first;
            units.Children.Add(button);
        }
        _depthChartOrganizer.Children.Add(units);
        var selector = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) }, ColumnSpacing = 12 };
        selector.Add(_depthPositionPicker, 0);
        selector.Add(new Label { Text = $"PPOS {code} · {label}", TextColor = DynastyTheme.Accent, FontSize = 12, VerticalTextAlignment = TextAlignment.Center }, 1);
        _depthChartOrganizer.Children.Add(selector);
        var depth = DepthChartService.AtPosition(state, team, code);
        var section = new VerticalStackLayout { Spacing = 7 };
        section.Children.Add(new Label { Text = $"{label} DEPTH  ·  {depth.Count} players", FontAttributes = FontAttributes.Bold, FontSize = 13, TextColor = DynastyTheme.Accent });
        if (depth.Count == 0) section.Children.Add(new Label { Text = "No players assigned. Choose a candidate below.", FontSize = 13, TextColor = DynastyTheme.Muted });
        foreach (var (player, index) in depth.Select((p, i) => (p, i)))
            section.Children.Add(DepthPlayerCard(player, code, index + 1, depth.Count));
        var candidatePanel = new VerticalStackLayout { Spacing = 7 };
        candidatePanel.Children.Add(new Label { Text = "ADD / CHANGE POSITION", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = DynastyTheme.Accent });
        candidatePanel.Children.Add(new Label { Text = "A player has one assigned playing position. Moving them keeps their natural position, attributes and career profile.", FontSize = 12, TextColor = DynastyTheme.Muted });
        candidatePanel.Children.Add(_depthCandidateSearch);
        var candidates = roster.Where(p => DepthChartService.AssignedCode(state, p) != code &&
            (string.IsNullOrWhiteSpace(_depthCandidateSearch.Text) || p.FullName.Contains(_depthCandidateSearch.Text.Trim(), StringComparison.OrdinalIgnoreCase)))
            .OrderBy(p => p.IsRedshirted || p.CurrentInjury is not null).ThenByDescending(p => DepthChartService.PositionalRating(p, code)).ThenBy(p => p.FullName).ToArray();
        candidatePanel.Children.Add(new Label { Text = $"Best {Math.Min(12, candidates.Length)} of {candidates.Length} candidates · search to find anyone", FontSize = 11, TextColor = DynastyTheme.Muted });
        foreach (var player in candidates.Take(12)) candidatePanel.Children.Add(DepthPlayerCard(player, code, 0, depth.Count));
        _depthChartOrganizer.Children.Add(ResponsiveTableHeader(section, candidatePanel));
    }

    private View DepthPlayerCard(DynastyPlayer player, int code, int rank, int count)
    {
        var rating = DepthChartService.PositionalRating(player, code);
        var nativeCode = Ncaa06RatingService.PositionCode(player);
        var native = DepthChartService.PositionNames[nativeCode];
        var unavailable = player.IsRedshirted ? " · Redshirt" : player.CurrentInjury is not null ? " · Injured" : "";
        var row = new Grid { Padding = new Thickness(12, 10), ColumnSpacing = 10,
            ColumnDefinitions = new ColumnDefinitionCollection { new(30), new(GridLength.Star), new(48), new(GridLength.Auto) } };
        row.Add(new Label { Text = rank == 0 ? "↗" : rank.ToString(), FontSize = 17, FontAttributes = FontAttributes.Bold,
            VerticalTextAlignment = TextAlignment.Center, TextColor = DynastyTheme.Accent }, 0);
        row.Add(new VerticalStackLayout { Spacing = 3, Children =
        {
            new Label { Text = player.FullName, FontAttributes = FontAttributes.Bold, FontSize = 14 },
            new Label { Text = $"{native} · {FormatClassYear(player.ClassYear, player.IsRedshirted || player.HasRedshirted)} · Natural OVR {player.OverallRating}{unavailable}", FontSize = 11, TextColor = DynastyTheme.Muted }
        } }, 1);
        row.Add(new VerticalStackLayout { Spacing = 0, VerticalOptions = LayoutOptions.Center, Children =
        {
            new Label { Text = rating.ToString(), FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = DynastyTheme.Accent, HorizontalTextAlignment = TextAlignment.Center },
            new Label { Text = DepthChartService.PositionNames[code], FontSize = 10, TextColor = DynastyTheme.Muted, HorizontalTextAlignment = TextAlignment.Center }
        } }, 2);
        var actions = new VerticalStackLayout { Spacing = 2 };
        if (rank == 0)
        {
            var add = new Button { Text = "Assign", FontSize = 11, Padding = new Thickness(10, 5), IsEnabled = !player.IsRedshirted,
                AutomationId = "depth-assign-" + player.PlayerId.ToString("N") };
            add.Clicked += (_, _) => ApplyDepthAssignment(player.PlayerId, code, count + 1);
            actions.Children.Add(add);
        }
        else
        {
            foreach (var direction in new[] { -1, 1 })
            {
                var move = new Button { Text = direction < 0 ? "↑" : "↓", FontSize = 12, Padding = new Thickness(10, 1), MinimumHeightRequest = 32,
                    IsEnabled = direction < 0 ? rank > 1 : rank < count };
                SemanticProperties.SetDescription(move, $"Move {player.FullName} {(direction < 0 ? "up" : "down")} {DepthChartService.PositionNames[code]} depth");
                move.Clicked += (_, _) => ApplyDepthAssignment(player.PlayerId, code, rank + direction);
                actions.Children.Add(move);
            }
        }
        row.Add(actions, 3);
        var card = new Border { StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 12 }, Content = row,
            BackgroundColor = DynastyTheme.Palette(rank == 1 ? "#244B45" : "#243542"), Opacity = player.IsRedshirted ? .6 : 1 };
        if (rank > 0)
        {
            var drag = new DragGestureRecognizer { CanDrag = true };
            drag.DragStarting += (_, e) => e.Data.Properties["DepthPlayerId"] = player.PlayerId.ToString();
            var drop = new DropGestureRecognizer { AllowDrop = true };
            drop.Drop += (_, e) => { if (e.Data.Properties.TryGetValue("DepthPlayerId", out var value) && Guid.TryParse(value?.ToString(), out var id)) ApplyDepthAssignment(id, code, rank); };
            card.GestureRecognizers.Add(drag); card.GestureRecognizers.Add(drop);
        }
        return card;
    }

    private void ApplyDepthAssignment(Guid player, int code, int order)
    {
        if (_currentDynasty is null) return;
        _currentDynasty = DepthChartService.Assign(_currentDynasty, _currentDynasty.UserTeamName, player, code, order);
        RenderCurrentDynasty();
    }
}
