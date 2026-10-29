using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class PersonnelRotationTests
{
    [Fact]
    public void InjuredPlayersNeverEnterAndPackageControlsPersonnel()
    {
        var roster = Offense();
        var injured = roster.Single(player => player.Position == Position.RB && player.DepthChartOrder == 1);
        roster = roster.Select(player => player.PlayerId == injured.PlayerId
            ? player with { CurrentInjury = new PlayerInjury { WeeksRemaining = 2 } } : player).ToArray();

        var personnel = PersonnelRotationService.SelectOffense(roster, "Test", OffensivePersonnel.Twelve,
            new GameFatigueState(), new Random(4));

        Assert.DoesNotContain(injured.PlayerId, personnel.PlayerIds);
        Assert.Equal(2, personnel.PlayerIds.Count(id => roster.Single(player => player.PlayerId == id).Position == Position.TE));
        Assert.Equal(5, personnel.PlayerIds.Count(id => roster.Single(player => player.PlayerId == id).Position == Position.OL));
    }

    [Fact]
    public void ExhaustionRotatesBacksButHealthyOffensiveLineStaysTogether()
    {
        var roster = Offense();
        var fatigue = new GameFatigueState();
        var rb1 = roster.Single(player => player.Position == Position.RB && player.DepthChartOrder == 1);
        var ol1 = roster.Single(player => player.Position == Position.OL && player.DepthChartOrder == 1);
        fatigue.Set(rb1.PlayerId, 10);
        fatigue.Set(ol1.PlayerId, 90);

        var personnel = PersonnelRotationService.SelectOffense(roster, "Test", OffensivePersonnel.Eleven, fatigue, new Random(2));

        Assert.DoesNotContain(rb1.PlayerId, personnel.PlayerIds);
        Assert.Contains(ol1.PlayerId, personnel.PlayerIds);
    }

    [Fact] public void ThreeBacksShareWorkWhileHealthyQuarterbackAndLineStayStable()
    {
        var roster=Offense();var fatigue=new GameFatigueState();var random=new Random(712);var counts=new Dictionary<Guid,int>();var qb=roster.First(p=>p.Position==Position.QB);
        for(var i=0;i<1000;i++)
        {
            var unit=PersonnelRotationService.SelectOffense(roster,"Test",OffensivePersonnel.Eleven,fatigue,random);
            Assert.Equal(11,unit.PlayerIds.Count);Assert.Equal(11,unit.PlayerIds.Distinct().Count());Assert.Contains(qb.PlayerId,unit.PlayerIds);
            foreach(var id in unit.PlayerIds)counts[id]=counts.GetValueOrDefault(id)+1;
        }
        Assert.True(roster.Count(p=>p.Position==Position.RB&&counts.GetValueOrDefault(p.PlayerId)>15)>=3);
        Assert.All(roster.Where(p=>p.Position==Position.OL&&p.DepthChartOrder<=5),p=>Assert.Equal(1000,counts[p.PlayerId]));
    }
    [Fact] public void ThinPackageUsesHealthySameUnitAlternativesToFieldEleven()
    {
        var roster=Offense().Where(p=>p.Position!=Position.TE).ToArray();var unit=PersonnelRotationService.SelectOffense(roster,"Test",OffensivePersonnel.Twelve,new(),new Random(2));
        Assert.Equal(11,unit.PlayerIds.Count);Assert.Equal(11,unit.PlayerIds.Distinct().Count());Assert.All(unit.PlayerIds,id=>Assert.Contains(roster,p=>p.PlayerId==id));
    }
    [Fact] public void DefensiveFrontRotatesBeyondTheFirstBackupPair()
    {
        var offense=Offense();var roster=offense.Where(p=>p.Position!=Position.QB).Take(30).Select((p,i)=>p with{Position=i<6?Position.DE:i<12?Position.DT:i<16?Position.OLB:i<20?Position.MLB:i<26?Position.CB:i<28?Position.FS:Position.SS,DepthChartOrder=i%6+1}).ToArray();
        var used=new HashSet<Guid>();var fatigue=new GameFatigueState();var random=new Random(42);
        for(var i=0;i<400;i++){var unit=PersonnelRotationService.SelectDefense(roster,"Test",DefensivePersonnel.BaseFourThree,fatigue,random);Assert.Equal(11,unit.PlayerIds.Distinct().Count());used.UnionWith(unit.PlayerIds);}
        Assert.True(roster.Count(p=>p.Position==Position.DE&&used.Contains(p.PlayerId))>=4);Assert.True(roster.Count(p=>p.Position==Position.DT&&used.Contains(p.PlayerId))>=4);
    }

    private static DynastyPlayer[] Offense()
    {
        var players = new List<DynastyPlayer>();
        Add(Position.QB, 2); Add(Position.RB, 4); Add(Position.WR, 7); Add(Position.TE, 4); Add(Position.OL, 10);
        return players.ToArray();
        void Add(Position position, int count)
        {
            for (var i = 1; i <= count; i++) players.Add(new DynastyPlayer
            {
                PlayerId = Guid.NewGuid(), FullName = $"{position} {i}", TeamName = "Test", Position = position,
                ClassYear = 2, TalentLevel = 70, OverallRating = 90 - i, PotentialRating = 90,
                SpeedRating = 70, StrengthRating = 70, AgilityRating = 70, AwarenessRating = 70,
                InjuryRating = 80, DepthChartOrder = i
            });
        }
    }
}
