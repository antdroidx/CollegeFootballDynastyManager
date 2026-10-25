using DynastyManager.Core.Models;
namespace DynastyManager.App;

/// <summary>Shared college table schemas and cells; each host supplies identity links and league passer rating.</summary>
public static class FootballStatViews
{
    public static bool Eligible(PlayerSeasonStatLine line, string category) => category switch
    {
        "Passing" => line.PassAttempts > 0,
        "Rushing" => line.RushAttempts > 0,
        "Receiving" => line.Receptions > 0 || line.ReceivingYards > 0,
        "Defense" => line.Tackles > 0 || line.Sacks > 0 || line.Interceptions > 0 || line.PassesDefended > 0 || line.FumbleRecoveries > 0,
        "Kicking" => line.ExtraPointsAttempted > 0 || line.FieldGoalsAttempted > 0,
        "Punting" => line.Punts > 0,
        "Returns" => line.KickReturns > 0 || line.PuntReturns > 0,
        _ => true
    };

    public static (string Title, double Width)[] Columns(string category) => category switch
    {
        "Passing" => [("Player", 165), ("School", 195), ("GP", 45), ("Cmp", 55), ("Att", 55), ("Cmp%", 60),
            ("Yds", 75), ("TD", 45), ("INT", 45), ("YPG", 60), ("YPA", 55), ("Rate", 60)],
        "Rushing" => [("Player", 165), ("School", 195), ("GP", 45), ("Att", 55), ("Yds", 75), ("YPC", 55),
            ("Fum", 48), ("YPG", 60), ("TD", 45), ("20+", 48)],
        "Receiving" => [("Player", 165), ("School", 195), ("GP", 45), ("Rec", 55), ("Yds", 75), ("TD", 45),
            ("YPR", 55), ("YAC", 60), ("Drop", 50), ("Fum", 48), ("20+", 48), ("YPG", 60)],
        "Defense" => [("Player", 165), ("School", 195), ("GP", 45), ("Tkl", 55), ("TFL", 50), ("Sack", 55),
            ("PBU", 56), ("INT", 48), ("FR", 45), ("Ret yd", 65), ("Def TD", 65)],
        "Kicking" => [("Player", 165), ("School", 195), ("GP", 45), ("XPM", 50), ("XPA", 50),
            ("FGM", 50), ("FGA", 50), ("FG%", 60)],
        "Punting" => [("Player", 165), ("School", 195), ("GP", 45), ("Punts", 55), ("Avg", 55),
            ("Long", 55), ("In 20", 55), ("In 10", 55)],
        _ => [("Player", 165), ("School", 195), ("GP", 45), ("KR", 45), ("KR yds", 70), ("KR avg", 60),
            ("KR TD", 55), ("PR", 45), ("PR yds", 70), ("PR avg", 60), ("PR TD", 55)]
    };

    public static View[] Cells(PlayerSeasonStatLine line, string category,
            Func<Guid, string, View> PlayerLink, Func<string, View> SchoolCell, Func<string, View> TableText,
            Func<int, int, string> Percent, Func<int, int, string> Average, Func<PlayerSeasonStatLine, string> PasserRating)
    {
        View[] Start() => [PlayerLink(line.PlayerId, line.PlayerName), SchoolCell(line.TeamName), TableText(line.Games.ToString())];
        var cells = Start().ToList();
        void Add(object value) => cells.Add(TableText(value.ToString() ?? "—"));
        switch (category)
        {
            case "Passing":
                Add(line.PassCompletions); Add(line.PassAttempts); Add(Percent(line.PassCompletions, line.PassAttempts));
                Add(line.PassYards.ToString("N0")); Add(line.PassingTouchdowns); Add(line.InterceptionsThrown);
                Add(Average(line.PassYards, line.Games)); Add(Average(line.PassYards, line.PassAttempts)); Add(PasserRating(line)); break;
            case "Rushing":
                Add(line.RushAttempts); Add(line.RushYards.ToString("N0")); Add(Average(line.RushYards, line.RushAttempts));
                Add(line.RushingFumbles); Add(Average(line.RushYards, line.Games)); Add(line.RushingTouchdowns); Add(line.Rushing20Plus); break;
            case "Receiving":
                Add(line.Receptions); Add(line.ReceivingYards.ToString("N0")); Add(line.ReceivingTouchdowns);
                Add(Average(line.ReceivingYards, line.Receptions)); Add(line.YardsAfterCatch); Add(line.Drops);
                Add(line.ReceivingFumbles); Add(line.Receiving20Plus); Add(Average(line.ReceivingYards, line.Games)); break;
            case "Defense":
                Add(line.Tackles); Add(line.TacklesForLoss); Add(line.Sacks); Add(line.PassesDefended);
                Add(line.Interceptions); Add(line.FumbleRecoveries); Add(line.DefensiveReturnYards); Add(line.DefensiveTouchdowns); break;
            case "Kicking":
                Add(line.ExtraPointsMade); Add(line.ExtraPointsAttempted); Add(line.FieldGoalsMade);
                Add(line.FieldGoalsAttempted); Add(Percent(line.FieldGoalsMade, line.FieldGoalsAttempted)); break;
            case "Punting":
                Add(line.Punts); Add(Average(line.PuntYards, line.Punts)); Add(line.LongPunt);
                Add(line.PuntsInside20); Add(line.PuntsInside10); break;
            default:
                Add(line.KickReturns); Add(line.KickReturnYards); Add(Average(line.KickReturnYards, line.KickReturns));
                Add(line.KickReturnTouchdowns); Add(line.PuntReturns); Add(line.PuntReturnYards);
                Add(Average(line.PuntReturnYards, line.PuntReturns)); Add(line.PuntReturnTouchdowns); break;
        }
        return cells.ToArray();
    }

}
