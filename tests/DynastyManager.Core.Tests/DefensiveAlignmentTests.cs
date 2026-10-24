using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class DefensiveAlignmentTests
{
    [Theory]
    [InlineData(DefensiveFormation.FourThree,2,3)]
    [InlineData(DefensiveFormation.Nickel,3,2)]
    [InlineData(DefensiveFormation.Dime,4,1)]
    public void DefaultPackagesHaveDistinctSymmetricStarts(DefensiveFormation formation,int corners,int linebackers)
    {
        var roster=GameDayEngineTests.Session(GameDayMode.Play).AwayRoster;
        var players=roster.Where(p=>p.Position==Position.DE).Take(2)
            .Concat(roster.Where(p=>p.Position==Position.DT).Take(2))
            .Concat(roster.Where(p=>p.Position is Position.MLB or Position.OLB).Take(linebackers))
            .Concat(roster.Where(p=>p.Position==Position.CB).Take(corners))
            .Concat(roster.Where(p=>p.Position==Position.FS).Take(1))
            .Concat(roster.Where(p=>p.Position==Position.SS).Take(1)).ToArray();
        var play=new DefensivePlayDefinition("test","Test",formation,DefensivePersonnel.Nickel,DefensiveApproach.Balanced,"");
        var starts=DefensivePursuitService.Alignments(players,play);
        Assert.Equal(11,players.Length);Assert.Equal(11,starts.Values.Distinct().Count());
        var edges=players.Where(p=>p.Position==Position.DE).Select(p=>starts[p.PlayerId].Y).ToArray();
        Assert.Equal(1,edges.Sum(),6);
        var lbLanes=players.Where(p=>p.Position is Position.MLB or Position.OLB).Select(p=>starts[p.PlayerId].Y);
        Assert.Equal(.5,lbLanes.Average(),6);
        var cb=players.Where(p=>p.Position==Position.CB).Select(p=>starts[p.PlayerId]).ToArray();
        Assert.Equal(new GameFieldPoint(5,.14),cb[0]);Assert.Equal(new GameFieldPoint(5,.86),cb[1]);
        Assert.All(cb.Skip(2),point=>{Assert.Equal(4,point.X);Assert.InRange(point.Y,.3,.7);});
    }

    [Fact]
    public void MiddleLinebackerStaysInsideTheOutsideLinebackers()
    {
        var roster=GameDayEngineTests.Session(GameDayMode.Play).AwayRoster;
        var middle=roster.First(p=>p.Position==Position.MLB);
        var outside=roster.Where(p=>p.Position==Position.OLB).Take(2).ToArray();
        var players=outside.Append(middle).ToArray();
        var play=new DefensivePlayDefinition("test","Test",DefensiveFormation.FourThree,DefensivePersonnel.BaseFourThree,DefensiveApproach.Balanced,"");
        var starts=DefensivePursuitService.Alignments(players,play);
        Assert.Equal(.5,starts[middle.PlayerId].Y);
        Assert.True(starts[outside[0].PlayerId].Y<.5);Assert.True(starts[outside[1].PlayerId].Y>.5);
    }

    [Fact]
    public void PartialCustomAssignmentsDoNotShiftOtherPlayersIntoTheSameSlot()
    {
        var corners=GameDayEngineTests.Session(GameDayMode.Play).AwayRoster.Where(p=>p.Position==Position.CB).Take(3).ToArray();
        var play=new DefensivePlayDefinition("test","Test",DefensiveFormation.Nickel,DefensivePersonnel.Nickel,DefensiveApproach.Balanced,"")
        {Assignments=[new(){Position=Position.CB,RoleIndex=0,Depth=5,Lane=.14,Task=AssignmentTask.Man}]};
        var starts=DefensivePursuitService.Alignments(corners,play);
        Assert.Equal(new GameFieldPoint(5,.14),starts[corners[0].PlayerId]);
        Assert.Equal(new GameFieldPoint(5,.86),starts[corners[1].PlayerId]);
        Assert.Equal(new GameFieldPoint(4,.34),starts[corners[2].PlayerId]);
    }

    [Fact]
    public void CustomBlitzOnlyMakesTheAssignedRoleARusher()
    {
        var s=GameDayEngineTests.Session(GameDayMode.Play);var dt=s.AwayRoster.First(p=>p.Position==Position.DT);
        var corners=s.AwayRoster.Where(p=>p.Position==Position.CB).Take(2).ToArray();
        var defenders=new[]{dt}.Concat(corners).ToArray();
        var play=new DefensivePlayDefinition("corner-blitz","Corner blitz",DefensiveFormation.Nickel,DefensivePersonnel.Nickel,DefensiveApproach.Pressure,"")
        {Assignments=[new(){Position=Position.DT,Task=AssignmentTask.Zone,Depth=3,Lane=.5},
            new(){Position=Position.CB,RoleIndex=0,Task=AssignmentTask.Rush,Depth=5,Lane=.2},
            new(){Position=Position.CB,RoleIndex=1,Task=AssignmentTask.Man,Depth=1,Lane=.5}]};
        var result=DefensivePursuitService.Select(defenders,play,-5,.5,true,true,0,3,null,0,new Random(1));
        Assert.Equal(corners[0].PlayerId,result.Tackler!.PlayerId);Assert.Equal(new[]{corners[0].PlayerId},result.AttemptIds);
        var noRush=play with {Assignments=play.Assignments.Select(a=>a with {Task=AssignmentTask.Zone}).ToArray()};
        Assert.Null(DefensivePursuitService.Select(defenders,noRush,-5,.5,true,true,0,3,null,0,new Random(1)).Tackler);
        var qb=s.HomeRoster.First(p=>p.Position==Position.QB);
        var offense=new[]{qb,s.HomeRoster.First(p=>p.Position==Position.WR),s.HomeRoster.First(p=>p.Position==Position.OL)};
        var read=PassingReadService.Read(PlaybookCatalog.OffensivePlays.First(p=>p.Id=="off-spread-verts"),noRush,offense,defenders,qb,75,new Random(1));
        Assert.Equal(0,read.RusherCount);
    }
}
