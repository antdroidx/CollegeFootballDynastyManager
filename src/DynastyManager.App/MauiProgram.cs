using Microsoft.Maui.Hosting;
#if ANDROID
using Android.Content.Res;
using Microsoft.Maui.Handlers;
#endif

namespace DynastyManager.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        Microsoft.Maui.Handlers.LabelHandler.Mapper.AppendToMapping("ThemeContrast", (_, view) =>
        {
            if (view is Microsoft.Maui.Controls.Label label)
                label.Loaded += (_, _) => label.TextColor = DynastyTheme.ReadableInk(label.TextColor, label);
        });
        Microsoft.Maui.Handlers.ButtonHandler.Mapper.AppendToMapping("ThemeContrast", (_, view) =>
        {
            if (view is Microsoft.Maui.Controls.Button button)
                button.Loaded += (_, _) => button.TextColor = DynastyTheme.ReadableInk(button.TextColor, button);
        });
        Microsoft.Maui.Handlers.SearchBarHandler.Mapper.AppendToMapping("ThemeContrast", (_, view) =>
        {
            if (view is Microsoft.Maui.Controls.SearchBar search)
                search.Loaded += (_, _) => search.TextColor = DynastyTheme.ReadableInk(search.TextColor, search);
        });
#if ANDROID
        PickerHandler.Mapper.AppendToMapping("FlatPicker", (handler, _) =>
            handler.PlatformView.BackgroundTintList = ColorStateList.ValueOf(Android.Graphics.Color.Transparent));
        PickerHandler.Mapper.AppendToMapping("ThemedChoices", (handler, view) =>
        {
            if (view is Microsoft.Maui.Controls.Picker picker)
                handler.PlatformView.SetOnClickListener(new ThemedPickerClick(picker));
        });
        SearchBarHandler.Mapper.AppendToMapping("FlatSearch", (handler, _) =>
            handler.PlatformView.Background = null);
#endif
        return builder.Build();
    }
}
