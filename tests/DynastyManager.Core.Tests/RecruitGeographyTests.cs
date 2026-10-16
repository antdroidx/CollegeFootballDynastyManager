using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Import;
using DynastyManager.Data.Persistence;
using Xunit;

namespace DynastyManager.Core.Tests;

public class RecruitGeographyTests
{
    [Theory]
    [InlineData("West", "Alaska,Arizona,California,Colorado,Hawaii,Idaho,Montana,Nevada,Oregon,Utah,Washington,Wyoming")]
    [InlineData("Plains", "Iowa,Kansas,Minnesota,Missouri,Nebraska,New Mexico,North Dakota,Oklahoma,South Dakota,Texas")]
    [InlineData("Midwest", "Illinois,Indiana,Michigan,Ohio,Wisconsin")]
    [InlineData("East", "Connecticut,Delaware,Maine,Maryland,Massachusetts,New Hampshire,New Jersey,New York,Pennsylvania,Rhode Island,Vermont,Virginia,West Virginia")]
    [InlineData("South", "Alabama,Arkansas,Florida,Georgia,Kentucky,Louisiana,Mississippi,North Carolina,South Carolina,Tennessee")]
    public void StatesHaveExplicitGeographicRegions(string region, string states)
    {
        foreach (var state in states.Split(','))
        {
            var index = RecruitGeography.StateNames.ToList().IndexOf(state);
            var recruit = Recruit(index) with { HomeRegion = index / 10 };
            Assert.Equal(state, RecruitGeography.GetStateName(recruit));
            Assert.Equal(region, RecruitGeography.GetRegionName(recruit));
        }
    }

    [Fact]
    public void FactoryUsesSameMappingAsDisplayAndFilters()
    {
        var recruits = HighSchoolRecruitingPoolFactory.Create(Guid.NewGuid(), 2027, 130, []);
        Assert.Equal(50, recruits.Select(item => item.HomeState).Distinct().Count());
        Assert.All(recruits, recruit => Assert.Equal(
            RecruitGeography.GetRegionForState(recruit.HomeState), recruit.HomeRegion));
    }

    [Theory]
    [InlineData(17)]
    [InlineData(8)]
    public async Task LoadingSaveRepairsRegionsWithoutReplacingRecruits(int schema)
    {
        var path = Path.Combine(Path.GetTempPath(), $"geography-{Guid.NewGuid():N}.db");
        try
        {
            await using var repository = new SqliteDynastySaveRepository(path);
            var recruit = schema < 9 ? Recruit(0) with { HomeRegion = 9 } : Recruit(9) with { HomeRegion = 0 };
            var state = new DynastyState { DynastyName = "Geography", UserTeamName = "Washington",
                SchemaVersion = schema, HighSchoolRecruitingPool = new[] { recruit } };
            var save = await repository.SaveAsync(state, SaveKind.Manual);
            var loaded = await repository.LoadAsync(save);
            var restored = Assert.Single(loaded!.HighSchoolRecruitingPool);
            Assert.Equal(recruit.RecruitId, restored.RecruitId);
            Assert.Equal("Georgia", RecruitGeography.GetStateName(restored));
            Assert.Equal("South", RecruitGeography.GetRegionName(restored));
            Assert.Equal(4, restored.HomeRegion);
        }
        finally
        {
            foreach (var suffix in new[] { "", "-wal", "-shm" })
                if (File.Exists(path + suffix)) File.Delete(path + suffix);
        }
    }

    private static HighSchoolRecruit Recruit(int state) => new()
    {
        RecruitId = Guid.NewGuid(), FullName = "State Prospect", Position = Position.WR,
        HomeState = state, HomeRegion = 0
    };
}
