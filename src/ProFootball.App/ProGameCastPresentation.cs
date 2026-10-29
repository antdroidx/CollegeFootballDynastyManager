using DynastyManager.App;
using DynastyManager.Core.Models;
using ProFootball.Core;
namespace ProFootball.App;

/// <summary>Pro branding and NFL passer rating over the shared college statistics page.</summary>
static class ProGameCastPresentation
{
    public static GameCastBoxScoreStyle BoxStyle(ProLeague league) => new()
    {
        SectionTitle = (title, description) => new VerticalStackLayout { Spacing = 3, Children = { Text(title, true, 20), Text(description, false, 12) } },
        Text = (text, bold) => Text(text, bold), Table = Table,
        Eligible = FootballStatViews.Eligible,
        Columns = category => FootballStatViews.Columns(category).Select(c => c.Title == "School" ? ("Team", 85d) : c).ToArray(),
        Cells = (line, category) => FootballStatViews.Cells(line, category, (_, name) => Text(name),
            team => Text(Short(league, team)), text => Text(text), (made, attempts) => Average(made * 100, attempts), Average,
            line => ProStatistics.PasserRating(line).ToString("F1")),
        Average = Average, Abbreviation = team => Short(league, team)
    };
    static string Short(ProLeague league, string team) => league.Teams.FirstOrDefault(t => t.Name == team)?.Abbreviation ?? team;
    static string Average(int total, int count) => (count == 0 ? 0 : (double)total / count).ToString("F1");
    static Label Text(string text, bool bold = false, int size = 13) => new() { Text = text, TextColor = DynastyTheme.Text, FontSize = size,
        FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None, VerticalTextAlignment = TextAlignment.Center };
    static View Table((string Title,double Width)[] columns,IEnumerable<View[]> rows) =>
        FootballDataGrid.Create(columns.Select(c=>(c.Title,(int)c.Width)).ToArray(),rows);
}
