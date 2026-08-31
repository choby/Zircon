namespace Server.Web.Models;

public sealed record MapCellData(
    byte BackFile,
    ushort BackImage,
    byte MiddleFile,
    ushort MiddleImage,
    byte FrontFile,
    ushort FrontImage,
    byte MiddleAnimationFrame,
    byte FrontAnimationFrame,
    byte Light,
    bool Blocked);

public sealed record MapDocument(
    string FileName,
    int Width,
    int Height,
    IReadOnlyList<MapCellData> Cells,
    string VersionTag);

public sealed record MapRegionAdminModel(
    int Index,
    int MapIndex,
    string MapFileName,
    string MapDescription,
    string Description,
    string RegionType,
    int Size,
    string ETag);

public sealed record MapCellPoint(int X, int Y);
