using MandelbrotLib;
using MandelbrotLib.Coloring;
using MandelbrotLib.Implementations;
using MandelbrotLib.Utils;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Mandelbrot;

[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "It's ok for MainWindow, because the lifetime of the MainWindow is the application lifetime")]
public partial class MainWindow : Window
{
    readonly int DefaultNumTasks;

#if DEBUG
    const int StartupMaxIterations = 100;
    const int DefaultMaxIterations = 500;
#else
    const int StartupMaxIterations = 1000;
    const int DefaultMaxIterations = 10000;
#endif

    const int StartupImageHeight = 600;

    bool loaded;

    Type mandelbrotType = typeof(MandelbrotFmaDouble);

    int ImageWidth => ImageUserControl.GetWidthFromHeight(ImageHeight);
    int ImageHeight { get => field > 0 ? field : ImageUserControl.GetImageHeightFromImageCanvas(); set; } = StartupImageHeight;

    readonly List<MandelbrotRegion> mandelbrotRegions = [MandelbrotRegionFactory.DefaultRegion];

    string colorPaletteName = string.Empty;
    int colorPaletteSize;
    double colorPaletteOffset;

    GrowingArray<UInt32> colorPalette = new();

    readonly MandelbrotGenerator mandelbrotGenerator = new();

    readonly DispatcherTimer resizeTimer = new();

    readonly Lock mandelbrotResultAndIterationsLock = new();

    TwoDimensionalGrowingArray<int> mandelbrotIterationsArray = new(widthAlignment: 4, heightAlignment: 2);
    MandelbrotIterationsInfo mandelbrotIterationsInfo;
    MandelbrotResult mandelbrotResult = new();

    TwoDimensionalGrowingArray<UInt32> mandelbrotPixelsBgr32Array = new(widthAlignment: 4, heightAlignment: 2);

    void InitializeMenu()
    {
        // Implementation menu...

        MenuItemImplementation.CheckedItemChanged += MenuItemImplementation_CheckedItemChanged;

        foreach (var mandelbrotName in MandelbrotFactory.MandelbrotNames)
        {
            MenuItemImplementation.AddItem(new MenuItemWithCustomProperty<Type> { Header = mandelbrotName, CustomProperty = MandelbrotFactory.GetMandelbrotType(mandelbrotName), IsEnabled = MandelbrotFactory.IsSupported(mandelbrotName) });
        }

        MenuItemImplementation.CheckFirstEnabledItem();

        // Image Size menu...

        MenuItemImageSize.CheckedItemChanged += MenuItemImageSize_CheckedItemChanged;

        MenuItemImageSize.AddItem(new MenuItemWithCustomProperty<int> { Header = "Actual size", CustomProperty = -1 });
        MenuItemImageSize.AddItem(new MenuItemWithCustomProperty<int> { Header = "1800 x 1200", CustomProperty = 1200 });
        MenuItemImageSize.AddItem(new MenuItemWithCustomProperty<int> { Header = "1620 x 1080", CustomProperty = 1080 });
        MenuItemImageSize.AddItem(new MenuItemWithCustomProperty<int> { Header = "1440 x 960", CustomProperty = 960 });
        MenuItemImageSize.AddItem(new MenuItemWithCustomProperty<int> { Header = "900 x 600", CustomProperty = 600 });

        MenuItemImageSize.CheckFirstEnabledItem();

        // Region menu...

        foreach (var regionName in MandelbrotRegionFactory.RegionNames)
        {
            MenuItem menuItem = new() { Header = regionName };
            menuItem.Click += MenuItemRegion_Click;
            MenuItemRegion.Items.Add(menuItem);
        }

        // Palette menu...

        MenuItemPalette.CheckedItemChanged += MenuItemPalette_CheckedItemChanged;

        foreach (var colorPaletteName in ColorPaletteFactory.ColorPaletteNames)
        {
            MenuItemPalette.AddItem(new MenuItemWithCustomProperty<string> { Header = colorPaletteName, CustomProperty = colorPaletteName });
        }

        MenuItemPalette.CheckFirstEnabledItem();

        // Palette Size menu...

        MenuItemPaletteSize.CheckedItemChanged += MenuItemPaletteSize_CheckedItemChanged;

        MenuItemPaletteSize.AddItem(new() { Header = "100", CustomProperty = 100 });
        MenuItemPaletteSize.AddItem(new() { Header = "250", CustomProperty = 250 });
        MenuItemPaletteSize.AddItem(new() { Header = "500", CustomProperty = 500 });
        MenuItemPaletteSize.AddItem(new() { Header = "1000", CustomProperty = 1000 });

        MenuItemPaletteSize.CheckFirstEnabledItem();

        // Palette Offset menu...

        MenuItemPaletteOffset.CheckedItemChanged += MenuItemPaletteOffset_CheckedItemChanged;

        for (int i = 0; i < 10; i++)
        {
            MenuItemWithCustomProperty<double> menuItem = new() { Header = $"{i * 10}%", CustomProperty = 0.1 * i };
            menuItem.InputGestureText = $"Ctrl+{i}";
            var hotkey = new KeyGesture(Key.D0 + i, ModifierKeys.Control);
            var command = new RoutedCommand();
            CommandBindings.Add(new CommandBinding(command, (sender, e) => menuItem.IsChecked = true));
            InputBindings.Add(new InputBinding(command, hotkey));
            MenuItemPaletteOffset.AddItem(menuItem);
        }

        MenuItemPaletteOffset.CheckFirstEnabledItem();
    }

