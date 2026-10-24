using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Persistence;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private async Task OpenPlaybookEditorAsync(TeamGamePlan plan, Guid dynastyId)
    {
        var createdDefense = plan.CreatedDefensivePlays.ToList();
        var createdOffense = plan.CreatedOffensivePlays.ToList();
        var offenseIds = (plan.CustomOffensivePlayIds.Count > 0 ? plan.CustomOffensivePlayIds :
            GamePlanService.AvailableOffensivePlays(plan).Select(p => p.Id)).ToHashSet(StringComparer.Ordinal);
        var defenseIds = (plan.CustomDefensivePlayIds.Count > 0 ? plan.CustomDefensivePlayIds :
            GamePlanService.AvailableDefensivePlays(plan).Select(p => p.Id)).ToHashSet(StringComparer.Ordinal);
        var page = new ContentPage { Title = "Playbook Editor", BackgroundColor = DynastyTheme.Background };
        var side = new Picker { Title = "Side", ItemsSource = new[] { "Offense", "Defense" }, SelectedIndex = 0 };
        if (_currentDynasty?.UserCoachRole is StaffRole.OffensiveCoordinator or StaffRole.DefensiveCoordinator) { side.SelectedIndex = _currentDynasty.UserCoachRole == StaffRole.OffensiveCoordinator ? 0 : 1; side.IsEnabled = false; }
        var formation = new Picker { Title = "Browse formation" };
        var count = new Label { TextColor = DynastyTheme.Accent };
        var status = new Label { TextColor = DynastyTheme.Muted };
        var library = new VerticalStackLayout { Spacing = 8 };
        var save = new Button { Text = "Save & use Custom playbooks" };
        var back = new Button { Text = "Cancel" };
        back.Clicked += async (_, _) => await Navigation.PopModalAsync();
        void Render()
        {
            library.Children.Clear();
            count.Text = $"Offense {offenseIds.Count}/17 · Defense {defenseIds.Count}/14";
            var selected = Math.Max(0, formation.SelectedIndex);
            if (side.SelectedIndex == 0)
            {
                var value = Enum.GetValues<OffensiveFormation>()[selected];
                foreach (var p in PlaybookCatalog.OffensivePlays.Concat(createdOffense).Where(p => p.Formation == value))
                {
                    library.Children.Add(PlayLibraryRow(p.Id, p.Name,
                        $"{GamePlanService.Label(p.Formation)} · {GamePlanService.Label(p.Concept)}", p.Description,
                        true, p.Concept.ToString(), offenseIds, Render, p));
                    var edit = new Button { Text = $"Edit / copy {p.Name}" };
                    edit.Clicked += async (_, _) => { var result = await EditOffensivePlayAsync(p); if (result is not null) { createdOffense.Add(result); Render(); } };
                    library.Children.Add(edit);
                }
            }
            else
            {
                var value = Enum.GetValues<DefensiveFormation>()[selected];
                foreach (var p in PlaybookCatalog.DefensivePlays.Concat(createdDefense).Where(p => p.Formation == value))
                {
                    library.Children.Add(PlayLibraryRow(p.Id, p.Name,
                        $"{GamePlanService.Label(p.Formation)} · {GamePlanService.Label(p.Approach)}", p.Description,
                        false, p.Approach.ToString(), defenseIds, Render, defensiveDefinition: p));
                    var edit = new Button { Text = $"Edit / copy {p.Name}" };
                    edit.Clicked += async (_, _) => { var result = await EditDefensivePlayAsync(p); if (result is not null) { createdDefense.Add(result); Render(); } };
                    library.Children.Add(edit);
                }
            }
            save.IsEnabled = offenseIds.Count > 0 && defenseIds.Count > 0;
        }
        void ChangeSide()
        {
            formation.ItemsSource = side.SelectedIndex == 0
                ? Enum.GetValues<OffensiveFormation>().Select(GamePlanService.Label).ToArray()
                : Enum.GetValues<DefensiveFormation>().Select(GamePlanService.Label).ToArray();
            formation.SelectedIndex = 0;
            Render();
        }
        side.SelectedIndexChanged += (_, _) => ChangeSide();
        formation.SelectedIndexChanged += (_, _) => Render();
        save.Clicked += async (_, _) =>
        {
            if (_currentDynasty is null || _currentDynasty.DynastyId != dynastyId || _isAdvancing) return;
            save.IsEnabled = false;
            try
            {
                _currentDynasty = GamePlanService.SetPlan(_currentDynasty, plan with
                {
                    Offense = OffensivePlaybook.Custom, Defense = DefensivePlaybook.Custom,
                    CreatedOffensivePlays = createdOffense.ToArray(), CreatedDefensivePlays = createdDefense.ToArray(),
                    CustomOffensivePlayIds = offenseIds.ToArray(), CustomDefensivePlayIds = defenseIds.ToArray(),
                    OpeningPlayIds = [], OpeningScript = []
                });
                _cachedCurrentProfiles = null;
                await SaveCurrentAsync(SaveKind.AutosaveCurrent);
                RenderGamePlanScreen();
                await Navigation.PopModalAsync();
            }
            catch (Exception ex) { status.Text = $"Could not save playbook: {ex.Message}"; save.IsEnabled = true; }
        };
        ChangeSide();
        page.Content = new ScrollView { Content = new VerticalStackLayout
        {
            Padding = 14, Spacing = 10, Children = { back,
                SectionTitle("Playbook Editor", "Browse every formation in the full catalog. Remove a call before adding another when a book is full. Saving replaces the active playbooks."),
                side, formation, count, save, status, library }
        } };
        await Navigation.PushModalAsync(page);
    }
}
