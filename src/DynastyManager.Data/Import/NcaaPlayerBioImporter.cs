using System.Globalization;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;

namespace DynastyManager.Data.Import;

public static class NcaaPlayerBioImporter
{
    public static DynastyPlayer Apply(DynastyPlayer player, IReadOnlyDictionary<string, string> row)
    {
        int? Read(string key, int min, int max)
        {
            if (!row.TryGetValue(key, out var text) || string.IsNullOrWhiteSpace(text)) return null;
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value < min || value > max)
                throw new FormatException($"{key} must be {min}–{max}.");
            return value;
        }
        var rawHeight = Read("PHGT", 48, 90); var height = Read("HEIGHTINCHES", 48, 90);
        var rawWeight = Read("PWGT", 0, 255); var weight = Read("WEIGHTPOUNDS", 100, 450);
        if (rawHeight.HasValue && height.HasValue && rawHeight != height) throw new FormatException("PHGT and HeightInches disagree.");
        if (rawWeight.HasValue && weight.HasValue && rawWeight + 160 != weight) throw new FormatException("PWGT and WeightPounds disagree.");
        var appearance = new NcaaPlayerAppearance { SkinTone = Read("PSKI", 0, 7), HairColor = Read("PHCL", 0, 5),
            HairStyle = Read("PHED", 0, 14), Face = Read("PFMP", 0, 63), FacialHair = Read("FACIALHAIR", 0, 3) };
        return PlayerPhysicalService.EnsureMeasurements(player with
        {
            HeightInches = height ?? rawHeight ?? player.HeightInches,
            WeightPounds = weight ?? (rawWeight.HasValue ? rawWeight.Value + 160 : player.WeightPounds),
            Appearance = appearance.SkinTone.HasValue || appearance.HairColor.HasValue || appearance.HairStyle.HasValue || appearance.Face.HasValue || appearance.FacialHair.HasValue
                ? appearance : player.Appearance
        });
    }
}
