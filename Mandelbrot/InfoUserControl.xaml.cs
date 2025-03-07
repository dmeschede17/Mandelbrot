using MandelbrotLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Mandelbrot;

public partial class InfoUserControl : UserControl
{
    public InfoUserControl()
    {
        InitializeComponent();

        foreach (var infoElement in StackPanelInfo.Children)
        {
            if (infoElement is TextBox textBox)
            {
                textBox.Focusable = false;
                textBox.IsReadOnly = true;
            }
        }

    }

    const int FLOPPerIteration = 11; // See README.md

    internal void SetResults(in MandelbrotIterationsInfo iterationsInfo, in MandelbrotResult result)
    {
        int numTotalPixels = result.Width * result.Height;
        double totalSeconds = result.ElapsedTime.TotalSeconds;
        double pixelsPerSeconds = totalSeconds > 0 ? numTotalPixels / totalSeconds : 0;

        var iterationsPerSeconds = totalSeconds > 0 ? iterationsInfo.TotalNumberOfIterations / totalSeconds : 0;
        var iterationsPerPixel = numTotalPixels > 0 ? iterationsInfo.TotalNumberOfIterations / (double)numTotalPixels : 0;

        var region = result.Region;

        TextBoxInfoImplementation.Text = result.ImplementationName;
        TextBoxInfoNumTasks.Text = result.NumTasks.ToString(CultureInfo.InvariantCulture);
        TextBoxInfoRegion.Text = $"{region.X0:+0.0000000;-0.0000000}, {region.Y0:+0.0000000;-0.0000000}\n{region.X1:+0.0000000;-0.0000000}, {region.Y1:+0.0000000;-0.0000000}";
        TextBoxInfoImageSize.Text = $"{result.Width:N0} x {result.Height:N0} = {numTotalPixels:N0}";
        TextBoxInfoElapsedTime.Text = $"{result.ElapsedTime.TotalMilliseconds:N1} ms";
        TextBoxInfoPixelsPerSecond.Text = $"{pixelsPerSeconds:N1}";
        TextBoxInfoIterationsPerSecond.Text = $"{iterationsPerSeconds:N1}";
        TextBoxInfoIterationsPerPixelAverage.Text = $"{iterationsPerPixel:N1}";
        TextBoxInfoIterationsPerPixelMinimum.Text = $"{iterationsInfo.MinIterations:N0}";
        TextBoxInfoIterationsPerPixelMaximum.Text = $"{iterationsInfo.MaxIterations:N0}";
        TextBoxInfoFLOPS.Text = $"{iterationsPerSeconds * FLOPPerIteration:N1}";
    }
}
