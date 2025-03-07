using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;

namespace Mandelbrot;

internal class MenuItemOneChecked<TProperty> : MenuItem 
{
    readonly List<MenuItemWithCustomProperty<TProperty>> menuItems = [ ];

    public event Action<TProperty>? CheckedItemChanged;

    private new ItemCollection Items => base.Items;

    public void AddItem(MenuItemWithCustomProperty<TProperty> menuItem)
    {
        menuItem.Checked += MenuItem_Checked;
        menuItem.IsCheckable = true;
        Items.Add(menuItem);
        menuItems.Add(menuItem);
    }

    public IReadOnlyCollection<MenuItemWithCustomProperty<TProperty>> GetMenuItems() => menuItems.AsReadOnly();

    void MenuItem_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItemWithCustomProperty<TProperty> menuItemChecked)
        {
            return;
        }

        foreach (var item in Items)
        {
            if (item != menuItemChecked && item is MenuItem menuItem)
            {
                menuItem.IsChecked = false;
            }
        }

        CheckedItemChanged?.Invoke(menuItemChecked.CustomProperty);
    }

    void UncheckAllItems()
    {
        foreach (var menuItem in menuItems)
        {
            menuItem.IsChecked = false;
        }
    }

    public void CheckFirstEnabledItem()
    {
        UncheckAllItems();

        MenuItemWithCustomProperty<TProperty>? firstEnabledMenuItem = menuItems.FirstOrDefault(i => i.IsEnabled);

        if (firstEnabledMenuItem != null)
        {
            firstEnabledMenuItem.IsChecked = true;
        }
    }

    public void CheckEnabledItem(TProperty propertyValue)
    {
        MenuItemWithCustomProperty<TProperty>? menuItem = menuItems.FirstOrDefault(item => item?.CustomProperty?.Equals(propertyValue) == true);

        if (menuItem != null)
        {
            UncheckAllItems();
            menuItem.IsChecked = true;
        }
        else
        {
            CheckFirstEnabledItem();
        }
    }
}

[SuppressMessage("Compiler", "CA1812", Justification = "It's used in XAML")]
[SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "It's necessary")]
internal sealed class MenuItemOneCheckedDouble : MenuItemOneChecked<double> { }

[SuppressMessage("Compiler", "CA1812", Justification = "It's used in XAML")]
[SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "It's necessary")]
internal sealed class MenuItemOneCheckedInt : MenuItemOneChecked<int> { }

[SuppressMessage("Compiler", "CA1812", Justification = "It's used in XAML")]
[SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "It's necessary")]
internal sealed class MenuItemOneCheckedString : MenuItemOneChecked<string> { }

[SuppressMessage("Compiler", "CA1812", Justification = "It's used in XAML")]
[SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "It's necessary")]
internal sealed class MenuItemOneCheckedType : MenuItemOneChecked<Type> { }
