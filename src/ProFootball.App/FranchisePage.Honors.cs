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
        var s=state!;body.Add(Text("Awards & All-Pros",25,true));
        if(s.Honors.Count==0){body.Add(Text("Season awards and All-Pro teams are announced after the regular season."));return;}
        var years=s.Honors.OrderByDescending(h=>h.Year).ToArray();var year=new Picker{ItemsSource=years.Select(h=>h.Year.ToString()).ToArray(),SelectedIndex=0};body.Add(year);var tabs=new Picker {ItemsSource=new[]{"Awards","All-Pros","Pro Bowl"},SelectedIndex=Array.IndexOf(new[]{"Awards","All-Pros","Pro Bowl"},honorsTab)};body.Add(tabs);var details=new VerticalStackLayout{Spacing=10};body.Add(details);
        void Draw()
        {
            details.Clear();var h=years[year.SelectedIndex];details.Add(Text(h.Year+" season honors",21,true));
            if(honorsTab=="Awards") details.Add(Table([("Award",240),("Winner",190),("Team",180)],h.Awards.Select(a=>new View[]{Text(a.Title),Text(a.Name),Text(a.Team)})));
            if(honorsTab=="All-Pros") { details.Add(Text("First and second All-Pro teams",21,true));details.Add(Text("League-wide recognition by position.",12));
            details.Add(Table([("All-Pro team",105),("Pos",55),("Player",200),("Team",180),("OVR",55)],(h.AllPros.Count>0?h.AllPros:ProHonors.SelectAllPros(h.Selections.Select(x=>x.Player))).Select(x=>new View[]{Text(x.Team==1?"First team":"Second team"),Text(OffensiveLineService.Label(x.Player.Football)),Text(x.Player.Football.FullName),Text(x.Player.Football.TeamName),Text(x.Player.Football.OverallRating.ToString())}))); }
            if(honorsTab!="Pro Bowl")return;
            var actions=new ActionRow();
            if(h.Showcase?.HasPlayed==true)actions.Add(Action("Pro Bowl box score",()=>ShowBoxScore(h.Showcase)));
            else if(h.Year<s.Year||s.Phase==ProPhase.Offseason||s.Phase==ProPhase.Playoffs&&s.Week==4)
            {
                actions.Add(Action("Watch Pro Bowl",()=>Showcase(h,GameDayMode.Watch)));
                actions.Add(Action("Play Pro Bowl",()=>Showcase(h,GameDayMode.Play)));
                actions.Add(Action("Simulate Pro Bowl",async()=>{var session=ProHonors.StartShowcase(state!,h.Year,GameDayMode.Simulate);await Task.Run(()=>GameDayEngine.SimulateToEnd(session));state=ProHonors.FinishShowcase(state!,h.Year,session);await Save();Render();}));
            }
            details.Add(actions);details.Add(Text("AFC vs NFC all-star game. Available after the conference championships. Participation does not affect league standings, injuries or career statistics.",12));
        }
        year.SelectedIndexChanged+=(_,_)=>Draw();tabs.SelectedIndexChanged+=(_,_)=>{honorsTab=(string)tabs.SelectedItem;Draw();};Draw();
    }
    async Task Showcase(ProSeasonHonors honors,GameDayMode mode)
    {
        var league=ProHonors.ShowcaseLeague(honors);var controlled=league.Teams[1].Name;
        if(mode==GameDayMode.Play){var selected=await GameDialogs.ActionSheetAsync(this,"Choose Pro Bowl team","Cancel",null,league.Teams.Select(t=>t.Name).ToArray());if(selected is null or "Cancel")return;controlled=selected;}
        var session=ProHonors.StartShowcase(state!,honors.Year,mode,controlled);
        await GameCastCoinToss.ShowAsync(Navigation,session,controlled,mode==GameDayMode.Play,name=>{var team=league.Teams.Single(t=>t.Name==name);return new(team.Primary,team.Secondary,team.Abbreviation);});
        await Navigation.PushModalAsync(new ProLivePage(session,league,async()=>{state=ProHonors.FinishShowcase(state!,honors.Year,session);await Save();Render();},chooseTeamOnTakeover:true));
    }
}
