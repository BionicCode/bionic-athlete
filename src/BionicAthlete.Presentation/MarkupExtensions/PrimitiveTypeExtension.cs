namespace BionicAthlete.Presentation;

using System.Windows.Markup;

public class PrimitiveTypeExtension : MarkupExtension
{
    private object? _primitiveValue;
    public bool Boolean { get; set => _primitiveValue = value; }
    public int Int32 { get; set => _primitiveValue = value; }
    public double Double { get; set => _primitiveValue = value; }
    public string String { get; set => _primitiveValue = value; }
    public PrimitiveTypeExtension() => _primitiveValue = null;

    #region constructors

    public PrimitiveTypeExtension(bool booleanValue) => _primitiveValue = booleanValue;

    public PrimitiveTypeExtension(double doubleValue) => _primitiveValue = doubleValue;

    public PrimitiveTypeExtension(string stringValue) => _primitiveValue = stringValue;

    public PrimitiveTypeExtension(int int32Value) => _primitiveValue = int32Value;

    #endregion

    #region Overrides of MarkupExtension

    public override object ProvideValue(IServiceProvider serviceProvider) => _primitiveValue;

    #endregion
}
