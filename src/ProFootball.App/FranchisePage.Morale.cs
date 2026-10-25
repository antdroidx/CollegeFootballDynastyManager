using DynastyManager.App;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using ProFootball.Core;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    View WeeklyTalkButton()
    {
        var available=ProMorale.TalkAvailable(state!);var button=Action(available?"Weekly coach talk":"Weekly coach talk · Used this week",WeeklyTalk);
        button.IsEnabled=available;button.Opacity=available?1:.45;return button;
    }
    async Task WeeklyTalk()
    {
        var role=ProCareer.Role(state!);var audience=role==StaffRole.OffensiveCoordinator?CoachTalkAudience.Offense:role==StaffRole.DefensiveCoordinator?CoachTalkAudience.Defense:CoachTalkAudience.Team;
        if(role==StaffRole.HeadCoach)
        {
            var choice=await GameDialogs.ActionSheetAsync(this,"Coach talk audience","Cancel",null,"Team","Offense","Defense");
            if(choice is null or "Cancel")return;audience=Enum.Parse<CoachTalkAudience>(choice);
        }
        var tone=await GameDialogs.ActionSheetAsync(this,"Weekly coach talk","Cancel",null,"Encourage","Praise","Challenge","Stay composed");
        if(tone is null or "Cancel")return;
        var selected=tone=="Stay composed"?CoachTalkTone.StayComposed:Enum.Parse<CoachTalkTone>(tone);
        await Update(s=>ProMorale.Talk(s,selected,audience));var talk=state!.WeeklyTalks.Last().Talk;
        await ShowPage("Coach talk reactions",Text($"{talk.Audience} · {tone} · {state.Year} Week {state.Week}",17,true),Text("Players may respond positively or negatively. Reactions fade over time; some personalities take longer to move on.",12),
            Table([("Player",200),("Change",70),("Reaction",260)],talk.Reactions.Select(r=>new View[]{Text(r.PlayerName),Text(r.Change.ToString("+0;-0;0")),Text(r.Response,12)})));
    }
}
