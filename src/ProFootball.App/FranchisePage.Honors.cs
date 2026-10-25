using DynastyManager.App;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using ProFootball.Core;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    string honorsTab="Awards";
    void Honors()
    {
        var s=state!;body.Add(Text("Awards & all-stars",25,true));
        if(s.Honors.Count==0){body.Add(Text("Season awards and conference all-star selections are announced after the regular season. The exhibition opens after the championship."));return;}
        var years=s.Honors.OrderByDescending(h=>h.Year).ToArray();var year=new Picker{ItemsSource=years.Select(h=>h.Year.ToString()).ToArray(),SelectedIndex=0};body.Add(year);var tabs=new Picker {ItemsSource=new[]{"Awards","All-stars","Showcase"},SelectedIndex=Array.IndexOf(new[]{"Awards","All-stars","Showcase"},honorsTab)};body.Add(tabs);var details=new VerticalStackLayout{Spacing=10};body.Add(details);
        void Draw()
        {
            details.Clear();var h=years[year.SelectedIndex];details.Add(Text(h.Year+" season honors",21,true));
            if(honorsTab=="Awards") details.Add(Table([("Award",240),("Winner",190),("Team",180)],h.Awards.Select(a=>new View[]{Text(a.Title),Text(a.Name),Text(a.Team)})));
            if(honorsTab=="All-stars") { details.Add(Text("Conference all-star selections",21,true));details.Add(Text("Selected from season performance and position ratings. Offensive-line selections preserve specialist line roles.",12));
            details.Add(Table([("Conf",70),("Pos",55),("Player",200),("Team",180),("OVR",55)],h.Selections.Select(x=>new View[]{Text(x.Conference),Text(OffensiveLineService.Label(x.Player.Football)),Text(x.Player.Football.FullName),Text(x.Player.Football.TeamName),Text(x.Player.Football.OverallRating.ToString())}))); }
            if(honorsTab!="Showcase")return;
            var actions=new ActionRow();
            if(h.Showcase?.HasPlayed==true)actions.Add(Action("Showcase box score",()=>ShowBoxScore(h.Showcase)));
            else if(h.Year<s.Year||s.Phase==ProPhase.Offseason)
            {
                actions.Add(Action("Watch all-star showcase",()=>Showcase(h,GameDayMode.Watch)));
                actions.Add(Action("Play all-star showcase",()=>Showcase(h,GameDayMode.Play)));
                actions.Add(Action("Simulate showcase",async()=>{var session=ProHonors.StartShowcase(state!,h.Year,GameDayMode.Simulate);await Task.Run(()=>GameDayEngine.SimulateToEnd(session));state=ProHonors.FinishShowcase(state!,h.Year,session);await Save();Render();}));
            }
            details.Add(actions);details.Add(Text("Optional traditional football exhibition. Results, injuries and statistics stay separate from your franchise season.",12));
        }
        year.SelectedIndexChanged+=(_,_)=>Draw();tabs.SelectedIndexChanged+=(_,_)=>{honorsTab=(string)tabs.SelectedItem;Draw();};Draw();
    }
    async Task Showcase(ProSeasonHonors honors,GameDayMode mode)
    {
        var league=ProHonors.ShowcaseLeague(honors);var controlled=league.Teams[1].Name;
        if(mode==GameDayMode.Play){var selected=await GameDialogs.ActionSheetAsync(this,"Choose showcase team","Cancel",null,league.Teams.Select(t=>t.Name).ToArray());if(selected is null or "Cancel")return;controlled=selected;}
        var session=ProHonors.StartShowcase(state!,honors.Year,mode,controlled);
        await GameCastCoinToss.ShowAsync(Navigation,session,controlled,mode==GameDayMode.Play,name=>{var team=league.Teams.Single(t=>t.Name==name);return new(team.Primary,team.Secondary,team.Abbreviation);});
        await Navigation.PushModalAsync(new ProLivePage(session,league,async()=>{state=ProHonors.FinishShowcase(state!,honors.Year,session);await Save();Render();},chooseTeamOnTakeover:true));
    }
}
