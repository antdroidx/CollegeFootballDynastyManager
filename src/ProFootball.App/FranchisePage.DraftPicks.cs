using DynastyManager.App;
using ProFootball.Core;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    Task DraftPicks()
    {
        var s=state!;body.Clear();body.Add(Action("‹ Front Office",()=>Open("Front Office")));body.Add(Text("Draft picks",25,true));
        var clubs=s.League.Teams.OrderBy(t=>t.Name).ToArray();var team=new Picker{Title="Team",AutomationId="pro-picks-team",ItemsSource=clubs.Select(t=>t.Name).ToArray(),SelectedIndex=Array.FindIndex(clubs,t=>t.Name==s.UserTeam)};
        var years=s.Picks.Select(p=>p.Year).Concat(s.CompensatoryProjections.Select(p=>p.Year)).Where(y=>y>=s.Year).Append(s.Year+1).Distinct().Order().ToArray();
        var year=new Picker{Title="Draft year",AutomationId="pro-picks-year",ItemsSource=years.Select(y=>y.ToString()).ToArray(),SelectedIndex=Math.Max(0,Array.IndexOf(years,s.Year+1))};
        body.Add(CompactFilters.Create((team,210),(year,120)));var list=new VerticalStackLayout{Spacing=10};body.Add(list);
        void Draw()
        {
            if(team.SelectedIndex<0||year.SelectedIndex<0)return;list.Clear();var club=clubs[team.SelectedIndex];var selected=years[year.SelectedIndex];
            var picks=s.Picks.Where(p=>p.Year==selected&&p.Owner==club.Name).OrderBy(p=>p.Round).ThenBy(p=>p.Slot).ToArray();
            list.Add(Text($"{club.Name} · {selected} · {picks.Length} allocated picks",19,true));
            list.Add(Table([("Round",60),("Original team",180),("Type",130),("Status",110)],picks.Select(p=>new View[]{Text(p.Round.ToString()),TeamCell(s.League.Teams.Single(t=>t.Name==p.OriginalTeam)),Text(p.SpecialCompensatory?"Staff compensation":p.Compensatory?"Compensatory":"Standard"),Text(ProTrade.Available(s,p)?"Available":"Selected")})));
            if(!s.CompensatoryAwardYears.Contains(selected))
            {
                var projected=ProCompensatoryDraft.Project(s,selected).Where(p=>p.Team==club.Name).ToArray();
                list.Add(Text("Projected compensatory picks",19,true));list.Add(Text("Final allocation occurs before the draft. Projected picks cannot be traded.",12));
                if(projected.Length==0)list.Add(Text("No compensatory selections projected."));
                else list.Add(Table([("Round",60),("Compensation for",210),("Type",140)],projected.Select(p=>new View[]{Text(p.Round.ToString()),Text(p.Reason),Text(p.Special?"Staff departure":"Free-agent departure")})));
            }
            else list.Add(Text($"{picks.Count(p=>p.Compensatory)} compensatory picks awarded. These selections are included in the draft and can be traded."));
        }
        team.SelectedIndexChanged+=(_,_)=>Draw();year.SelectedIndexChanged+=(_,_)=>Draw();Draw();return Task.CompletedTask;
    }
}
