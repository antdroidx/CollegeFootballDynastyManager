using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;

namespace DynastyManager.App;

public sealed partial class FoundationPage
{
    private async Task<DefensivePlayDefinition?> EditDefensivePlayAsync(DefensivePlayDefinition source)
    {
        var completion = new TaskCompletionSource<DefensivePlayDefinition?>();
        var page = new ContentPage { Title = "Defensive Play Editor", BackgroundColor = DynastyTheme.Background };
        var name = new Entry { Text = source.Name + " Custom", MaxLength = 60 };
        var approach = new Picker { ItemsSource = Enum.GetNames<DefensiveApproach>(), SelectedIndex = (int)source.Approach };
        var rows = new VerticalStackLayout { Spacing = 8 };
        var assignments = source.Assignments.ToList();
        var positions = PersonnelRotationService.DefensivePositions(source.Personnel);
        var slots = positions.Select((position, index) => new DynastyPlayer { PlayerId = new Guid(index + 1, 0, 0, new byte[8]), FullName = "Editor", TeamName = "Editor", Position = position }).ToArray();
        var starts = DefensivePursuitService.Alignments(slots, source with { Assignments = [] });
        var counts = new Dictionary<Position, int>();
        var slotIndex = 0;
        foreach (var position in positions)
        {
            var role = counts.GetValueOrDefault(position); counts[position] = role + 1;
            var initial = assignments.FirstOrDefault(a => a.Position == position && a.RoleIndex == role) ?? new PlayAssignment
            { Position = position, RoleIndex = role, Depth = position is Position.DE or Position.DT ? 1 : 6,
                Lane = Math.Clamp(.15 + rows.Count * .07, .05, .95), Task = position is Position.DE or Position.DT ? AssignmentTask.Rush : AssignmentTask.Zone };
            var alignment = starts[slots[slotIndex++].PlayerId];
            initial = initial with { Depth = alignment.X, Lane = alignment.Y };
            assignments.RemoveAll(a => a.Position == position && a.RoleIndex == role); assignments.Add(initial);
            var task = new Picker { ItemsSource = new[] { "Rush", "Man", "Zone" }, SelectedIndex = initial.Task == AssignmentTask.Rush ? 0 : initial.Task == AssignmentTask.Man ? 1 : 2 };

            var label = new Label { FontAttributes = FontAttributes.Bold };
            void Update()
            {
                assignments.RemoveAll(a => a.Position == position && a.RoleIndex == role);
                assignments.Add(initial with { Depth = alignment.X, Lane = alignment.Y, Task = (AssignmentTask)(task.SelectedIndex + 2) });
                label.Text = $"{position} {role + 1} · package alignment locked";
            }
            task.SelectedIndexChanged += (_, _) => Update(); Update();
            rows.Children.Add(new VerticalStackLayout { Children = { label, task } });
        }
        var save = new Button { Text = "Save to library" }; var cancel = new Button { Text = "Cancel" };
        save.Clicked += async (_, _) => { completion.TrySetResult(source with { Id = "custom-" + Guid.NewGuid().ToString("N"),
            Name = string.IsNullOrWhiteSpace(name.Text) ? source.Name : name.Text.Trim(), Approach = (DefensiveApproach)approach.SelectedIndex,
            Assignments = assignments.ToArray() }); await Navigation.PopModalAsync(); };
        cancel.Clicked += async (_, _) => { completion.TrySetResult(null); await Navigation.PopModalAsync(); };
        page.Disappearing += (_, _) => completion.TrySetResult(null);
        page.Content = new ScrollView { Content = new VerticalStackLayout { Padding = 14, Spacing = 10, Children =
        { cancel, SectionTitle("Defensive Play Editor", "Copy the selected package and assign rush, man coverage or zone responsibility to each slot. Front alignment follows the selected package."), name, approach, save, rows } } };
        await Navigation.PushModalAsync(page); return await completion.Task;
    }

