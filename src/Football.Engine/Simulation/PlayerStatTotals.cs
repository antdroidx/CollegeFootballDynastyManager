using System.Reflection;
using DynastyManager.Core.Models;
namespace DynastyManager.Core.Simulation;
public static class PlayerStatTotals
{
    static readonly Dictionary<string,PropertyInfo> Counters=typeof(PlayerSeasonStatLine).GetProperties().Where(p=>p.PropertyType==typeof(int)&&p.CanWrite&&p.Name is not ("SeasonYear" or "HeightInches" or "WeightPounds")).ToDictionary(p=>p.Name);
    static bool Maximum(string name)=>name.StartsWith("Longest")||name is "LongPunt" or "LongFieldGoal";
    public static PlayerSeasonStatLine Add(PlayerSeasonStatLine a,PlayerSeasonStatLine b)
    {
        var result=a with{};
        foreach(var (name,p) in Counters){var x=(int)p.GetValue(a)!;var y=(int)p.GetValue(b)!;p.SetValue(result,Maximum(name)?Math.Max(x,y):checked(x+y));}
        return result;
    }
    public static PlayerSeasonStatLine Total(IEnumerable<PlayerSeasonStatLine> lines)
    {
        var rows=lines.OrderByDescending(p=>p.SeasonYear).ToArray();return rows.Skip(1).Aggregate(rows[0],Add);
    }
    public static PlayerSeasonStatLine Increment(PlayerSeasonStatLine line,string field,int amount)
    {var result=line with{};var p=Counters[field];p.SetValue(result,checked((int)p.GetValue(line)!+amount));return result;}
    public static PlayerSeasonStatLine Longest(PlayerSeasonStatLine line,string field,int value)
    {var result=line with{};var p=Counters[field];p.SetValue(result,Math.Max((int)p.GetValue(line)!,value));return result;}
}
