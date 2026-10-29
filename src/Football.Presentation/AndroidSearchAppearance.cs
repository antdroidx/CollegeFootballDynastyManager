using Microsoft.Maui;
namespace DynastyManager.App;

/// <summary>Keep Android search icons platform-owned: MAUI's vector re-tint can crash on API 36.</summary>
public static class AndroidSearchAppearance
{
    public static void Configure()
    {
#if ANDROID
        // https://github.com/dotnet/maui/issues/39208 — do not call the default icon tint maps.
        // Text and hints still follow the game palette; icon vectors retain Android's native theme.
        Microsoft.Maui.Handlers.SearchBarHandler.Mapper.ModifyMapping(nameof(Microsoft.Maui.ISearchBar.TextColor), (handler,view,_) =>
        {
            if(handler is Microsoft.Maui.Handlers.SearchBarHandler search && search.QueryEditor is {} input)
                input.SetTextColor(Microsoft.Maui.Platform.ColorExtensions.ToPlatform(view.TextColor ?? DynastyTheme.Text));
        });
        Microsoft.Maui.Handlers.SearchBarHandler.Mapper.ModifyMapping(nameof(Microsoft.Maui.ISearchBar.PlaceholderColor), (handler,view,_) =>
        {
            if(handler is Microsoft.Maui.Handlers.SearchBarHandler search && search.QueryEditor is {} input)
                input.SetHintTextColor(Microsoft.Maui.Platform.ColorExtensions.ToPlatform(view.PlaceholderColor ?? DynastyTheme.Muted));
        });
        Microsoft.Maui.Handlers.SearchBarHandler.Mapper.ModifyMapping(nameof(Microsoft.Maui.ISearchBar.CancelButtonColor), (_,_,_) => { });
        Microsoft.Maui.Handlers.SearchBarHandler.Mapper.ModifyMapping("SearchIconColor", (_,_,_) => { });
#endif
    }
}
