namespace FitToCsvConverter.Controls;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using global::System.Diagnostics;
using global::System.Windows.Data;

public class UniformToolBarPanel : Panel
{
    /// <summary>
    ///     Instantiates a new instance of this class.
    /// </summary>
    public UniformToolBarPanel() : base()
    {
        _plan = new LayoutPlan();
    }

    #region Layout

    internal double MinLength
    {
        get;
        private set;
    }

    internal double MaxLength
    {
        get;
        private set;
    }

    private bool MeasureGeneratedItems(bool asNeededPass, Size constraint, bool horizontal, double maxExtent, ref Size panelDesiredSize, out double overflowExtent)
    {
        ToolBarOverflowPanel overflowPanel = ToolBarOverflowPanel;
        bool sendToOverflow = false; // Becomes true when the first AsNeeded does not fit
        bool hasOverflowItems = false;
        bool overflowNeedsInvalidation = false;
        overflowExtent = 0.0;
        UIElementCollection children = InternalChildren;
        int childrenCount = children.Count;
        int childrenIndex = 0;

        for (int i = 0; i < _generatedItemsCollection.Count; i++)
        {
            UIElement child = _generatedItemsCollection[i];
            OverflowMode overflowMode = System.Windows.Controls.ToolBar.GetOverflowMode(child);
            bool asNeededMode = overflowMode == OverflowMode.AsNeeded;

            // MeasureGeneratedItems is called twice to do a complete measure.
            // The first pass measures Always and Never items -- items that
            // are guaranteed to be or not to be in the overflow menu.
            // The second pass measures AsNeeded items and determines whether
            // there is enough room for them in the main bar or if they should
            // be placed in the overflow menu.
            // Check here whether the overflow mode matches a mode we should be
            // examining in this pass.
            if (asNeededMode == asNeededPass)
            {
                DependencyObject visualParent = VisualTreeHelper.GetParent(child);

                // In non-Always overflow modes, measure for main bar placement.
                if ((overflowMode != OverflowMode.Always) && !sendToOverflow)
                {
                    // Children may change their size depending on whether they are in the overflow
                    // menu or not. Ensure that when we measure, we are using the main bar size.
                    // If the item goes to overflow, this property will be updated later in this loop
                    // when it is removed from the visual tree.
                    UniformToolBar.SetIsOverflowItem(child, BooleanBoxes.FalseBox);
                    child.Measure(constraint);
                    Size childDesiredSize = child.DesiredSize;

                    // If the child is an AsNeeded, check if it fits. If it doesn't then
                    // this child and all subsequent AsNeeded children should be sent
                    // to the overflow menu.
                    if (asNeededMode)
                    {
                        double newExtent;
                        if (horizontal)
                        {
                            newExtent = childDesiredSize.Width + panelDesiredSize.Width;
                        }
                        else
                        {
                            newExtent = childDesiredSize.Height + panelDesiredSize.Height;
                        }

                        if (DoubleUtil.GreaterThan(newExtent, maxExtent))
                        {
                            // It doesn't fit, send to overflow
                            sendToOverflow = true;
                        }
                    }

                    // The child has been validated as belonging in the main bar.
                    // Update the panel desired size dimensions, and ensure the child
                    // is in the main bar's visual tree.
                    if (!sendToOverflow)
                    {
                        if (horizontal)
                        {
                            panelDesiredSize.Width += childDesiredSize.Width;
                            panelDesiredSize.Height = Math.Max(panelDesiredSize.Height, childDesiredSize.Height);
                        }
                        else
                        {
                            panelDesiredSize.Width = Math.Max(panelDesiredSize.Width, childDesiredSize.Width);
                            panelDesiredSize.Height += childDesiredSize.Height;
                        }

                        if (visualParent != this)
                        {
                            if ((visualParent == overflowPanel) && (overflowPanel != null))
                            {
                                overflowPanel.Children.Remove(child);
                            }

                            if (childrenIndex < childrenCount)
                            {
                                children.Insert(childrenIndex, child);
                            }
                            else
                            {
                                _ = children.Add(child);
                            }

                            childrenCount++;
                        }

                        Debug.Assert(children[childrenIndex] == child, "InternalChildren is out of sync with _generatedItemsCollection.");
                        childrenIndex++;
                    }
                }

                // The child should go to the overflow menu
                if ((overflowMode == OverflowMode.Always) || sendToOverflow)
                {
                    hasOverflowItems = true;

                    // If a child is in the overflow menu, we don't want to keep measuring.
                    // However, we need to calculate the MaxLength as well as set the desired height
                    // correctly. Thus, we will use the DesiredSize of the child. There is a problem
                    // that can occur if the child changes size while in the overflow menu and
                    // was recently displayed. It will be measure clean, yet its DesiredSize
                    // will not be accurate for the MaxLength calculation.
                    if (child.IsMeasureValid)
                    {
                        // Set this temporarily in case the size is different while in the overflow area
                        UniformToolBar.SetIsOverflowItem(child, BooleanBoxes.FalseBox);
                        child.Measure(constraint);
                    }

                    // Even when in the overflow, we need two pieces of information:
                    // 1. We need to continue to track the maximum size of the non-logical direction
                    //    (i.e. height in horizontal bars). This way, ToolBars with everything in
                    //    the overflow will still have some height.
                    // 2. We want to track how much of the space we saved by placing the child in
                    //    the overflow menu. This is used to calculate MinLength and MaxLength.
                    Size childDesiredSize = child.DesiredSize;
                    if (horizontal)
                    {
                        overflowExtent += childDesiredSize.Width;
                        panelDesiredSize.Height = Math.Max(panelDesiredSize.Height, childDesiredSize.Height);
                    }
                    else
                    {
                        overflowExtent += childDesiredSize.Height;
                        panelDesiredSize.Width = Math.Max(panelDesiredSize.Width, childDesiredSize.Width);
                    }

                    // Set the flag to indicate that the child is in the overflow menu
                    UniformToolBar.SetIsOverflowItem(child, BooleanBoxes.TrueBox);

                    // If the child is in this panel's visual tree, remove it.
                    if (visualParent == this)
                    {
                        Debug.Assert(children[childrenIndex] == child, "InternalChildren is out of sync with _generatedItemsCollection.");
                        children.Remove(child);
                        childrenCount--;
                        overflowNeedsInvalidation = true;
                    }
                    // If the child isnt connected to the visual tree, notify the overflow panel to pick it up.
                    else if (visualParent == null)
                    {
                        overflowNeedsInvalidation = true;
                    }
                }
            }
            else
            {
                // We are not measure this child in this pass. Update the index into the
                // visual children collection.
                if ((childrenIndex < childrenCount) && (children[childrenIndex] == child))
                {
                    childrenIndex++;
                }
            }
        }

        // A child was added to the overflow panel, but since we don't add it
        // to the overflow panel's visual collection until that panel's measure
        // pass, we need to mark it as measure dirty.
        if (overflowNeedsInvalidation && (overflowPanel != null))
        {
            overflowPanel.InvalidateMeasure();
        }

        return hasOverflowItems;
    }

