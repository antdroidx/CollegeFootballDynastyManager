using DynastyManager.App;
using DynastyManager.Core.Models;
using ProFootball.Core;
namespace ProFootball.App;

public sealed partial class FranchisePage
{
    static string RoleName(StaffRole role) => role switch { StaffRole.HeadCoach => "Head coach", StaffRole.OffensiveCoordinator => "Offensive coordinator", StaffRole.DefensiveCoordinator => "Defensive coordinator", StaffRole.SpecialTeamsCoordinator => "Special teams coordinator", _ => role.ToString() };
    static string Money(long value) => "$" + (value / 1_000_000d).ToString("0.00") + "m";
    View Link(string text, Func<Task> action) { var b = Action(text, action); b.BackgroundColor = Colors.Transparent; b.BorderWidth=0; b.TextColor=DynastyTheme.Accent; b.FontSize = 12; b.Padding = 4; return b; }
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
        var before=state;
        var p = state!.Players.Single(x => x.Football.PlayerId == original.Football.PlayerId); var c = state.Contracts.LastOrDefault(c => c.Active && c.PlayerId == p.Football.PlayerId);
        var choice = await GameDialogs.ActionSheetAsync(this, p.Football.FullName + " · " + p.Football.Position, "Close", null, "Player & contract", p.Reserve ? "Activate from reserve" : "Organize depth chart", "Extend contract", "Restructure contract", "Franchise tag", state.TradeBlock.Contains(p.Football.PlayerId) ? "Remove from trading block" : "Add to trading block", "Release player");
        if (choice == "Player & contract") { await ShowPage("Player contract",ContractDetails(p,c),ContractComparisons(p)); }

