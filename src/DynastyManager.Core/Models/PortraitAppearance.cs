namespace DynastyManager.Core.Models;

/// <summary>Fictional appearance derived only from persistent identity, never ratings or school.</summary>
public sealed record PortraitAppearance(int Skin, int HairColor, int HairStyle, int FaceShape,
    int Brow, int FacialHair, int Detail)
{
    public static PortraitAppearance ForPlayer(Guid id, NcaaPlayerAppearance? appearance)
    {
        var fallback = ForIdentity(id);
        if (appearance is null) return fallback;
        var face = appearance.Face is >= 0 and <= 63 ? appearance.Face.Value : (int?)null;
        return fallback with
        {
            Skin = appearance.SkinTone switch { >= 0 and <= 6 => appearance.SkinTone.Value, 7 => 1, _ => fallback.Skin },
            HairColor = appearance.HairColor switch { 0 => 0, 1 => 4, 2 => 2, 3 => 5, 4 => 3, 5 => 6, _ => fallback.HairColor },
            HairStyle = appearance.HairStyle switch
            {
                0 or 8 or 9 => 4, 1 => 5, 2 => 3, 3 => 6, 4 or 11 => 0,
                5 or 12 => 7, 6 or 10 => 8, 7 => 9, 13 => 10, 14 => 11, _ => fallback.HairStyle
            },
            FaceShape = face.HasValue ? face.Value % 4 : fallback.FaceShape,
            Brow = face.HasValue ? face.Value / 4 % 3 : fallback.Brow,
            Detail = face.HasValue ? face.Value / 12 % 4 : fallback.Detail,
            // NCAA face textures cannot be reconstructed as exact vector likenesses.
            FacialHair = face.HasValue ? 0 : fallback.FacialHair
        };
    }

    public static PortraitAppearance ForIdentity(Guid id)
    {
        // Explicit byte order and arithmetic keep faces identical across devices and launches.
        uint seed = 2166136261;
        foreach (var value in id.ToByteArray()) seed = unchecked((seed ^ value) * 16777619);
        int Next(int count)
        {
            seed = unchecked(seed * 1664525 + 1013904223);
            return (int)((seed >> 16) % count);
        }
        return new(Next(7), Next(6), Next(5), Next(4), Next(3), Next(4), Next(4));
    }
}
