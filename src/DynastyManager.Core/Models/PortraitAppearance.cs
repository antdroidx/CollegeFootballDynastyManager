namespace DynastyManager.Core.Models;

/// <summary>Fictional appearance derived only from persistent identity, never ratings or school.</summary>
public sealed record PortraitAppearance(int Skin, int HairColor, int HairStyle, int FaceShape,
    int Brow, int FacialHair, int Detail)
{
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
