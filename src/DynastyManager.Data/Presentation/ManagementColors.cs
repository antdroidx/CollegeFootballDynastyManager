using System.Globalization;

namespace DynastyManager.Data.Presentation;

/// <summary>Readable UI accents; original school branding is never changed.</summary>
public static class ManagementColors
{
    private static (int R, int G, int B) Rgb(string hex)
    {
        var value = int.Parse(hex.TrimStart('#'), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return ((value >> 16) & 255, (value >> 8) & 255, value & 255);
    }

    private static double Luminance(string hex)
    {
        var (r,g,b)=Rgb(hex);
        static double Linear(int channel) => channel / 255.0 <= .04045
            ? channel / 255.0 / 12.92 : Math.Pow((channel / 255.0 + .055) / 1.055, 2.4);
        return .2126 * Linear(r) + .7152 * Linear(g) + .0722 * Linear(b);
    }

    public static double ContrastRatio(string foreground, string background)
    {
        var a=Luminance(foreground); var b=Luminance(background);
        return (Math.Max(a,b)+.05)/(Math.Min(a,b)+.05);
    }

    public static string ReadableAccent(string brand, string background)
    {
        if(ContrastRatio(brand,background)>=4.5)return brand;
        var (r,g,b)=Rgb(brand);
        var target=ContrastRatio("#FFFFFF",background)>ContrastRatio("#000000",background)?255:0;
        for(var step=1;step<=20;step++)
        {
            var mix=step/20.0;
            var candidate=$"#{(int)Math.Round(r*(1-mix)+target*mix):X2}{(int)Math.Round(g*(1-mix)+target*mix):X2}{(int)Math.Round(b*(1-mix)+target*mix):X2}";
            if(ContrastRatio(candidate,background)>=4.5)return candidate;
        }
        return target==255?"#FFFFFF":"#000000";
    }
}