    /// <summary>
    /// Measure the content and store the desired size of the content
    /// </summary>
    /// <param name="constraint"></param>
    /// <returns></returns>
    protected override Size MeasureOverride(Size constraint)
    {
        var stackDesiredSize = new Size();

        if (IsItemsHost)
        {
            Size layoutSlotSize = constraint;
            double maxExtent;
            bool horizontal = Orientation == Orientation.Horizontal;

            if (horizontal)
            {
                layoutSlotSize.Width = double.PositiveInfinity;
                maxExtent = constraint.Width;
            }
            else
            {
                layoutSlotSize.Height = double.PositiveInfinity;
                maxExtent = constraint.Height;
            }

            // This first call will measure all of the non-AsNeeded elements (i.e. we know
            // whether they're going into the overflow or not.
            // overflowExtent will be the size of the Always elements, which is not actually
            // needed for subsequent calculations.
            bool hasAlwaysOverflowItems = MeasureGeneratedItems(/* asNeeded = */ false, layoutSlotSize, horizontal, maxExtent, ref stackDesiredSize, out _);

            // At this point, the desired size is the minimum size of the ToolBar.
            MinLength = horizontal ? stackDesiredSize.Width : stackDesiredSize.Height;

            // This second call will measure all of the AsNeeded elements and place
            // them in the appropriate location.
            bool hasAsNeededOverflowItems = MeasureGeneratedItems(/* asNeeded = */ true, layoutSlotSize, horizontal, maxExtent, ref stackDesiredSize, out double overflowExtent);

            // At this point, the desired size is complete. The desired size plus overflowExtent
            // is the maximum size of the ToolBar.
            MaxLength = (horizontal ? stackDesiredSize.Width : stackDesiredSize.Height) + overflowExtent;

            UniformToolBar? toolbar = ToolBar;
            toolbar?.SetValue(UniformToolBar.HasOverflowItemsPropertyKey, hasAlwaysOverflowItems || hasAsNeededOverflowItems);
        }
        else
        {
            stackDesiredSize = base.MeasureOverride(constraint);
        }

        return stackDesiredSize;
    }