    private async Task<OffensivePlayDefinition?> EditOffensivePlayAsync(OffensivePlayDefinition source)
    {
        var completion = new TaskCompletionSource<OffensivePlayDefinition?>();
        var page = new ContentPage { Title = "Play Editor", BackgroundColor = DynastyTheme.Background };
        var name = new Entry { Text = source.Name + " Custom", Placeholder = "Play name", MaxLength = 60 };
        var formation = new Picker { Title = "Formation", ItemsSource = Enum.GetNames<OffensiveFormation>(), SelectedIndex = (int)source.Formation };
        var concept = new Picker { Title = "Concept", ItemsSource = Enum.GetNames<OffensiveConcept>(), SelectedIndex = (int)source.Concept };
        var assignments = source.Assignments.ToDictionary(a => (a.Position, a.RoleIndex));
        var rows = new VerticalStackLayout { Spacing = 8 };
        var preview = new GraphicsView { HeightRequest = 180, BackgroundColor = DynastyTheme.Palette("#102A24") };
        OffensivePlayDefinition Draft() => source with { Id = "custom-" + Guid.NewGuid().ToString("N"), Name = name.Text?.Trim() ?? "",
            Formation = (OffensiveFormation)Math.Max(0, formation.SelectedIndex),
            Personnel = (OffensiveFormation)formation.SelectedIndex switch { OffensiveFormation.Flexbone => OffensivePersonnel.TripleBack, OffensiveFormation.IForm => OffensivePersonnel.TwentyOne, OffensiveFormation.Empty => OffensivePersonnel.Empty, _ => source.Personnel is OffensivePersonnel.Empty or OffensivePersonnel.TripleBack ? OffensivePersonnel.Eleven : source.Personnel }, Concept = (OffensiveConcept)Math.Max(0, concept.SelectedIndex),
            Assignments = assignments.Values.ToArray(), Description = "Created in Play Editor" };
        void Draw() { var draft = Draft(); preview.Drawable = new PlayArtDrawable(draft.Id, draft.Name, true, draft.Concept.ToString(), draft); preview.Invalidate(); }
        void BuildAssignments()
        {
            rows.Children.Clear();
            var positions = new[] { Position.QB, Position.OL, Position.OL, Position.OL, Position.OL, Position.OL }
                .Concat(GameDayAnimationPlanner.EligiblePositions(Draft().Personnel));
            var counts = new Dictionary<Position, int>();
            foreach (var position in positions)
            {
                var role = counts.GetValueOrDefault(position); counts[position] = role + 1;
                var slot = (position, role);
                var alignment = GameDayAnimationPlanner.OffensiveAlignment(Draft(), position, role);
                var original = assignments.GetValueOrDefault(slot) ?? new PlayAssignment { Position = position, RoleIndex = role,
                    Depth = alignment.X, Lane = alignment.Y, Task = position == Position.OL ? AssignmentTask.Block : AssignmentTask.Route,
                    Route = GameDayAnimationPlanner.AssignedRoute(source, position, role).ToString() };
                original = original with { Depth = alignment.X, Lane = alignment.Y };
                assignments[slot] = original;
                var task = new Picker { ItemsSource = new[] { "Route", "Block" }, SelectedIndex = original.Task == AssignmentTask.Block ? 1 : 0 };
                var route = new Picker { ItemsSource = Enum.GetNames<GameDayRouteFamily>(), SelectedIndex = (int)Enum.Parse<GameDayRouteFamily>(original.Route) };
                task.IsEnabled = position != Position.OL && position != Position.QB;
                if (position == Position.OL || position == Position.QB) task.SelectedIndex = 1;
                var label = new Label { FontAttributes = FontAttributes.Bold };
                void Update()
                {
                    assignments[slot] = original with { Depth = alignment.X, Lane = alignment.Y,
                        Task = task.SelectedIndex == 1 ? AssignmentTask.Block : AssignmentTask.Route,
                        Route = Enum.GetNames<GameDayRouteFamily>()[Math.Max(0, route.SelectedIndex)] };
                    label.Text = $"{position} {role + 1} · {(alignment.X >= -.5 ? "On line" : "Backfield / off line")} · formation alignment locked";
                    route.IsEnabled = task.SelectedIndex == 0; Draw();
                }
                task.SelectedIndexChanged += (_, _) => Update(); route.SelectedIndexChanged += (_, _) => Update();

                rows.Children.Add(new Border { StrokeThickness = 0, Padding = 10, BackgroundColor = DynastyTheme.Palette("#1B2B37"),
                    Content = new VerticalStackLayout { Spacing = 3, Children = { label, task, route } } });
                Update();
            }
        }
        var save = new Button { Text = "Save to play library" }; var cancel = new Button { Text = "Cancel" };
        var status = new Label { TextColor = DynastyTheme.Accent };
        save.Clicked += async (_, _) =>
        {
            var draft = Draft();
            try { PlayEditorService.Validate(new TeamGamePlan { TeamName = "Editor", CreatedOffensivePlays = [draft] }); }
            catch (ArgumentException ex) { status.Text = ex.Message; return; }
            completion.TrySetResult(draft); await Navigation.PopModalAsync();
        };
        cancel.Clicked += async (_, _) => { completion.TrySetResult(null); await Navigation.PopModalAsync(); };
        page.Disappearing += (_, _) => completion.TrySetResult(null);
        formation.SelectedIndexChanged += (_, _) => { assignments.Clear(); BuildAssignments(); }; concept.SelectedIndexChanged += (_, _) => Draw();
        BuildAssignments();
        page.Content = new ScrollView { Content = new VerticalStackLayout { Padding = 14, Spacing = 10, Children =
        { cancel, SectionTitle("Play Editor", "Choose a formation, then edit routes and blocking assignments. Player alignments follow formation rules. Save it to the library, then add it to your active Custom playbook."),
            name, formation, concept, preview, save, status, rows } } };
        await Navigation.PushModalAsync(page);
        return await completion.Task;
    }
}
