using DynastyManager.Core.Models;
using DynastyManager.Data.Branding;
namespace DynastyManager.App;
public sealed partial class FoundationPage
{
    private Task ShowCoinTossAsync(GameDaySession session, string controlledTeam, bool interactive) =>
        GameCastCoinToss.ShowAsync(Navigation, session, controlledTeam, interactive, name =>
        {
            var brand = TeamBrandCatalog.Get(name);
            return new(brand.Primary, brand.Secondary, TeamBrandCatalog.Monogram(name));
        }, page => ShowGameCoachTalkAsync(page, session, controlledTeam));
}