    public MainWindow()
    {
        InitializeComponent();

        InitializeMenu();

        mandelbrotGenerator.CalculationCompleted += OnCalculationCompleted;

        DefaultNumTasks = Math.Max(1, Environment.ProcessorCount - 1);

        TextBoxNumTasks.Text = DefaultNumTasks.ToString(CultureInfo.InvariantCulture);
        TextBoxMaxIterations.Text = DefaultMaxIterations.ToString(CultureInfo.InvariantCulture);

        mandelbrotGenerator.StartCalculation(typeof(MandelbrotFmaFloat), ImageUserControl.GetWidthFromHeight(StartupImageHeight), StartupImageHeight, MandelbrotRegionFactory.DefaultRegion, StartupMaxIterations, DefaultNumTasks);

        resizeTimer.Interval = TimeSpan.FromMilliseconds(200);
        resizeTimer.Tick += ResizeTimer_Tick;

        ImageUserControl.ZoomOut += ImageUserControl_ZoomOut;
        ImageUserControl.ZoomIn += ImageUserControl_ZoomIn;
    }

    MandelbrotRegion GetMandelbrotRegion() => mandelbrotRegions.Count > 0 ? mandelbrotRegions.Last() : MandelbrotRegionFactory.DefaultRegion;

    int GetNumTasks()
    {
        if (!Int32.TryParse(TextBoxNumTasks.Text.Trim(), out int numTasks))
        {
            numTasks = DefaultNumTasks;
        }

        return Math.Max(1, numTasks);
    }

    int GetMaxIterations()
    {
        if (!Int32.TryParse(TextBoxMaxIterations.Text.Trim(), out int maxIterations))
        {
            maxIterations = DefaultMaxIterations;
        }

        return (Math.Max(1, maxIterations) + 1) & ~1;
    }

    void Calculate(MandelbrotRegion? region = null, int? maxIterations = null)
    {
        region ??= GetMandelbrotRegion();
        int numTasks = GetNumTasks();
        int maxIterationsValue = maxIterations ?? GetMaxIterations();

        mandelbrotGenerator.StartCalculation(mandelbrotType, ImageWidth, ImageHeight, region.Value, maxIterationsValue, numTasks);
    }

    void CalculateIfLoaded()
    {
        if (loaded)
        {
            Calculate();
        }
    }

    void OnCalculationCompleted(MandelbrotBase mandelbrot, MandelbrotResult result)
    {
        StoreMandelbrotCalculation(mandelbrot, result);
        Dispatcher.Invoke(() => SetImageAndResults());
    }

    void StoreMandelbrotCalculation(MandelbrotBase mandelbrot, MandelbrotResult result)
    {
        lock (mandelbrotResultAndIterationsLock)
        {
            mandelbrotIterationsArray.CopyFrom(mandelbrot.IterationsArray);
            mandelbrotIterationsInfo = MandelbrotIterationsInfo.Calculate(mandelbrot.IterationsArray);
            mandelbrotResult = result;
        }
    }

    void SetImageAndResults()
    {
        MandelbrotIterationsInfo iterationsInfo;
        MandelbrotResult result;

        lock (mandelbrotResultAndIterationsLock)
        {
            result = mandelbrotResult;
            iterationsInfo = this.mandelbrotIterationsInfo;

            if (result.Width <= 0 || result.Height <= 0)
            {
                return; // ### RETURN ###
            }

            Colorizer.Colorize(in mandelbrotIterationsArray, result.MaxIterations, colorPalette.AsReadOnlySpan(), colorPaletteOffset, ref mandelbrotPixelsBgr32Array);
        }

        BitmapSource bitmapSource = BitmapSource.Create(mandelbrotPixelsBgr32Array.Width, mandelbrotPixelsBgr32Array.Height, 0, 0, PixelFormats.Bgr32, null, mandelbrotPixelsBgr32Array.GetArray(), sizeof(Int32) * mandelbrotPixelsBgr32Array.RowSize);

        ImageUserControl.ImageSource =bitmapSource;
        InfoUserControl.SetResults(iterationsInfo, result);
    }

    void Window_Loaded(object sender, RoutedEventArgs e)
    {
        loaded = true;
        SizeChanged += MainWindow_SizeChanged;
    }

