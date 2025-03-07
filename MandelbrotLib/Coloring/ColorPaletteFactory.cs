using MandelbrotLib.Utils;
using System.Collections.Immutable;
using System.Diagnostics;

namespace MandelbrotLib.Coloring;

public static class ColorPaletteFactory
{
    internal readonly struct ColorPaletteConfig
    {
        internal ColorPaletteConfig(string name, UInt32[] colors, bool loop = false, bool mirror = false)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));
            Debug.Assert(colors.Length > 0);

            Name = name;

            if (loop)
            {
                Debug.Assert(!mirror);

                Colors = new UInt32[colors.Length + 1];
                colors.CopyTo(Colors);
                Colors[colors.Length] = colors[0];
            }
            else if (mirror)
            {
                Debug.Assert(!loop);

                Colors = new UInt32[colors.Length * 2 - 1];

                Span<UInt32> reversedColors = Colors.AsSpan(colors.Length);

                colors.CopyTo(Colors, 0);
                colors.AsSpan(0, colors.Length - 1).CopyTo(reversedColors);
                reversedColors.Reverse();
            }
            else
            {
                Colors = new UInt32[colors.Length];
                colors.CopyTo(Colors);
            }
        }

        public string Name { get; init; }
        public UInt32[] Colors { get; init; }
    }

    static readonly ColorPaletteConfig[] ColorPaletteConfigs = 
    [
        new("Fire", [0x200000, 0x900000, 0xFF0000, 0xFF8000, 0xFFFF00, 0xFFFFFF ], mirror: true),
        new("Blue & Green", [0x0047AB, 0xADFF2F ], loop: true),
        new("Red & Gold", [0xA30000, 0xFFD700], loop: true),
        new("LCH", [0x16aefc, 0x2660f6, 0xea1dfa, 0xfb1b8a, 0xfb1d26, 0xfca816, 0xdbfb1e, 0x1dfd6e, 0x3afbde, 0x30dffb], loop: true),
        new("Gray", [0x202020, 0xFFFFFF], loop: true),
    ];

    public static ImmutableArray<string> ColorPaletteNames { get; } = [.. ColorPaletteConfigs.Select(p => p.Name)];

    static readonly ImmutableDictionary<string, ColorPaletteConfig> ColorPaletteConfigsDict = ColorPaletteConfigs.ToImmutableDictionary(p => p.Name);

    public static void CreatePalette(string name, int size, ref GrowingArray<UInt32> colorPalette)
    {
        if (!string.IsNullOrEmpty(name) && ColorPaletteConfigsDict.TryGetValue(name, out ColorPaletteConfig paletteConfig))
        {
            ColorPaletteCreator.CreatePalette(paletteConfig.Colors, size, ref colorPalette);
        }
        else
        {
            ColorPaletteCreator.CreatePalette([], size, ref colorPalette);
        }
    }
}
