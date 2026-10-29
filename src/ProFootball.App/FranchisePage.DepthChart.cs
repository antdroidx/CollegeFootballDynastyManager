using DynastyManager.App;
using DynastyManager.Core.Simulation;
using ProFootball.Core;
namespace ProFootball.App;

public sealed partial class FranchisePage
{
    Task DepthChart()
    {
        var team = state!.UserTeam;
        var picker = new Picker { Title = "Playing position", AutomationId = "pro-depth-position", ItemsSource = Enumerable.Range(0, 21).Select(ProDepthChart.Label).ToArray() };
        var search = new Entry { Placeholder = "Find any active player", AutomationId = "pro-depth-search" };
        var assigned = new VerticalStackLayout { Spacing = 6 };
        var candidates = new VerticalStackLayout { Spacing = 6 };
        var message = Text("Hold ≡ and drag onto a depth slot. Use Move to for a long move. Assign any active player; their natural position stays unchanged.", 12);
        var changing = false;
        async Task Apply(Guid id, int code, int rank)
        {
            if (changing) return;
            changing = true; var previous = state!;
            try
            {
                state = ProDepthChart.Assign(previous, team, id, code, rank);
                await Save(); Draw(); Render();
            }
            catch (Exception e)
            {
                state = previous;
                await GameDialogs.AlertAsync(this, "Unable to change depth", e.Message, "OK");
            }
            finally { changing = false; }
        }
        bool Allowed(ProPlayer p, int code) => ProCareer.Controls(state!, p.Football.Position) && ProCareer.Controls(state!, DepthChartService.PositionForCode(ProDepthChart.AssignedCode(state!, p))) && ProCareer.Controls(state!, DepthChartService.PositionForCode(code));
        View Card(ProPlayer p, int code, int rank, int count)
        {
            var id = p.Football.PlayerId; var enabled = Allowed(p, code);
            var handle = Text("≡", 24); handle.AutomationId = "pro-depth-drag-" + id; handle.WidthRequest = 34;
            var drag = new DragGestureRecognizer { CanDrag = enabled };
            drag.DragStarting += (_, e) => { e.Data.Properties["DepthPlayerId"] = id.ToString(); e.Data.Properties["DepthTeam"] = team; };
            handle.GestureRecognizers.Add(drag);
            var name = Link(p.Football.FullName, () => Player(p));
            var details = new VerticalStackLayout { Spacing = 2, Children = { name, Text($"Natural {OffensiveLineService.Label(p.Football)} · OVR {p.Football.OverallRating} · {ProDepthChart.Label(code)} rating {DepthChartService.PositionalRating(p.Football, code)}", 11) } };
            if (!ProReserve.MedicallyAvailable(state!, p)) details.Add(Text("Injured · unavailable for games", 11));
            var move = new Button { Text = rank > 0 ? $"{rank} · Move to…" : "Assign", IsEnabled = enabled, FontSize = 12, Padding = 6, BackgroundColor = DynastyTheme.Panel, TextColor = DynastyTheme.Text, AutomationId = "pro-depth-move-" + id };
            move.Clicked += async (_, _) =>
            {
                if (rank == 0) { await Apply(id, code, count + 1); return; }
                var choice = await GameDialogs.ActionSheetAsync(this, "Move " + p.Football.FullName, "Cancel", null, Enumerable.Range(1, count).Select(i => "Depth " + i).ToArray());
                if (choice?.StartsWith("Depth ") == true && int.TryParse(choice[6..], out var target)) await Apply(id, code, target);
            };
            var grid = new Grid { ColumnDefinitions = new() { new(34), new(GridLength.Star), new(GridLength.Auto) }, ColumnSpacing = 6 };
            grid.Add(handle, 0); grid.Add(details, 1); grid.Add(move, 2);
            var card = new Border { Content = grid, Padding = 8, BackgroundColor = DynastyTheme.Surface, AutomationId = "pro-depth-card-" + id };
            if (rank > 0)
            {
                var drop = new DropGestureRecognizer { AllowDrop = enabled };
                drop.Drop += async (_, e) =>
                {
                    if (e.Data.Properties.TryGetValue("DepthTeam", out var owner) && owner as string == team && e.Data.Properties.TryGetValue("DepthPlayerId", out var value) && Guid.TryParse(value as string, out var source)) await Apply(source, code, rank);
                };
                card.GestureRecognizers.Add(drop);
            }
            return card;
        }
        void Draw()
        {
            assigned.Clear(); candidates.Clear(); var code = picker.SelectedIndex; if (code < 0) return;
            var players = ProDepthChart.AtPosition(state!, team, code);
            assigned.Add(Text($"{ProDepthChart.Label(code)} depth · {players.Count}", 19, true));
            for (var i = 0; i < players.Count; i++) assigned.Add(Card(players[i], code, i + 1, players.Count));
            if (players.Count == 0) assigned.Add(Text("No players assigned. Choose a player below."));
            var available = state!.Players.Where(p => ProDepthChart.Active(p) && p.Football.TeamName == team && ProDepthChart.AssignedCode(state, p) != code && (string.IsNullOrWhiteSpace(search.Text) || p.Football.FullName.Contains(search.Text, StringComparison.OrdinalIgnoreCase))).OrderByDescending(p => DepthChartService.PositionalRating(p.Football, code)).ThenBy(p => p.Football.FullName).ToArray();
            candidates.Add(Text($"Other active players · {available.Length}", 19, true));
            foreach (var p in available) candidates.Add(Card(p, code, 0, players.Count));
        }
        picker.SelectedIndexChanged += (_, _) => Draw(); search.TextChanged += (_, _) => Draw(); picker.SelectedIndex = 0;
        return ShowPage("Depth chart", message, picker, assigned, search, candidates);
    }
}
