using System.Globalization;

namespace DynastyManager.Data.Presentation;

public static class TableOrdering
{
    public static IReadOnlyList<T> Sort<T>(IEnumerable<T> rows, Func<T, string?> key, bool descending) =>
        rows.OrderBy(key, Comparer<string?>.Create((left, right) => Compare(left, right, descending))).ToArray();

    public static int Compare(string? left, string? right, bool descending)
    {
        static bool Missing(string? value) => string.IsNullOrWhiteSpace(value) || value.Trim() is "—" or "-";
        if (Missing(left)) return Missing(right) ? 0 : 1;
        if (Missing(right)) return -1;
        static bool Number(string value, out decimal number) => decimal.TryParse(value.Trim().TrimStart('#').TrimEnd('★'),
            NumberStyles.Number, CultureInfo.InvariantCulture, out number);
        var comparison = Number(left!, out var l) && Number(right!, out var r)
            ? l.CompareTo(r) : StringComparer.OrdinalIgnoreCase.Compare(left, right);
        return descending ? -comparison : comparison;
    }
}
