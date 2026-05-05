namespace BionicAthlete.Infrastructure.FileSystem;

using BionicCode.Utilities.Net;
using Microsoft.AspNetCore.StaticFiles;

public class AspNetCoreMimeMediaTypeMapProvider : IMimeMediaTypeMapProvider
{
    private readonly FileExtensionContentTypeProvider _extensionToMediaTypeMap;
    public AspNetCoreMimeMediaTypeMapProvider() => _extensionToMediaTypeMap = new FileExtensionContentTypeProvider();

    public bool TryGetMediaTypeForExtension(FileExtension fileExtension, out string mediaType) => _extensionToMediaTypeMap.TryGetContentType(fileExtension.Value, out mediaType);
}