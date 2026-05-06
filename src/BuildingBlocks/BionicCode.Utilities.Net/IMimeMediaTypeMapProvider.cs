namespace BionicCode.Utilities.Net;

public interface IMimeMediaTypeMapProvider
{
    string DefaultMediaType { get; }
    bool TryGetMediaTypeForExtension(FileExtension fileExtension, out string mediaType);
}
