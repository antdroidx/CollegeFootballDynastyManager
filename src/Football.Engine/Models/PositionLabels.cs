namespace DynastyManager.Core.Models;
public static class PositionLabels
{
    public static string DisplayName(this Position position) => position==Position.RB?"HB":position.ToString();
    public static Position Parse(string name) => name=="HB"?Position.RB:Enum.Parse<Position>(name);
    public static bool TryParse(string? name,out Position position) => Enum.TryParse(name=="HB"?"RB":name,out position);
    public static string[] Names => Enum.GetValues<Position>().OrderBy(p=>p switch {Position.QB=>0,Position.RB=>1,Position.FB=>2,_=>(int)p+2}).Select(DisplayName).ToArray();
}
