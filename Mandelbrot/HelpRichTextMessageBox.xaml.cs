using System.Windows;

namespace Mandelbrot;

public partial class HelpRichTextMessageBox : Window
{
    public HelpRichTextMessageBox()
    {
        InitializeComponent();
    }

    void ButtonOk_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
