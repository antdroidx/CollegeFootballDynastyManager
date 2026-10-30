using DynastyManager.Core.Models;
namespace DynastyManager.Core.Simulation;
public static class PlayerStatCatalog
{
    public static readonly string[] Categories=["Passing","Rushing","Receiving","Blocking","Defense","Kicking","Punting","Returns","Playing time"];
    public static readonly Dictionary<string,string[]> Fields=new()
    {
        ["Passing"]=["Games","PassCompletions","PassAttempts","Cmp%","PassYards","PassingTouchdowns","InterceptionsThrown","PassYPG","YPA","Rate","TrackedGames","Dropbacks","TimesSacked","SackYardsLost","PressuredDropbacks","Press%","LongestPass"],
        ["Rushing"]=["Games","RushAttempts","RushYards","YPC","RushingFumbles","RushYPG","RushingTouchdowns","Rushing20Plus","TrackedGames","LongestRush","BrokenTackles"],
        ["Receiving"]=["Games","Receptions","ReceivingYards","ReceivingTouchdowns","YPR","YardsAfterCatch","Drops","ReceivingFumbles","Receiving20Plus","RecYPG","TrackedGames","Targets","Catch%","LongestReception"],
        ["Blocking"]=["TrackedGames","PassBlockSnaps","RunBlockSnaps","PassBlockWins","PB Win%","RunBlockWins","RB Win%","PressuresAllowed","Pressure%","SacksAllowed","Penalties","PenaltyYards"],
        ["Defense"]=["Games","Tackles","TacklesForLoss","Sacks","PassesDefended","Interceptions","FumbleRecoveries","DefensiveReturnYards","DefensiveTouchdowns","TrackedGames","DefensiveSnaps","DefensivePressures","ForcedFumbles","CoverageTargets","CoverageReceptions","CoverageYards","Allowed%"],
        ["Kicking"]=["Games","ExtraPointsMade","ExtraPointsAttempted","FieldGoalsMade","FieldGoalsAttempted","FG%","TrackedGames","LongFieldGoal","FieldGoalsMadeUnder40","FieldGoalsAttemptedUnder40","FieldGoalsMade40To49","FieldGoalsAttempted40To49","FieldGoalsMade50Plus","FieldGoalsAttempted50Plus"],
        ["Punting"]=["Games","Punts","PuntAvg","LongPunt","PuntsInside20","PuntsInside10","TrackedGames","NetAvg","PuntTouchbacks"],
        ["Returns"]=["Games","KickReturns","KickReturnYards","KRAvg","KickReturnTouchdowns","PuntReturns","PuntReturnYards","PRAvg","PuntReturnTouchdowns","TrackedGames","FairCatches"],
        ["Playing time"]=["Games","TrackedGames","OffensiveSnaps","DefensiveSnaps","SpecialTeamsSnaps","Penalties","PenaltyYards"]
    };
    static readonly Dictionary<string,string> Labels=new()
    {
        ["PassYPG"]="YPG",["RushYPG"]="YPG",["RecYPG"]="YPG",["BrokenTackles"]="BTK",["Games"]="GP",["TrackedGames"]="Tracked GP",["PassCompletions"]="Cmp",["PassAttempts"]="Att",["PassYards"]="Yds",["PassingTouchdowns"]="TD",["InterceptionsThrown"]="INT",["RushAttempts"]="Att",["RushYards"]="Yds",["RushingTouchdowns"]="TD",["RushingFumbles"]="Fum",["Rushing20Plus"]="20+",["Receptions"]="Rec",["ReceivingYards"]="Yds",["ReceivingTouchdowns"]="TD",["YardsAfterCatch"]="YAC",["Drops"]="Drop",["ReceivingFumbles"]="Fum",["Receiving20Plus"]="20+",["Tackles"]="Tkl",["TacklesForLoss"]="TFL",["PassesDefended"]="PBU",["Interceptions"]="INT",["FumbleRecoveries"]="FR",["DefensiveReturnYards"]="Ret yd",["DefensiveTouchdowns"]="Def TD",["ExtraPointsMade"]="XPM",["ExtraPointsAttempted"]="XPA",["FieldGoalsMade"]="FGM",["FieldGoalsAttempted"]="FGA",["PuntAvg"]="Avg",["LongPunt"]="Long",["PuntsInside20"]="In 20",["PuntsInside10"]="In 10",["KickReturns"]="KR",["KickReturnYards"]="KR yds",["KRAvg"]="KR avg",["KickReturnTouchdowns"]="KR TD",["PuntReturns"]="PR",["PuntReturnYards"]="PR yds",["PRAvg"]="PR avg",["PuntReturnTouchdowns"]="PR TD",
        ["OffensiveSnaps"]="Off snaps",["DefensiveSnaps"]="Def snaps",["SpecialTeamsSnaps"]="ST snaps",["PassBlockSnaps"]="PB snaps",["RunBlockSnaps"]="RB snaps",["PassBlockWins"]="PB wins",["RunBlockWins"]="RB wins",["PressuresAllowed"]="Press A",["SacksAllowed"]="Sacks A",["PressuredDropbacks"]="Press DB",["TimesSacked"]="Sacked",["SackYardsLost"]="Sk yds",["LongestPass"]="Long",["LongestRush"]="Long",["LongestReception"]="Long",["DefensivePressures"]="Press",["ForcedFumbles"]="FF",["CoverageTargets"]="Cov tgt",["CoverageReceptions"]="Rec A",["CoverageYards"]="Yds A",["Penalties"]="Pen",["PenaltyYards"]="Pen yd",["LongFieldGoal"]="Long",["FieldGoalsMadeUnder40"]="<40 M",["FieldGoalsAttemptedUnder40"]="<40 A",["FieldGoalsMade40To49"]="40–49 M",["FieldGoalsAttempted40To49"]="40–49 A",["FieldGoalsMade50Plus"]="50+ M",["FieldGoalsAttempted50Plus"]="50+ A",["PuntTouchbacks"]="TB",["NetAvg"]="Net avg",["FairCatches"]="FC"
    };
    public static string Label(string field)=>Labels.GetValueOrDefault(field,field);
    static readonly HashSet<string> NewFields=new(["TrackedGames","OffensiveSnaps","DefensiveSnaps","SpecialTeamsSnaps","PassBlockSnaps","RunBlockSnaps","PassBlockWins","RunBlockWins","PressuresAllowed","SacksAllowed","Dropbacks","PressuredDropbacks","TimesSacked","SackYardsLost","Targets","TrackedReceptions","LongestPass","BrokenTackles","LongestRush","LongestReception","DefensivePressures","ForcedFumbles","CoverageTargets","CoverageReceptions","CoverageYards","Penalties","PenaltyYards","LongFieldGoal","FieldGoalsMadeUnder40","FieldGoalsAttemptedUnder40","FieldGoalsMade40To49","FieldGoalsAttempted40To49","FieldGoalsMade50Plus","FieldGoalsAttempted50Plus","PuntTouchbacks","TrackedPunts","NetPuntYards","FairCatches","PB Win%","RB Win%","Pressure%","Press%","Catch%","Allowed%","NetAvg"]);
    public static double? Value(PlayerSeasonStatLine s,string field)
    {
        if(NewFields.Contains(field)&&field!="TrackedGames"&&s.TrackedGames==0)return null;
        double? Ratio(int n,int d,double multiplier=1)=>d==0?null:n/(double)d*multiplier;
        return field switch {
            "Cmp%"=>Ratio(s.PassCompletions,s.PassAttempts,100),"YPA"=>Ratio(s.PassYards,s.PassAttempts),"PassYPG"=>Ratio(s.PassYards,s.Games),"RushYPG"=>Ratio(s.RushYards,s.Games),"RecYPG"=>Ratio(s.ReceivingYards,s.Games),
            "YPC"=>Ratio(s.RushYards,s.RushAttempts),"YPR"=>Ratio(s.ReceivingYards,s.Receptions),"FG%"=>Ratio(s.FieldGoalsMade,s.FieldGoalsAttempted,100),"PuntAvg"=>Ratio(s.PuntYards,s.Punts),"KRAvg"=>Ratio(s.KickReturnYards,s.KickReturns),"PRAvg"=>Ratio(s.PuntReturnYards,s.PuntReturns),
            "PB Win%"=>Ratio(s.PassBlockWins,s.PassBlockSnaps,100),"RB Win%"=>Ratio(s.RunBlockWins,s.RunBlockSnaps,100),"Pressure%"=>Ratio(s.PressuresAllowed,s.PassBlockSnaps,100),"Press%"=>Ratio(s.PressuredDropbacks,s.Dropbacks,100),"Catch%"=>Ratio(s.TrackedReceptions,s.Targets,100),"Allowed%"=>Ratio(s.CoverageReceptions,s.CoverageTargets,100),"NetAvg"=>Ratio(s.NetPuntYards,s.TrackedPunts),
            "Rate"=>null,_=>typeof(PlayerSeasonStatLine).GetProperty(field)?.GetValue(s) is int n?n:null};
    }
    public static string Format(PlayerSeasonStatLine line,string field)=>Value(line,field) is double value?value.ToString(field.Contains('%')||field is "YPA" or "PassYPG" or "RushYPG" or "RecYPG" or "YPC" or "YPR" or "PuntAvg" or "KRAvg" or "PRAvg" or "NetAvg"?"0.0":"0"):"—";
    public static bool Eligible(PlayerSeasonStatLine s,string category)=>category switch {
        "Passing"=>s.PassAttempts>0||s.TimesSacked>0,"Rushing"=>s.RushAttempts>0,"Receiving"=>s.Receptions>0||s.Targets>0,"Blocking"=>s.PassBlockSnaps+s.RunBlockSnaps>0,"Defense"=>s.DefensiveSnaps+s.Tackles+s.DefensivePressures+s.Interceptions+s.PassesDefended+s.FumbleRecoveries>0,"Kicking"=>s.FieldGoalsAttempted+s.ExtraPointsAttempted>0,"Punting"=>s.Punts>0,"Returns"=>s.KickReturns+s.PuntReturns+s.FairCatches>0,_=>s.OffensiveSnaps+s.DefensiveSnaps+s.SpecialTeamsSnaps+s.Penalties>0};
}
