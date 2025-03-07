using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Mandelbrot;

public partial class ImageUserControl : UserControl
{
    public event Action? ZoomOut;
    public event Action<double, double, double, double>? ZoomIn;

    MouseButton? mouseMoveButton;
    Point mouseMoveStart;

    public ImageUserControl()
    {
        InitializeComponent();
    }

    const double AspectRatio = 3.0 / 2; // Must match GetWidthFromHeight
    const double InvAspectRatio = 1 / AspectRatio; // Must match GetHeightFromWidth

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetWidthFromHeight(int height) => (height / 2) * 3; // Must match AspectRatio

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetHeightFromWidth(int width) => (width / 3) * 2; // Must match InvAspectRatio

    public BitmapSource? ImageSource
    {
        get => Image.Source as BitmapSource;
        set => Image.Source = value;
    }

    public int GetImageHeightFromImageCanvas()
    {
        double actualWidth = ImageCanvas.ActualWidth;
        double actualHeight = ImageCanvas.ActualHeight;
        PresentationSource presentationSource = PresentationSource.FromVisual(this);
        if (presentationSource != null)
        {
            Matrix transformToDevice = presentationSource.CompositionTarget.TransformToDevice;
            actualWidth = actualWidth * transformToDevice.M11;
            actualHeight = actualHeight * transformToDevice.M22;
        }
        int actualHeightInt = (int)actualHeight;
        int actualWidthInt = (int)actualWidth;
        int widthFromActualHeight = GetWidthFromHeight(actualHeightInt);

        return widthFromActualHeight <= actualWidth ? actualHeightInt : GetHeightFromWidth(actualWidthInt);
    }

    void ImageCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (mouseMoveButton == null)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (!ImageCanvas.CaptureMouse())
                {
                    return;
                }

                mouseMoveButton = MouseButton.Left;
                mouseMoveStart = Mouse.GetPosition(ImageCanvas);

                ImageRectangle.Width = 0;
                ImageRectangle.Height = 0;

                Canvas.SetLeft(ImageRectangle, mouseMoveStart.X);
                Canvas.SetTop(ImageRectangle, mouseMoveStart.Y);

                ImageRectangle.Visibility = Visibility.Visible;
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                if (!ImageCanvas.CaptureMouse())
                {
                    return;
                }

                mouseMoveButton = MouseButton.Right;
            }
        }
        else
        {
            ImageCanvas.ReleaseMouseCapture();
            ImageRectangle.Visibility = Visibility.Hidden;

            mouseMoveButton = null;
        }
    }

    void ImageCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (mouseMoveButton == null)
        {
            return;
        }

        if (mouseMoveButton == MouseButton.Left)
        {
            Point mousePosition = e.GetPosition(ImageCanvas);

            ImageRectangle.Width = Math.Abs(mouseMoveStart.X - mousePosition.X);
            ImageRectangle.Height = Math.Abs(mouseMoveStart.Y - mousePosition.Y);

            Canvas.SetLeft(ImageRectangle, Math.Min(mouseMoveStart.X, mousePosition.X));
            Canvas.SetTop(ImageRectangle, Math.Min(mouseMoveStart.Y, mousePosition.Y));
        }

        e.Handled = true;
    }

    void ImageCanvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (mouseMoveButton == null)
        {
            return;
        }

        ImageCanvas.ReleaseMouseCapture();
        ImageRectangle.Visibility = Visibility.Hidden;

        if (mouseMoveButton == MouseButton.Left)
        {
            var imageMoveStart = ImageCanvas.TransformToDescendant(Image).Transform(mouseMoveStart);
            var imageMoveEnd = e.GetPosition(Image);

            double centerX = 0.5 * (imageMoveEnd.X + imageMoveStart.X);
            double centerY = 0.5 * (imageMoveEnd.Y + imageMoveStart.Y);

            double width = Math.Abs(imageMoveEnd.X - imageMoveStart.X);
            double height = Math.Abs(imageMoveEnd.Y - imageMoveStart.Y);

            if (width > height * AspectRatio)
            {
                height = width * InvAspectRatio;
            }
            else
            {
                width = height * AspectRatio;
            }

            if (width < 4 || Image.ActualWidth <= 0 || Image.ActualHeight <= 0)
            {
                return;
            }

            ZoomIn?.Invoke((centerX - 0.5 * width) / Image.ActualWidth, (centerX + 0.5 * width) / Image.ActualWidth, (centerY - 0.5 * height) / Image.ActualHeight, (centerY + 0.5 * height) / Image.ActualHeight);
        }
        else if (mouseMoveButton == MouseButton.Right)
        {
            ZoomOut?.Invoke();
        }

        mouseMoveButton = null;

        e.Handled = true;
    }
}
