using DynastyManager.App;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using ProFootball.Core;
namespace ProFootball.App;

public sealed partial class ProLivePage
{
    void FrameCamera(double x = .5, double y = .5) => GameCastCamera.Apply([turf, players], field.Width, field.Height, cameraMode, x, y);
    async Task ViewingOptions()
    {
        var pick = await GameDialogs.ActionSheetAsync(this, "Viewing options", "Cancel", null, "Camera", "Game speed", "Replay speed", "Automatic replays", "Player names", "Weather", "Live box score");
        if (pick == "Camera")
        {
            var names = new[] { "Full field", "Broadcast", "Follow ball" };
            var mode = await GameDialogs.ActionSheetAsync(this, "Camera", "Cancel", null, names);
            var index = Array.IndexOf(names, mode); if (index >= 0) { cameraMode = index; Preferences.Set("pro-camera", index); FrameCamera(); selected.Text = "Camera: " + names[index]; }
        }
        else if (pick is "Game speed" or "Replay speed") await SetSpeed(pick == "Replay speed");
        else if (pick == "Automatic replays") { autoReplays = !autoReplays; Preferences.Set("pro-auto-replays", autoReplays); selected.Text = "Automatic replays: " + (autoReplays ? "On" : "Off"); }
        else if (pick == "Player names") { showNames = !showNames; Preferences.Set("pro-player-names", showNames); selected.Text = "Player names: " + (showNames ? "On" : "Off"); }
        else if (pick == "Weather") await GameDialogs.AlertAsync(this, "Game conditions", session.Weather.Summary, "Close");
        else if (pick == "Live box score") await ShowBox();
    }
    async Task ChooseSpeed()
    {
        var kind = await GameDialogs.ActionSheetAsync(this, "Speed", "Cancel", null, "Game speed", "Replay speed");
        if (kind is "Game speed" or "Replay speed") await SetSpeed(kind == "Replay speed");
    }
    async Task SetSpeed(bool replay)
    {
        var pick = await GameDialogs.ActionSheetAsync(this, replay ? "Replay speed" : "Game speed", "Cancel", null, "0.5x", "1x", "2x");
        if (pick is not ("0.5x" or "1x" or "2x")) return;
        var value = pick == "0.5x" ? .5 : pick == "2x" ? 2 : 1;
        if (replay) replaySpeed = value; else speed = value;
        Preferences.Set(replay ? "pro-replay-speed" : "pro-game-speed", value); selected.Text = (replay ? "Replay speed: " : "Game speed: ") + pick;
    }
    Task ShowBox() => GameCastBoxScore.ShowAsync(Navigation, GameDayEngine.BoxScore(session), ProGameCastPresentation.BoxStyle(league));
    async Task Playbook(bool defending)
    {
        var plan = (session.ControlledTeamName == session.Game.HomeTeamName ? session.HomeProfile : session.AwayProfile).GamePlan ?? new TeamGamePlan { TeamName = session.ControlledTeamName! };
        var plays = PlayEditorService.OffensiveLibrary(plan); var defenses = PlayEditorService.DefensiveLibrary(plan);
        var choices = defending
            ? defenses.Select(p => new GameDayCallChoice(p.Id, p.Name, GamePlanService.Label(p.Formation), GamePlanService.Label(p.Approach), new PlayArtDrawable(p.Id, p.Name, false, p.Approach.ToString(), defensiveDefinition: p))).ToArray()
            : plays.Select(p => new GameDayCallChoice(p.Id, p.Name, GamePlanService.Label(p.Formation), p.Concept is OffensiveConcept.InsideRun or OffensiveConcept.OutsideRun ? "RUN" : "PASS", new PlayArtDrawable(p.Id, p.Name, true, p.Concept.ToString(), p)))
                .Concat(new[] { new GameDayCallChoice("special-punt", "Punt", "Special teams", "KICK", new PlayArtDrawable("punt", "Punt", true, "Kick")), new GameDayCallChoice("special-field-goal", "Field goal", "Special teams", "KICK", new PlayArtDrawable("fg", "Field goal", true, "Kick")) }).ToArray();
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        closePicker = () => { pickerLayer.IsVisible = false; pickerLayer.Clear(); closePicker = null; closed.TrySetResult(); };
        pickerLayer.Clear(); pickerLayer.Add(new GameDayCallPicker(defending ? "Choose defense" : "Choose play", choices, (defending ? defense : offense) ?? "", id =>
        {
            if (defending) defense = id; else offense = id;
            selected.Text = (defending ? "Defense: " : "Offense: ") + choices.Single(c => c.Id == id).Name; closePicker?.Invoke();
        }, () => closePicker?.Invoke()));
        pickerLayer.IsVisible = true; await closed.Task;
    }
    async Task DepthChart()
    {
        var positions = session.Roster.Where(p => p.TeamName == session.ControlledTeamName).Select(p => OffensiveLineService.Label(p)).Distinct().Order().ToArray();
        var position = await GameDialogs.ActionSheetAsync(this, "Position", "Cancel", null, positions);
        if (position is null or "Cancel") return;
        var starters = GameDayCoachingService.Starters(session, session.ControlledTeamName!);
        var pool = session.Roster.Where(p => p.TeamName == session.ControlledTeamName && OffensiveLineService.Label(p) == position).OrderBy(p => p.DepthChartOrder).ToArray();
        var names = pool.Select(p => $"{p.FullName} · OVR {p.OverallRating}" + (starters.Contains(p.PlayerId) ? " · In game" : "") + (p.CurrentInjury is not null ? " · Injured" : "")).ToArray();
        var name = await GameDialogs.ActionSheetAsync(this, "Choose player", "Cancel", null, names); var index = Array.IndexOf(names, name);
        if (index >= 0) selected.Text = GameDayCoachingService.Substitute(session, session.ControlledTeamName!, pool[index].PlayerId) ? pool[index].FullName + " in for next snap" : "Player unavailable";
    }
}
