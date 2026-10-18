using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class RecruitGeography
{
    public static readonly IReadOnlyList<string> StateNames = new[]
    {
        "Alabama", "Alaska", "Arizona", "Arkansas", "California",
        "Colorado", "Connecticut", "Delaware", "Florida", "Georgia",
        "Hawaii", "Idaho", "Illinois", "Indiana", "Iowa",
        "Kansas", "Kentucky", "Louisiana", "Maine", "Maryland",
        "Massachusetts", "Michigan", "Minnesota", "Mississippi", "Missouri",
        "Montana", "Nebraska", "Nevada", "New Hampshire", "New Jersey",
        "New Mexico", "New York", "North Carolina", "North Dakota", "Ohio",
        "Oklahoma", "Oregon", "Pennsylvania", "Rhode Island", "South Carolina",
        "South Dakota", "Tennessee", "Texas", "Utah", "Vermont",
        "Virginia", "Washington", "West Virginia", "Wisconsin", "Wyoming"
    };

    public static readonly IReadOnlyList<string> RegionNames = new[]
    {
        "West", "Plains", "Midwest", "East", "South"
    };

    public static int GetStateIndex(HighSchoolRecruit recruit) =>
        recruit.HomeState != 0 || recruit.HomeRegion <= 4
            ? Math.Clamp(recruit.HomeState, 0, 49)
            : Math.Clamp(recruit.HomeRegion, 0, 49);

    // State indexes follow StateNames, never alphabetical buckets. Keep the
    // five region IDs aligned with the imported league's west-to-east layout.
    public static int GetRegionForState(int stateIndex) => Math.Clamp(stateIndex, 0, 49) switch
    {
        1 or 2 or 4 or 5 or 10 or 11 or 25 or 27 or 36 or 43 or 46 or 49 => 0,
        14 or 15 or 22 or 24 or 26 or 30 or 33 or 35 or 40 or 42 => 1,
        12 or 13 or 21 or 34 or 48 => 2,
        6 or 7 or 18 or 19 or 20 or 28 or 29 or 31 or 37 or 38 or 44 or 45 or 47 => 3,
        _ => 4
    };

    public static int GetRegion(HighSchoolRecruit recruit) =>
        GetRegionForState(GetStateIndex(recruit));

    public static string GetStateName(HighSchoolRecruit recruit) =>
        StateNames[GetStateIndex(recruit)];

    public static string GetRegionName(HighSchoolRecruit recruit) =>
        RegionNames[GetRegion(recruit)];
}
