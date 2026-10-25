using DynastyManager.App;
using DynastyManager.Core.Models;
using ProFootball.Core;
namespace ProFootball.App;

public sealed partial class FranchisePage
{
    static string RoleName(StaffRole role) => role switch { StaffRole.HeadCoach => "Head coach", StaffRole.OffensiveCoordinator => "Offensive coordinator", StaffRole.DefensiveCoordinator => "Defensive coordinator", StaffRole.SpecialTeamsCoordinator => "Special teams coordinator", _ => role.ToString() };
    static string Money(long value) => "$" + (value / 1_000_000d).ToString("0.00") + "m";
    View Link(string text, Func<Task> action) { var b = Action(text, action); b.BackgroundColor = Colors.Transparent; b.FontSize = 12; b.Padding = 4; return b; }
    View Table((string Name, int Width)[] columns, IEnumerable<View[]> rows) => FootballDataGrid.Create(columns, rows);
    async Task Update(Func<ProFranchise, ProFranchise> mutation)
    {
        var before = state!; state = await Task.Run(() => mutation(before)); await Save(); Render();
        if (state.GmDecisions.Count > before.GmDecisions.Count && state.GmDecisions.Last() is { Approved: false } decision)
            await GameDialogs.AlertAsync(this, "GM decision", decision.Reason, "Close");
    }
    Task Manage(string action, Guid id, long commitment, Func<ProFranchise, ProFranchise> mutation) => Update(s => ProManagement.Submit(s, action, [id], commitment, mutation));
    async Task ManagePlayer(ProPlayer original)
    {
        var p = state!.Players.Single(x => x.Football.PlayerId == original.Football.PlayerId); var c = state.Contracts.LastOrDefault(c => c.Active && c.PlayerId == p.Football.PlayerId);
        var choice = await GameDialogs.ActionSheetAsync(this, p.Football.FullName + " · " + p.Football.Position, "Close", null, "Player & contract", p.PracticeSquad ? "Promote to active" : "Move to practice squad", "Extend contract", "Franchise tag", "Release player");
        if (choice == "Player & contract") await GameDialogs.AlertAsync(this, p.Football.FullName, $"{p.Age} years · OVR {p.Football.OverallRating} · Morale {p.Morale}\n" + (c is null ? "No contract" : $"Cap hit {Money(ProEconomy.CapHit(c, state.Year))}\nSigning bonus {Money(c.SigningBonus)}\n" + string.Join("\n", c.Years.Select(y => $"{y.Year}: {Money(y.Salary)} · guaranteed {Money(y.Guaranteed)}")) + "\n" + (c.Estimated ? "Initial contract is estimated." : "Franchise contract.")), "Close");
        if (choice == "Promote to active" || choice == "Move to practice squad") await Manage("Roster move", p.Football.PlayerId, 0, s => ProRoster.Practice(s, s.UserTeam, p.Football.PlayerId, !p.PracticeSquad));
        if (choice == "Extend contract") { var offer = await ContractTerms(p, true); if (offer is { } o) await Manage("Contract extension", p.Football.PlayerId, 0, s => ProEconomy.Extend(s, s.UserTeam, p.Football.PlayerId, o.Years, o.Salary)); }
        if (choice == "Franchise tag") await Manage("Franchise tag", p.Football.PlayerId, 0, s => ProEconomy.Tag(s, s.UserTeam, p.Football.PlayerId));
        if (choice == "Release player" && await GameDialogs.AlertAsync(this, "Release player?", "Remaining guarantees and unamortized signing bonus will count as dead money. Waiver eligibility applies.", "Release", "Cancel")) await Manage("Release player", p.Football.PlayerId, 0, s => ProEconomy.Release(s, s.UserTeam, p.Football.PlayerId));
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
        var s = state!; var team = s.League.Teams.Single(t => t.Name == s.UserTeam); var tools = new ActionRow(); tools.Add(Action("Game plan", GamePlan)); tools.Add(Action("Injuries & availability", InjuryReport)); body.Add(tools); body.Add(Text("Staff ratings are game estimates. Changes open during the offseason.", 12));
        foreach (var c in s.Coaches.Where(c => c.TeamId == team.Id).OrderBy(c => c.Role))
        { var coach = c; var row = new ActionRow(); var hire = Action("Hire / replace", () => Hire(coach)); hire.IsEnabled = s.Phase == ProPhase.Offseason && ProCareer.Role(s) == StaffRole.HeadCoach && !s.Career!.Unemployed && c.Role != ProCareer.Role(s); row.Add(hire); var fire = Action("Dismiss", () => Update(x => ProStaff.Fire(x, x.UserTeam, coach.Role))); fire.IsEnabled = s.Phase == ProPhase.Offseason && !coach.Vacant && ProCareer.Role(s) == StaffRole.HeadCoach && !s.Career!.Unemployed && coach.Role != ProCareer.Role(s); row.Add(fire); body.Add(Panel(RoleName(c.Role), CoachIdentity(c, team), Action("Coach profile", () => CoachProfile(coach)), Text(c.Title, 12), Text($"Coaching {c.Rating} · Development {c.Development}"), row)); }
    }
    async Task Hire(ProCoach coach)
    { var names = state!.CoachCandidates.Select(c => c.Name).Distinct().Order().ToArray(); var name = await GameDialogs.ActionSheetAsync(this, "Available staff", "Cancel", null, names); if (name is not null && name != "Cancel") await Update(s => ProStaff.Hire(s, s.UserTeam, coach.Role, name)); }
    void FrontOffice()
    {
        var s = state!; body.Add(Text($"Contract coverage · {s.Contracts.Count(c => c.Active && c.Team == s.UserTeam && c.Source != null)} sourced · {s.Contracts.Count(c => c.Active && c.Team == s.UserTeam && c.Estimated)} estimated", 12)); body.Add(Panel("Salary cap", Text(Money(ProEconomy.Room(s, s.UserTeam)) + " available", 25, true), Text($"Cap {Money(s.SalaryCap)} · Used {Money(ProEconomy.Used(s, s.UserTeam))} · Pending bids {Money(ProEconomy.Pending(s, s.UserTeam))}"), Text("Contracts are labeled by source. OTC snapshots retain reported cap charges; unmatched contracts are estimates. Rollover, team adjustments and historical dead money are not yet included.", 12)));
        var row = new ActionRow(); row.Add(Action("Free agents", FreeAgents, "pro-free-agents")); row.Add(Action("Trade center", Trade)); row.Add(Action("Trade history", TradeHistory)); row.Add(Action("Draft & scouting", Draft)); body.Add(row);
        var rosterTools = new ActionRow(); rosterTools.Add(Action("Ask GM to fill roster", () => Update(x => ProCpu.Manage(x, true)))); rosterTools.Add(Action("Ask GM to trim to 53", () => Update(x => ProCpu.Trim(x, x.UserTeam)))); body.Add(rosterTools);
        body.Add(Text("Contracts · select a player", 21, true)); body.Add(Table([("Player", 170), ("Ends", 55), ("Cap hit", 95), ("Bonus", 95)], s.Contracts.Where(c => c.Active && c.Team == s.UserTeam).OrderByDescending(c => ProEconomy.CapHit(c, s.Year)).Select(c => { var p = s.Players.Single(p => p.Football.PlayerId == c.PlayerId); return new View[] { Link(p.Football.FullName, () => Player(p)), Text(c.Years.Max(y => y.Year).ToString()), Text(Money(ProEconomy.CapHit(c, s.Year))), Text(Money(c.SigningBonus)) }; })));
        body.Add(Panel("Dead money", Text(string.Join("\n", s.DeadCap.Where(d => d.Team == s.UserTeam && d.Year == s.Year).Select(d => $"{s.Players.FirstOrDefault(p => p.Football.PlayerId == d.PlayerId)?.Football.FullName}: {Money(d.Amount)}")), 12)));
    }
    string freeAgentPosition = "All positions", freeAgentSearch = "";
    Task FreeAgents()
    {
        body.Clear(); body.Add(Action("‹ Front office", () => Open("Front office"))); body.Add(Text("Free agency & waivers", 25, true)); body.Add(Text("Offers compete when you advance. Pending bids reserve cap space.", 12));
        var positions = new[] { "All positions" }.Concat(Enum.GetNames<Position>()).ToArray();
        var position = new Picker { Title = "Position", ItemsSource = positions, SelectedIndex = Array.IndexOf(positions, freeAgentPosition), MinimumWidthRequest = 130 };
        var search = new Entry { Placeholder = "Find a free agent", Text = freeAgentSearch };
        var filters = new Grid { ColumnDefinitions = new() { new(GridLength.Star), new(GridLength.Auto) }, ColumnSpacing = 8 };
        filters.Add(search, 0, 0); filters.Add(position, 1, 0); body.Add(filters);
        var count = Text("", 12); body.Add(count); var list = new VerticalStackLayout { Spacing = 3 }; body.Add(list);
        void Draw()
        {
            list.Clear();
            var players = state!.Players.Where(p => p.Football.TeamName == "Free agent" && !p.Retired && ProCareer.Controls(state, p.Football.Position)
                && (freeAgentPosition == "All positions" || p.Football.Position.ToString() == freeAgentPosition)
                && (string.IsNullOrWhiteSpace(freeAgentSearch) || p.Football.FullName.Contains(freeAgentSearch.Trim(), StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(p => p.Football.OverallRating).ThenBy(p => p.Football.FullName).ToArray();
            count.Text = players.Length + " available · tap a heading to sort";
            list.Add(Table([("Pos",48),("Player",200),("Age",48),("OVR",52),("Market / yr",100),("Status",100),("Offer",110)], players.Select(p =>
            {
                var player = p; var waiver = state.Waivers.Any(w => w.PlayerId == p.Football.PlayerId);
                var pending = state.Bids.FirstOrDefault(b => b.Team == state.UserTeam && b.PlayerId == p.Football.PlayerId);
                return new View[] { Text(p.Football.Position.ToString()), Link(p.Football.FullName, () => Player(player)), Text(p.Age.ToString()), Text(p.Football.OverallRating.ToString()), Text(Money(ProEconomy.MarketSalary(p))), Text(waiver ? "Waivers" : pending is null ? "Available" : "Bid pending"), Action(waiver ? "Claim" : pending is null ? "Make offer" : "Revise bid", async () =>
                {
                    if (waiver) await Manage("Waiver claim", player.Football.PlayerId, 0, s => ProRoster.Claim(s,s.UserTeam,player.Football.PlayerId));
                    else { var offer = await ContractTerms(player); if (offer is { } o) await Manage("Free-agent offer",player.Football.PlayerId,o.Salary + o.Bonus/o.Years,s => ProEconomy.Offer(s,s.UserTeam,player.Football.PlayerId,o.Years,o.Salary,o.Bonus)); }
                    await FreeAgents();
                }) };
            })));
            if (players.Length == 0) list.Add(Text("No available players match these filters."));
        }
        search.TextChanged += (_, _) => { freeAgentSearch = search.Text ?? ""; Draw(); };
        position.SelectedIndexChanged += (_, _) => { freeAgentPosition = (string?)position.SelectedItem ?? "All positions"; Draw(); };
        Draw(); return Task.CompletedTask;
    }
    Task TradeHistory()
    {
        var s = state!;
        string Names(IEnumerable<Guid> ids) => string.Join(", ", ids.Select(id => s.Players.FirstOrDefault(p => p.Football.PlayerId == id)?.Football.FullName ?? id.ToString()));
        var rows = s.TradeHistory.AsEnumerable().Reverse().Select(t => Panel($"{t.Year} W{t.Week} · {t.From} / {t.To}",
            Text(t.From + " sent: " + Names(t.GivenPlayers) + " " + string.Join(", ", t.GivenPicks.Select(p => $"{p.Year} R{p.Round} ({p.OriginalTeam})"))),
            Text(t.To + " sent: " + Names(t.ReceivedPlayers) + " " + string.Join(", ", t.ReceivedPicks.Select(p => $"{p.Year} R{p.Round} ({p.OriginalTeam})"))))).ToArray();
        return ShowPage("Trade history", rows.Length == 0 ? Text("No completed trades yet.") : new VerticalStackLayout { Spacing = 12, Children = { } }, Panel("Completed deals", rows));
    }
    async Task Trade()
    {
        var s = state!; var other = await GameDialogs.ActionSheetAsync(this, "Trade partner", "Cancel", null, s.League.Teams.Where(t => t.Name != s.UserTeam).Select(t => t.Name).ToArray()); if (other is null or "Cancel") return;
        var give = await TradePackage(s.UserTeam, other); if (give is null) return;
        var get = await TradePackage(other, s.UserTeam); if (get is null) return;
        if (!await GameDialogs.AlertAsync(this, "Propose trade?", "Give: " + string.Join("; ", give.Select(a => a.Label)) + "\nReceive: " + string.Join("; ", get.Select(a => a.Label)) + "\nCPU evaluation, cap and roster limits apply.", "Propose", "Cancel")) return;
        await Update(x => ProManagement.Submit(x, "Trade recommendation", give.Concat(get).Where(a => !a.Pick).Select(a => a.Id).ToArray(), 0,
            y => ProTrade.Propose(y, y.UserTeam, other, give.Where(a => !a.Pick).Select(a => a.Id).ToArray(), get.Where(a => !a.Pick).Select(a => a.Id).ToArray(), give.Where(a => a.Pick).Select(a => a.Id).ToArray(), get.Where(a => a.Pick).Select(a => a.Id).ToArray()), sharedPicks: give.Concat(get).Any(a => a.Pick)));
    }
    async Task<List<(Guid Id, bool Pick, string Label)>?> TradePackage(string team, string recipient)
    {
        var selected = new List<(Guid Id, bool Pick, string Label)>();
        while (true)
        {
            var choice = await GameDialogs.ActionSheetAsync(this, team + " · " + selected.Count + " assets", "Cancel", null, "Add player or pick", "Remove asset", "Package ready");
            if (choice is null or "Cancel") return null;
            if (choice == "Package ready") return selected;
            if (choice == "Remove asset")
            {
                var remove = await GameDialogs.ActionSheetAsync(this, "Remove asset", "Cancel", null, selected.Select(a => a.Label).ToArray());
                selected.RemoveAll(a => a.Label == remove); continue;
            }
            var assets = state!.Players.Where(p => p.Football.TeamName == team && !p.Retired && ProCareer.Controls(state, p.Football.Position)).Select(p => (Id: p.Football.PlayerId, Pick: false, Label: $"{p.Football.FullName} · {p.Football.Position} · {p.Football.OverallRating} · Value {ProTrade.PlayerValue(state, p, recipient):0}"))
                .Concat(state.Picks.Where(p => p.Owner == team && ProTrade.Available(state, p)).Select(p => (Id: p.Id, Pick: true, Label: $"{p.Year} R{p.Round} · {p.OriginalTeam} · Value {ProTrade.PickValue(state, p):0}")))
                .Where(a => !selected.Any(v => v.Id == a.Id)).ToArray();
            var add = await GameDialogs.ActionSheetAsync(this, "Add asset", "Cancel", null, assets.Select(a => a.Label).ToArray());
            if (assets.FirstOrDefault(a => a.Label == add) is var asset && asset.Id != Guid.Empty) selected.Add(asset);
        }
    }
    Task Draft()
    {
        body.Clear(); body.Add(Action("‹ Front office", () => Open("Front office"))); body.Add(Text("Draft & scouting", 25, true)); var s = state!; var current = ProDraft.Current(s);
        body.Add(Text(s.Phase == ProPhase.Offseason ? $"{s.OffseasonStep} · " + (current is null ? "Draft complete" : $"R{current.Round} #{current.Slot} · {current.Owner}") : "Scouting opens during the offseason."));
        if (s.OffseasonStep == OffseasonStep.Draft && s.Phase == ProPhase.Offseason) body.Add(Action("Auto draft remaining picks", async () => { state = await Task.Run(() => ProDraft.AutoUntilUser(state!, true)); await Save(); await Draft(); }));
        foreach (var p in s.Prospects.OrderByDescending(p => s.DraftBoard.Contains(p.Football.PlayerId)).ThenByDescending(p => p.Football.OverallRating).Take(80))
        {
            var player = p; var report = s.Scouting.FirstOrDefault(r => r.Team == s.UserTeam && r.PlayerId == p.Football.PlayerId); var actions = new ActionRow(); actions.Add(Action(report is null ? "Scout" : "Scouted", async () => { ProCareer.RequirePlayer(state!, player.Football.PlayerId); state = ProDraft.Scout(state!, state!.UserTeam, player.Football.PlayerId); await Save(); await Draft(); })); actions.Add(Action(s.DraftBoard.Contains(p.Football.PlayerId) ? "Remove from board" : "Add to board", async () => { state = state! with { DraftBoard = state.DraftBoard.Contains(player.Football.PlayerId) ? state.DraftBoard.Where(id => id != player.Football.PlayerId).ToList() : state.DraftBoard.Append(player.Football.PlayerId).ToList() }; await Save(); await Draft(); }));
            if (s.Phase == ProPhase.Offseason && s.OffseasonStep == OffseasonStep.Draft && current?.Owner == s.UserTeam) actions.Add(Action("Draft", async () => { state = ProDraft.AutoUntilUser(ProManagement.Submit(state!, "Draft recommendation", [player.Football.PlayerId], 0, x => ProDraft.Pick(x, player.Football.PlayerId), true)); await Save(); await Draft(); }));
            body.Add(Panel(p.Football.FullName, Text($"{p.Football.Position} · Age {p.Age} · " + (report is null ? "Unscouted" : $"Grade {report.Grade} · Confidence {report.Confidence}%"), 12), actions));
        }
        return Task.CompletedTask;
    }
}
