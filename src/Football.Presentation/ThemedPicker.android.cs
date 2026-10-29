#if ANDROID
using Android.Views;
using Microsoft.Maui.Controls;

namespace DynastyManager.App;

/// <summary>Keep native picker accessibility and binding while using the same choices UI as the game.</summary>
public sealed class ThemedPickerClick(Picker picker) : Java.Lang.Object, Android.Views.View.IOnClickListener
{
    private bool opening;
    public async void OnClick(Android.Views.View? view)
    {
        if (opening || !picker.IsEnabled || picker.Window?.Page is not { } owner) return;
        opening = true;
        try
        {
            var selected = await GameDialogs.ChooseIndexAsync(owner, string.IsNullOrWhiteSpace(picker.Title) ? "Choose option" : picker.Title,
                picker.Items.ToArray(), picker.SelectedIndex);
            if (selected >= 0 && selected < picker.Items.Count) picker.SelectedIndex = selected;
        }
        catch (Exception error) { System.Diagnostics.Trace.WriteLine("Picker presentation failed: " + error); }
        finally { opening = false; }
    }
}
#endif
