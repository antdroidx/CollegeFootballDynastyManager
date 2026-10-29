namespace DynastyManager.App;

/// <summary>WCAG relative luminance for opaque sRGB colors; independent of native widgets.</summary>
public static class ColorContrast
{
    static double Linear(double c) => c <= .04045 ? c / 12.92 : Math.Pow((c + .055) / 1.055, 2.4);
    public static double Luminance(double r, double g, double b) => .2126 * Linear(r) + .7152 * Linear(g) + .0722 * Linear(b);
    public static double Ratio(double r, double g, double b, double br, double bg, double bb)
    {
        var a = Luminance(r,g,b); var z = Luminance(br,bg,bb);
        return (Math.Max(a,z) + .05) / (Math.Min(a,z) + .05);
    }
    public static (double R,double G,double B) Ensure(double r,double g,double b,double br,double bg,double bb)
    {
        if (Ratio(r,g,b,br,bg,bb) >= 4.5) return (r,g,b);
        var end = Ratio(0,0,0,br,bg,bb) >= Ratio(1,1,1,br,bg,bb) ? 0d : 1d;
        double low=0,high=1;
        for(var i=0;i<24;i++)
        {
            var t=(low+high)/2;
            if(Ratio(r+(end-r)*t,g+(end-g)*t,b+(end-b)*t,br,bg,bb)>=4.5)high=t;else low=t;
        }
        return (r+(end-r)*high,g+(end-g)*high,b+(end-b)*high);
    }
}
