using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Branding;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private async Task<NcaaPlayerAppearance?> EditPortraitAsync(Guid id, string name, string school,
        NcaaPlayerAppearance? current, bool staff = false, int age = 20)
    {
        var result = new TaskCompletionSource<NcaaPlayerAppearance?>();
        var initial = current ?? new NcaaPlayerAppearance();
        var facial = new Picker { Title = "Facial hair", ItemsSource = new[] { "Automatic", "Clean shaven", "Stubble", "Goatee", "Beard" }, SelectedIndex = (initial.FacialHair ?? -1) + 1 };
        var hairColor = new Picker { Title = "Hair color", ItemsSource = new[] { "Original / automatic", "Black", "Blonde", "Brown", "Red", "Light brown", "Gray" }, SelectedIndex = (initial.HairColor ?? -1) + 1 };
        var hairStyle = new Picker { Title = "Hair style", ItemsSource = new[] { "Original / automatic", "Bald", "Cornrows", "Afro", "Flat top", "Buzz cut", "Fade", "Balding", "Close crop", "Bald (hidden)", "Bald 2", "Balding 2", "Buzz cut 2", "Fade 2", "Mullet", "Dreadlocks" }, SelectedIndex = (initial.HairStyle ?? -1) + 1 };
        var preview = new ContentView { HorizontalOptions = LayoutOptions.Center };
        var brand = TeamBrandCatalog.Get(school);
        NcaaPlayerAppearance Selection() => initial with
        {
            FacialHair = facial.SelectedIndex <= 0 ? null : facial.SelectedIndex - 1,
            HairColor = hairColor.SelectedIndex <= 0 ? null : hairColor.SelectedIndex - 1,
            HairStyle = hairStyle.SelectedIndex <= 0 ? null : hairStyle.SelectedIndex - 1
        };
        void Refresh() => preview.Content = PersonPortrait.Create(id, name, Color.FromArgb(brand.Primary), Color.FromArgb(brand.Secondary), 136, staff, age, Selection());
        facial.SelectedIndexChanged += (_, _) => Refresh(); hairColor.SelectedIndexChanged += (_, _) => Refresh(); hairStyle.SelectedIndexChanged += (_, _) => Refresh();
        var save = new Button { Text = "Save appearance", BackgroundColor = DynastyTheme.Palette("#315D59") };
        var cancel = new Button { Text = "Cancel" };
        var page = new PortraitEditorPage { Content = new ScrollView { Content = new VerticalStackLayout { Padding = 18, Spacing = 12, MaximumWidthRequest = 540, Children =
        {
            new Label { Text = "Edit portrait", FontSize = 20, FontAttributes = FontAttributes.Bold },
            new Label { Text = name, FontSize = 14, TextColor = DynastyTheme.Accent }, preview,
            new Label { Text = "Facial hair", FontAttributes = FontAttributes.Bold }, facial,
            new Label { Text = "Hair color", FontAttributes = FontAttributes.Bold }, hairColor,
            new Label { Text = "Hair style", FontAttributes = FontAttributes.Bold }, hairStyle,
            new Label { Text = staff ? "Automatic hair ages with the coach: salt-and-pepper, gray, then silver." : "Imported skin tone and face shape are retained.", FontSize = 11, TextColor = DynastyTheme.Muted },
            new HorizontalStackLayout { Spacing = 10, Children = { cancel, save } }
        } } } };
        var saving = false;
        cancel.Clicked += async (_, _) => { await Navigation.PopModalAsync(); result.TrySetResult(null); };
        save.Clicked += async (_, _) =>
        {
            if (saving || _currentDynasty is null) return;
            saving = true; page.IsSaving = true; save.IsEnabled = false; cancel.IsEnabled = false;
            try
            {
                var next = Selection();
                var before = _currentDynasty;
                _currentDynasty = PortraitCustomizationService.Apply(before, id, next, staff);
                try { await SaveCurrentAsync(DynastyManager.Data.Persistence.SaveKind.AutosaveCurrent); }
                catch { _currentDynasty = before; throw; }
                await Navigation.PopModalAsync(); result.TrySetResult(next);
            }
            catch (Exception ex) { saving = false; page.IsSaving = false; save.IsEnabled = true; cancel.IsEnabled = true; await page.DisplayAlertAsync("Could not save appearance", ex.Message, "Close"); }
        };
        page.Disappearing += (_, _) => { if (!saving) result.TrySetResult(null); };
        Refresh(); await Navigation.PushModalAsync(page); return await result.Task;
    }

    private sealed class PortraitEditorPage : ContentPage
    {
        public bool IsSaving { get; set; }
        protected override bool OnBackButtonPressed() => IsSaving || base.OnBackButtonPressed();
    }
}
