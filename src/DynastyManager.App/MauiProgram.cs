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
#if ANDROID
        PickerHandler.Mapper.AppendToMapping("FlatPicker", (handler, _) =>
            handler.PlatformView.BackgroundTintList = ColorStateList.ValueOf(Android.Graphics.Color.Transparent));
        SearchBarHandler.Mapper.AppendToMapping("FlatSearch", (handler, _) =>
            handler.PlatformView.Background = null);
#endif
        return builder.Build();
    }
}
