using MandelbrotLib.Implementations;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Intrinsics.X86;

namespace MandelbrotLib;

public static class MandelbrotFactory
{
    public static ImmutableArray<string> MandelbrotNames { get; }

    static ImmutableDictionary<string, Type> MandelbrotTypeDict { get; }
    static ImmutableDictionary<string, bool> MandelbrotTypeIsSupportedDict { get; }

    static MandelbrotFactory()
    {
        var mandelbrotTypes = typeof(MandelbrotBase).Assembly.GetTypes().Where(t => typeof(MandelbrotBase).IsAssignableFrom(t) && !t.IsAbstract);
        var mandelbortTypesSorted = mandelbrotTypes.Select(t => (t, a: t.GetMandelbrotAttribute())).Where(x => x.a != null).OrderBy(x => x.a!.Priority).ToArray();

        MandelbrotNames = [.. mandelbortTypesSorted.Select(x => x.t.GetMandelbrotName())];
        MandelbrotTypeDict = mandelbortTypesSorted.ToImmutableDictionary(x => x.t.GetMandelbrotName(), x => x.t);
        MandelbrotTypeIsSupportedDict = mandelbortTypesSorted.ToImmutableDictionary(x => x.t.GetMandelbrotName(), x => !x.a!.Avx512FRequired || Avx512F.IsSupported);
    }

    public static Type GetMandelbrotType(string mandelbrotName) => MandelbrotTypeDict.TryGetValue(mandelbrotName, out Type? mandelbrotType) && mandelbrotType != null ? mandelbrotType : typeof(MandelbrotNull);
    public static bool IsSupported(string mandelbrotName) => MandelbrotTypeIsSupportedDict.TryGetValue(mandelbrotName, out bool isSupported) && isSupported;

    public static MandelbrotBase CreateMandelbrot(string mandelbrotName)
    {
        MandelbrotBase? mandelbrot = null;

        if (MandelbrotTypeDict.TryGetValue(mandelbrotName, out Type? mandelbrotType))
        {
            mandelbrot = CreateMandelbrot(mandelbrotType);
        }

        return mandelbrot ?? new MandelbrotNull();
    }

    [SuppressMessage("Style", "IDE0270:Use coalesce expression", Justification = "The if statement is easier to understand")]
    public static MandelbrotBase CreateMandelbrot(Type mandelbrotType)
    {
        ArgumentNullException.ThrowIfNull(mandelbrotType, nameof(mandelbrotType));

        if (!typeof(MandelbrotBase).IsAssignableFrom(mandelbrotType))
        {
            throw new ArgumentException($"Type must be assignable to MandelbrotBase!", nameof(mandelbrotType));
        }

        object? mandelbrot = Activator.CreateInstance(mandelbrotType);

        if (mandelbrot == null)
        {
            throw new ArgumentException($"Failed to create mandelbrot of type '{mandelbrotType.Name}'!", nameof(mandelbrotType));
        }

        return (MandelbrotBase)mandelbrot;
    }
}