    /// <summary>
    /// Content arrangement.
    /// </summary>
    /// <param name="arrangeSize">Arrange size</param>
    protected override Size ArrangeOverride(Size arrangeSize)
    {
        UIElementCollection children = InternalChildren;
        bool fHorizontal = Orientation == Orientation.Horizontal;
        var rcChild = new Rect(arrangeSize);
        double previousChildSize = 0.0d;

        //
        // Arrange and Position Children.
        //
        for (int i = 0, count = children.Count; i < count; ++i)
        {
            var child = (UIElement)children[i];

            if (fHorizontal)
            {
                rcChild.X += previousChildSize;
                previousChildSize = child.DesiredSize.Width;
                rcChild.Width = previousChildSize;
                rcChild.Height = Math.Max(arrangeSize.Height, child.DesiredSize.Height);
            }
            else
            {
                rcChild.Y += previousChildSize;
                previousChildSize = child.DesiredSize.Height;
                rcChild.Height = previousChildSize;
                rcChild.Width = Math.Max(arrangeSize.Width, child.DesiredSize.Width);
            }

            child.Arrange(rcChild);
        }

        return arrangeSize;
    }

    /// <summary>
    /// ToolBarPanel sets bindings a on its Orientation property to its TemplatedParent if the
    /// property is not already set.
    /// </summary>
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        /*Truth table for BaseValueSource
           ┌───────────────────┬───────────────┬───────────────────────┬──────────────────────────┐
           │ BaseValueSource   │ Has modifiers │ HasDefaultValue(...)  │ HasNonDefaultValue(...)  │
           ├───────────────────┼───────────────┼───────────────────────┼──────────────────────────┤
           │ Default           │ No            │ true                  │ false                    │ <=== Only in this case we want to set the binding, 
           │ Default           │ Yes           │ false                 │ true                     │      otherwise we might be overwriting a user set or inherited value
           │ Local             │ No            │ false                 │ true                     │
           │ Local             │ Yes           │ false                 │ true                     │
           │ Style             │ No            │ false                 │ true                     │
           │ Style             │ Yes           │ false                 │ true                     │
           │ Inherited         │ No            │ false                 │ true                     │
           │ Inherited         │ Yes           │ false                 │ true                     │
           └───────────────────┴───────────────┴───────────────────────┴──────────────────────────┘

        Since we don't want to override any expressions like Binding etc. (original ToolBarPanel overrides user expression) 
        we must add an extra check using ReadLocalValue to determine if the value is actually coming from a default value or not. 
        */
        ValueSource source = DependencyPropertyHelper.GetValueSource(this, OrientationProperty);
        bool isDefaultSource = source.BaseValueSource is BaseValueSource.Default;
        bool isOrientationDefaultValue = isDefaultSource
            && ReadLocalValue(OrientationProperty) == DependencyProperty.UnsetValue;
        
