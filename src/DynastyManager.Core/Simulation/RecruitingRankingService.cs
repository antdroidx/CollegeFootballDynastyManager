using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

/// <summary>
/// 247-style recruiting ratings and team class scoring.
///
/// The real 247Sports team formula weights each recruit by a Gaussian curve:
/// the best recruit is worth 100% of his value, with progressively smaller
/// contributions deeper in the class. This service mirrors that structure
/// using the game's generated recruiting ratings.
/// </summary>
public static class RecruitingRankingService
{
    public static double GetProspectRating(
        DynastyState state,
        RecruitingSource source,
        Guid prospectId)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (source == RecruitingSource.HighSchool)
        {
            var recruit = state.HighSchoolRecruitingPool
                .FirstOrDefault(item =>
                    item.RecruitId == prospectId);

            if (recruit is not null)
                return GetHighSchoolRating(recruit);
        }
        else
        {
            var transfer = state.TransferPortalEntries
                .FirstOrDefault(item =>
                    item.Player.PlayerId == prospectId);

            if (transfer is not null)
                return GetTransferRating(transfer.Player);

            var pending = state.PendingTransferCommitments
                .FirstOrDefault(item =>
                    item.ProspectId == prospectId);

            if (pending is not null)
                return GetTransferRating(pending.Player);
        }

        var commitment = state.RecruitingCommitments
            .FirstOrDefault(item =>
                item.SeasonYear == state.SeasonYear &&
                item.Source == source &&
                item.ProspectId == prospectId);

