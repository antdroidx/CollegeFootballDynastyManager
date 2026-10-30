namespace ProFootball.Core;

/// <summary>Read legacy squad data, settle its real obligations once, and retire the feature.</summary>
public static class ProPracticeRetirement
{
    public static ProFranchise Apply(ProFranchise state)
    {
        if(state.PracticeSquadRemoved&&state.League.PracticeSquadLimit==0&&!state.Players.Any(p=>p.PracticeSquad)&&state.InternationalPracticeDesignations.Count==0)return state;
        var previous=state.Transactions;
        foreach(var player in state.Players.Where(p=>p.PracticeSquad&&!p.Reserve&&!p.Retired&&state.League.Teams.Any(t=>t.Name==p.Football.TeamName)).ToArray())
            state=ProEconomy.Release(state,player.Football.TeamName,player.Football.PlayerId,false);
        return state with {PracticeSquadRemoved=true, League=state.League with {PracticeSquadLimit=0},
            Players=state.Players.Select(p=>p.PracticeSquad?p with {PracticeSquad=false,RosterStatus=p.Reserve?p.RosterStatus:"FA"}:p).ToList(),
            InternationalPracticeDesignations=[], Transactions=previous};
    }
}