        if (TemplatedParent is ToolBar && isOrientationDefaultValue)
        {
            var binding = new Binding
            {
                RelativeSource = RelativeSource.TemplatedParent,
                Path = new PropertyPath(System.Windows.Controls.ToolBar.OrientationProperty)
            };
            _ = SetBinding(OrientationProperty, binding);
        }
    }

    #endregion

    #region Item Generation

    //internal override void GenerateChildren()
    //{
    //    base.GenerateChildren();

    //    // This could re-enter InternalChildren, but after base.GenerateChildren, the collection
    //    // should be fully instantiated and ready to go.
    //    UIElementCollection children = InternalChildren;
    //    if (_generatedItemsCollection == null)
    //    {
    //        _generatedItemsCollection = [with(children.Count)];
    //    }
    //    else
    //    {
    //        _generatedItemsCollection.Clear();
    //    }

    //    ToolBarOverflowPanel overflowPanel = ToolBarOverflowPanel;
    //    if (overflowPanel != null)
    //    {
    //        overflowPanel.Children.Clear();
    //        overflowPanel.InvalidateMeasure();
    //    }

    //    int childrenCount = children.Count;
    //    for (int i = 0; i < childrenCount; i++)
    //    {
    //        UIElement child = children[i];

    //        // Reset the overflow decision. This will be re-evaluated on the next measure
    //        // by ToolBarPanel.MeasureOverride.
    //        ToolBar.SetIsOverflowItem(child, BooleanBoxes.FalseBox);

    //        _generatedItemsCollection.Add(child);
    //    }
    //}

    //// This method returns a bool to indicate if or not the panel layout is affected by this collection change
    //internal override bool OnItemsChangedInternal(object sender, ItemsChangedEventArgs args)
    //{
    //    switch (args.Action)
    //    {
    //        case NotifyCollectionChangedAction.Add:
    //            AddChildren(args.Position, args.ItemCount);
    //            break;
    //        case NotifyCollectionChangedAction.Remove:
    //            RemoveChildren(args.Position, args.ItemUICount);
    //            break;
    //        case NotifyCollectionChangedAction.Replace:
    //            ReplaceChildren(args.Position, args.ItemCount, args.ItemUICount);
    //            break;
    //        case NotifyCollectionChangedAction.Move:
    //            MoveChildren(args.OldPosition, args.Position, args.ItemUICount);
    //            break;

    //        case NotifyCollectionChangedAction.Reset:
    //            base.OnItemsChangedInternal(sender, args);
    //            break;
    //    }

    //    return true;
    //}

    //private void AddChildren(GeneratorPosition pos, int itemCount)
    //{
    //    var generator = (IItemContainerGenerator)Generator;
    //    using (generator.StartAt(pos, GeneratorDirection.Forward))
    //    {
    //        for (int i = 0; i < itemCount; i++)
    //        {
    //            if (generator.GenerateNext() is UIElement e)
    //            {
    //                _generatedItemsCollection.Insert(pos.Index + 1 + i, e);
    //                generator.PrepareItemContainer(e);
    //            }
    //            else
    //            {
    //                var icg = Generator as ItemContainerGenerator;
    //                icg?.Verify();
    //            }
    //        }
    //    }
    //}

    //private void RemoveChild(UIElement child)
    //{
    //    DependencyObject visualParent = VisualTreeHelper.GetParent(child);

    //    if (visualParent == this)
    //    {
    //        InternalChildren.RemoveInternal(child);
    //    }
    //    else
    //    {
    //        ToolBarOverflowPanel overflowPanel = ToolBarOverflowPanel;
    //        if ((visualParent == overflowPanel) && (overflowPanel != null))
    //        {
    //            overflowPanel.Children.Remove(child);
    //        }
    //    }
    //}

    //private void RemoveChildren(GeneratorPosition pos, int containerCount)
    //{
    //    for (int i = 0; i < containerCount; i++)
    //    {
    //        RemoveChild(_generatedItemsCollection[pos.Index + i]);
    //    }

    //    _generatedItemsCollection.RemoveRange(pos.Index, containerCount);
    //}

    //private void ReplaceChildren(GeneratorPosition pos, int itemCount, int containerCount)
    //{
    //    Debug.Assert(itemCount == containerCount, "ToolBarPanel expects Replace to affect only realized containers");

    //    var generator = (IItemContainerGenerator)Generator;
    //    using (generator.StartAt(pos, GeneratorDirection.Forward, true))
    //    {
    //        for (int i = 0; i < itemCount; i++)
    //        {
    //            var e = generator.GenerateNext(out bool isNewlyRealized) as UIElement;

    //            Debug.Assert(e != null && !isNewlyRealized, "ToolBarPanel expects Replace to affect only realized containers");
    //            if (e != null && !isNewlyRealized)
    //            {
    //                RemoveChild(_generatedItemsCollection[pos.Index + i]);
    //                _generatedItemsCollection[pos.Index + i] = e;
    //                generator.PrepareItemContainer(e);
    //            }
    //            else
    //            {
    //                var icg = Generator as ItemContainerGenerator;
    //                icg?.Verify();
    //            }
    //        }
    //    }
    //}

    //private void MoveChildren(GeneratorPosition fromPos, GeneratorPosition toPos, int containerCount)
    //{
    //    if (fromPos == toPos)
    //    {
    //        return;
    //    }

    //    var generator = (IItemContainerGenerator)Generator;
    //    int toIndex = generator.IndexFromGeneratorPosition(toPos);

    //    var elements = new UIElement[containerCount];

    //    for (int i = 0; i < containerCount; i++)
    //    {
    //        UIElement child = _generatedItemsCollection[fromPos.Index + i];
    //        RemoveChild(child);
    //        elements[i] = child;
    //    }

    //    _generatedItemsCollection.RemoveRange(fromPos.Index, containerCount);

    //    for (int i = 0; i < containerCount; i++)
    //    {
    //        _generatedItemsCollection.Insert(toIndex + i, elements[i]);
    //    }
    //}

    #endregion

    #region Helpers

    private UniformToolBar? ToolBar => TemplatedParent as UniformToolBar;

    private ToolBarOverflowPanel? ToolBarOverflowPanel => ToolBar?.ToolBarOverflowPanel;

    // Accessed by ToolBarOverflowPanel
    //internal List<UIElement> GeneratedItemsCollection => _generatedItemsCollection;

    #endregion

    #region Data

    //private List<UIElement> _generatedItemsCollection;

    #endregion
}
