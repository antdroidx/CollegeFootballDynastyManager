namespace DynastyManager.Data.Presentation;

/// <summary>Fictional editorial color only. Recorded facts stay in the source story body.</summary>
public static class NewsEditorial
{
    public static string Context(string category, string school) => category switch
    {
        "Injuries" => $"The depth chart is under the microscope at {school}. The next opportunity may belong to a reserve, while the staff balances preparation with the recovery timetable. Watch the injury report for availability rather than assuming a return date.",
        "Recruiting" => $"The class is taking shape at {school}. A commitment adds a new name to the program's future, but the bigger story is how the incoming group fits the returning roster. Follow the recruiting board as the rest of the class makes its decisions.",
        "Upset" => $"A result like this changes the conversation around {school}. The scoreboard is settled; the debate about what it means is just beginning. The next weekly polls and the conference race will provide the wider context.",
        "Preview" => $"Preparation takes center stage at {school}. Depth, availability and the matchup all deserve a closer look before kickoff. The schedule page has the game outlook; the final verdict will be delivered on the field.",
        "National championship" or "Championship" or "Playoff" or "Bowl" => $"These are the games that become part of a program's history. For {school}, this chapter belongs alongside the season's results and the people who produced them. Explore the box score and program records for the full picture.",
        _ => $"Around {school}, the focus now turns from the final whistle to what comes next. One result is part of a longer season: the standings, upcoming schedule and player performances tell the rest of the story. Follow the league's weekly recap for the broader view."
    };
}
