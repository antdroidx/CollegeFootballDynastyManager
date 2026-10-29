using DynastyManager.App;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using ProFootball.Core;
namespace ProFootball.App;

public sealed partial class FranchisePage
{
    View RatingDelta(ProPlayer p)
    {
        var delta = ProDevelopment.Delta(p);
        var label = Text(delta is null ? "—" : delta > 0 ? "+" + delta : delta.ToString()!, 14, true);
        if (delta > 0) label.TextColor = Color.FromArgb(DynastyTheme.IsLight ? "#146B40" : "#71D8A2");
        else if (delta < 0) label.TextColor = Color.FromArgb(DynastyTheme.IsLight ? "#A52828" : "#FF9A8E");
        return label;
    }
    View PlayerCell(ProPlayer p) => new HorizontalStackLayout { Spacing = 8, Children = { Portrait(p, 38), Link(p.Football.FullName, () => Player(p)) } };
    View Portrait(ProPlayer p, double size)
    {
        var team = state!.League.Teams.FirstOrDefault(t => t.Name == p.Football.TeamName) ?? state.League.Teams.Single(t => t.Name == state.UserTeam);
        return PersonPortrait.Create(p.Football.PlayerId, p.Football.FullName, Color.FromArgb(team.Primary), Color.FromArgb(team.Secondary), size, age: p.Age, appearance: p.Football.Appearance);
    }
    static Guid CoachId(ProCoach c) => new(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(c.Name))[..16]);
    View CoachIdentity(ProCoach c, ProTeam team)
    {
        if(c.Vacant)return Text("Vacant",22,true);
        var portrait=ProCoachPortraits.Find(c.Name);
        return new HorizontalStackLayout { Spacing = 12, Children = { PersonPortrait.Create(CoachId(c), c.Name, Color.FromArgb(team.Primary), Color.FromArgb(team.Secondary), 70, staff: true, age: 48, appearance:portrait?.Appearance,glasses:portrait?.Glasses,moustache:portrait?.Moustache??false), Text(c.Name.Replace("\uFFFD","’"), 22, true) } };
    }
    async Task ShowPage(string title, params View[] views)
    {
        var page = new ContentPage { Title = title, BackgroundColor = DynastyTheme.Background, AutomationId = "pro-detail" };
        var stack = new VerticalStackLayout { Padding = 16, Spacing = 12, MaximumWidthRequest = 1000, HorizontalOptions = LayoutOptions.Fill };
        var close = new Button { Text = "‹ Back" }; close.Clicked += async (_, _) => await Navigation.PopModalAsync(); stack.Add(close); stack.Add(Text(title, 26, true));
        foreach (var view in views) stack.Add(view);
        page.Content = new ScrollView { Content = stack }; await Navigation.PushModalAsync(page);
    }
    async Task Player(ProPlayer original)
    {
        var s = state!; var p = s.Players.FirstOrDefault(x => x.Football.PlayerId == original.Football.PlayerId) ?? original; var f = p.Football;
        var contract = s.Contracts.LastOrDefault(c => c.Active && c.PlayerId == f.PlayerId);
        var identity = new HorizontalStackLayout { Spacing = 14, Children = { Portrait(p, 108), new VerticalStackLayout { Children = { Text(f.FullName, 23, true), Text($"#{f.JerseyNumber} · {OffensiveLineService.Label(f)} · {f.TeamName}"), Text($"Age {p.Age} · Experience {p.Experience} · {f.HeightInches / 12}′{f.HeightInches % 12}″ · {f.WeightPounds} lb") } } } };
        var attributes = new List<View> { Text($"OVR {f.OverallRating} · Projected ceiling {ProDevelopment.Ceiling(p)}", 20, true), Text("Remaining growth · " + ProDevelopment.Growth(p)), new HorizontalStackLayout { Spacing = 8, Children = { Text(p.PreviousSeasonOverall is not null ? "Change since last offseason" : "Change since season start"), RatingDelta(p) } } };
        foreach (var group in PlayerAttributeCatalog.Groups(f))
            attributes.Add(PlayerAttributeViews.Group(group.Title, group.Subtitle, group.Attributes,
                (title, subtitle) => new VerticalStackLayout { Spacing = 3, Children = { Text(title, 15, true), Text(subtitle, 11) } }));
        attributes.Add(Text("Higher ratings are better. Injury represents resistance to injury and wear.", 11));
        var actions = new ActionRow(); actions.Add(Action("Edit appearance",()=>EditPlayerAppearance(p))); if (f.TeamName == s.UserTeam) actions.Add(Action("Contract & roster actions", () => ManagePlayer(p)));
        var lines = s.Games.Where(g => g.Phase == ProPhase.RegularSeason && g.Football.HasPlayed).SelectMany(g => g.Football.PlayerStats).Where(x => x.PlayerId == f.PlayerId).ToArray();
        await ShowPage("Player profile", identity, ReserveActivationControls(p), WaiverControls(p), Panel("Player attributes", attributes.ToArray()), Panel("Status", Text(p.Retired ? "Retired" : p.Reserve ? "Reserve" : p.PracticeSquad ? "Practice squad" : "Active roster"), Text(ProReserve.Availability(state!,p)), Text($"Morale {p.Morale} · Playing {ProDepthChart.Label(ProDepthChart.AssignedCode(s,p))} · Depth {ProDepthChart.AtPosition(s,f.TeamName,ProDepthChart.AssignedCode(s,p)).ToList().FindIndex(x=>x.Football.PlayerId==f.PlayerId)+1} · {ProMorale.Personality(p)}", 12), Text(p.TradeRequested ? "Trade requested · sustained dissatisfaction" : p.ContractConcernWeeks>0 ? "Concerned about contract value" : "No reported contract concern",12)),
            ContractDetails(p,contract),
            PlayerAnalytics(p), Panel("Regular season", Text($"Passing {lines.Sum(x => x.PassYards)} yd · {lines.Sum(x => x.PassingTouchdowns)} TD\nRushing {lines.Sum(x => x.RushYards)} yd · Receiving {lines.Sum(x => x.ReceivingYards)} yd\nTackles {lines.Sum(x => x.Tackles)} · Sacks {lines.Sum(x => x.Sacks)}")), actions);
    }
    View WaiverControls(ProPlayer p)
    {
        var entry=state!.Waivers.FirstOrDefault(w=>w.PlayerId==p.Football.PlayerId);
        if(entry is null)return new VerticalStackLayout();
        var ownRelease=entry.OriginalContracts?.Any(c=>c.Team==state.UserTeam)==true;
        var claimed=entry.Claims.Contains(state.UserTeam);var controls=ProCareer.Controls(state,p.Football.Position);
        var button=Action(ownRelease?"Released by your team":claimed?"Claim submitted":"Claim from waivers",async()=>{
            await ClaimWaiver(p);await Navigation.PopModalAsync();await Player(p);
        });
        button.IsEnabled=!ownRelease&&!claimed&&controls;button.AutomationId="pro-waiver-profile-claim";
        return Panel("Waivers",Text(ownRelease?"Other teams may claim this contract. Your club must wait for it to clear waivers.":claimed?"Claim submitted. Priority, roster space and salary-cap room are checked when waivers resolve.":controls?"Assume the existing salary contract. Review remaining pay and years before submitting.":"The head coach manages waiver claims for the other unit.",12),button);
    }
    View ReserveActivationControls(ProPlayer p)
    {
        if(!p.Reserve||p.Football.TeamName!=state!.UserTeam)return new VerticalStackLayout();
        var reason=ProReserve.ActivationReason(state,state.UserTeam,p.Football.PlayerId);
        if(!ProCareer.Controls(state,p.Football.Position))reason="The head coach manages reserve activation for the other unit.";
        var button=Action("Activate to roster",async()=>{
            await Manage("Reserve activation",p.Football.PlayerId,0,s=>ProReserve.Activate(s,s.UserTeam,p.Football.PlayerId));
            var current=state!.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId);
            if(!current.Reserve){await Navigation.PopModalAsync();await Player(current);}
        });
        button.AutomationId="pro-activate-reserve";button.IsEnabled=reason is null;
        return Panel("Reserve activation",Text(reason??"Available to activate. Your existing contract is retained; roster and cap limits are checked.",12),button);
    }
    async Task EditPlayerAppearance(ProPlayer original)
    {
        var current=state!.Players.FirstOrDefault(p=>p.Football.PlayerId==original.Football.PlayerId)??original;
        var appearance=current.Football.Appearance??new NcaaPlayerAppearance();
        var tone=new Picker{AutomationId="pro-skin-tone",Title="Skin tone",ItemsSource=new[]{"Current appearance","Light","Light medium","Medium","Tan","Brown","Dark brown","Deep"},SelectedIndex=appearance.SkinColorHex is not null?0:PortraitAppearance.ForPlayer(current.Football.PlayerId,appearance).Skin+1};
        var hair=new Picker{Title="Hair color",ItemsSource=new[]{"Automatic","Black","Blonde","Brown","Red","Light brown","Gray"},SelectedIndex=(appearance.HairColor??-1)+1};
        var style=new Picker{Title="Hair style",ItemsSource=new[]{"Automatic","Bald","Cornrows","Afro","Flat top","Buzz cut","Fade","Balding","Close crop","Bald (hidden)","Bald 2","Balding 2","Buzz cut 2","Fade 2","Mullet","Dreadlocks"},SelectedIndex=(appearance.HairStyle??-1)+1};
        var beard=new Picker{Title="Facial hair",ItemsSource=new[]{"Automatic","Clean shaven","Stubble","Goatee","Beard"},SelectedIndex=(appearance.FacialHair??-1)+1};
        NcaaPlayerAppearance Selected()=>appearance with{SkinTone=tone.SelectedIndex==0?appearance.SkinTone:tone.SelectedIndex-1,SkinColorHex=tone.SelectedIndex==0?appearance.SkinColorHex:null,HairColor=hair.SelectedIndex<=0?null:hair.SelectedIndex-1,HairStyle=style.SelectedIndex<=0?null:style.SelectedIndex-1,FacialHair=beard.SelectedIndex<=0?null:beard.SelectedIndex-1};
        var preview=new ContentView();
        void Refresh()=>preview.Content=Portrait(current with{Football=current.Football with{Appearance=Selected()}},120);
        tone.SelectedIndexChanged+=(_,_)=>Refresh();hair.SelectedIndexChanged+=(_,_)=>Refresh();style.SelectedIndexChanged+=(_,_)=>Refresh();beard.SelectedIndexChanged+=(_,_)=>Refresh();Refresh();
        await ShowPage("Edit appearance",Text(current.Football.FullName,20,true),Text("Customize this portrait. Your changes also apply to the player in GameCast.",12),preview,Text("Skin tone"),tone,Text("Hair color"),hair,Text("Hair style"),style,Text("Facial hair"),beard,
            Action("Save appearance",async()=>{
                var id=current.Football.PlayerId;var selected=Selected();
                state=state! with{Players=state.Players.Select(p=>p.Football.PlayerId==id?p with{AppearanceOrigin="custom",Football=p.Football with{Appearance=selected}}:p).ToList()};
                await Save();await Navigation.PopModalAsync();await Navigation.PopModalAsync();Render();await Player(state.Players.Single(p=>p.Football.PlayerId==id));
            }));
    }
    Task CoachProfile(ProCoach coach)
    {
        var team = state!.League.Teams.Single(t => t.Id == coach.TeamId);
        return ShowPage("Coach profile", CoachIdentity(coach, team), Panel(RoleName(coach.Role), Text(coach.Title), Text($"Coaching {coach.Rating} · Development {coach.Development}"), Text(coach.Vacant ? "Position vacant" : "Employed by " + team.Name)));
    }
    Task GamePlan()
    {
        var plan = state!.GamePlans.FirstOrDefault(p => p.TeamName == state.UserTeam) ?? new TeamGamePlan { TeamName = state.UserTeam };
        Picker Choices<T>(T selected) where T : struct, Enum => new() { ItemsSource = Enum.GetNames<T>(), SelectedIndex = Array.IndexOf(Enum.GetValues<T>(), selected) };
        var offense = Choices(plan.Offense); var defense = Choices(plan.Defense); var approach = Choices(plan.Approach); var tempo = Choices(plan.Tempo);
        offense.IsEnabled = tempo.IsEnabled = ProCareer.Role(state!) != StaffRole.DefensiveCoordinator; defense.IsEnabled = approach.IsEnabled = ProCareer.Role(state!) != StaffRole.OffensiveCoordinator;
        var save = new Button { Text = "Save game plan" }; var saved = Text("");
        save.Clicked += async (_, _) =>
        {
            var next = plan with { Offense = Enum.Parse<OffensivePlaybook>((string)offense.SelectedItem), Defense = Enum.Parse<DefensivePlaybook>((string)defense.SelectedItem), Approach = Enum.Parse<DefensiveApproach>((string)approach.SelectedItem), Tempo = Enum.Parse<OffensiveTempo>((string)tempo.SelectedItem) };
            next = ProCareer.LimitPlan(state!, next); state = state! with { GamePlans = state.GamePlans.Where(p => p.TeamName != state.UserTeam).Append(next).ToList() }; await Save(); saved.Text = "Saved · applies to future games";
        };
        return ShowPage("Game plan", Text("Offensive playbook"), offense, Text("Defensive playbook"), defense, Text("Defensive approach"), approach, Text("Tempo"), tempo, save, saved);
    }
    Task InjuryReport()
    {
        var players = state!.Players.Where(p => p.Football.TeamName == state.UserTeam && !p.Retired && (p.Reserve || p.Football.CurrentInjury is { WeeksRemaining: > 0 })).ToArray();
        return ShowPage("Injuries & availability", players.Length == 0 ? Text("No reported injuries or reserve players.") : new VerticalStackLayout { Spacing = 12, Children = { } },
            Panel("Unavailable players", players.Select(p => Panel(p.Football.FullName, PlayerCell(p), Text(ProReserve.Availability(state!,p)))).ToArray()));
    }
    string newsFilter = "All";
    void News()
    {
        state = ProNews.Refresh(state!); var s = state!;
        body.Add(Text("League news", 23, true));
        body.Add(Action("Filter: " + newsFilter, async () => { var filter = await GameDialogs.ActionSheetAsync(this, "News", "Cancel", null, new[] { "All", "My team", "Unread" }.Concat(s.News.Select(n => n.Category).Distinct()).ToArray()); if (filter is null or "Cancel") return; newsFilter = filter; Render(); }));
        var stories = s.News.Where(n => newsFilter == "All" || newsFilter == "My team" && (n.Team == s.UserTeam || n.Headline.Contains(s.UserTeam)) || newsFilter == "Unread" && !s.ReadNewsIds.Contains(n.Id) || n.Category == newsFilter).Take(60).ToArray();
        if (stories.Length == 0) body.Add(Text("No stories in this view yet. Completed games and league activity will appear here."));
        foreach (var story in stories)
        {
            var n = story; var heading=new Grid {ColumnSpacing=10,ColumnDefinitions=new(){new(GridLength.Auto),new(GridLength.Star)}};
            heading.Add(NewsThumbnail(n),0,0);heading.Add(Text((s.ReadNewsIds.Contains(n.Id)?"":"● ")+n.Headline,18,true),1,0);
            body.Add(Panel("", heading, Text($"{n.Year} · Week {n.Week} · {n.Category}", 12), Text(n.Body), Action(n.GameId is null ? "Read story" : "Game report ›", async () =>
            {
                state = state! with { ReadNewsIds = state.ReadNewsIds.Append(n.Id).Distinct().ToList() }; await Save();
                var game = state.Games.Concat(state.History.SelectMany(h => h.Games)).FirstOrDefault(g => g.Football.GameId == n.GameId);
                if (game is not null) await BoxScore(game.Football); else await GameDialogs.AlertAsync(this, n.Headline, n.Body, "Close");
            })));
        }
    }
    View NewsThumbnail(ProNewsStory story)
    {
        var team = state!.League.Teams.FirstOrDefault(t => t.Name == story.Team);
        var player = state.Players.FirstOrDefault(p => p.Football.TeamName == story.Team && story.Headline.Contains(p.Football.FullName, StringComparison.OrdinalIgnoreCase));
        if (player is not null) return Portrait(player, 42);
        return team is not null ? new TeamBadgeView(team, 42) : Text("LEAGUE", 12, true);
    }
    Task ShowBoxScore(ScheduledGame g) => GameCastBoxScore.ShowAsync(Navigation, new()
    {
        HomeTeamName = g.HomeTeamName, AwayTeamName = g.AwayTeamName, HomeScore = g.HomeScore ?? 0, AwayScore = g.AwayScore ?? 0,
        HomeStats = g.HomeStats ?? new(), AwayStats = g.AwayStats ?? new(), Periods = g.PeriodScores, Players = g.PlayerStats,
        Quarter = Math.Max(4, g.PeriodScores.Select(p => p.Period).DefaultIfEmpty(4).Max()), ClockSeconds = 0, IsComplete = g.HasPlayed,
        HomeMorale = g.HomeEndingMorale, AwayMorale = g.AwayEndingMorale, Weather = g.Weather, PlayerOfGame = g.PlayerOfGame
    }, ProGameCastPresentation.BoxStyle(state!.League));
}