    void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        resizeTimer.Stop();
        resizeTimer.Start();
    }

    void ResizeTimer_Tick(object? sender, EventArgs e)
    {
        resizeTimer.Stop();
        Calculate();
    }

    void ImageUserControl_ZoomOut()
    {
        if (mandelbrotRegions.Count > 1)
        {
            mandelbrotRegions.RemoveAt(mandelbrotRegions.Count - 1);

            Calculate();
        }
        else if (mandelbrotRegions.Count == 1)
        {
            MandelbrotRegionCenterAndZoomFactor currentRegion = mandelbrotRegions.Last().GetCenterAndZoomFactor();

            if (currentRegion.ZoomFactor > 1.0)
            {
                mandelbrotRegions.Clear();
                double zoomFactor = Math.Max(1.0, 0.5 * currentRegion.ZoomFactor);
                MandelbrotRegion region = zoomFactor == 1.0 ? MandelbrotRegionFactory.DefaultRegion : new MandelbrotRegion(currentRegion.X, currentRegion.Y, zoomFactor);
                mandelbrotRegions.Add(region);

                Calculate();
            }
        }
    }

    void ImageUserControl_ZoomIn(double x0Percent, double x1Percent, double y0Percent, double y1Percet)
    {
        MandelbrotRegion mandelbrotRegion = mandelbrotRegions.Last();

        var x0 = mandelbrotRegion.X0 + (mandelbrotRegion.X1 - mandelbrotRegion.X0) * x0Percent;
        var x1 = mandelbrotRegion.X0 + (mandelbrotRegion.X1 - mandelbrotRegion.X0) * x1Percent;
        var y0 = mandelbrotRegion.Y0 + (mandelbrotRegion.Y1 - mandelbrotRegion.Y0) * y0Percent;
        var y1 = mandelbrotRegion.Y0 + (mandelbrotRegion.Y1 - mandelbrotRegion.Y0) * y1Percet;

        mandelbrotRegions.Add(new MandelbrotRegion { X0 = x0, X1 = x1, Y0 = y0, Y1 = y1 });

        Calculate();
    }

    void ButtonCalculate_Click(object sender, RoutedEventArgs e)
    {
        Calculate();
    }

    void ButtonBenchmark_Click(object sender, RoutedEventArgs e)
    {
        int numTasks = GetNumTasks();
        int maxIterations = GetMaxIterations();

        if (mandelbrotRegions.Last() != MandelbrotRegionFactory.BlackRegion)
        {
            mandelbrotRegions.Add(MandelbrotRegionFactory.BlackRegion);
        }

        mandelbrotGenerator.Calculate(mandelbrotType, ImageWidth, ImageHeight, MandelbrotRegionFactory.BlackRegion, maxIterations, numTasks, benchmark: true);

        StoreMandelbrotCalculation(mandelbrotGenerator.Mandelbrot, mandelbrotGenerator.Result);
        SetImageAndResults();
    }

    void MenuItemImplementation_CheckedItemChanged(Type mandelbrotType)
    {
        this.mandelbrotType = mandelbrotType;

        CalculateIfLoaded();
    }

    void MenuItemImageSize_CheckedItemChanged(int imageHeight)
    {
        ImageHeight = imageHeight;

        CalculateIfLoaded();
    }

    void MenuItemRegion_Click(object sender, RoutedEventArgs e)
    {
        MandelbrotRegionFactory.GetMandelbrotRegion((sender as MenuItem)?.Header as string, out MandelbrotRegion region);

        mandelbrotRegions.Clear();
        mandelbrotRegions.Add(region);

        Calculate();
    }

    void MenuItemPalette_CheckedItemChanged(string colorPaletteName)
    {
        this.colorPaletteName = colorPaletteName;
        ColorPaletteFactory.CreatePalette(colorPaletteName, colorPaletteSize, ref colorPalette);
        SetImageAndResults();
    }

    void MenuItemPaletteSize_CheckedItemChanged(int size)
    {
        colorPaletteSize = size;
        ColorPaletteFactory.CreatePalette(colorPaletteName, size, ref colorPalette);
        SetImageAndResults();
    }

    void MenuItemPaletteOffset_CheckedItemChanged(double offset)
    {
        colorPaletteOffset = offset;
        ColorPaletteFactory.CreatePalette(colorPaletteName, colorPaletteSize, ref colorPalette);
        SetImageAndResults();
    }

    void MenuItemEditCopyImage_Click(object sender, RoutedEventArgs e)
    {
        BitmapSource? bitmapSource = ImageUserControl.ImageSource;
        if (bitmapSource != null)
        {
            Clipboard.SetImage(bitmapSource);
        }
    }

    static readonly JsonSerializerOptions RegionJsonSerializerOptions = new() { WriteIndented = true };

    void MenuItemEditCopyRegion_Click(object sender, RoutedEventArgs e)
    {
        MandelbrotRegionCenterAndZoomFactor region = mandelbrotResult.Region.GetCenterAndZoomFactor();
        string regionJson = JsonSerializer.Serialize(region, RegionJsonSerializerOptions);
        Clipboard.SetText(regionJson);
    }

    void MenuItemHelp_Click(object sender, RoutedEventArgs e)
    {
        new HelpRichTextMessageBox().ShowDialog();
    }
}
