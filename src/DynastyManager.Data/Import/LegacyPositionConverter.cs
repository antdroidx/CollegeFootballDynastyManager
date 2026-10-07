using System.Security.Cryptography;
using System.Text;
using DynastyManager.Core.Models;

namespace DynastyManager.Data.Import;

public static class LegacyPositionConverter
{
    public static Position Convert(string rawPosition, string stableKey, out string? warning)
    {
        var normalized = rawPosition.Trim().ToUpperInvariant();
        warning = null;

        if (Enum.TryParse<Position>(normalized, ignoreCase: true, out var modern))
            return modern;

        var bucket = StableBucket(stableKey);

        return normalized switch
        {
            "DL" => WithWarning(bucket < 55 ? Position.DE : Position.DT,
                "Legacy position DL converted to DE/DT. Edit the CSV to specify the exact modern position.", out warning),

            "LB" => WithWarning(bucket < 55 ? Position.OLB : Position.MLB,
                "Legacy position LB converted to OLB/MLB. Edit the CSV to specify the exact modern position.", out warning),

            "S" => WithWarning(bucket < 50 ? Position.FS : Position.SS,
                "Legacy position S converted to FS/SS. Edit the CSV to specify the exact modern position.", out warning),

            _ => throw new FormatException($"Unknown player position '{rawPosition}'.")
        };
    }

    private static Position WithWarning(Position position, string message, out string? warning)
    {
        warning = message;
        return position;
    }

    private static int StableBucket(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return bytes[0] % 100;
    }
}
