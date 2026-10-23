namespace DynastyManager.Core.Simulation;

/// <summary>Fixed attribute-score curves. Derived from NEXT26 reference data, never from a live league percentile.</summary>
public static class Next26OverallCalibration
{
    private sealed record Curve(double[] Scores, double[] Ratings);
    private static readonly Curve[] Curves =
    [
        new([1d, 71.872d, 73.962d, 74.78d, 76.31d, 79.12d, 81.69d, 85.166d, 87.452d, 89.987d, 99d], [40d, 63.0d, 66.0d, 68.0d, 70.0d, 73.0d, 76.0d, 82.0d, 86.0d, 91.0d, 99d]),
        new([1d, 71.327d, 74.47d, 75.811d, 77.475d, 79.43d, 81.61d, 83.33d, 84.465d, 86.923d, 99d], [40d, 64.41d, 69.0d, 71.0d, 74.0d, 78.0d, 82.0d, 85.9d, 87.0d, 91.0d, 99d]),
        new([1d, 62.89d, 64.102d, 64.902d, 65.825d, 66.84d, 67.985d, 71.348d, 73.269d, 76.653d, 99d], [40d, 57.0d, 58.0d, 60.0d, 61.0d, 64.0d, 67.0d, 73.6d, 75.3d, 84.3d, 99d]),
        new([1d, 76.172d, 77.529d, 78.422d, 80.29d, 82.41d, 84.735d, 86.66d, 88.141d, 90.17d, 99d], [40d, 57.0d, 62.0d, 64.0d, 67.0d, 72.0d, 77.0d, 82.0d, 85.0d, 90.0d, 99d]),
        new([1d, 60.519d, 62.873d, 65.142d, 67.635d, 69.74d, 72.385d, 74.296d, 75.253d, 77.164d, 99d], [40d, 51.0d, 56.0d, 61.0d, 66.0d, 70.0d, 75.0d, 79.0d, 83.0d, 89.0d, 99d]),
        new([1d, 67.285d, 68.948d, 69.968d, 72.005d, 74.39d, 77.295d, 80.444d, 81.979d, 87.837d, 99d], [40d, 58.0d, 61.0d, 63.0d, 66.0d, 71.0d, 75.0d, 79.0d, 82.0d, 90.0d, 99d]),
        new([1d, 66.499d, 68.656d, 69.906d, 71.458d, 73.77d, 76.887d, 80.255d, 81.896d, 87.275d, 99d], [40d, 58.0d, 61.0d, 63.0d, 66.0d, 71.0d, 75.0d, 80.0d, 83.0d, 91.0d, 99d]),
        new([1d, 65.056d, 68.708d, 69.915d, 72.098d, 74.39d, 78.022d, 81.779d, 83.992d, 87.051d, 99d], [40d, 54.09d, 60.0d, 62.0d, 66.0d, 70.0d, 75.0d, 81.1d, 85.0d, 90.0d, 99d]),
        new([1d, 66.942d, 68.81d, 69.68d, 71.62d, 73.98d, 77.09d, 79.59d, 81.17d, 84.636d, 99d], [40d, 58.0d, 61.0d, 63.0d, 67.0d, 71.0d, 75.0d, 79.0d, 82.0d, 87.6d, 99d]),
        new([1d, 66.261d, 68.57d, 69.559d, 71.528d, 74.39d, 77.273d, 80.206d, 82.77d, 88.188d, 99d], [40d, 57.77d, 61.0d, 63.0d, 67.0d, 71.0d, 74.25d, 79.0d, 82.0d, 91.0d, 99d]),
        new([1d, 70.59d, 72.483d, 73.27d, 74.88d, 77.08d, 78.975d, 81.238d, 82.52d, 85.737d, 99d], [40d, 59.0d, 62.0d, 64.0d, 67.0d, 72.0d, 76.0d, 80.0d, 83.0d, 90.0d, 99d]),
        new([1d, 70.158d, 72.032d, 73.008d, 74.795d, 76.95d, 79.272d, 81.497d, 82.656d, 85.528d, 99d], [40d, 59.0d, 61.15d, 64.0d, 68.0d, 71.0d, 77.0d, 81.7d, 84.0d, 90.14d, 99d]),
        new([1d, 70.723d, 72.55d, 73.84d, 76.292d, 78.255d, 80.232d, 82.425d, 83.502d, 86.389d, 99d], [40d, 58.15d, 62.0d, 64.0d, 69.0d, 73.0d, 77.0d, 81.0d, 83.0d, 89.85d, 99d]),
        new([1d, 70.314d, 72.512d, 73.718d, 75.11d, 76.88d, 78.72d, 81.036d, 82.662d, 86.827d, 99d], [40d, 53.36d, 57.8d, 63.0d, 66.0d, 69.0d, 73.0d, 76.0d, 80.2d, 88.64d, 99d]),
        new([1d, 70.19d, 72.09d, 73.441d, 75.238d, 77.55d, 80.542d, 83.379d, 84.817d, 87.264d, 99d], [40d, 54.51d, 60.0d, 63.0d, 66.0d, 70.0d, 75.0d, 80.0d, 83.0d, 89.49d, 99d]),
        new([1d, 71.142d, 73.036d, 74.146d, 75.68d, 78.02d, 80.125d, 82.868d, 84.645d, 87.555d, 99d], [40d, 57.66d, 62.0d, 64.0d, 67.0d, 70.0d, 75.0d, 80.4d, 84.0d, 90.0d, 99d]),
        new([1d, 73.534d, 75.332d, 76.036d, 77.64d, 79.48d, 81.79d, 83.872d, 84.922d, 86.717d, 99d], [40d, 60.0d, 64.0d, 65.0d, 68.0d, 72.0d, 77.0d, 82.0d, 85.0d, 90.0d, 99d]),
        new([1d, 73.482d, 74.907d, 75.699d, 76.78d, 78.485d, 80.542d, 82.559d, 83.804d, 86.557d, 99d], [40d, 60.0d, 63.0d, 65.0d, 67.0d, 72.0d, 76.0d, 81.0d, 85.0d, 91.0d, 99d]),
        new([1d, 70.801d, 73.092d, 73.954d, 75.54d, 77.63d, 79.49d, 81.526d, 83.368d, 85.622d, 99d], [40d, 60.0d, 63.0d, 65.0d, 69.0d, 73.0d, 77.0d, 82.0d, 85.0d, 89.96d, 99d]),
        new([1d, 69.025d, 72.05d, 74.25d, 76.975d, 79.35d, 84.525d, 87.8d, 89.8d, 92.8d, 99d], [40d, 58.5d, 61.0d, 63.0d, 66.0d, 70.0d, 75.0d, 79.0d, 82.0d, 84.5d, 99d]),
        new([1d, 67.178d, 73.78d, 76.13d, 78.1d, 81.25d, 84.85d, 88.55d, 91.18d, 93.215d, 99d], [40d, 57.82d, 63.0d, 65.0d, 68.0d, 71.0d, 75.0d, 79.0d, 82.3d, 84.06d, 99d]),
    ];

