namespace BionicCode.Utilities.Net;

public enum PathSegmentKind
{
    Undefined = 0,
    FullyQualifiedRoot,
    RelativeRoot,
    CurrentDirectory,
    ParentDirectory,
    DirectoryName,
    FileName
}
