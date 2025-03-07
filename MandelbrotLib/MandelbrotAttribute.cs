using System.Reflection;

namespace MandelbrotLib;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MandelbrotAttribute() : Attribute
{
    public required string Name { get; init; }
    public int Priority { get; init;  }
    public bool Avx512FRequired { get; init; }
}

public static class MandelbrotAttributeTypeExtensions
{
    public static MandelbrotAttribute? GetMandelbrotAttribute(this Type type)
    {
        return type?.GetCustomAttributes(typeof(MandelbrotAttribute), false).FirstOrDefault() as MandelbrotAttribute;
    }

    const string MandelbrotTypePrefix = "Mandelbrot";

    public static string GetMandelbrotName(this Type mandelbrotType)
    {
        ArgumentNullException.ThrowIfNull(mandelbrotType, nameof(mandelbrotType));

        MandelbrotAttribute? mandelbrotAttribute = mandelbrotType.GetCustomAttribute<MandelbrotAttribute>();

        if (!string.IsNullOrEmpty(mandelbrotAttribute?.Name))
        {
            return mandelbrotAttribute.Name;
        }

        string name = mandelbrotType.Name;

        return name.StartsWith(MandelbrotTypePrefix, StringComparison.OrdinalIgnoreCase) ? name[MandelbrotTypePrefix.Length..] : name;
    }
}