    public static int Calculate(int positionCode, double weightedScore)
    {
        if (positionCode is < 0 or > 20) throw new ArgumentOutOfRangeException(nameof(positionCode));
        if (!double.IsFinite(weightedScore)) throw new ArgumentOutOfRangeException(nameof(weightedScore));
        var curve = Curves[positionCode];
        var score = Math.Clamp(weightedScore, 1, 99);
        var i = 0;
        while (i < curve.Scores.Length - 2 && score > curve.Scores[i + 1]) i++;
        // The proposed reference undershot the adjusted roster. Lift the body by two
        // points, tapering to zero at 99. Explicit endpoints prevent tail extrapolation.
        double Target(int index)
        {
            var rating = curve.Ratings[index];
            return index == 0 ? 40 : rating + 2 * Math.Clamp((99 - rating) / 11, 0, 1);
        }
        var fraction = (score - curve.Scores[i]) / (curve.Scores[i + 1] - curve.Scores[i]);
        var overall = Target(i) + fraction * (Target(i + 1) - Target(i));
        // Reserve 99 for a near-perfect weighted profile; rounding a shallow
        // positional tail must not create a 99 below this common threshold.
        return Math.Clamp((int)Math.Round(overall, MidpointRounding.ToEven), 40, score < 98.5 ? 98 : 99);
    }
}
