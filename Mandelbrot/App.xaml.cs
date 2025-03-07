using System.Runtime.Intrinsics.X86;
using System.Windows;

namespace Mandelbrot;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (!Avx2.IsSupported || !Fma.IsSupported)
        {
            MessageBox.Show("This application requires a CPU with support for AVX2 and FMA instruction set. The application will now exit.", "Mandelbrot", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }
}
