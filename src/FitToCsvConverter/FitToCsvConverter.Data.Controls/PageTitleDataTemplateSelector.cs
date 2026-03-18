namespace BionicCode.BionicSwipePageFrame;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

internal class PageTitleDataTemplateSelector : DataTemplateSelector
{
    public required DataTemplate TextDataTemplate { get; set; }
    public required DataTemplate ImageSourceDataTemplate { get; set; }
    public required DataTemplate ObjectDataTemplate { get; set; }

    #region Overrides of TitleDataTemplateSelector

    /// <inheritdoc />
    public override DataTemplate SelectTemplate(object item, DependencyObject container) => item switch
    {
        string _ => this.TextDataTemplate,
        ImageSource _ => this.ImageSourceDataTemplate,
        _ => this.ObjectDataTemplate,
    };

    #endregion
}
