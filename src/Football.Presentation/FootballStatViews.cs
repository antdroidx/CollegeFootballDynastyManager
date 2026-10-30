using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
namespace DynastyManager.App;
public static class FootballStatViews
{
    public static bool Eligible(PlayerSeasonStatLine line,string category)=>PlayerStatCatalog.Eligible(line,category);
    public static (string Title,double Width)[] Columns(string category)=>new[]{("Player",165d),("School",195d)}.Concat(PlayerStatCatalog.Fields[category].Select(f=>(PlayerStatCatalog.Label(f),f=="TrackedGames"?85d:75d))).ToArray();
    public static View[] Cells(PlayerSeasonStatLine line,string category,Func<Guid,string,View> PlayerLink,Func<string,View> SchoolCell,Func<string,View> TableText,Func<int,int,string> Percent,Func<int,int,string> Average,Func<PlayerSeasonStatLine,string> PasserRating)
        =>new[]{PlayerLink(line.PlayerId,line.PlayerName),SchoolCell(line.TeamName)}.Concat(PlayerStatCatalog.Fields[category].Select(f=>TableText(f=="Rate"?(line.PassAttempts>0?PasserRating(line):"—"):PlayerStatCatalog.Format(line,f)))).ToArray();
}
