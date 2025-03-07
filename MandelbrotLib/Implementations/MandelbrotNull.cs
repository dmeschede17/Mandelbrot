namespace MandelbrotLib.Implementations;

public class MandelbrotNull() : MandelbrotBase(widthAlignment: 1, heightAlignment: 1) 
{
    protected override void Calculate(MandelbrotRegion rectangle, int maxIterations, int firstRowDiv2, int numRows, int rowDeltaDiv2) { }
}
