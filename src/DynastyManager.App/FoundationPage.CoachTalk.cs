using DynastyManager.Core.Simulation;
using DynastyManager.Data.Persistence;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private async Task ShowGameCoachTalkAsync(Microsoft.Maui.Controls.Page page, DynastyManager.Core.Models.GameDaySession session, string team)
    {
        var availability = GameDayCoachingService.TalkAvailability(session, team);
        if (availability != "Available") return;
        var scope = await page.DisplayActionSheetAsync("Address the squad", "Cancel", null, "Team", "Offense", "Defense");
        if (!Enum.TryParse<CoachTalkAudience>(scope, out var audience)) return;
        var choice = await page.DisplayActionSheetAsync($"{scope} talk · reactions may be mixed", "Cancel", null, "Encourage", "Praise", "Challenge", "Stay composed");
        if (!Enum.TryParse<CoachTalkTone>(choice?.Replace(" ", ""), out var tone)) return;
        var talk = GameDayCoachingService.Talk(session, team, tone, audience);
        if (talk is null) { await page.DisplayAlertAsync("Let the message settle", "One talk per timeout or quarter break, shared across team, offense and defense. Two per half.", "OK"); return; }
        if (session.IsComplete && _currentDynasty is { } state)
        {
            _currentDynasty = state with { Schedule = state.Schedule.Select(g => g.GameId == session.Game.GameId ? g with { CoachTalks = session.CoachTalks.ToArray() } : g).ToArray() };
            await SaveCurrentAsync(SaveKind.AutosaveCurrent);
        }
        await page.DisplayAlertAsync("Player reactions", $"{talk.Reactions.Count(r => r.Change > 0)} positive · {talk.Reactions.Count(r => r.Change < 0)} negative · {talk.Reactions.Count(r => r.Change == 0)} neutral\n" +
            string.Join("\n", talk.Reactions.Take(4).Select(r => $"{r.PlayerName}: {r.Response} ({r.Change:+0;-0;0})")) + "\nEffects fade as play continues.", "OK");
    }

    private async Task ShowSeasonCoachTalkAsync()
    {
        if (_currentDynasty is not { } state) return;
        if (state.CoachTalks.Any(t => t.TeamName == state.UserTeamName && t.SeasonYear == state.SeasonYear && t.Week == state.Week))
        {
            await DisplayAlertAsync("Let the message settle", "One team talk per week. Reactions fade over three weeks; repeated messages lose impact.", "OK");
            return;
        }
        var choice = await DisplayActionSheetAsync("Team talk · may help, fall flat, or backfire", "Cancel", null, "Encourage", "Praise", "Challenge", "Stay composed");
        if (!Enum.TryParse<CoachTalkTone>(choice?.Replace(" ", ""), out var tone)) return;
        var morale = GameMoraleService.SeasonMorale(state, state.UserTeamName);
        var repetition = state.CoachTalks.Count(t => t.TeamName == state.UserTeamName && t.SeasonYear == state.SeasonYear && state.Week - t.Week <= 3 && t.Tone == tone);
        var talk = GameDayCoachingService.CreateTalk(state.ActiveRoster, state.UserTeamName, tone, state.SeasonYear, state.Week, 0, 0, morale - 50, repetition);
        _currentDynasty = state with { CoachTalks = state.CoachTalks.Append(talk).ToArray() };
        await SaveCurrentAsync(SaveKind.AutosaveCurrent);
        await DisplayAlertAsync("Team reaction", $"{talk.Reactions.Count(r => r.Change > 0)} positive · {talk.Reactions.Count(r => r.Change < 0)} negative · {talk.Reactions.Count(r => r.Change == 0)} neutral\n" +
            string.Join("\n", talk.Reactions.Take(4).Select(r => $"{r.PlayerName}: {r.Response} ({r.Change:+0;-0;0})")), "OK");
        ShowSection(AppSection.Program);
    }
}
