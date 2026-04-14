namespace FitToCsvConverter.Controls;

using System.Windows;
using System.Windows.Controls;

internal class UniformToolBarOverflowPanel : Panel
{
    #region UniformSize

    /// <summary>
    ///     The key needed set a read-only property.
    /// </summary>
    protected static readonly DependencyProperty UniformSizeProperty =
            DependencyProperty.Register(
                    "UniformSize",
                    typeof(Size),
                    typeof(UniformToolBarOverflowPanel),
                    new FrameworkPropertyMetadata(new Size(double.NaN, double.NaN)));

    /// <summary>
    /// The uniform item size as provided by <see cref="ItemHeight"/> and <see cref="ItemWidth"/>.
    /// </summary>
    public Size UniformSize
    {
        get => (Size)GetValue(UniformSizeProperty);

        internal set => SetValue(UniformSizeProperty, value);
    }
    #endregion UniformSize

    public UniformToolBarOverflowPanel()
    {
    }

    /// <summary>
    /// Measure the content and store the desired finalDesiredPanelSize of the content
    /// </summary>
    /// <param name="availableSize"></param>
    /// <returns></returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (InternalChildren.Count == 0)
        {
            return base.MeasureOverride(availableSize);
        }

        double newPanelWidth = 0.0;
        double newPanelHeight = 0.0;
        foreach (UIElement itemContainer in InternalChildren)
        {
            itemContainer.Measure(availableSize);
            newPanelHeight += itemContainer.DesiredSize.Height;
            newPanelWidth = Math.Max(newPanelHeight, itemContainer.DesiredSize.Width);
        }

        var newPanelSize = new Size(newPanelWidth, newPanelHeight);
        return newPanelSize;
    }

    /// <summary>
    /// Vertical content arrangement.
    /// </summary>
    /// <param name="finalSize">Arrange finalDesiredPanelSize</param>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (InternalChildren.Count == 0)
        {
            return base.ArrangeOverride(finalSize);
        }

        Rect layoutSlot = new(UniformSize);
        var offset = new Vector(0, layoutSlot.Height);
        layoutSlot.Offset(-offset);

        foreach (UIElement itemContainer in InternalChildren)
        {
            layoutSlot.Offset(offset);
            itemContainer.Arrange(layoutSlot);
        }

        var finalDesiredPanelSize = new Size(layoutSlot.Right, layoutSlot.Bottom);
        return finalDesiredPanelSize;
    }
}