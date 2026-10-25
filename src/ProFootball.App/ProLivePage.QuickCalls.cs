using DynastyManager.App;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using ProFootball.Core;
namespace ProFootball.App;

public sealed partial class ProLivePage
{
    void UpdateQuickCalls()
    {
        var defending = ProLiveControl.CanCall(session, true);
        quickCalls.IsVisible = defending || ProLiveControl.CanCall(session, false);
        quickCalls.Clear();
        if (!quickCalls.IsVisible) return;
        var profile = session.ControlledTeamName == session.Game.HomeTeamName ? session.HomeProfile : session.AwayProfile;
        var plan = profile.GamePlan ?? new TeamGamePlan { TeamName = session.ControlledTeamName! };
        var choices = new List<GameDayCallChoice>();
        string chosen;
        if (defending)
        {
            var library = PlayEditorService.DefensiveLibrary(plan);
            var recommendation = GamePlanService.RecommendDefensivePlay(plan, session.Down, session.Distance);
            var first = library.FirstOrDefault(p => p.Id == defense) ?? library.FirstOrDefault(p => p.Id == recommendation.Id) ?? library.First();
            chosen = first.Id; defense = chosen;
            foreach (var play in new[] { first }.Concat(library.Where(p => p.Id != first.Id).OrderBy(p => p.Approach == first.Approach ? 1 : 0)).Take(3))
                choices.Add(new(play.Id, play.Name, GamePlanService.Label(play.Formation), GamePlanService.Label(play.Approach), new PlayArtDrawable(play.Id, play.Name, false, play.Approach.ToString(), defensiveDefinition: play)));
        }
        else
        {
            var library = PlayEditorService.OffensiveLibrary(plan);
            var recommendation = GameDayRecommendationService.Recommend(session, plan);
            chosen = offense ?? recommendation.PlayId;
            var first = library.FirstOrDefault(p => p.Id == chosen);
            if (chosen is "special-punt" or "special-field-goal")
                choices.Add(new(chosen, chosen == "special-punt" ? "Punt" : "Field goal", "Special teams", "KICK", new PlayArtDrawable(chosen, "Kick", true, "Kick")));
            else
            {
                first ??= recommendation.Play ?? library.First(); chosen = first.Id;
                choices.Add(OffensiveChoice(first));
            }
            offense = chosen;
            foreach (var play in library.Where(p => p.Id != chosen).OrderBy(p => first is not null && p.Concept == first.Concept ? 1 : 0).Take(3 - choices.Count))
                choices.Add(OffensiveChoice(play));
        }
        for (var i = 0; i < choices.Count; i++)
        {
            var choice = choices[i];
            var card = GameDayCallPicker.Card(choice.Name, choice.Formation, choice.Id == chosen ? "SELECTED · " + choice.Type : choice.Type, choice.Art,
                () => { if (working || pending || !ProLiveControl.CanCall(session, defending)) return; if (defending) defense = choice.Id; else offense = choice.Id; selected.Text = "Selected: " + choice.Name; Update(); }, compact: true, portrait: Width < 650);
            card.AutomationId = "quick-call-" + i; card.Stroke = DynastyTheme.Selected; card.StrokeThickness = choice.Id == chosen ? 2 : 0;
            quickCalls.Add(card, i, 0);
        }
    }
    static GameDayCallChoice OffensiveChoice(OffensivePlayDefinition play) => new(play.Id, play.Name, GamePlanService.Label(play.Formation),
        play.Concept is OffensiveConcept.InsideRun or OffensiveConcept.OutsideRun ? "RUN" : "PASS", new PlayArtDrawable(play.Id, play.Name, true, play.Concept.ToString(), play));
}
