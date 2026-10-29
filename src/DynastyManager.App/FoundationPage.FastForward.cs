using DynastyManager.Core.Models;
using DynastyManager.Data.Persistence;
namespace DynastyManager.App;
public sealed partial class FoundationPage
{
    private async Task SimulateCurrentPhaseAsync()
    {
        if(_currentDynasty is null||_isAdvancing||_isBatchSimulating)return;
        var phase=_currentDynasty.Phase;var year=_currentDynasty.SeasonYear;
        bool InPhase(SeasonPhase current)=>phase is SeasonPhase.ConferenceChampionship or SeasonPhase.Postseason ? current is SeasonPhase.ConferenceChampionship or SeasonPhase.Postseason : current==phase;
        if(phase is not (SeasonPhase.Preseason or SeasonPhase.RegularSeason or SeasonPhase.ConferenceChampionship or SeasonPhase.Postseason))return;
        if(!await GameDialogs.AlertAsync(this,_simulatePhaseButton.Text+"?","Simulate the remaining games and normal weekly decisions. Each week saves automatically. Stop at the next phase; postseason includes conference championships, bowls and playoffs.","Simulate","Cancel"))return;
        _isBatchSimulating=true;_batchStopRequested=false;_stopPhaseSimulation.IsVisible=true;_stopPhaseSimulation.IsEnabled=true;SetDynastyControlsEnabled(false);
        try
        {
            for(var i=0;i<32&&!_batchStopRequested&&_currentDynasty is not null&&_currentDynasty.SeasonYear==year&&InPhase(_currentDynasty.Phase)&&!_currentDynasty.UserCoachIsFired;i++)
            {
                var before=(_currentDynasty.Phase,_currentDynasty.Week);
                await AdvanceOneWeekAsync();
                if(_advanceFailed)break;
                await SaveCurrentAsync(SaveKind.AutosaveCurrent);
                if(before==(_currentDynasty.Phase,_currentDynasty.Week))break;
            }
        }
        catch(Exception error){await GameDialogs.AlertAsync(this,"Simulation stopped",error.Message,"OK");}
        finally{_isBatchSimulating=false;_stopPhaseSimulation.IsVisible=false;_advanceOverlay.IsVisible=false;SetDynastyControlsEnabled(_currentDynasty is not null);RenderCurrentDynasty();}
    }
}
