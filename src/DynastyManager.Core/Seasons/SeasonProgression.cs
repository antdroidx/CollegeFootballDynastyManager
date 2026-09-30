using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;

namespace DynastyManager.Core.Seasons;

/// <summary>
/// Advances the dynasty calendar through the legacy-style season phases.
/// Week 0 is an optional opening regular-season slot; only teams scheduled
/// there play, but the league calendar still advances through it.
/// </summary>
public static class SeasonProgression
{
    public const int DefaultRegularSeasonWeeks = 13;

    public static DynastyState Advance(
        DynastyState state,
        int regularSeasonWeeks = DefaultRegularSeasonWeeks)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (regularSeasonWeeks < 1)
            throw new ArgumentOutOfRangeException(
                nameof(regularSeasonWeeks),
                "Regular season must contain at least one week.");

        return state.Phase switch
        {
            SeasonPhase.Preseason => state with
            {
                Week = 0,
                Phase = SeasonPhase.RegularSeason
            },

            SeasonPhase.RegularSeason when state.Week < regularSeasonWeeks =>
                state with { Week = state.Week + 1 },

            SeasonPhase.RegularSeason => state with
            {
                Week = Math.Max(state.Week, regularSeasonWeeks) + 1,
                Phase = SeasonPhase.ConferenceChampionship
            },

            SeasonPhase.ConferenceChampionship => state with
            {
                Week = state.Week + 1,
                Phase = SeasonPhase.Postseason
            },

            SeasonPhase.Postseason when state.Week <
                CollegeFootballPlayoffService.NationalChampionshipWeek =>
                state with
                {
                    Week = state.Week + 1,
                    Phase = SeasonPhase.Postseason
                },

            SeasonPhase.Postseason => state with
            {
                Week = state.Week + 1,
                Phase = SeasonPhase.TransferPortal
            },

            SeasonPhase.TransferPortal => state with
            {
                Week = state.Week + 1,
                Phase = SeasonPhase.Recruiting
            },

            SeasonPhase.Recruiting => state with
            {
                Week = state.Week + 1,
                Phase = SeasonPhase.RosterManagement
            },

            SeasonPhase.RosterManagement => state with
            {
                Week = state.Week + 1,
                Phase = SeasonPhase.Offseason
            },

            SeasonPhase.Offseason => state with
            {
                SeasonYear = state.SeasonYear + 1,
                Week = 0,
                Phase = SeasonPhase.Preseason
            },

            _ => throw new ArgumentOutOfRangeException(
                nameof(state),
                state.Phase,
                "Unknown season phase.")
        };
    }
}
