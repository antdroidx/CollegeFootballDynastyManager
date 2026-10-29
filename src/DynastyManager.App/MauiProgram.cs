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
        AndroidSearchAppearance.Configure();
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        DynastyTheme.ConfigureContrast();
#if ANDROID
        ButtonHandler.Mapper.AppendToMapping("FlatButtons", (handler, _) =>
        {
            handler.PlatformView.StateListAnimator = null;
            handler.PlatformView.Elevation = 0;
            handler.PlatformView.TranslationZ = 0;
        });
        static void ApplyButtonAvailability(IButtonHandler handler, Microsoft.Maui.IButton view)
        {
            if (view is Microsoft.Maui.Controls.Button button)
                handler.PlatformView.Alpha = (float)(button.IsEnabled ? button.Opacity : button.Opacity);
        }
        ButtonHandler.Mapper.AppendToMapping(nameof(Microsoft.Maui.IView.IsEnabled), ApplyButtonAvailability);
        ButtonHandler.Mapper.AppendToMapping(nameof(Microsoft.Maui.IView.Opacity), ApplyButtonAvailability);
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
