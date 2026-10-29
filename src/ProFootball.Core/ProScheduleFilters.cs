namespace ProFootball.Core;
public sealed record ProScheduleWeek(ProPhase? Phase,int? Week,string Label);
public static class ProScheduleFilters
{
    public static IReadOnlyList<ProScheduleWeek> Weeks(ProPhase currentPhase,int currentWeek,ProPhase? selectedPhase)
    {
        var result=new List<ProScheduleWeek>{new(null,null,"All weeks")};
        foreach(var phase in new[]{ProPhase.Preseason,ProPhase.RegularSeason,ProPhase.Playoffs}.Where(p=>selectedPhase is null||p==selectedPhase))
        {
            var count=phase==ProPhase.Preseason?3:phase==ProPhase.RegularSeason?18:4;
            for(var week=1;week<=count;week++)
            {
                var label=phase==ProPhase.Preseason?$"Preseason · P{week}":phase==ProPhase.RegularSeason?$"Regular season · W{week}":new[]{"Wild card","Divisional round","Conference championships","Championship"}[week-1];
                result.Add(new(phase,week,label+(phase==currentPhase&&week==currentWeek?" · Current":"")));
            }
        }
        return result;
    }
}
