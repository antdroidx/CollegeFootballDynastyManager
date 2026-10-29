using DynastyManager.Core.Models;
namespace DynastyManager.Core.Simulation;

/// <summary>One-time new-save generation. Persist the resulting players, never regenerate on load.</summary>
public static class FictionalRosterService
{
    public static IReadOnlyList<DynastyPlayer> College(IReadOnlyList<DynastyPlayer> template,IReadOnlyList<Team> teams,Random? random=null)
    {
        random??=new Random();var context=new DynastyState{DynastyName="Generated roster",UserTeamName=teams[0].Name};
        
        return template.Select(p=>{
            var target=Math.Clamp(p.OverallRating+random.Next(-5,6),40,96);
            var year=random.Next(1,5);return Player(p,template,random,context,target,year) with{PotentialRating=Math.Clamp(target+random.Next(2,Math.Max(3,17-year*2)),target,99)};
        }).ToArray();
    }
    public static DynastyPlayer Player(DynastyPlayer template,IReadOnlyList<DynastyPlayer> names,Random random,DynastyState context,int overall,int year,DynastyPlayer? archetype=null)
    {
        var bytes=new byte[16];random.NextBytes(bytes);var first=names[random.Next(names.Count)].FullName.Split(' ',StringSplitOptions.RemoveEmptyEntries)[0];
        var parts=names[random.Next(names.Count)].FullName.Split(' ',StringSplitOptions.RemoveEmptyEntries);var last=parts.LastOrDefault(p=>p is not ("Jr." or "Jr" or "II" or "III" or "IV"))??"Carter";
        var p=template with{PlayerId=new Guid(bytes),FullName=first+" "+last,ClassYear=year,OverallRating=overall,SeasonStartOverall=overall,PotentialRating=Math.Min(99,overall+5),TalentLevel=3,
            SpeedRating=0,StrengthRating=0,AgilityRating=0,AwarenessRating=0,InjuryRating=0,Skills=null,CurrentInjury=null,Appearance=null,HeightInches=0,WeightPounds=0,IsRedshirted=false,IsWalkOn=false,
            JerseyNumber=random.Next(1,100),OriginalRecruitingStars=overall>=86?5:overall>=78?4:overall>=68?3:overall>=58?2:1,RecruitingStarsEstimated=true};
        archetype??=template;
        if(archetype.Skills is not null && archetype.SpeedRating>0)
        {
            // Preserve realistic position-specific athletic/skill profiles, while varying ratings.
            var basis=Ncaa06RosterRatings.Export(archetype).Where(x=>Ncaa06RosterRatings.RatingFields.Contains(x.Key)).ToDictionary(x=>x.Key,x=>Math.Clamp(x.Value+random.Next(-4,5),1,99));
            var best=p;var distance=int.MaxValue;
            for(var shift=-20;shift<=20;shift++)
            {
                var candidate=Ncaa06RosterRatings.Apply(p,basis.ToDictionary(x=>x.Key,x=>Math.Clamp(x.Value+shift,1,99)));
                var gap=Math.Abs(candidate.OverallRating-overall);if(gap<distance){best=candidate;distance=gap;}if(distance==0)break;
            }
            p=best;
        }
        else p=PlayerRatingService.EnsureInitialized(context,p);
        p=PlayerPhysicalService.EnsureMeasurements(p);
        return p with{SeasonStartOverall=p.OverallRating};
    }
}
