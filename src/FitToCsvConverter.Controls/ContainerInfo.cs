namespace FitToCsvConverter.Controls;

using System.Windows;

public class ContainerInfo
{
    public int ItemIndex { get; }
    public object Item { get; }
    public UIElement Container { get; }
    public ContainerInfo(int itemIndex, object item, UIElement container)
    {
        ItemIndex = itemIndex;
        Item = item;
        Container = container;
    }
}
