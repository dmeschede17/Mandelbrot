using System.Collections.Immutable;

namespace MandelbrotLib;

public static class MandelbrotRegionFactory
{
    public static readonly MandelbrotRegion DefaultRegion = new(0, 0, 1);
    public static readonly MandelbrotRegion BlackRegion = new(0, 0, MandelbrotRegion.DefaultRegionHeight * 5);

    static ImmutableArray<MandelbrotNamedRegion> MandelbrotNamedRegions { get; } =
    [
        new() { Name = "Default", Region = DefaultRegion },
        new() { Name = "Flower", Region = new(-1.999985881160, 0, 683173100)},
        new() { Name = "Julia Island", Region = new(-1.76877883, -0.001738898, 5498200)},
        new() { Name = "Starfish", Region = new(-0.374004139,  0.659792175, 1600) },
        new() { Name = "Sun", Region = new(-0.776592847, -0.136640848, 194250) },
        new() { Name = "Tendrils", Region = new(-0.22626663, 1.11617444, 2302800) },
        new() { Name = "Tree", Region = new(-1.940157353, -0.0000011, 2054780) },
        new() { Name = "Outside", Region = new(-2.85, 2.1, MandelbrotRegion.DefaultRegionHeight * 5) },
        new() { Name = "Black", Region = BlackRegion },
    ];

    public static ImmutableArray<string> RegionNames { get; } = [.. MandelbrotNamedRegions.Select(x => x.Name)];

    static ImmutableDictionary<string, MandelbrotRegion> MandelbrotRegionDict { get; } = MandelbrotNamedRegions.ToImmutableDictionary(x => x.Name, x => x.Region);

    public static void GetMandelbrotRegion(string? regionName, out MandelbrotRegion region)
    {
        if (string.IsNullOrEmpty(regionName) || !MandelbrotRegionDict.TryGetValue(regionName, out region))
        {
            region = DefaultRegion;
        }
    }
}
