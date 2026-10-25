namespace DynastyManager.Core.Models;

public static class RecruitingPoolRules
{
    public const int RecruitsPerFbsTeam = 25;

    public static int GetTargetPoolSize(int teamCount)
    {
        if (teamCount < 1)
            throw new ArgumentOutOfRangeException(nameof(teamCount));

        return checked(teamCount * RecruitsPerFbsTeam);
    }
}
