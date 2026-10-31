using DynastyManager.App;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using ProFootball.Core;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    string draftPosition="All positions",draftSearch="",draftView="Prospects";
    async Task Draft()
    {
        if(state!.Phase==ProPhase.Offseason&&state.OffseasonStep==OffseasonStep.Draft)
        {
            var before=state.DraftIndex;state=await Task.Run(()=>ProDraft.AutoUntilUser(state));if(before!=state.DraftIndex)await Save();
        }
        var scouted=ProDraft.AutoScout(state!);if(!scouted.Scouting.SequenceEqual(state!.Scouting)){state=scouted;await Save();}
        var s=state!;var current=ProDraft.Current(s);var active=s.Phase==ProPhase.Offseason&&s.OffseasonStep==OffseasonStep.Draft;
        body.Clear();body.Add(Action("‹ Front Office",()=>Open("Front Office")));body.Add(PageHeader($"{s.Year} Draft room","Scout, rank and select prospects"));
        var headline=current is null?"Draft complete":active?$"On the clock · R{current.Round} · Pick {ProDraft.Overall(s,current)} · {current.Owner}":"Scouting & draft preparation";
        var next=s.Picks.Where(p=>p.Year==s.Year&&p.Owner==s.UserTeam).OrderBy(p=>p.Round).ThenBy(p=>p.Slot).FirstOrDefault(p=>!s.DraftHistory.Any(h=>h.Year==s.Year&&h.Round==p.Round&&h.Slot==p.Slot));
                var chips=new HorizontalStackLayout{Spacing=8};
        chips.Add(new Border{Padding=new Thickness(8,3),StrokeShape=new Microsoft.Maui.Controls.Shapes.RoundRectangle{CornerRadius=10},BackgroundColor=DynastyTheme.Panel,Content=new Label{Text=$"{s.Prospects.Count} prospects",FontSize=11,TextColor=DynastyTheme.Muted}});
        chips.Add(new Border{Padding=new Thickness(8,3),StrokeShape=new Microsoft.Maui.Controls.Shapes.RoundRectangle{CornerRadius=10},BackgroundColor=DynastyTheme.Panel,Content=new Label{Text=$"{s.DraftHistory.Count(h2=>h2.Year==s.Year)} selections",FontSize=11,TextColor=DynastyTheme.Muted}});
        chips.Add(new Border{Padding=new Thickness(8,3),StrokeShape=new Microsoft.Maui.Controls.Shapes.RoundRectangle{CornerRadius=10},BackgroundColor=DynastyTheme.Accent,Content=new Label{Text=ProScouting.DepthSummary(s),FontSize=11,TextColor=DynastyTheme.IsLight?Colors.White:Color.FromArgb("#0B1726")}});
        body.Add(chips);
        body.Add(Panel(headline,Text($"{s.DraftHistory.Count(h=>h.Year==s.Year)} selections made · {s.Prospects.Count} available · "+(next is null?"No team picks remaining":$"Your next pick: R{next.Round} · #{ProDraft.Overall(s,next)}"),13)));
        var tools=new ActionRow();if(active)tools.Add(Action("Delegate remaining picks",async()=>{state=await Task.Run(()=>ProDraft.AutoUntilUser(state!,true));await Save();await Draft();},"pro-draft-auto"));
        if(s.DraftIndex==0)tools.Add(CollegeClassImportButton());tools.Add(Action("Free agents",FreeAgents));body.Add(tools);
        var views=new ActionRow();foreach(var name in new[]{"Prospects","My board","Draft results","Order"}){var n=name;var button=Action(name,()=>{draftView=n;return Draft();},"pro-draft-view-"+name.Replace(" ","-").ToLowerInvariant());button.BackgroundColor=draftView==name?DynastyTheme.Selected:Colors.Transparent;views.Add(button);}body.Add(views);
        if(draftView=="Draft results")
        {
            body.Add(Table([("Round",48),("Pick",48),("Team",185),("Player",180),("Pos",45)],s.DraftHistory.Where(h=>h.Year==s.Year).OrderBy(h=>h.Round).ThenBy(h=>h.Slot).Select(h=>new View[]{Text(h.Round.ToString()),Text((s.Picks.FirstOrDefault(p=>p.Year==h.Year&&p.Round==h.Round&&p.Slot==h.Slot) is {} pick?ProDraft.Overall(s,pick):h.Slot).ToString()),TeamCell(s.League.Teams.Single(t=>t.Name==h.Team)),Link(h.Player.Football.FullName,()=>Player(h.Player)),Text(h.Player.Football.Position.DisplayName())})));OffseasonGuide();return;
        }
        if(draftView=="Order")
        {
            body.Add(Table([("Round",48),("Pick",48),("Team",185),("Original",185),("Status",180)],s.Picks.Where(p=>p.Year==s.Year).OrderBy(p=>p.Round).ThenBy(p=>p.Slot).Select(p=>{
                var selection=s.DraftHistory.FirstOrDefault(h=>h.Year==p.Year&&h.Round==p.Round&&h.Slot==p.Slot);
                return new View[]{Text(p.Round.ToString()),Text(ProDraft.Overall(s,p).ToString()),TeamCell(s.League.Teams.Single(t=>t.Name==p.Owner)),TeamCell(s.League.Teams.Single(t=>t.Name==p.OriginalTeam)),Text(selection?.Player.Football.FullName??(current?.Id==p.Id&&active?"On the clock":p.Compensatory?"Compensatory":"Upcoming"),12)};
            })));OffseasonGuide();return;
        }
        var choices=new[]{"All positions"}.Concat(PositionLabels.Names).ToArray();var position=new Picker{ItemsSource=choices,SelectedIndex=Array.IndexOf(choices,draftPosition),AutomationId="pro-draft-position"};var search=new SearchBar{Placeholder="Find a prospect",Text=draftSearch};body.Add(CompactFilters.Create((search,210),(position,150)));var rows=new VerticalStackLayout{Spacing=6};body.Add(rows);
        void Draw()
        {
            rows.Clear();var prospects=state!.Prospects.Where(p=>(draftView!="My board"||state.DraftBoard.Contains(p.Football.PlayerId))&&(draftPosition=="All positions"||p.Football.Position.DisplayName()==draftPosition)&&p.Football.FullName.Contains(draftSearch.Trim(),StringComparison.OrdinalIgnoreCase)).OrderByDescending(p=>state.DraftBoard.Contains(p.Football.PlayerId)).ThenByDescending(ProDraft.PublicGrade).ToArray();
            rows.Add(Text($"{prospects.Length} prospects · open a name for player/scouting details",12));
            rows.Add(Table([("Prospect",175),("Pos",45),("Age",42),("Depth",80),("Scouted",65),("Projected",70),("Confidence",85),("Pick",90),("Action",105)],prospects.Select(p=>{
                var id=p.Football.PlayerId;
                var report=state.Scouting.FirstOrDefault(r=>r.Team==state.UserTeam&&r.Year==state.Year&&r.PlayerId==id);
                var target=state.ScoutTargets.FirstOrDefault(t=>t.Team==state.UserTeam&&t.Year==state.Year&&t.PlayerId==id);
                var depth=(target is null?ScoutDepth.Basic:target.Depth);
                var tier=Action(depth==ScoutDepth.Visit?"Clear visit":depth==ScoutDepth.Priority?"Scout visit":depth==ScoutDepth.Basic?"Clear basic":"Scout priority",async()=>{
                    ScoutDepth? nextDepth=depth==ScoutDepth.Priority?ScoutDepth.Visit:depth==ScoutDepth.Visit?null:(ScoutDepth?)null;
                    if(nextDepth is not null){state=ProScouting.SetTarget(state!,state.UserTeam,id,nextDepth);state=ProDraft.AutoScout(state!);}
                    else state=ProScouting.SetTarget(state!,state.UserTeam,id,null);
                    await Save();Draw();
                });
                tier.FontSize=11;tier.MinimumHeightRequest=44;
                var choose=Action("Draft",async()=>{state=ProDraft.AutoUntilUser(ProManagement.Submit(state!,"Draft recommendation",[id],0,x=>ProDraft.Pick(x,id),true));await Save();await Draft();});choose.IsEnabled=active&&current?.Owner==state.UserTeam&&ProCareer.Controls(state,p.Football.Position);choose.FontSize=11;choose.MinimumHeightRequest=44;
                var board=Action(state.DraftBoard.Contains(id)?"Remove":"Shortlist",async()=>{state=state! with{DraftBoard=state.DraftBoard.Contains(id)?state.DraftBoard.Where(x=>x!=id).ToList():state.DraftBoard.Append(id).ToList()};await Save();Draw();});board.FontSize=11;board.MinimumHeightRequest=44;
                return new View[]{Link(p.Football.FullName,()=>ProspectProfile(p)),Text(p.Football.Position.DisplayName()),Text(p.Age.ToString()),Text(depth.ToString()),Text(report?.Grade.ToString()??"—"),Text(ProDraft.PublicGrade(p).ToString()),Text(report is null?"Pending":report.Confidence+"%"),choose,board};
            })));        }
        search.TextChanged+=(_,e)=>{draftSearch=e.NewTextValue??"";Draw();};position.SelectedIndexChanged+=(_,_)=>{if(position.SelectedIndex>=0){draftPosition=choices[position.SelectedIndex];Draw();}};Draw();OffseasonGuide();
    }
    Task ProspectProfile(ProPlayer p)
    {
        var f=p.Football;var report=state!.Scouting.FirstOrDefault(r=>r.Team==state.UserTeam&&r.Year==state.Year&&r.PlayerId==f.PlayerId);var grade=report?.Grade??ProDraft.PublicGrade(p);var uncertainty=Math.Max(3,(100-(report?.Confidence??35))/5);
        var views=new List<View>{new HorizontalStackLayout{Spacing=12,Children={Portrait(p,64),Text($"{f.FullName} · {f.Position.DisplayName()}",22,true)}},Text($"Age {p.Age} · {f.HeightInches/12}′{f.HeightInches%12}″ · {f.WeightPounds} lb"),Text($"Projected {ProDraft.PublicGrade(p)} · Scout grade {(report?.Grade.ToString()??"Pending")} · Confidence {(report?.Confidence??0)}%",14,true)};
        foreach(var group in PlayerAttributeCatalog.Groups(f))views.Add(Panel(group.Title,Table([("Attribute",180),("Scout range",110)],group.Attributes.Select(a=>{
            var estimate=Math.Clamp(a.Rating+grade-f.OverallRating,1,99);return new View[]{Text(a.Name),Text($"{Math.Max(1,estimate-uncertainty)}–{Math.Min(99,estimate+uncertainty)}")};
        }))));
        return ShowPage("Prospect profile",views.ToArray());
    }
}
