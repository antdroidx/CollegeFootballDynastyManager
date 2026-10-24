using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class ProgramDisciplineService
{
    public static IReadOnlyList<DisciplineEventDefinition> Events { get; } = Array.AsReadOnly(new[]
    {
        new DisciplineEventDefinition("study", "Study hall missed", "A missed academic appointment prompts extra study sessions.", 3),
        new DisciplineEventDefinition("curfew", "Team curfew violation", "A curfew violation leads to internal accountability work.", 4),
        new DisciplineEventDefinition("practice", "Practice standards slip", "Repeated lateness prompts a review of team expectations.", 5),
        new DisciplineEventDefinition("sideline", "Sideline disagreement", "A sideline disagreement draws a warning from the staff.", 4),
        new DisciplineEventDefinition("academic", "Academic eligibility review", "The program strengthens academic oversight after an eligibility review.", 9, true),
        new DisciplineEventDefinition("compliance", "Recruiting compliance review", "A review of recruiting procedures identifies inadequate oversight.", 12, true),
        new DisciplineEventDefinition("benefits", "Benefits reporting error", "Incomplete benefits reporting triggers a program compliance review.", 10, true),
        new DisciplineEventDefinition("service", "Community service recognized", "The team's community service earns recognition.", -5, true),
        new DisciplineEventDefinition("leadership", "Captains set the standard", "Player leaders organize a successful academic and accountability initiative.", -4, true)
    });

    public static bool PostseasonEligible(DynastyState state, string team) => !state.LeagueRules.ProgramDiscipline ||
        !state.ProgramConduct.Any(p => p.TeamName == team && p.PostseasonBanSeason == state.SeasonYear);

    public static DynastyState AdvanceWeek(DynastyState state)
    {
        if (!state.LeagueRules.ProgramDiscipline || state.Phase is not (SeasonPhase.RegularSeason or SeasonPhase.ConferenceChampionship)) return state;
        var records = state.ProgramConduct.ToDictionary(p => p.TeamName, StringComparer.OrdinalIgnoreCase);
        var history = state.DisciplineHistory.ToList();
        foreach (var team in state.LeagueTeams)
        {
            var record = records.GetValueOrDefault(team.Name) ?? new ProgramConductRecord { TeamName = team.Name };
            if (record.LastProcessedYear == state.SeasonYear && record.LastProcessedWeek == state.Week) continue;
            var random = new Random(SimulationSeed.Create(state.DynastyId, state.SeasonYear, state.Week, team.Name, "program-conduct"));
            var players = state.ActiveRoster.Where(p => p.TeamName == team.Name).ToArray();
            var discipline = players.Length == 0 ? 65 : players.Average(p => p.Skills?.Discipline ?? 65);
            var coach = state.Staff.FirstOrDefault(s => s.TeamName == team.Name && s.Role == StaffRole.HeadCoach);
            var leadership = coach?.Leadership ?? 65;
            var target = Math.Clamp((int)Math.Round(45 + discipline * .3 + leadership * .2), 55, 95);
            var score = record.Score + Math.Sign(target - record.Score) * (random.NextDouble() < .3 ? 1 : 0);
            if (random.NextDouble() < Math.Clamp(.045 + (65 - discipline) * .001 + (65 - leadership) * .0005, .015, .12))
            {
                var definition = Events[random.Next(Events.Count)];
                score = Math.Clamp(score - definition.Severity, 0, 100);
                history.Add(new() { SeasonYear = state.SeasonYear, Week = state.Week, TeamName = team.Name,
                    EventId = definition.Id, Title = definition.Title, Description = definition.Description,
                    Severity = definition.Severity });
            }
            var repeatedMajor = history.Count(i => i.TeamName == team.Name && i.SeasonYear >= state.SeasonYear - 2 && i.Severity >= 9);
            if (score < 35 && repeatedMajor >= 3 && record.LastSanctionYear < state.SeasonYear - 2 && random.NextDouble() < .12)
            {
                record = record with { LastSanctionYear = state.SeasonYear, PostseasonBanSeason = state.SeasonYear };
                history.Add(new() { SeasonYear = state.SeasonYear, Week = state.Week, TeamName = team.Name, EventId = "sanction",
                    Title = "Postseason sanction issued", Description = "Repeated compliance failures lead to a one-season postseason ban. Recruiting remains open while oversight is rebuilt.", Severity = 0, SanctionIssued = true });
            }
            records[team.Name] = record with { Score = score, LastProcessedYear = state.SeasonYear, LastProcessedWeek = state.Week };
        }
        return state with { ProgramConduct = records.Values.ToArray(), DisciplineHistory = history.ToArray() };
    }

    public static DynastyState ImproveOversight(DynastyState state, string team)
    {
        // Once each season: a deliberate response, not a button that can erase repeated misconduct.
        if (state.DisciplineHistory.Any(i => i.TeamName == team && i.SeasonYear == state.SeasonYear && i.EventId == "oversight")) return state;
        var record = state.ProgramConduct.FirstOrDefault(p => p.TeamName == team) ?? new ProgramConductRecord { TeamName = team };
        return state with
        {
            ProgramConduct = state.ProgramConduct.Where(p => p.TeamName != team).Append(record with { Score = Math.Min(100, record.Score + 8) }).ToArray(),
            DisciplineHistory = state.DisciplineHistory.Append(new DisciplineIncident { TeamName = team, SeasonYear = state.SeasonYear,
                Week = state.Week, EventId = "oversight", Title = "Oversight initiative", Description = "The program invests staff time in academic support and compliance education.", Severity = -8 }).ToArray()
        };
    }
}
