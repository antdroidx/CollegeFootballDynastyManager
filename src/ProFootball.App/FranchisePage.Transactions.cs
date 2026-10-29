using ProFootball.Core;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    void Transactions()
    {
        state=ProTransactions.Migrate(state!);var s=state!;body.Add(Text("All transactions",25,true));
        var seasons=new[]{"All seasons"}.Concat(s.TransactionLog.Select(t=>t.Year).Distinct().OrderDescending().Select(y=>y.ToString())).ToArray();
        var season=new Picker{ItemsSource=seasons,SelectedIndex=0};var weeks=new[]{"All weeks"}.Concat(Enumerable.Range(1,18).Select(w=>"Week "+w)).ToArray();var week=new Picker{ItemsSource=weeks,SelectedIndex=0};var search=new Entry{Placeholder="Find a team, player or transaction"};
        body.Add(new HorizontalStackLayout{Spacing=12,Children={season,week}});body.Add(search);var rows=new VerticalStackLayout();body.Add(rows);
        void Draw(){rows.Clear();var entries=s.TransactionLog.AsEnumerable().Reverse().Where(t=>(season.SelectedIndex<=0||t.Year.ToString()==seasons[season.SelectedIndex])&&(week.SelectedIndex<=0||t.Week==week.SelectedIndex)&&(string.IsNullOrWhiteSpace(search.Text)||t.Activity.Contains(search.Text.Trim(),StringComparison.OrdinalIgnoreCase))).ToArray();
            rows.Add(Text(entries.Length+" transactions",12));rows.Add(Table([("Year",65),("Week",55),("Stage",115),("Activity",650)],entries.Select(t=>new View[]{Text(t.Year.ToString()),Text(ProTransactions.Period(t.Phase,t.Week)),Text(t.Phase?.ToString()??"Season activity"),Text(t.Activity)})));}
        season.SelectedIndexChanged+=(_,_)=>Draw();week.SelectedIndexChanged+=(_,_)=>Draw();search.TextChanged+=(_,_)=>Draw();Draw();
    }
}
