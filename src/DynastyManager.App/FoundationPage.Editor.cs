using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Persistence;
using Microsoft.Maui.Controls;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private View BuildEditorEntry()
    {
        var open = new Button { Text = "Open player & staff editor", FontSize = 12 };
        open.Clicked += async (_, _) => await ShowEditorAsync();
        return new VerticalStackLayout { Spacing = 6, Children = {
            new Label { Text = "Adjust player attributes, change positions and edit staff ratings. Changes save immediately.", FontSize = 12, TextColor = DynastyTheme.Muted },
            open } };
    }
    private async Task ConvertPlayerPositionAsync(Guid playerId, Position target)
    {
        if (_currentDynasty is null) return;
        var player = _currentDynasty.ActiveRoster.FirstOrDefault(p => p.PlayerId == playerId && p.TeamName == _currentDynasty.UserTeamName);
        if (player is null) return;
        if (player.Position == target) return;
        var converted = PlayerPositionConversion.Convert(player, target);
        _currentDynasty = _currentDynasty with
        {
            ActiveRoster = _currentDynasty.ActiveRoster.Select(p => p.PlayerId == playerId ? converted : p).ToArray(),
            DepthChartAssignments = _currentDynasty.DepthChartAssignments.Where(a => a.PlayerId != playerId).ToArray()
        };
        await SaveCurrentAsync(SaveKind.AutosaveCurrent);
        RenderCurrentDynasty();
    }

    private async Task ShowEditorAsync()
    {
        if (_currentDynasty is null) return;
        var state = _currentDynasty;
        var players = state.ActiveRoster.Where(p => p.TeamName == state.UserTeamName).OrderBy(p => p.FullName).ToArray();
        var staff = state.Staff.Where(s => s.TeamName == state.UserTeamName).OrderBy(s => s.Role).ToArray();
        var options = players.Select(p => "Player · " + p.FullName)
            .Concat(staff.Select(s => "Staff · " + FormatStaffRole(s.Role) + " · " + s.FullName)).ToArray();
        if (options.Length == 0) { await GameDialogs.AlertAsync(this, "Editor", "Create or load a dynasty first.", "OK"); return; }
        var pageHost = new VerticalStackLayout { Padding = 14, Spacing = 10 };
        var close = new Button { Text = "‹ Back" };
        pageHost.Add(close);
        pageHost.Add(SectionTitle("Editor", "Adjust players, positions and staff ratings. Changes save immediately."));
        var entity = new Picker { Title = "Player or staff", ItemsSource = options, SelectedIndex = 0 };
        pageHost.Add(entity);
        var body = new VerticalStackLayout { Spacing = 10 };
        pageHost.Add(body);
        View Input(string label, string value, out Entry entry)
        {
            entry = new Entry { Text = value };
            return new VerticalStackLayout { Children = { new Label { Text = label, TextColor = DynastyTheme.Muted, FontSize = 11 }, entry } };
        }
        void Draw()
        {
            body.Children.Clear();
            var selected = options[entity.SelectedIndex];
            if (selected.StartsWith("Player · "))
            {
                var player = players.First(p => "Player · " + p.FullName == selected);
                var name = Input("Name", player.FullName, out var nameEntry);
                var position = new Picker { Title = "Position", ItemsSource = PositionLabels.Names.ToArray(), SelectedIndex = Array.FindIndex(PositionLabels.Names.ToArray(), n => n == player.Position.DisplayName()) };
                var ovr = Input("Overall", player.OverallRating.ToString(), out var ovrEntry);
                var awareness = Input("Awareness", player.AwarenessRating.ToString(), out var awarenessEntry);
                var apply = new Button { Text = "Apply player changes" };
                apply.Clicked += async (_, _) =>
                {
                    var target = Enum.GetValues<Position>().First(p => p.DisplayName() == (string)position.SelectedItem);
                    if (target != player.Position) await ConvertPlayerPositionAsync(player.PlayerId, target);
                    if (_currentDynasty is null) return;
                    _currentDynasty = _currentDynasty with
                    {
                        ActiveRoster = _currentDynasty.ActiveRoster.Select(p => p.PlayerId != player.PlayerId ? p : p with
                        {
                            FullName = string.IsNullOrWhiteSpace(nameEntry.Text) ? p.FullName : nameEntry.Text.Trim(),
                            OverallRating = Math.Clamp(ParseEditor(ovrEntry.Text, p.OverallRating), 1, 99),
                            AwarenessRating = Math.Clamp(ParseEditor(awarenessEntry.Text, p.AwarenessRating), 1, 99)
                        }).ToArray()
                    };
                    await SaveCurrentAsync(SaveKind.AutosaveCurrent);
                    RenderCurrentDynasty();
                };
                body.Children.Add(CoachingCard("PLAYER", name, position, ovr, awareness, apply));
            }
            else
            {
                var member = staff.First(s => "Staff · " + FormatStaffRole(s.Role) + " · " + s.FullName == selected);
                var name = Input("Name", member.FullName, out var nameEntry);
                var ovr = new Label { Text = $"Overall {member.OverallRating} · derived from traits", FontSize = 12, TextColor = DynastyTheme.Muted };
                var leadership = Input("Leadership", member.Leadership.ToString(), out var leadershipEntry);
                var development = Input("Development", member.PlayerDevelopment.ToString(), out var developmentEntry);
                var recruiting = Input("Recruiting", member.Recruiting.ToString(), out var recruitingEntry);
                var scheme = Input("Scheme", member.Scheme.ToString(), out var schemeEntry);
                var apply = new Button { Text = "Apply staff changes" };
                apply.Clicked += async (_, _) =>
                {
                    if (_currentDynasty is null) return;
                    _currentDynasty = _currentDynasty with
                    {
                        Staff = _currentDynasty.Staff.Select(s => s.StaffId != member.StaffId ? s : s with
                        {
                            FullName = string.IsNullOrWhiteSpace(nameEntry.Text) ? s.FullName : nameEntry.Text.Trim(),
                            Leadership = Math.Clamp(ParseEditor(leadershipEntry.Text, s.Leadership), 1, 99),
                            PlayerDevelopment = Math.Clamp(ParseEditor(developmentEntry.Text, s.PlayerDevelopment), 1, 99),
                            Recruiting = Math.Clamp(ParseEditor(recruitingEntry.Text, s.Recruiting), 1, 99),
                            Scheme = Math.Clamp(ParseEditor(schemeEntry.Text, s.Scheme), 1, 99)
                        }).ToArray()
                    };
                    await SaveCurrentAsync(SaveKind.AutosaveCurrent);
                    RenderCurrentDynasty();
                };
                body.Children.Add(CoachingCard("STAFF", name, ovr, leadership, development, recruiting, scheme, apply));
            }
        }
        entity.SelectedIndexChanged += (_, _) => Draw();
        Draw();
        close.Clicked += async (_, _) => await Navigation.PopModalAsync();
        await Navigation.PushModalAsync(new ContentPage { Content = new ScrollView { Content = pageHost } });
    }

    private static int ParseEditor(string? text, int fallback) => int.TryParse(text, out var value) ? value : fallback;
}