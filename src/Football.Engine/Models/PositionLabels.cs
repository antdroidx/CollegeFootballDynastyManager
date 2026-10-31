namespace DynastyManager.Core.Models;
public static class PositionLabels
{
    public static string DisplayName(this Position position) => position==Position.RB?"HB":position.ToString();
    public static Position Parse(string name) => name=="HB"?Position.RB:Enum.Parse<Position>(name);
    public static bool TryParse(string? name,out Position position) => Enum.TryParse(name=="HB"?"RB":name,out position);
    public static int RosterOrder(this Position position)=>position switch{Position.QB=>0,Position.RB=>1,Position.FB=>2,Position.K=>13,Position.P=>14,_=>(int)position+1};
    public static bool IsSpecialist(this Position position) => position is Position.K or Position.P;
    public static DynastyPlayer NormalizeSpecialistPosition(this DynastyPlayer player) => player.NcaaPositionCode switch
    {
        19 => player.Position == Position.K ? player : player with { Position = Position.K },
        20 => player.Position == Position.P ? player : player with { Position = Position.P },
        _ => player
    };
    public static string[] Names => Enum.GetValues<Position>().OrderBy(RosterOrder).Select(DisplayName).ToArray();
}
