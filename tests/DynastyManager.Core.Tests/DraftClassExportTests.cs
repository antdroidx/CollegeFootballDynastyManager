using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;
namespace DynastyManager.Core.Tests;
public class DraftClassExportTests
{
    static DynastyPlayer Player(string name,int year)=>new(){PlayerId=Guid.NewGuid(),FullName=name,TeamName="College",Position=Position.QB,ClassYear=year,OverallRating=80,PotentialRating=90,SpeedRating=70,StrengthRating=70,AgilityRating=70,AwarenessRating=80,Skills=new(){PassingPower=85,PassingAccuracy=82}};
    [Fact] public void DeparturesCaptureFullClassWithoutTransfersOrRedshirtsAndPreviewIsReadOnly()
    {
        var senior=Player("Graduate",4);var redshirt=Player("Redshirt",4) with{IsRedshirted=true};var junior=Player("Returning",3);
        var s=new DynastyState{DynastyName="Test",UserTeamName="College",SeasonYear=2026,Phase=SeasonPhase.TransferPortal,ActiveRoster=[senior,redshirt,junior]};
        var preview=PlayerExportService.Latest(s);Assert.Null(s.LatestProDraftClass);Assert.Equal(3,s.ActiveRoster.Count);var p=Assert.Single(preview.Players);Assert.Equal(senior,p);Assert.Equal(2027,preview.Year);
        var next=OffseasonPlayerLifecycleService.EnterTransferPortal(s);Assert.Equal(preview.Players,next.LatestProDraftClass!.Players);Assert.DoesNotContain(next.ActiveRoster,x=>x.PlayerId==senior.PlayerId);Assert.Equal(85,next.LatestProDraftClass.Players[0].Skills!.PassingPower);
        Assert.Equal(PlayerExportService.Serialize(preview),PlayerExportService.Serialize(PlayerExportService.Latest(next)));
    }
    [Fact] public void OldSummaryOnlySavesDoNotInventAttributes()
    {
        var s=new DynastyState{DynastyName="Old",UserTeamName="College",SeasonYear=2026,Phase=SeasonPhase.Recruiting,RecentPlayerDepartures=[new(){FullName="Old graduate",TeamName="College",PlayerId=Guid.NewGuid(),SeasonYear=2026,Position=Position.QB,OverallRating=90}]};
        Assert.Throws<InvalidOperationException>(()=>PlayerExportService.Latest(s));
    }
}
