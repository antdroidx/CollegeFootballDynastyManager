using DynastyManager.App;
namespace ProFootball.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder().UseMauiApp<App>();
#if ANDROID
        Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("ThemedChoices", (handler, view) => { if (view is Picker picker) handler.PlatformView.SetOnClickListener(new ThemedPickerClick(picker)); });
        Microsoft.Maui.Handlers.ButtonHandler.Mapper.AppendToMapping("Flat", (handler, _) => { handler.PlatformView.StateListAnimator = null; handler.PlatformView.Elevation = 0; });
#endif
        return builder.Build();
    }
}
public sealed class App : Application
{
    public App() { DynastyTheme.ProfessionalPalette = true; Resources = DynastyTheme.CreateResources(); UserAppTheme = DynastyTheme.RequestedTheme; }
    protected override Window CreateWindow(IActivationState? activationState) => new(new NavigationPage(new FranchisePage()) { BarBackgroundColor = DynastyTheme.Surface, BarTextColor = DynastyTheme.Text });
}
