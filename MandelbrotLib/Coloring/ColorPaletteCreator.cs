using MandelbrotLib.Utils;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MandelbrotLib.Coloring;

internal static class ColorPaletteCreator
{
    const UInt32 FallbackColor = 0xFFFFFF;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Vector128<byte> CreateShuffleMaskFromBgr32() => Vector128.Create(0, 0x80, 0x80, 0x80, 1, 0x80, 0x80, 0x80, 2, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Vector128<byte> CreateShuffleMaskToBgr32() => Vector128.Create(0x80080400).AsByte();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Vector128<int> FromBgr32(uint bgr32, Vector128<byte> shuffleMaskFromBgr32) => Ssse3.Shuffle(Vector128.Create(bgr32).AsByte(), shuffleMaskFromBgr32).AsInt32();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static uint ToBgr32(Vector128<int> vBgr, Vector128<byte> shuffleMaskToBgr32) => Ssse3.Shuffle(vBgr.AsByte(), shuffleMaskToBgr32).AsUInt32().GetElement(0);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Linear(uint color0, uint color1, Span<uint> colors, Vector128<byte> shuffleMaskFromBgr32, Vector128<byte> shuffleMaskToBgr32)
    {
        int colorsLength = colors.Length;

        if (colorsLength == 0)
        {
            return;
        }

        Vector128<int> vColor0 = FromBgr32(color0, shuffleMaskFromBgr32);
        Vector128<int> vColor1 = FromBgr32(color1, shuffleMaskFromBgr32);

        Vector128<float> vColor0AsFloat = Sse2.ConvertToVector128Single(vColor0);

        Vector128<float> vInvLength = Sse.Reciprocal(Vector128.Create((float)colorsLength));
        Vector128<float> vColorDelta = Sse2.ConvertToVector128Single(vColor1 - vColor0);
        Vector128<float> vColorDeltaTimesInvLength = vColorDelta * vInvLength;

        Vector128<float> vOne = Vector128.Create(1.0f);

        Vector128<float> vI = Vector128<float>.Zero;

        for (int i = 0; i < colorsLength; i++)
        {
            Vector128<float> vColor = Fma.MultiplyAdd(vColorDeltaTimesInvLength, vI, vColor0AsFloat);
            colors[i] = ToBgr32(Sse2.ConvertToVector128Int32WithTruncation(vColor), shuffleMaskToBgr32);
            vI += vOne;
        }
    }

    internal static void CreatePalette(ReadOnlySpan<UInt32> colors, int size, ref GrowingArray<UInt32> colorPalette)
    {
        int colorsLength = colors.Length;
        int paletteLength = Math.Max(1, size);

        colorPalette.Length = paletteLength;

        if (colorsLength <= 1)
        {
            colorPalette.AsSpan().Fill(colorsLength == 0 ? FallbackColor : colors[0]);

            return; // ### RETURN ###
        }

        Debug.Assert(colorsLength > 1);
        Debug.Assert(paletteLength > 0);

        Span<UInt32> colorPaletteSpan = colorPalette.AsSpan();

        int numSegments = colors.Length - 1;

        Debug.Assert(numSegments > 0);

        Vector128<byte> shuffleMaskFromBgr32 = CreateShuffleMaskFromBgr32();
        Vector128<byte> shuffleMaskToBgr32 = CreateShuffleMaskToBgr32();

        int i0 = 0;

        for (int k = 0; k < numSegments; k++)
        {
            int i1 = (k + 1) * paletteLength / numSegments;

            Linear(colors[k], colors[k + 1], colorPaletteSpan.Slice(i0, i1 - i0), shuffleMaskFromBgr32, shuffleMaskToBgr32);

            i0 = i1;
        }

        Debug.Assert(i0 == paletteLength);
    }
}
