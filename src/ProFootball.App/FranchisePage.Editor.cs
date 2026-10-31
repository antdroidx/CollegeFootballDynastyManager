using DynastyManager.App;
using DynastyManager.Core.Models;
using Microsoft.Maui.Controls;
using ProFootball.Core;
namespace ProFootball.App;

public sealed partial class FranchisePage
{
    string editorEntity = "";
    Task Editor()
    {
        var s = state!;
        var club = s.League.Teams.Single(t => t.Name == s.UserTeam);
        var players = s.Players.Where(p => p.Football.TeamName == s.UserTeam && !p.Retired).OrderBy(p => p.Football.FullName).ToArray();
        var coaches = s.Coaches.Where(c => c.TeamId == club.Id).OrderBy(c => c.Role).ToArray();
        var options = players.Select(p => "Player · " + p.Football.FullName)
            .Concat(coaches.Select(c => "Staff · " + RoleName(c.Role) + " · " + c.Name)).ToArray();
        if (!options.Contains(editorEntity)) editorEntity = options.FirstOrDefault() ?? "";
        var entity = new Picker { Title = "Player or staff", ItemsSource = options, SelectedIndex = Math.Max(0, Array.IndexOf(options, editorEntity)), AutomationId = "pro-editor-entity" };
        var host = new VerticalStackLayout { Spacing = 10 };
        body.Add(PageHeader("Editor", "Adjust player attributes, positions and staff ratings. Changes save immediately."));
        body.Add(entity); body.Add(host);

        View Field(string label, string value, out Entry entry)
        {
            entry = new Entry { Text = value };
            return new VerticalStackLayout { Spacing = 2, Children = { new Label { Text = label, TextColor = DynastyTheme.Muted, FontSize = 11 }, entry } };
        }
        View Number(string label, int value, out Entry entry) => Field(label, value.ToString(), out entry);
        void Draw()
        {
            host.Clear();
            var selected = editorEntity;
            if (selected.StartsWith("Player · "))
            {
                var player = players.First(p => "Player · " + p.Football.FullName == selected);
                var f = player.Football;
                var position = new Picker { Title = "Position", ItemsSource = PositionLabels.Names.ToArray(), SelectedIndex = Array.FindIndex(PositionLabels.Names.ToArray(), n => n == f.Position.DisplayName()) };
                var name = Field("Name", f.FullName, out var nameEntry);
                var ovr = Number("Overall", f.OverallRating, out var ovrEntry);
                var potential = Number("Potential", f.PotentialRating, out var potentialEntry);
                var morale = Number("Morale", player.Morale, out var moraleEntry);
                var apply = Action("Apply player changes", async () =>
                {
                    var target = Enum.GetValues<Position>().First(pos => pos.DisplayName() == (string)position.SelectedItem);
                    if (target != f.Position) state = ProPlayerPositions.Convert(state!, f.PlayerId, target);
                    state = state! with
                    {
                        Players = state.Players.Select(p => p.Football.PlayerId != f.PlayerId ? p : p with
                        {
                            Morale = Math.Clamp(Parse(moraleEntry.Text, player.Morale), 1, 99),
                            Football = p.Football with
                            {
                                FullName = string.IsNullOrWhiteSpace(nameEntry.Text) ? p.Football.FullName : nameEntry.Text.Trim(),
                                OverallRating = Math.Clamp(Parse(ovrEntry.Text, p.Football.OverallRating), 1, 99),
                                PotentialRating = Math.Clamp(Parse(potentialEntry.Text, p.Football.PotentialRating), 1, 99)
                            }
                        }).ToList()
                    };
                    await Save(); await Editor();
                }, "pro-editor-apply-player");
                host.Add(SectionCard("Player", f.FullName, name, position, StatRow(ovr, potential, morale), apply));
            }
            else if (selected.StartsWith("Staff · "))
            {
                var coach = coaches.First(c => "Staff · " + RoleName(c.Role) + " · " + c.Name == selected);
                var p = ProStaff.Profile(coach);
                var name = Field("Name", coach.Name, out var nameEntry);
                var rating = Number("Overall", p.Coaching, out var ratingEntry);
                var development = Number("Development", p.Development, out var developmentEntry);
                var offense = Number("Offense", p.Offense, out var offenseEntry);
                var defense = Number("Defense", p.Defense, out var defenseEntry);
                var specials = Number("Special teams", p.SpecialTeams, out var specialsEntry);
                var leadership = Number("Leadership", p.Leadership, out var leadershipEntry);
                var offScheme = Field("Preferred offense", p.OffenseScheme, out var offEntry);
                var defScheme = Field("Preferred defense", p.DefenseScheme, out var defEntry);
                var apply = Action("Apply staff changes", async () =>
                {
                    state = state! with
                    {
                        Coaches = state.Coaches.Select(c => c != coach ? c : c with
                        {
                            Name = string.IsNullOrWhiteSpace(nameEntry.Text) ? c.Name : nameEntry.Text.Trim(),
                            Rating = Clamp(ratingEntry.Text, p.Coaching),
                            Development = Clamp(developmentEntry.Text, p.Development),
                            OffenseRating = Clamp(offenseEntry.Text, p.Offense),
                            DefenseRating = Clamp(defenseEntry.Text, p.Defense),
                            SpecialTeamsRating = Clamp(specialsEntry.Text, p.SpecialTeams),
                            Leadership = Clamp(leadershipEntry.Text, p.Leadership),
                            PreferredOffense = string.IsNullOrWhiteSpace(offEntry.Text) ? p.OffenseScheme : offEntry.Text.Trim(),
                            PreferredDefense = string.IsNullOrWhiteSpace(defEntry.Text) ? p.DefenseScheme : defEntry.Text.Trim()
                        }).ToList()
                    };
                    await Save(); await Editor();
                }, "pro-editor-apply-staff");
                host.Add(SectionCard("Staff", coach.Title, name, StatRow(rating, development, leadership),
                    offense, defense, specials, offScheme, defScheme, apply));
            }
        }
        entity.SelectedIndexChanged += (_, _) => { if (entity.SelectedIndex >= 0) { editorEntity = options[entity.SelectedIndex]; Draw(); } };
        Draw();
        return Task.CompletedTask;
    }
    static int Parse(string? text, int fallback) => int.TryParse(text, out var v) ? v : fallback;
    static int Clamp(string? text, int fallback) => Math.Clamp(Parse(text, fallback), 1, 99);
}