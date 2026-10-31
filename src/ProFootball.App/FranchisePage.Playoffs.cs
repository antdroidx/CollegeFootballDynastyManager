using DynastyManager.App;
using ProFootball.Core;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    void PlayoffTree()
    {
        var s=state!;body.Add(Text("Playoff bracket",25,true));body.Add(Text("Wild card → Divisional → Conference → Championship. Divisional matchups are reseeded after wild-card results; unconfirmed matchups remain TBD.",12));
        var years=s.History.Select(h=>h.Year).Append(s.Year).Distinct().OrderDescending().ToArray();var picker=new Picker{Title="Season",ItemsSource=years.Select(y=>y.ToString()).ToArray(),SelectedIndex=0};body.Add(picker);var bracket=new VerticalStackLayout{Spacing=16};body.Add(bracket);
        View Card(ProPlayoffSlot slot)
        {
            var game=slot.Game?.Football;var contents=new VerticalStackLayout{Spacing=5};
            foreach(var home in new[]{false,true})
            {
                var name=home?slot.Home:slot.Away;var seed=home?slot.HomeSeed:slot.AwaySeed;var club=s.League.Teams.FirstOrDefault(t=>t.Name==name);var score=game?.HasPlayed==true?(home?game.HomeScore:game.AwayScore)?.ToString():"";
                var row=new HorizontalStackLayout{Spacing=5};if(club is not null)row.Add(new TeamBadgeView(club,24));row.Add(Text((seed is null?"":"#"+seed+" ")+(club?.Abbreviation??"TBD")+"  "+score,14,game?.WinnerTeamName==name&&name is not null));contents.Add(row);
            }
            contents.Add(Text(game?.HasPlayed==true?"Final":slot.Projected?(slot.Round==1?"Projected seeds":"Awaiting results"):"Scheduled",11));
            if(game?.HasPlayed==true)contents.Add(Link("Box score",()=>BoxScore(game)));
            else if(slot.Game is {} live&&live.Phase==s.Phase&&live.Football.Week==s.Week){var actions=new ActionRow();actions.Add(Action("Watch",()=>Live(live,DynastyManager.Core.Models.GameDayMode.Watch)));actions.Add(Action("Play",()=>Live(live,DynastyManager.Core.Models.GameDayMode.Play)));contents.Add(actions);}
            return new Border{Padding=8,BackgroundColor=DynastyTheme.Panel,WidthRequest=190,Content=contents};
        }
        void Draw()
        {
            bracket.Clear();var year=years[picker.SelectedIndex];var slots=ProFootball.Core.ProPlayoffTree.Build(s,year);
            foreach(var conference in s.League.Teams.Select(t=>t.Conference).Distinct())
            {
                var rows=new Grid{ColumnSpacing=10,ColumnDefinitions=new(){new(190),new(24),new(190),new(24),new(190)}};
                var titles=new[]{"Wild card","Divisional","Conference final"};
                for(var round=1;round<=3;round++)
                {
                    var stack=new VerticalStackLayout{Spacing=14,VerticalOptions=LayoutOptions.Center};stack.Add(Text(titles[round-1],16,true));
                    foreach(var slot in slots.Where(x=>x.Conference==conference&&x.Round==round))stack.Add(Card(slot));rows.Add(stack,(round-1)*2,0);
                    if(round<3)rows.Add(Text("→",22,true),round*2-1,0);
                }
                var yearGames=s.Games.Concat(s.History.SelectMany(h=>h.Games)).Where(g=>g.Football.SeasonYear==year).DistinctBy(g=>g.Football.GameId).ToList();var seeds=ProStandings.Seeds(s with{Year=year,Games=yearGames},conference);
                bracket.Add(Text(conference+" · #1 "+seeds[0].Team.Abbreviation+" first-round bye",20,true));bracket.Add(new ScrollView{Orientation=ScrollOrientation.Horizontal,Content=rows});
            }
            bracket.Add(Text("Championship",22,true));bracket.Add(Card(slots.Single(x=>x.Round==4)));
        }
        picker.SelectedIndexChanged+=(_,_)=>Draw();
        Draw();
        // Playoff stories surface alongside the bracket so the page feels complete.
        state=ProNews.Refresh(state!);
        var playoffStories=state.News.Where(x=>x.Category=="Playoffs"||x.Category=="Awards").Take(5).ToArray();
        if(playoffStories.Length>0)
        {
            var stack=new VerticalStackLayout{Spacing=8};
            foreach(var story in playoffStories)
            {
                var n=story;
                stack.Add(new VerticalStackLayout{Spacing=2,Children={new Label{Text=n.Headline,FontSize=12,FontAttributes=FontAttributes.Bold,TextColor=DynastyTheme.Text},new Label{Text=$"{n.Year} · W{n.Week} · {n.Category}",FontSize=10,TextColor=DynastyTheme.Muted}}});
            }
            body.Add(SectionCard("Playoff & awards news",null,stack,Action("See all ›",()=>Open("News"))));
        }
    }
}
