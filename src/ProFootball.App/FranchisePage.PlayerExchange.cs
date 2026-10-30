using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using ProFootball.Core;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    async Task ImportCollegeClass()
    {
        ProDraftClassImport.RequireOpen(state!);
        var file=await FilePicker.Default.PickAsync(new PickOptions{PickerTitle="Choose a College draft-class JSON file"});if(file is null)return;
        using var stream=await file.OpenReadAsync();using var reader=new StreamReader(stream);
        var buffer=new char[PlayerExportService.MaxBytes+1];var count=await reader.ReadBlockAsync(buffer,0,buffer.Length);
        if(count>PlayerExportService.MaxBytes)throw new InvalidDataException("Draft-class files must be under 16 MB.");
        var export=PlayerExportService.Parse(new string(buffer,0,count));var preview=ProDraftClassImport.Preview(state!,export);
        var content=new VerticalStackLayout{Spacing=10};content.Add(Text($"College {export.Year} → Pro {state!.Year}",22,true));
        content.Add(Text($"{preview.Imported.Count} imported players + {preview.Fillers.Count} generated prospects. This replaces the unstarted draft class and resets its scouting/shortlist; current players, contracts and draft-pick ownership stay intact."));
        content.Add(Text("Pro ratings are recalculated from translated football attributes. Potential and scouting uncertainty remain part of the draft.",12));
        content.Add(Table([("Player",180),("Pos",45),("College OVR",90),("Pro OVR",70)],preview.Imported.OrderByDescending(p=>p.Football.OverallRating).Take(40).Select(p=>new View[]{Text(p.Football.FullName),Text(p.Football.Position.DisplayName()),Text(export.Players.Single(x=>x.PlayerId==p.Football.PlayerId).OverallRating.ToString()),Text(p.Football.OverallRating.ToString())})));
        content.Add(Action("Import class for "+state.Year,async()=>{await Update(s=>ProDraftClassImport.Apply(s,export));await Navigation.PopModalAsync();await Draft();},"pro-apply-college-class"));
        await ShowPage("Preview College draft class",content);
    }
    Button CollegeClassImportButton()
    {
        var button=Action("Import College draft class",ImportCollegeClass,"pro-import-college-class");
        try{ProDraftClassImport.RequireOpen(state!);}catch(InvalidOperationException){button.IsEnabled=false;button.Opacity=.45;}
        return button;
    }
}
