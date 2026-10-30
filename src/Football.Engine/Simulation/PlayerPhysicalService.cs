using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class PlayerPhysicalService
{
    public static (int Height, int Weight) Generate(Guid id, Position position)
    {
        var b = id.ToByteArray();
        var (height, weight, heightRange, weightRange) = position switch
        {
            Position.FB => (73, 240, 3, 20), Position.QB => (75, 215, 3, 20), Position.RB => (71, 210, 3, 25),
            Position.WR => (73, 195, 4, 20), Position.TE => (77, 245, 3, 25),
            Position.OL => (77, 305, 3, 30), Position.DE => (76, 260, 3, 25),
            Position.DT => (75, 305, 3, 30), Position.OLB or Position.MLB => (74, 235, 3, 20),
            Position.CB => (71, 190, 3, 15), Position.FS or Position.SS => (73, 205, 3, 20),
            _ => (73, 195, 3, 25)
        };
        return (height + (b[0] + b[7]) % (heightRange * 2 + 1) - heightRange,
            weight + (b[3] + b[11]) % (weightRange * 2 + 1) - weightRange);
    }

    public static DynastyPlayer EnsureMeasurements(DynastyPlayer player)
    {
        var (height, weight) = Generate(player.PlayerId, player.Position);
        return player with { HeightInches = player.HeightInches > 0 ? player.HeightInches : height,
            WeightPounds = player.WeightPounds > 0 ? player.WeightPounds : weight };
    }

    public static string Format(int height, int weight) =>
        $"{(height > 0 ? $"{height / 12}′{height % 12}″" : "Height unknown")} · {(weight > 0 ? $"{weight} lb" : "Weight unknown")}";
}
