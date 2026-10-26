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
        var choice = await GameDialogs.ActionSheetAsync(this, p.Football.FullName + " · " + p.Football.Position, "Close", null, "Player & contract", p.PracticeSquad ? "Promote to active" : "Move to practice squad", "Extend contract", "Restructure contract", "Franchise tag", state.TradeBlock.Contains(p.Football.PlayerId) ? "Remove from trading block" : "Add to trading block", "Release player");
        if (choice == "Player & contract") { await ShowPage("Player contract",ContractDetails(p,c),ContractComparisons(p)); }

        if (choice == "Promote to active" || choice == "Move to practice squad") await Manage("Roster move", p.Football.PlayerId, 0, s => ProRoster.Practice(s, s.UserTeam, p.Football.PlayerId, !p.PracticeSquad));
        if (choice == "Extend contract") { var offer = await ContractTerms(p, true); if (offer is { } o) await Manage("Contract extension", p.Football.PlayerId, 0, s => ProEconomy.Extend(s, s.UserTeam, p.Football.PlayerId, o.Years, o.Salary, o.Bonus)); }
        if(choice=="Restructure contract") await RestructureContract(p);
        if (choice is "Add to trading block" or "Remove from trading block") await Update(s => ProTradeMarket.List(s,s.UserTeam,p.Football.PlayerId,choice == "Add to trading block"));
        if (choice == "Franchise tag") await Manage("Franchise tag", p.Football.PlayerId, 0, s => ProEconomy.Tag(s, s.UserTeam, p.Football.PlayerId));
        if (choice == "Release player" && await ConfirmRelease(p)) await Manage("Release player", p.Football.PlayerId, 0, s => ProEconomy.Release(s, s.UserTeam, p.Football.PlayerId));
    }
    void Staff()
    {
        var s = state!; var team = s.League.Teams.Single(t => t.Name == s.UserTeam); var tools = new ActionRow(); tools.Add(Action("Game plan", GamePlan)); tools.Add(WeeklyTalkButton()); tools.Add(Action("Injuries & availability", InjuryReport)); body.Add(tools); body.Add(Text(ProCareer.Role(s)==StaffRole.HeadCoach?"Manage your coordinators throughout the season. Your own position is managed through Career.":"Staff appointments are handled by the head coach. Manage your own role through Career.",12));
        foreach (var c in s.Coaches.Where(c => c.TeamId == team.Id).OrderBy(c => c.Role))
        { var coach = c; var row = new ActionRow(); var hire = Action("Hire / replace", () => Hire(coach)); hire.IsEnabled = ProCareer.Role(s) == StaffRole.HeadCoach && !s.Career!.Unemployed && c.Role != ProCareer.Role(s); row.Add(hire); var fire = Action("Dismiss", () => Update(x => ProStaff.Fire(x, x.UserTeam, coach.Role))); fire.IsEnabled = !coach.Vacant && ProCareer.Role(s) == StaffRole.HeadCoach && !s.Career!.Unemployed && coach.Role != ProCareer.Role(s); row.Add(fire); body.Add(Panel(RoleName(c.Role), CoachIdentity(c, team), Action("Coach profile", () => CoachProfile(coach)), Text(c.Title, 12), Text($"Coaching {c.Rating} · Development {c.Development}"), row)); }
    }
    async Task Hire(ProCoach coach)
    { var names = state!.CoachCandidates.Select(c => c.Name).Distinct().Order().ToArray(); if(names.Length==0){await GameDialogs.AlertAsync(this,"Available staff","No coaches are currently available.","OK");return;} var name = await GameDialogs.ActionSheetAsync(this, "Available staff", "Cancel", null, names); if (name is not null && name != "Cancel") await Update(s => ProStaff.Hire(s, s.UserTeam, coach.Role, name)); }
    void FrontOffice()
    {
        var s = state!; body.Add(CapSummary(s));
        var row = new ActionRow(); row.Add(Action("Financial overview",FinanceHub)); row.Add(Action("Free agents", FreeAgents, "pro-free-agents")); row.Add(Action("Trade center", () => Trade())); row.Add(Action("Trading block", TradingBlock)); row.Add(Action("Trade history", TradeHistory)); row.Add(Action("Draft & scouting", Draft)); row.Add(Action("Expiring contracts", Retention)); body.Add(row);
        var rosterTools = new ActionRow(); rosterTools.Add(Action("Ask GM to fill roster", () => Update(x => ProCpu.Manage(x, true)))); rosterTools.Add(Action("Ask GM to trim to 53", () => Update(x => ProCpu.Trim(x, x.UserTeam)))); body.Add(rosterTools);
        body.Add(Text("Contracts · select a player", 21, true)); body.Add(Table([("Player", 170), ("Ends", 55), ("Salary",100), ("Bonus / season",110), ("Cap hit", 100)], s.Contracts.Where(c => c.Active && c.Team == s.UserTeam).OrderByDescending(c => ProEconomy.CapHit(c, s.Year)).Select(c => { var p = s.Players.Single(p => p.Football.PlayerId == c.PlayerId); return new View[] { Link(p.Football.FullName, () => Player(p)), Text(ProContractInsights.PlayingEnd(c).ToString()), Text(Money(c.Years.FirstOrDefault(y=>y.Year==s.Year)?.Salary??0)), Text(Money(ProEconomy.BonusCharge(c,s.Year))),Text(Money(ProEconomy.CapHit(c, s.Year))) }; })));
        body.Add(Panel("Dead money", Text(string.Join("\n", s.DeadCap.Where(d => d.Team == s.UserTeam && d.Year == s.Year).Select(d => $"{s.Players.FirstOrDefault(p => p.Football.PlayerId == d.PlayerId)?.Football.FullName}: {Money(d.Amount)}")), 12)));
    }
    string freeAgentPosition = "All positions", freeAgentSearch = "";
    Task FreeAgents()
    {
        body.Clear(); body.Add(Action("‹ Front Office", () => Open("Front Office"))); body.Add(Text("Free agency & waivers", 25, true)); body.Add(Text("Offers compete when you advance. Pending bids reserve cap space.", 12));
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
                return new View[] { Text(p.Football.Position.ToString()), Link(p.Football.FullName, () => Player(player)), Text(p.Age.ToString()), Text(p.Football.OverallRating.ToString()), Text(Money(ProEconomy.MarketSalary(state!,p))), Text(waiver ? "Waivers" : pending is null ? "Available" : "Bid pending"), Action(waiver ? "Claim" : pending is null ? "Make offer" : "Revise bid", async () =>
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
    string draftPosition="All positions",draftSearch="";
    async Task Draft()
    {
        var scouted=ProDraft.AutoScout(state!);if(scouted.Scouting.Count!=state!.Scouting.Count){state=scouted;await Save();}
        body.Clear();body.Add(Action("‹ Front Office",()=>Open("Front Office")));body.Add(Text("Draft & scouting",25,true));var s=state!;var current=ProDraft.Current(s);
        body.Add(Text(s.Phase==ProPhase.Offseason?$"{s.OffseasonStep} · "+(current is null?"Draft complete":$"R{current.Round} #{current.Slot} · {current.Owner}"):"Scouting opens during the offseason."));
        body.Add(Text($"Scout team reports automatically · {s.Scouting.Count(r=>r.Team==s.UserTeam&&r.Year==s.Year)}/40 reports · Projected grades are uncertain",12));
        if(s.Phase==ProPhase.Offseason&&s.OffseasonStep==OffseasonStep.Draft)body.Add(Action("Auto draft remaining picks",async()=>{state=await Task.Run(()=>ProDraft.AutoUntilUser(state!,true));await Save();await Draft();}));
        var search=new Entry{Placeholder="Find a prospect",Text=draftSearch};var choices=new[]{"All positions"}.Concat(Enum.GetNames<Position>()).ToArray();var position=new Picker{Title="Position",ItemsSource=choices,SelectedIndex=Array.IndexOf(choices,draftPosition)};
        var filters=new Grid{ColumnDefinitions=new(){new(GridLength.Star),new(GridLength.Auto)},ColumnSpacing=8};filters.Add(search,0,0);filters.Add(position,1,0);body.Add(filters);var rows=new VerticalStackLayout();body.Add(rows);
        void Draw()
        {
            rows.Clear();var prospects=state!.Prospects.Where(p=>(draftPosition=="All positions"||p.Football.Position.ToString()==draftPosition)&&p.Football.FullName.Contains(draftSearch.Trim(),StringComparison.OrdinalIgnoreCase)).OrderByDescending(p=>state.DraftBoard.Contains(p.Football.PlayerId)).ThenByDescending(ProDraft.PublicGrade).ToArray();
            rows.Add(Text($"{prospects.Length} prospects · tap a heading to sort",12));
            rows.Add(Table([("Pos",48),("Prospect",180),("Age",45),("Projected",70),("Scouted",70),("Confidence",80),("Board",105),("Select",80)],prospects.Select(p=>{
                var report=state.Scouting.FirstOrDefault(r=>r.Team==state.UserTeam&&r.Year==state.Year&&r.PlayerId==p.Football.PlayerId);
                var choose=Action("Draft",async()=>{state=ProDraft.AutoUntilUser(ProManagement.Submit(state!,"Draft recommendation",[p.Football.PlayerId],0,x=>ProDraft.Pick(x,p.Football.PlayerId),true));await Save();await Draft();});choose.IsEnabled=state.Phase==ProPhase.Offseason&&state.OffseasonStep==OffseasonStep.Draft&&current?.Owner==state.UserTeam&&ProCareer.Controls(state,p.Football.Position);
                return new View[]{Text(p.Football.Position.ToString()),Text(p.Football.FullName),Text(p.Age.ToString()),Text(ProDraft.PublicGrade(p).ToString()),Text(report?.Grade.ToString()??"—"),Text(report is null?"Pending":report.Confidence+"%"),Action(state.DraftBoard.Contains(p.Football.PlayerId)?"On board":"Add to board",async()=>{state=state! with{DraftBoard=state.DraftBoard.Contains(p.Football.PlayerId)?state.DraftBoard.Where(id=>id!=p.Football.PlayerId).ToList():state.DraftBoard.Append(p.Football.PlayerId).ToList()};await Save();Draw();}),choose};
            })));
        }
        search.TextChanged+=(_,e)=>{draftSearch=e.NewTextValue??"";Draw();};position.SelectedIndexChanged+=(_,_)=>{if(position.SelectedIndex>=0){draftPosition=choices[position.SelectedIndex];Draw();}};Draw();
    }
}
