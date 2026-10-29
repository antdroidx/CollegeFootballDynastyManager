using System.Globalization;
using System.Text;
using System.Text.Json;
using DynastyManager.Core.Models;
namespace ProFootball.Core;
public sealed record ProCoachPortrait(string Name,NcaaPlayerAppearance Appearance,bool Glasses,bool Moustache,string Source,string ImageUrl);
/// <summary>Reviewed illustrative features, keyed by real identity so changing teams preserves the face.</summary>
public static class ProCoachPortraits
{
    static readonly Lazy<IReadOnlyDictionary<string,ProCoachPortrait>> Profiles=new(()=>{
        var assembly=typeof(ProCoachPortraits).Assembly;using var stream=assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().Single(n=>n.EndsWith("nfl-coach-appearances.json")))!;
        var entries=JsonSerializer.Deserialize<List<ProCoachPortrait>>(stream,new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        return entries.ToDictionary(p=>Key(p.Name),StringComparer.Ordinal);
    });
    static string Key(string name)=>new string(name.Normalize(NormalizationForm.FormD).Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    public static ProCoachPortrait? Find(string name)=>Profiles.Value.GetValueOrDefault(Key(name));
    public static IReadOnlyCollection<ProCoachPortrait> All=>Profiles.Value.Values.ToArray();
}
