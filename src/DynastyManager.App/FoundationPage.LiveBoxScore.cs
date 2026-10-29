using DynastyManager.Core.Models;
namespace DynastyManager.App;
public sealed partial class FoundationPage
{
    private Task ShowLiveBoxScoreAsync(GameDayBoxScore box) => GameCastBoxScore.ShowAsync(Navigation, box, new()
    {
        SectionTitle = SectionTitle, Text = (text, bold) => TableText(text, bold),
        Table = (columns, rows) => DataTable(columns, rows), Eligible = CategoryEligible, Columns = CategoryColumns,
        Cells = CategoryCells, Average = Average,
        Abbreviation = team => _teamsByName.TryGetValue(team, out var school) ? school.Abbreviation : team
    });
}
