using DynastyManager.App;
using DynastyManager.Core.Models;
using ProFootball.Core;
namespace ProFootball.App;

public sealed partial class FranchisePage
{
    static string RoleName(StaffRole role) => role switch { StaffRole.HeadCoach => "Head coach", StaffRole.OffensiveCoordinator => "Offensive coordinator", StaffRole.DefensiveCoordinator => "Defensive coordinator", StaffRole.SpecialTeamsCoordinator => "Special teams coordinator", _ => role.ToString() };
    static string Money(long value) => "$" + (value / 1_000_000d).ToString("0.00") + "m";
    View Link(string text, Func<Task> action) { var b = Action(text, action); b.BackgroundColor = Colors.Transparent; b.FontSize = 12; b.Padding = 4; return b; }
    View Table((string Name, int Width)[] columns, IEnumerable<View[]> rows)
    {
        var grid = new Grid { RowSpacing = 1, ColumnSpacing = 8 }; foreach (var c in columns) grid.ColumnDefinitions.Add(new(c.Width));
        grid.RowDefinitions.Add(new(GridLength.Auto)); for (var c = 0; c < columns.Length; c++) grid.Add(Text(columns[c].Name, 12, true), c, 0);
        var row = 1; foreach (var cells in rows) { grid.RowDefinitions.Add(new(GridLength.Auto)); for (var c = 0; c < cells.Length; c++) { cells[c].VerticalOptions = LayoutOptions.Center; grid.Add(cells[c], c, row); } row++; }
        return new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = grid };
    }
    async Task Update(Func<ProFranchise, ProFranchise> mutation) { state = await Task.Run(() => mutation(state!)); await Save(); Render(); }
    async Task Player(ProPlayer original)
    {
        var p = state!.Players.Single(x => x.Football.PlayerId == original.Football.PlayerId); var c = state.Contracts.LastOrDefault(c => c.Active && c.PlayerId == p.Football.PlayerId);
        var choice = await GameDialogs.ActionSheetAsync(this, p.Football.FullName + " · " + p.Football.Position, "Close", null, "Player & contract", p.PracticeSquad ? "Promote to active" : "Move to practice squad", "Extend contract", "Franchise tag", "Release player");
        if (choice == "Player & contract") await GameDialogs.AlertAsync(this, p.Football.FullName, $"{p.Age} years · OVR {p.Football.OverallRating} · Morale {p.Morale}\n{p.RatingSource}\n" + (c is null ? "No contract" : $"Cap hit {Money(ProEconomy.CapHit(c, state.Year))}\nSigning bonus {Money(c.SigningBonus)}\n" + string.Join("\n", c.Years.Select(y => $"{y.Year}: {Money(y.Salary)} · guaranteed {Money(y.Guaranteed)}")) + "\n" + (c.Estimated ? "Initial contract is estimated." : "Franchise contract.")), "Close");
        if (choice == "Promote to active" || choice == "Move to practice squad") await Update(s => ProRoster.Practice(s, s.UserTeam, p.Football.PlayerId, !p.PracticeSquad));
        if (choice == "Extend contract") { var offer = await ContractTerms(p, true); if (offer is { } o) await Update(s => ProEconomy.Extend(s, s.UserTeam, p.Football.PlayerId, o.Years, o.Salary)); }
        if (choice == "Franchise tag") await Update(s => ProEconomy.Tag(s, s.UserTeam, p.Football.PlayerId));
        if (choice == "Release player" && await GameDialogs.AlertAsync(this, "Release player?", "Remaining guarantees and unamortized signing bonus will count as dead money. Waiver eligibility applies.", "Release", "Cancel")) await Update(s => ProEconomy.Release(s, s.UserTeam, p.Football.PlayerId));
    }
    async Task<(int Years, long Salary, long Bonus)?> ContractTerms(ProPlayer p, bool extension = false)
    {
        var years = await GameDialogs.ActionSheetAsync(this, "Contract length", "Cancel", null, "1 year", "2 years", "3 years", "4 years", "5 years"); if (string.IsNullOrEmpty(years) || years == "Cancel") return null;
        var salary = Math.Max(1_000_000, ProEconomy.MarketSalary(p)); var level = await GameDialogs.ActionSheetAsync(this, "Annual salary · market " + Money(salary), "Cancel", null, "Market", "110% of market", "125% of market"); if (level is null or "Cancel") return null;
        salary = (long)(salary * (level == "110% of market" ? 1.1 : level == "125% of market" ? 1.25 : 1)); long bonus = 0;
        if (!extension) { var b = await GameDialogs.ActionSheetAsync(this, "Signing bonus", "Cancel", null, "No bonus", "Half annual salary", "One annual salary"); if (b is null or "Cancel") return null; bonus = b == "Half annual salary" ? salary / 2 : b == "One annual salary" ? salary : 0; }
        return (int.Parse(years[..1]), salary, bonus);
    }
    void Staff()
    {
        var s = state!; var team = s.League.Teams.Single(t => t.Name == s.UserTeam); body.Add(Text("Staff ratings are game estimates. Changes open during the offseason.", 12));
        foreach (var c in s.Coaches.Where(c => c.TeamId == team.Id).OrderBy(c => c.Role))
        { var coach = c; var row = new ActionRow(); var hire = Action("Hire / replace", () => Hire(coach)); hire.IsEnabled = s.Phase == ProPhase.Offseason; row.Add(hire); var fire = Action("Dismiss", () => Update(x => ProStaff.Fire(x, x.UserTeam, coach.Role))); fire.IsEnabled = s.Phase == ProPhase.Offseason && !coach.Vacant; row.Add(fire); body.Add(Panel(RoleName(c.Role), Text(c.Name, 22, true), Text(c.Title, 12), Text($"Coaching {c.Rating} · Development {c.Development}"), row)); }
    }
    async Task Hire(ProCoach coach)
    { var names = state!.CoachCandidates.Select(c => c.Name).Distinct().Order().ToArray(); var name = await GameDialogs.ActionSheetAsync(this, "Available staff", "Cancel", null, names); if (name is not null && name != "Cancel") await Update(s => ProStaff.Hire(s, s.UserTeam, coach.Role, name)); }
    void FrontOffice()
    {
        var s = state!; body.Add(Panel("Salary cap", Text(Money(ProEconomy.Room(s, s.UserTeam)) + " available", 25, true), Text($"Cap {Money(s.SalaryCap)} · Used {Money(ProEconomy.Used(s, s.UserTeam))} · Pending bids {Money(ProEconomy.Pending(s, s.UserTeam))}"), Text("Initial contracts are estimated, not real-world contract figures. Bonuses prorate; release guarantees and bonus balances become dead money.", 12)));
        var row = new ActionRow(); row.Add(Action("Free agents", FreeAgents, "pro-free-agents")); row.Add(Action("Trade center", Trade)); row.Add(Action("Draft & scouting", Draft)); body.Add(row);
        var rosterTools = new ActionRow(); rosterTools.Add(Action("Auto fill roster", () => Update(x => ProCpu.Manage(x, true)))); rosterTools.Add(Action("Auto trim to 53", () => Update(x => ProCpu.Trim(x, x.UserTeam)))); body.Add(rosterTools);
        body.Add(Text("Contracts · select a player", 21, true)); body.Add(Table([("Player", 170), ("Ends", 55), ("Cap hit", 95), ("Bonus", 95)], s.Contracts.Where(c => c.Active && c.Team == s.UserTeam).OrderByDescending(c => ProEconomy.CapHit(c, s.Year)).Select(c => { var p = s.Players.Single(p => p.Football.PlayerId == c.PlayerId); return new View[] { Link(p.Football.FullName, () => Player(p)), Text(c.Years.Max(y => y.Year).ToString()), Text(Money(ProEconomy.CapHit(c, s.Year))), Text(Money(c.SigningBonus)) }; })));
        body.Add(Panel("Dead money", Text(string.Join("\n", s.DeadCap.Where(d => d.Team == s.UserTeam && d.Year == s.Year).Select(d => $"{s.Players.FirstOrDefault(p => p.Football.PlayerId == d.PlayerId)?.Football.FullName}: {Money(d.Amount)}")), 12)));
    }
    Task FreeAgents()
    {
        body.Clear(); body.Add(Action("‹ Front office", () => Open("Front office"))); body.Add(Text("Free agency & waivers", 25, true)); body.Add(Text("Offers compete with CPU bids when you advance. Pending offers reserve cap space.", 12));
        foreach (var p in state!.Players.Where(p => p.Football.TeamName == "Free agent" && !p.Retired).OrderByDescending(p => p.Football.OverallRating).Take(80))
        { var player = p; var waiver = state.Waivers.Any(w => w.PlayerId == p.Football.PlayerId); var pending = state.Bids.FirstOrDefault(b => b.Team == state.UserTeam && b.PlayerId == p.Football.PlayerId); body.Add(Panel(p.Football.FullName, Text($"{p.Football.Position} · Age {p.Age} · OVR {p.Football.OverallRating} · Market {Money(ProEconomy.MarketSalary(p))}", 12), Action(waiver ? "Submit waiver claim" : pending is null ? "Make offer" : "Revise pending offer", async () => { if (waiver) await Update(s => ProRoster.Claim(s, s.UserTeam, player.Football.PlayerId)); else { var offer = await ContractTerms(player); if (offer is { } o) await Update(s => ProEconomy.Offer(s, s.UserTeam, player.Football.PlayerId, o.Years, o.Salary, o.Bonus)); } }))); }
        return Task.CompletedTask;
    }
    async Task Trade()
    {
        var s = state!; var other = await GameDialogs.ActionSheetAsync(this, "Trade partner", "Cancel", null, s.League.Teams.Where(t => t.Name != s.UserTeam).Select(t => t.Name).ToArray()); if (other is null or "Cancel") return;
        var give = await TradeAsset(s.UserTeam); if (give is null) return; var get = await TradeAsset(other); if (get is null) return;
        if (await GameDialogs.AlertAsync(this, "Propose trade?", $"Give: {give.Value.Label}\nReceive: {get.Value.Label}\nCPU evaluation, cap and roster limits apply.", "Propose", "Cancel")) await Update(x => ProTrade.Propose(x, x.UserTeam, other, give.Value.Pick ? [] : [give.Value.Id], get.Value.Pick ? [] : [get.Value.Id], give.Value.Pick ? [give.Value.Id] : [], get.Value.Pick ? [get.Value.Id] : []));
    }
    async Task<(Guid Id, bool Pick, string Label)?> TradeAsset(string team)
    {
        var assets = state!.Players.Where(p => p.Football.TeamName == team && !p.Retired).Select(p => (Id: p.Football.PlayerId, Pick: false, Label: $"{p.Football.FullName} · {p.Football.Position} · {p.Football.OverallRating}"))
            .Concat(state.Picks.Where(p => p.Owner == team && ProTrade.Available(state, p)).Select(p => (Id: p.Id, Pick: true, Label: $"{p.Year} R{p.Round} · {p.OriginalTeam}"))).ToArray();
        var choice = await GameDialogs.ActionSheetAsync(this, team + " · choose asset", "Cancel", null, assets.Select(a => a.Label).ToArray()); return assets.Where(a => a.Label == choice).Select(a => ((Guid, bool, string)?)a).FirstOrDefault();
    }
    Task Draft()
    {
        body.Clear(); body.Add(Action("‹ Front office", () => Open("Front office"))); body.Add(Text("Draft & scouting", 25, true)); var s = state!; var current = ProDraft.Current(s);
        body.Add(Text(s.Phase == ProPhase.Offseason ? $"{s.OffseasonStep} · " + (current is null ? "Draft complete" : $"R{current.Round} #{current.Slot} · {current.Owner}") : "Scouting opens during the offseason."));
        if (s.OffseasonStep == OffseasonStep.Draft && s.Phase == ProPhase.Offseason) body.Add(Action("Auto draft remaining picks", async () => { state = await Task.Run(() => ProDraft.AutoUntilUser(state!, true)); await Save(); await Draft(); }));
        foreach (var p in s.Prospects.OrderByDescending(p => s.DraftBoard.Contains(p.Football.PlayerId)).ThenByDescending(p => p.Football.OverallRating).Take(80))
        {
            var player = p; var report = s.Scouting.FirstOrDefault(r => r.Team == s.UserTeam && r.PlayerId == p.Football.PlayerId); var actions = new ActionRow(); actions.Add(Action(report is null ? "Scout" : "Scouted", async () => { state = ProDraft.Scout(state!, state!.UserTeam, player.Football.PlayerId); await Save(); await Draft(); })); actions.Add(Action(s.DraftBoard.Contains(p.Football.PlayerId) ? "Remove from board" : "Add to board", async () => { state = state! with { DraftBoard = state.DraftBoard.Contains(player.Football.PlayerId) ? state.DraftBoard.Where(id => id != player.Football.PlayerId).ToList() : state.DraftBoard.Append(player.Football.PlayerId).ToList() }; await Save(); await Draft(); }));
            if (s.Phase == ProPhase.Offseason && s.OffseasonStep == OffseasonStep.Draft && current?.Owner == s.UserTeam) actions.Add(Action("Draft", async () => { state = ProDraft.AutoUntilUser(ProDraft.Pick(state!, player.Football.PlayerId)); await Save(); await Draft(); }));
            body.Add(Panel(p.Football.FullName, Text($"{p.Football.Position} · Age {p.Age} · " + (report is null ? "Unscouted" : $"Grade {report.Grade} · Confidence {report.Confidence}%"), 12), actions));
        }
        return Task.CompletedTask;
    }
}
