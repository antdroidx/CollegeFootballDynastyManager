using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
namespace DynastyManager.App;
public static class PlayerEfficiencyView
{
    public static View Create(IEnumerable<PlayerSeasonStatLine> lines,Guid id)
    {
        var metrics=PlayerEfficiency.Build(lines,id);var stack=new VerticalStackLayout{Spacing=8};
        stack.Add(new Label{Text="Player efficiency · same-position peers",FontSize=17,FontAttributes=FontAttributes.Bold,TextColor=DynastyTheme.Text});
        if(metrics.Count==0)stack.Add(new Label{Text="Rates appear after 20 pass attempts, 10 carries or 5 catches. No assignment grades are inferred from box scores.",TextColor=DynastyTheme.Muted,FontSize=12});
        else stack.Add(FootballDataGrid.Create([("Metric",190),("Player",70),("League",70),("Rank",65),("Sample",65)],metrics.Select(m=>new View[]{Cell(m.Name),Cell(m.Value.ToString("0.00")),Cell(m.PeerAverage.ToString("0.00")),Cell($"{m.Rank}/{m.Players}"),Cell(m.Opportunities.ToString())})));
        return stack;
    }
    static Label Cell(string s)=>new(){Text=s,TextColor=DynastyTheme.Text};
}
