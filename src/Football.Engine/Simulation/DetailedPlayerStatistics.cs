using DynastyManager.Core.Models;
namespace DynastyManager.Core.Simulation;
public static class DetailedPlayerStatistics
{
    public static void Record(GameSnapEvent snap,GameDaySession session,Func<Guid,PlayerSeasonStatLine> line,Action<PlayerSeasonStatLine> save)
    {
        if(snap.StatsVersion<1)return;
        bool Known(Guid id)=>session.PlayersById.ContainsKey(id);
        void Add(Guid id,string key,int amount=1){if(Known(id))save(PlayerStatTotals.Increment(line(id),key,amount));}
        var scrimmage=GameDayEngine.IsScrimmage(snap);
        if(scrimmage)
        {
            foreach(var id in snap.OffensePersonnel.Distinct())Add(id,"OffensiveSnaps");
            foreach(var id in snap.DefensePersonnel.Distinct())Add(id,"DefensiveSnaps");
        }
        else if(snap.Outcome is not (SnapOutcome.ClockStoppage or SnapOutcome.Penalty))
            foreach(var id in snap.OffensePersonnel.Concat(snap.DefensePersonnel).Distinct())Add(id,"SpecialTeamsSnaps");
        if(snap.PenalizedPlayerId is Guid penalized){Add(penalized,"Penalties");Add(penalized,"PenaltyYards",snap.PenaltyYards??0);}
        foreach(var block in snap.Blocks)
        {
            Add(block.BlockerId,block.PassBlock?"PassBlockSnaps":"RunBlockSnaps");
            if(block.Won)Add(block.BlockerId,block.PassBlock?"PassBlockWins":"RunBlockWins");
            if(block.PassBlock&&!block.Won&&block.DefenderId is Guid defender&&snap.PressureRushers.Contains(defender))Add(block.BlockerId,"PressuresAllowed");
        }
        foreach(var id in snap.PressureRushers.Distinct())Add(id,"DefensivePressures");
        if(snap.IsDropback&&snap.QuarterbackId is Guid qb)
        {
            Add(qb,"Dropbacks");if(snap.PressureRushers.Count>0)Add(qb,"PressuredDropbacks");
            if(snap.Outcome==SnapOutcome.Sack){Add(qb,"TimesSacked");Add(qb,"SackYardsLost",Math.Max(0,-snap.Yards));if(snap.SackBlockerId is Guid blocker)Add(blocker,"SacksAllowed");}
        }
        var caught=snap.Outcome is SnapOutcome.Completion or SnapOutcome.Touchdown;
        if(snap.TargetId is Guid receiver&&snap.Outcome!=SnapOutcome.Sack&&!snap.IsQuarterbackScramble)
        {
            Add(receiver,"Targets");if(caught){Add(receiver,"TrackedReceptions");save(PlayerStatTotals.Longest(line(receiver),"LongestReception",snap.Yards));if(snap.QuarterbackId is Guid passer)save(PlayerStatTotals.Longest(line(passer),"LongestPass",snap.Yards));}
            if(snap.CoverageDefenderId is Guid coverage){Add(coverage,"CoverageTargets");if(caught){Add(coverage,"CoverageReceptions");Add(coverage,"CoverageYards",snap.Yards);}}
        }
        if(scrimmage&&snap.BallCarrierId is Guid runner){save(PlayerStatTotals.Longest(line(runner),"LongestRush",snap.Yards));if(snap.BrokeTackle)Add(runner,"BrokenTackles");}
        if(snap.Outcome==SnapOutcome.Fumble&&snap.PrimaryDefenderId is Guid forcer)Add(forcer,"ForcedFumbles");
        if(snap.KickerId is Guid kicker&&snap.Outcome is SnapOutcome.FieldGoal or SnapOutcome.MissedFieldGoal)
        {
            var distance=117-snap.StartYardLine;var bucket=distance<40?"Under40":distance<50?"40To49":"50Plus";
            Add(kicker,"FieldGoalsAttempted"+bucket);
            if(snap.Outcome==SnapOutcome.FieldGoal){Add(kicker,"FieldGoalsMade"+bucket);save(PlayerStatTotals.Longest(line(kicker),"LongFieldGoal",distance));}
        }
        if(snap.Outcome==SnapOutcome.Punt&&snap.KickerId is Guid punter)
        {
            Add(punter,"TrackedPunts");
            Add(punter,"NetPuntYards",Math.Max(0,(snap.IsTouchback?80:snap.KickLandingYardLine)-snap.StartYardLine-snap.ReturnYards));
            if(snap.IsTouchback)Add(punter,"PuntTouchbacks");
            if(snap.IsFairCatch&&snap.ReturnerId is Guid returner)Add(returner,"FairCatches");
        }
    }
}
