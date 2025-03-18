namespace MandelbrotLib;

public readonly record struct MandelbrotRegion
{
    public const double DefaultRegionWidth = 6.3;
    public const double DefaultRegionHeight = 4.2;

    public double X0 { get; init; }
    public double Y0 { get; init; }
    public double X1 { get; init; }
    public double Y1 { get; init; }

    public MandelbrotRegion() { }

    public MandelbrotRegion(double x, double y, double zoomFactor)
    {
        X0 = x - 0.5 * DefaultRegionWidth / zoomFactor;
        Y0 = y - 0.5 * DefaultRegionHeight / zoomFactor;
        X1 = x + 0.5 * DefaultRegionWidth / zoomFactor;
        Y1 = y + 0.5 * DefaultRegionHeight / zoomFactor;
    }

    public MandelbrotRegionCenterAndZoomFactor GetCenterAndZoomFactor()
    {
        double x = 0.5 * (X0 + X1);
        double y = 0.5 * (Y0 + Y1);
        double dX = Math.Abs(X1 - X0);
        double zoomFactor = dX != 0 ? DefaultRegionWidth / dX : 1.0;
        return new MandelbrotRegionCenterAndZoomFactor { X = x, Y = y, ZoomFactor = zoomFactor };
    }
}

public readonly record struct MandelbrotRegionCenterAndZoomFactor
{
    public double X { get; init; }
    public double Y { get; init; }
    public double ZoomFactor { get; init; }
}

public readonly record struct MandelbrotNamedRegion()
{
    public required string Name { get; init; }
    public MandelbrotRegion Region { get; init; }
}
