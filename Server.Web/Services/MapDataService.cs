using System.Security.Cryptography;
using Server.Envir;
using Server.Web.Models;

namespace Server.Web.Services;

public sealed class MapDataService
{
    public IReadOnlyList<string> GetMapFiles()
    {
        string root = ResolveMapRoot();
        if (!Directory.Exists(root)) return [];
        return Directory.EnumerateFiles(root, "*.map", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null)
            .Cast<string>()
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<MapDocument> LoadAsync(string fileName, CancellationToken cancellationToken = default)
    {
        string root = ResolveMapRoot();
        string safeName = Path.GetFileNameWithoutExtension(fileName);
        string path = Path.Combine(root, safeName + ".map");
        if (!File.Exists(path)) throw new FileNotFoundException("地图文件不存在。", path);

        byte[] data = await File.ReadAllBytesAsync(path, cancellationToken);
        if (data.Length < 28) throw new InvalidDataException("地图文件头已损坏。");

        using MemoryStream stream = new(data, writable: false);
        using BinaryReader reader = new(stream);
        stream.Seek(22, SeekOrigin.Begin);
        int width = reader.ReadInt16();
        int height = reader.ReadInt16();
        if (width <= 0 || height <= 0 || width > 16_384 || height > 16_384)
            throw new InvalidDataException($"地图尺寸无效：{width}x{height}。");

        long expectedMinimum = 28L + (width / 2L) * (height / 2L) * 3L + (long)width * height * 14L;
        if (data.LongLength < expectedMinimum)
            throw new InvalidDataException("地图文件被截断。");

        stream.Seek(28, SeekOrigin.Begin);
        MapCellData[] cells = new MapCellData[checked(width * height)];
        byte[] backFiles = new byte[cells.Length];
        ushort[] backImages = new ushort[cells.Length];

        for (int x = 0; x < width / 2; x++)
        for (int y = 0; y < height / 2; y++)
        {
            int index = (y * 2) * width + x * 2;
            backFiles[index] = reader.ReadByte();
            backImages[index] = reader.ReadUInt16();
        }

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            byte flag = reader.ReadByte();
            byte middleAnimation = reader.ReadByte();
            byte frontAnimationValue = reader.ReadByte();
            byte frontFile = reader.ReadByte();
            byte middleFile = reader.ReadByte();
            ushort middleImage = (ushort)(reader.ReadUInt16() + 1);
            ushort frontImage = (ushort)(reader.ReadUInt16() + 1);
            stream.Seek(3, SeekOrigin.Current);
            byte light = (byte)((reader.ReadByte() & 0x0F) * 2);
            stream.Seek(1, SeekOrigin.Current);
            int index = y * width + x;
            cells[index] = new MapCellData(
                backFiles[index], backImages[index], middleFile, middleImage, frontFile, frontImage,
                middleAnimation, frontAnimationValue == 255 ? (byte)0 : frontAnimationValue,
                light, ((flag & 0x01) != 1) || ((flag & 0x02) != 2));
        }

        string versionTag = Convert.ToHexString(SHA256.HashData(data));
        return new MapDocument(safeName, width, height, cells, versionTag);
    }

    private static string ResolveMapRoot()
    {
        string configured = Config.MapPath ?? ".";
        configured = configured.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configured));
    }
}