        if (choice == "Organize depth chart") { await DepthChart(); return; }
        if (choice == "Activate from reserve") await Manage("Reserve activation", p.Football.PlayerId, 0, s => ProReserve.Activate(s, s.UserTeam, p.Football.PlayerId));
        if (choice == "Extend contract") { var offer = await ContractTerms(p, true); if (offer is { } o) await Manage("Contract extension", p.Football.PlayerId, 0, s => ProEconomy.Extend(s, s.UserTeam, p.Football.PlayerId, o.Years, o.Salary, o.Bonus)); }
        if(choice=="Restructure contract") await RestructureContract(p);
        if (choice is "Add to trading block" or "Remove from trading block") await Update(s => ProTradeMarket.List(s,s.UserTeam,p.Football.PlayerId,choice == "Add to trading block"));
        if (choice == "Franchise tag") await Manage("Franchise tag", p.Football.PlayerId, 0, s => ProEconomy.Tag(s, s.UserTeam, p.Football.PlayerId));
        if (choice == "Release player") { var decision=await ConfirmRelease(p);if(decision.Release)await Manage("Release player", p.Football.PlayerId, 0, s => ProEconomy.Release(s, s.UserTeam, p.Football.PlayerId,postJune1:decision.PostJune1)); }
        if(!ReferenceEquals(before,state)&&Navigation.ModalStack.LastOrDefault()?.Title=="Player profile")
        {
            await Navigation.PopModalAsync();await Player(state!.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId));
        }
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
        if(ProCapInitialization.CanCorrectOpeningBudgets(s))body.Add(Action("Update 2026 cap budgets",CorrectOpeningBudgets));
        var row = new ActionRow(); row.Add(Action("Financial overview",FinanceHub)); row.Add(Action("Free agents", FreeAgents, "pro-free-agents")); row.Add(Action("Trade center", () => Trade())); row.Add(Action("Trading block", TradingBlock)); row.Add(Action($"Trade offers ({ProTradeAi.OpenOffers(s).Count})", TradeOffers)); row.Add(Action("Trade history", TradeHistory)); if(s.Phase==ProPhase.Offseason&&s.OffseasonStep is OffseasonStep.FreeAgency or OffseasonStep.Draft)row.Add(Action("Draft & scouting", Draft)); row.Add(Action("Draft picks", DraftPicks, "pro-draft-picks")); if(s.Phase==ProPhase.Offseason&&s.OffseasonStep is OffseasonStep.FreeAgency or OffseasonStep.Draft)row.Add(CollegeClassImportButton()); if(s.Phase!=ProPhase.Offseason||s.OffseasonStep==OffseasonStep.Retention)row.Add(Action("Expiring contracts", Retention)); if(s.Phase==ProPhase.Offseason&&s.OffseasonStep==OffseasonStep.Draft)row.Add(Action("Rookie options", RookieOptions)); if(s.Phase==ProPhase.Offseason)row.Add(Action("Current offseason stage",()=>Open("Offseason"))); body.Add(row);
        var rosterTools = new ActionRow(); rosterTools.Add(Action("Ask GM to fill roster", () => Update(x => ProCpu.Manage(x, true)))); rosterTools.Add(Action("Ask GM to trim to 53", () => Update(x => ProCpu.Trim(x, x.UserTeam)))); body.Add(rosterTools);
        body.Add(Text("Contracts · select a player", 21, true)); body.Add(Table([("Player", 170), ("Ends", 55), ("Salary",100), ("Bonus / season",110), ("Cap hit", 100)], s.Contracts.Where(c => c.Active && c.Team == s.UserTeam).OrderByDescending(c => ProEconomy.CapHit(c, s.Year)).Select(c => { var p = s.Players.Single(p => p.Football.PlayerId == c.PlayerId); return new View[] { Link(p.Football.FullName, () => Player(p)), Text(ProContractInsights.PlayingEnd(c).ToString()), Text(Money(c.Years.FirstOrDefault(y=>y.Year==s.Year)?.Salary??0)), Text(Money(ProEconomy.BonusCharge(c,s.Year))),Text(Money(ProEconomy.CapHit(c, s.Year))) }; })));
        body.Add(Panel("Dead money", Text(string.Join("\n", s.DeadCap.Where(d => d.Team == s.UserTeam && d.Year == s.Year).Select(d => $"{d.SourceName ?? s.Players.FirstOrDefault(p => p.Football.PlayerId == d.PlayerId)?.Football.FullName ?? "Prior contract"}: {Money(d.Amount)}")), 12)));
    }
    string freeAgentPosition = "All positions", freeAgentSearch = "";
    Task FreeAgents()
    {
        body.Clear(); body.Add(Action("‹ Front Office", () => Open("Front Office"))); body.Add(Text("Free agency & waivers", 25, true)); body.Add(Text("Offers compete when you advance. Pending bids reserve cap space.", 12));
        body.Add(FreeAgencyAssistanceControls());
        var positions = new[] { "All positions" }.Concat(PositionLabels.Names).ToArray();
        var position = new Picker { Title = "Position", ItemsSource = positions, SelectedIndex = Array.IndexOf(positions, freeAgentPosition), MinimumWidthRequest = 130 };
        var search = new Entry { Placeholder = "Find a free agent", Text = freeAgentSearch };
        var filters = new Grid { ColumnDefinitions = new() { new(GridLength.Star), new(GridLength.Auto) }, ColumnSpacing = 8 };
        filters.Add(search, 0, 0); filters.Add(position, 1, 0); body.Add(filters);
        var count = Text("", 12); body.Add(count); var list = new VerticalStackLayout { Spacing = 3 }; body.Add(list);
        void Draw()
        {
            list.Clear();
            var players = state!.Players.Where(p => p.Football.TeamName == "Free agent" && !p.Retired && ProCareer.Controls(state, p.Football.Position)
                && (freeAgentPosition == "All positions" || p.Football.Position.DisplayName() == freeAgentPosition)
                && (string.IsNullOrWhiteSpace(freeAgentSearch) || p.Football.FullName.Contains(freeAgentSearch.Trim(), StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(p => p.Football.OverallRating).ThenBy(p => p.Football.FullName).ToArray();
            count.Text = players.Length + " available · tap a heading to sort";
            list.Add(Table([("Pos",48),("Player",200),("Age",48),("OVR",52),("Market / yr",100),("Status",100),("Offer",110)], players.Select(p =>
            {
                var player = p; var waiver = state.Waivers.Any(w => w.PlayerId == p.Football.PlayerId);
                var pending = state.Bids.FirstOrDefault(b => b.Team == state.UserTeam && b.PlayerId == p.Football.PlayerId);
                var entry=state.Waivers.FirstOrDefault(w=>w.PlayerId==player.Football.PlayerId);
                var ownRelease=entry?.OriginalContracts?.Any(c=>c.Team==state.UserTeam)==true;
                var submitted=entry?.Claims.Contains(state.UserTeam)==true;
                var offerButton=Action(waiver?(ownRelease?"Released by you":submitted?"Claim submitted":"Claim"):pending is null?"Make offer":"Revise bid",async()=>
                {
                    if (waiver) await ClaimWaiver(player);
                    else { var offer = await ContractTerms(player); if (offer is { } o) await Manage("Free-agent offer",player.Football.PlayerId,o.Salary + o.Bonus/o.Years,s => ProEconomy.Offer(s,s.UserTeam,player.Football.PlayerId,o.Years,o.Salary,o.Bonus)); }
                    await FreeAgents();
                });
                offerButton.IsEnabled=!ownRelease&&!submitted;offerButton.BackgroundColor=DynastyTheme.Accent;offerButton.TextColor=DynastyTheme.IsLight?Colors.White:Color.FromArgb("#0B1726");offerButton.BorderWidth=1;offerButton.BorderColor=DynastyTheme.ControlEdge;offerButton.MinimumHeightRequest=44;offerButton.FontSize=11;
                return new View[] { Text(p.Football.Position.DisplayName()), Link(p.Football.FullName, () => Player(player)), Text(p.Age.ToString()), Text(p.Football.OverallRating.ToString()), Text(Money(ProEconomy.MarketSalary(state!,p))), Text(waiver?"Waivers":(pending is null?"Available":"Your bid pending")+" · "+state.Bids.Count(b=>b.PlayerId==p.Football.PlayerId&&b.Team!=state.UserTeam)+" rival bids",11),offerButton };
            })));
            if (players.Length == 0) list.Add(Text("No available players match these filters."));
        }
        search.TextChanged += (_, _) => { freeAgentSearch = search.Text ?? ""; Draw(); };
        position.SelectedIndexChanged += (_, _) => { freeAgentPosition = (string?)position.SelectedItem ?? "All positions"; Draw(); };
        Draw(); OffseasonGuide();return Task.CompletedTask;
    }
    async Task ClaimWaiver(ProPlayer player)
    {
        var entry=state!.Waivers.Single(w=>w.PlayerId==player.Football.PlayerId);
        var terms=entry.OriginalContracts is {Count:>0}?"Remaining salary this season: "+Dollars(ProWaivers.CurrentSalary(entry,state.Year))+". Contract through "+entry.OriginalContracts.Max(ProContractInsights.PlayingEnd)+". Existing salary terms and guarantees transfer; the former club retains signing-bonus charges.":"This legacy claim requires a new contract.";
        if(await GameDialogs.AlertAsync(this,"Claim "+player.Football.FullName+"?",terms,"Claim","Cancel"))
            await Manage("Waiver claim",player.Football.PlayerId,0,s=>ProRoster.Claim(s,s.UserTeam,player.Football.PlayerId));
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
}
