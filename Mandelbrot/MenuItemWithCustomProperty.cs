using System.Windows.Controls;

namespace Mandelbrot;

internal sealed class MenuItemWithCustomProperty<TProperty> : MenuItem 
{
    public required TProperty CustomProperty { get; init; }
}
