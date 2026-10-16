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
        "West", "North", "Midwest", "Southeast", "Southwest"
    };

    public static int GetStateIndex(HighSchoolRecruit recruit) =>
        recruit.HomeState != 0 || recruit.HomeRegion <= 4
            ? Math.Clamp(recruit.HomeState, 0, 49)
            : Math.Clamp(recruit.HomeRegion, 0, 49);

    public static int GetRegion(HighSchoolRecruit recruit) =>
        recruit.HomeRegion is >= 0 and <= 4
            ? recruit.HomeRegion
            : Math.Clamp(GetStateIndex(recruit) / 10, 0, 4);

    public static string GetStateName(HighSchoolRecruit recruit) =>
        StateNames[GetStateIndex(recruit)];

    public static string GetRegionName(HighSchoolRecruit recruit) =>
        RegionNames[GetRegion(recruit)];
}
