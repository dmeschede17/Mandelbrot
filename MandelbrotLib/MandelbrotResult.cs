namespace MandelbrotLib;

public class MandelbrotResult
{
    public string ImplementationName { get; init; } = string.Empty;
    public MandelbrotRegion Region { get; init; }
    public int MaxIterations { get; init; }
    public int NumTasks { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public TimeSpan ElapsedTime { get; set; }
}
