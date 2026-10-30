using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;
namespace DynastyManager.Core.Tests;
public class FullbackRoleTests
{
    [Fact] public void LeadBlockerQualityAffectsTheRunMatchup()
    {
        var source=GameDayEngineTests.Session(GameDayMode.Play);var template=source.HomeRoster.First(p=>p.Position==Position.RB);
        var fb=template with {Position=Position.FB,Skills=(template.Skills??new PlayerSkillRatings()) with {RunBlocking=99}};
        var weak=fb with {Skills=fb.Skills! with {RunBlocking=1}};
        Assert.True(GameDayMatchupService.PersonnelAdvantage(source.HomeRoster.Append(fb).ToArray(),source.AwayRoster,false)>
            GameDayMatchupService.PersonnelAdvantage(source.HomeRoster.Append(weak).ToArray(),source.AwayRoster,false));
    }
    [Fact] public void ImportedCodeTwoIsFullbackAndOldNumericPositionsStayStable()
    {
        var player=GameDayEngineTests.Session(GameDayMode.Play).Roster.First(p=>p.Position==Position.RB);
        var fields=Ncaa06RosterRatings.Export(player).ToDictionary(x=>x.Key,x=>x.Value);fields["PPOS"]=2;
        var fb=Ncaa06RosterRatings.Apply(player,fields);
        Assert.Equal(Position.FB,fb.Position);Assert.Equal(2,Ncaa06RatingService.PositionCode(fb));
        Assert.Equal(1,(int)Position.RB);Assert.Equal(12,(int)Position.K);Assert.Equal("HB",Position.RB.DisplayName());
        var state=new DynastyState{DynastyName="Old",UserTeamName=player.TeamName,OverallCalibrationVersion=1,ActiveRoster=[fb with {Position=Position.RB}]};
        Assert.Equal(Position.FB,PlayerRatingService.EnsureProfiles(state).ActiveRoster.Single().Position);
    }
    [Theory] [InlineData("off-iform-lead",false)] [InlineData("off-iform-trap",true)]
    [InlineData("off-flexbone-dive",true)]
    public void HandoffsUseTheIntendedBack(string playId,bool useFb)
    {
        var source=GameDayEngineTests.Session(GameDayMode.Play);
        var template=source.Roster.First(p=>p.TeamName==source.Game.HomeTeamName&&p.Position==Position.RB);
        var fb=template with {PlayerId=Guid.NewGuid(),Position=Position.FB,NcaaPositionCode=2,DepthChartOrder=1};
        var roster=source.Roster.Append(fb).ToArray();
        var s=GameDayEngine.Start(source.Game,source.HomeProfile,source.AwayProfile,roster,GameDayMode.Play);
        s.KickoffPending=false;s.PlayClockSeconds=40;
        var snap=GameDayEngine.Step(s,playId);
        Assert.Equal(fb.PlayerId,snap.FullbackId);
        Assert.Equal(useFb,snap.BallCarrierId==fb.PlayerId);
        Assert.Equal(11,snap.OffensePersonnel.Distinct().Count());
    }
    [Fact] public void TeamsWithoutFullbacksUseATightEndWithoutChangingHisRosterPosition()
    {
        var source=GameDayEngineTests.Session(GameDayMode.Play);
        var roster=source.Roster.Where(p=>p.Position!=Position.FB).ToArray();
        var play=PlaybookCatalog.OffensivePlays.Single(p=>p.Id=="off-iform-screen");
        var selected=PersonnelRotationService.SelectOffense(roster,source.Game.HomeTeamName,play.Personnel,new(),new Random(7));
        var actual=selected.PlayerIds.Select(id=>roster.Single(p=>p.PlayerId==id)).ToArray();
        var jobs=BackfieldRoles.Assign(play,actual);var fb=jobs.Single(p=>p.Position==Position.FB);
        Assert.Equal(Position.TE,roster.Single(p=>p.PlayerId==fb.PlayerId).Position);
        var read=PassingReadService.Read(play,PlaybookCatalog.DefensivePlays.First(),jobs,source.AwayRoster,jobs.First(p=>p.Position==Position.QB),80,new Random(9));
        Assert.Equal(fb.PlayerId,read.Target?.PlayerId);Assert.Equal(11,selected.PlayerIds.Distinct().Count());
    }
}
