using DynastyManager.App;
using ProFootball.Core;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    async Task CorrectOpeningBudgets()
    {
        var s=state!;var proposed=ProCapInitialization.CorrectOpeningBudgets(s);
        var before=ProFinance.Limit(s,s.UserTeam);var after=ProFinance.Limit(proposed,s.UserTeam);
        if(await GameDialogs.AlertAsync(this,"Update 2026 cap budgets?",$"Your team's 2026 budget changes from {Dollars(before)} to {Dollars(after)}. This updates opening allowances for all 32 teams and retains contracts, spending and completed transactions.","Update","Cancel"))
            await Update(ProCapInitialization.CorrectOpeningBudgets);
    }
}
