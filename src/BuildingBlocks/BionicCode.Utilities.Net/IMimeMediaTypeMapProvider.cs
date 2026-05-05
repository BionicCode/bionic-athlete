namespace BionicCode.Utilities.Net;

public interface IMimeMediaTypeMapProvider
{
    bool TryGetMediaTypeForExtension(FileExtension fileExtension, out string mediaType);
}
