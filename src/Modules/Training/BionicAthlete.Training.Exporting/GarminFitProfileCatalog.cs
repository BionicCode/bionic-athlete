namespace BionicAthlete.Training.Exporting;

using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json;

internal sealed class GarminFitProfileCatalog
{
    private const string CatalogResourceName = "BionicAthlete.Training.Domain.Reference.GarminFitProfileCatalog.json";

    private static readonly Lazy<GarminFitProfileCatalog> s_default = new(LoadDefaultCatalog);

    private readonly FrozenSet<string> _fieldKeysByName;
    private readonly FrozenSet<string> _fieldKeysByNumber;

    private GarminFitProfileCatalog(
        string sourceWorkbook,
        FrozenSet<string> fieldKeysByName,
        FrozenSet<string> fieldKeysByNumber)
    {
        SourceWorkbook = sourceWorkbook;
        _fieldKeysByName = fieldKeysByName;
        _fieldKeysByNumber = fieldKeysByNumber;
    }

    public static GarminFitProfileCatalog Default => s_default.Value;

    public string SourceWorkbook { get; }

    public bool ContainsField(string? messageFamily, ushort? messageNumber, string? fieldName)
    {
        bool hasNamedMatch = !string.IsNullOrWhiteSpace(messageFamily)
            && !string.IsNullOrWhiteSpace(fieldName)
            && _fieldKeysByName.Contains(CreateFieldNameKey(messageFamily, fieldName));
        if (hasNamedMatch)
        {
            return true;
        }

        return messageNumber.HasValue
            && !string.IsNullOrWhiteSpace(fieldName)
            && _fieldKeysByNumber.Contains(CreateFieldNumberKey(messageNumber.Value, fieldName));
    }

    private static GarminFitProfileCatalog LoadDefaultCatalog()
    {
        using Stream? resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(CatalogResourceName);
        if (resourceStream is null)
        {
            return CreateEmptyCatalog();
        }

        using JsonDocument catalogDocument = JsonDocument.Parse(resourceStream);
        JsonElement root = catalogDocument.RootElement;
        string sourceWorkbook = root.TryGetProperty("sourceWorkbook", out JsonElement sourceWorkbookElement)
            ? sourceWorkbookElement.GetString() ?? string.Empty
            : string.Empty;

        HashSet<string> fieldKeysByName = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> fieldKeysByNumber = new(StringComparer.OrdinalIgnoreCase);
        foreach (JsonElement messageElement in root.GetProperty("messages").EnumerateArray())
        {
            string? messageName = messageElement.GetProperty("name").GetString();
            ushort? messageNumber = TryGetUInt16(messageElement.GetProperty("number"));
            if (string.IsNullOrWhiteSpace(messageName))
            {
                continue;
            }

            foreach (JsonElement fieldElement in messageElement.GetProperty("fields").EnumerateArray())
            {
                string? fieldName = fieldElement.GetProperty("name").GetString();
                if (string.IsNullOrWhiteSpace(fieldName))
                {
                    continue;
                }

                _ = fieldKeysByName.Add(CreateFieldNameKey(messageName, fieldName));
                if (messageNumber.HasValue)
                {
                    _ = fieldKeysByNumber.Add(CreateFieldNumberKey(messageNumber.Value, fieldName));
                }
            }
        }

        return new GarminFitProfileCatalog(
            sourceWorkbook,
            fieldKeysByName.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
            fieldKeysByNumber.ToFrozenSet(StringComparer.OrdinalIgnoreCase));
    }

    private static GarminFitProfileCatalog CreateEmptyCatalog()
        => new(
            string.Empty,
            Enumerable.Empty<string>().ToFrozenSet(StringComparer.OrdinalIgnoreCase),
            Enumerable.Empty<string>().ToFrozenSet(StringComparer.OrdinalIgnoreCase));

    private static ushort? TryGetUInt16(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number
            && element.TryGetUInt16(out ushort value))
        {
            return value;
        }

        return null;
    }

    private static string CreateFieldNameKey(string messageFamily, string fieldName)
        => $"{messageFamily}.{fieldName}";

    private static string CreateFieldNumberKey(ushort messageNumber, string fieldName)
        => $"{messageNumber}:{fieldName}";
}