        return commitment is null
            ? 70.0
            : GetCommitmentRating(state, commitment);
    }

    public static double GetCommitmentRating(
        DynastyState state,
        RecruitingCommitmentRecord commitment)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(commitment);

        if (commitment.Source ==
            RecruitingSource.HighSchool)
        {
            var recruit = state.HighSchoolRecruitingPool
                .FirstOrDefault(item =>
                    item.RecruitId ==
                    commitment.ProspectId);

            if (recruit is not null)
                return GetHighSchoolRating(recruit);

            return GetLegacyHighSchoolRating(
                commitment.OverallRating);
        }

        var portal = state.TransferPortalEntries
            .FirstOrDefault(item =>
                item.Player.PlayerId ==
                commitment.ProspectId);
        if (portal is not null)
            return GetTransferRating(portal.Player);

        var pending = state.PendingTransferCommitments
            .FirstOrDefault(item =>
                item.ProspectId ==
                commitment.ProspectId);
        if (pending is not null)
            return GetTransferRating(pending.Player);

        return GetTransferRating(
            commitment.OverallRating,
            commitment.OverallRating);
    }

    public static double GetHighSchoolRating(
        HighSchoolRecruit recruit)
    {
        ArgumentNullException.ThrowIfNull(recruit);

        var (ratingMin, ratingMax, overallMin, overallMax) =
            recruit.StarRating switch
            {
                >= 5 => (98.00, 100.00, 82, 91),
                4 => (90.00, 97.99, 75, 85),
                3 => (80.00, 89.99, 68, 79),
                2 => (72.00, 79.99, 59, 73),
                _ => (70.00, 71.99, 50, 65)
            };

        var readiness = Normalize(
            recruit.TrueOverallRating,
            overallMin,
            overallMax);

        var upsideWindow = Math.Max(
            1,
            99 - recruit.TrueOverallRating);
        var upside = Math.Clamp(
            (recruit.PotentialRating -
             recruit.TrueOverallRating) /
            (double)Math.Min(20, upsideWindow),
            0.0,
            1.0);

        // 247-style prospect ratings reflect both current evaluation
        // and projection. Overall readiness is the larger component.
        var quality =
            readiness * 0.72 +
            upside * 0.28;

        return Math.Round(
            ratingMin +
            (ratingMax - ratingMin) * quality,
            2);
    }

    public static double GetTransferRating(
        DynastyPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return GetTransferRating(
            player.OverallRating,
            player.PotentialRating);
    }

    public static double GetTransferRating(
        int overall,
        int potential)
    {
        var baseRating =
            70.0 +
            (Math.Clamp(overall, 50, 99) - 50) *
            (29.0 / 49.0);

        var upsideBonus = Math.Clamp(
            potential - overall,
            0,
            15) * 0.08;

        return Math.Round(
            Math.Clamp(
                baseRating + upsideBonus,
                70.0,
                99.75),
            2);
    }

    public static int GetStars(double rating) =>
        rating switch
        {
            >= 98.0 => 5,
            >= 90.0 => 4,
            >= 80.0 => 3,
            >= 72.0 => 2,
            _ => 1
        };

    public static IReadOnlyList<RecruitingClassRanking>
        BuildTeamRankings(
            DynastyState state,
            IEnumerable<string> teamNames)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(teamNames);

        var names = teamNames
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var commitments = state.RecruitingCommitments
            .Where(item =>
                item.SeasonYear == state.SeasonYear)
            .ToArray();

        var countsByTeam = names.ToDictionary(
            name => name,
            name => commitments.Count(item =>
                item.TeamName.Equals(
                    name,
                    StringComparison.OrdinalIgnoreCase)),
            StringComparer.OrdinalIgnoreCase);

        var sigma = GetGaussianSigma(
            countsByTeam.Values);

        var rows = names
            .Select(teamName =>
            {
                var teamCommits = commitments
                    .Where(item =>
                        item.TeamName.Equals(
                            teamName,
                            StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                var ratings = teamCommits
                    .Select(item =>
                        GetCommitmentRating(state, item))
                    .OrderByDescending(value => value)
                    .ToArray();

                return new RecruitingClassRanking
                {
                    TeamName = teamName,
                    CommitCount = teamCommits.Length,
                    HighSchoolCount = teamCommits.Count(item =>
                        item.Source ==
                        RecruitingSource.HighSchool),
                    TransferCount = teamCommits.Count(item =>
                        item.Source ==
                        RecruitingSource.TransferPortal),
                    TeamScore =
                        CalculateTeamScore(
                            ratings,
                            sigma),
                    AverageRecruitRating =
                        ratings.Length == 0
                            ? 0.0
                            : Math.Round(
                                ratings.Average(),
                                2),
                    BestRecruitRating =
                        ratings.Length == 0
                            ? 0.0
                            : ratings[0],
                    GaussianSigma = sigma
                };
            })
            .OrderByDescending(item =>
                item.TeamScore)
            .ThenByDescending(item =>
                item.AverageRecruitRating)
            .ThenByDescending(item =>
                item.CommitCount)
            .ThenBy(item =>
                item.TeamName,
                StringComparer.OrdinalIgnoreCase)
            .Select((item, index) =>
                item with
                {
                    Rank = index + 1
                })
            .ToArray();

        return rows;
    }

    public static double CalculateTeamScore(
        IEnumerable<double> ratings,
        double sigma)
    {
        ArgumentNullException.ThrowIfNull(ratings);

        var ordered = ratings
            .OrderByDescending(value => value)
            .ToArray();

        var safeSigma = Math.Clamp(
            sigma,
            1.0,
            20.0);

        var score = 0.0;

        for (var index = 0;
             index < ordered.Length;
             index++)
        {
            var recruitValue =
                Math.Max(
                    0.0,
                    ordered[index] - 70.0);

            var gaussianWeight = Math.Exp(
                -0.5 *
                Math.Pow(
                    index / safeSigma,
                    2.0));

            score +=
                recruitValue *
                gaussianWeight;
        }

        return Math.Round(score, 2);
    }

    public static double GetGaussianSigma(
        IEnumerable<int> classSizes)
    {
        var values = classSizes
            .Select(value => (double)value)
            .ToArray();

        if (values.Length <= 1)
            return 7.0;

        var mean = values.Average();
        var variance = values.Sum(value =>
            Math.Pow(value - mean, 2.0)) /
            (values.Length - 1);

        // 247 varies sigma from class-size dispersion. A floor prevents
        // early-season tiny classes from making only the first 1-2 commits
        // matter, while the cap avoids returning to raw-volume scoring.
        return Math.Round(
            Math.Clamp(
                Math.Sqrt(variance),
                6.0,
                12.0),
            2);
    }

    private static double GetLegacyHighSchoolRating(
        int overall)
    {
        if (overall >= 82)
        {
            return Scale(
                overall,
                82,
                91,
                98.00,
                100.00);
        }

        if (overall >= 75)
        {
            return Scale(
                overall,
                75,
                85,
                90.00,
                97.99);
        }

        if (overall >= 68)
        {
            return Scale(
                overall,
                68,
                79,
                80.00,
                89.99);
        }

        if (overall >= 59)
        {
            return Scale(
                overall,
                59,
                73,
                72.00,
                79.99);
        }

        return Scale(
            overall,
            50,
            65,
            70.00,
            71.99);
    }

    private static double Normalize(
        int value,
        int minimum,
        int maximum)
    {
        if (maximum <= minimum)
            return 0.5;

        return Math.Clamp(
            (value - minimum) /
            (double)(maximum - minimum),
            0.0,
            1.0);
    }

    private static double Scale(
        int value,
        int inputMin,
        int inputMax,
        double outputMin,
        double outputMax) =>
        Math.Round(
            outputMin +
            (outputMax - outputMin) *
            Normalize(
                value,
                inputMin,
                inputMax),
            2);
}
