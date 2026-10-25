namespace DynastyManager.Data.Presentation;

// Layout policy only: never changes dynasty data or simulation state.
public static class ManagementLayout
{
    public static int Category(double width) => width >= 840 ? 2 : width >= 600 ? 1 : 0;
    public static bool Sidebar(double width, bool expanded) => width >= 600 || expanded;
    public static bool SplitProfile(double width) => width >= 840;
    public static double NavigationWidth(double width) => width >= 840 ? 164 : 132;
}
