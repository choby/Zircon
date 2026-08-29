using System.Collections.Concurrent;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using BCnEncoder.Decoder;
using BCnEncoder.Shared;
using Library;
using Server.Envir;
using SkiaSharp;

namespace Server.Web.Services;

public sealed class MapImageService
{
    // Increment whenever decoding or PNG encoding changes. It is part of both the
    // browser URL and ETag so a corrected decoder can never reuse older bad pixels.
    public const string DecoderVersion = "4";
    private static readonly byte[] Zl2Signature = "ZL2"u8.ToArray();
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private readonly ConcurrentDictionary<string, CachedLibrary> _libraries = new(StringComparer.OrdinalIgnoreCase);

    public MapAssetStatus GetStatus()
    {
        LibraryFile[] required = Libraries.KROrder.Values.Distinct().ToArray();
        List<string> assetVersions = new(required.Length);
        int available = 0;
        foreach (LibraryFile file in required)
        {
            if (!TryResolveLibrary(file, out string path)) continue;
            FileInfo info = new(path);
            available++;
            assetVersions.Add($"{(int)file}:{info.Length}:{info.LastWriteTimeUtc.Ticks}");
        }

        assetVersions.Sort(StringComparer.Ordinal);
        string versionSource = $"{DecoderVersion}|{string.Join('|', assetVersions)}";
        string version = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(versionSource)))[..16];
        string message = available == 0
            ? "未找到地图贴图库。请将客户端 Data 目录放在 ClientPath 下，或在 Server.ini 中正确设置 ClientPath。"
            : available < required.Length
                ? $"已找到 {available}/{required.Length} 个地图贴图库；缺失素材对应的地图块将保持透明。"
                : "地图贴图库可用。";
        return new MapAssetStatus(available > 0, available, required.Length, version, message);
    }

    public MapImageResult? GetImage(int mapFile, int imageIndex)
    {
        if (imageIndex < 0 || !Libraries.KROrder.TryGetValue(mapFile, out LibraryFile libraryFile) ||
            !TryResolveLibrary(libraryFile, out string path)) return null;

        long stamp = File.GetLastWriteTimeUtc(path).Ticks;
        CachedLibrary cached = _libraries.AddOrUpdate(path,
            _ => new CachedLibrary(stamp, ZlLibraryIndex.Load(path)),
            (_, current) => current.LastWriteTicks == stamp ? current : new CachedLibrary(stamp, ZlLibraryIndex.Load(path)));
        byte[]? png = cached.Index.ReadPng(imageIndex);
        return png is null
            ? null
            : new MapImageResult(png, $"\"map-{DecoderVersion}-{stamp:x}-{new FileInfo(path).Length:x}-{mapFile:x2}-{imageIndex:x}\"");
    }

    private static bool TryResolveLibrary(LibraryFile file, out string path)
    {
        path = string.Empty;
        if (!Libraries.LibraryList.TryGetValue(file, out string? relative)) return false;
        string clientRoot = PlatformPath.Resolve(string.IsNullOrWhiteSpace(Config.ClientPath) ? "." : Config.ClientPath);
        path = Path.GetFullPath(Path.Combine(clientRoot, PlatformPath.Normalize(relative)));
        return File.Exists(path);
    }

    private sealed record CachedLibrary(long LastWriteTicks, ZlLibraryIndex Index);

    private sealed class ZlLibraryIndex
    {
        private readonly string _path;
        private readonly ImageMetadata?[] _images;
        private readonly IReadOnlyDictionary<int, PayloadEntry>? _payloads;

        private ZlLibraryIndex(string path, ImageMetadata?[] images, IReadOnlyDictionary<int, PayloadEntry>? payloads)
        {
            _path = path;
            _images = images;
            _payloads = payloads;
        }

        public static ZlLibraryIndex Load(string path)
        {
            using FileStream stream = File.OpenRead(path);
            using BinaryReader reader = new(stream);
            byte[] signature = reader.ReadBytes(Zl2Signature.Length);
            stream.Position = 0;
            return signature.SequenceEqual(Zl2Signature) ? ReadCompressed(path, reader) : ReadClassic(path, reader);
        }

        public byte[]? ReadPng(int imageIndex)
        {
            if (imageIndex < 0 || imageIndex >= _images.Length || _images[imageIndex] is not { } image ||
                image.Width <= 0 || image.Height <= 0) return null;

            byte[] payload;
            using (FileStream stream = File.OpenRead(_path))
            using (BinaryReader reader = new(stream))
            {
                if (_payloads is not null)
                {
                    if (!_payloads.TryGetValue(image.Position, out PayloadEntry? entry)) return null;
                    stream.Position = entry.Offset;
                    byte[] compressed = reader.ReadBytes(entry.CompressedSize);
                    payload = Decompress(compressed, entry.UncompressedSize, entry.Compression);
                }
                else
                {
                    if (image.Position <= 0) return null;
                    stream.Position = image.Position;
                    payload = reader.ReadBytes(image.PrimarySize);
                }
            }

            int size = Math.Min(image.PrimarySize, payload.Length);
            if (size <= 0) return null;
            byte[] primary = size == payload.Length ? payload : payload[..size];
            return EncodePng(primary, image.Codec, image.Width, image.Height);
        }

        private static ZlLibraryIndex ReadClassic(string path, BinaryReader reader)
        {
            int headerSize = reader.ReadInt32();
            if (headerSize <= 0 || headerSize > reader.BaseStream.Length - sizeof(int))
                throw new InvalidDataException($"贴图库头无效：{Path.GetFileName(path)}");
            using MemoryStream metadataStream = new(reader.ReadBytes(headerSize), writable: false);
            using BinaryReader metadataReader = new(metadataStream);
            int value = metadataReader.ReadInt32();
            int count = value & 0x1FFFFFF;
            int version = (value >> 25) & 0x7F;
            if (version == 0) count = value;
            ImageMetadata?[] images = ReadImages(metadataReader, count, version);
            return new ZlLibraryIndex(path, images, null);
        }

        private static ZlLibraryIndex ReadCompressed(string path, BinaryReader reader)
        {
            reader.ReadBytes(Zl2Signature.Length);
            reader.ReadInt32(); // container version
            reader.ReadInt32(); // image count
            reader.ReadInt32(); // atlas count
            reader.ReadByte();  // default compression
            reader.ReadByte();  // flags
            reader.ReadInt16(); // reserved
            long metadataOffset = reader.ReadInt64();
            int metadataSize = reader.ReadInt32();
            long indexOffset = reader.ReadInt64();
            int indexSize = reader.ReadInt32();

            reader.BaseStream.Position = indexOffset;
            using MemoryStream indexStream = new(reader.ReadBytes(indexSize), writable: false);
            using BinaryReader indexReader = new(indexStream);
            int entryCount = indexReader.ReadInt32();
            Dictionary<int, PayloadEntry> entries = new(entryCount);
            for (int index = 0; index < entryCount; index++)
            {
                indexReader.ReadByte(); // entry type
                int id = indexReader.ReadInt32();
                int uncompressedSize = indexReader.ReadInt32();
                int compressedSize = indexReader.ReadInt32();
                long offset = indexReader.ReadInt64();
                Compression compression = (Compression)indexReader.ReadByte();
                indexReader.ReadByte(); // codec
                entries[id] = new PayloadEntry(uncompressedSize, compressedSize, offset, compression);
            }

            reader.BaseStream.Position = metadataOffset;
            using MemoryStream metadataStream = new(reader.ReadBytes(metadataSize), writable: false);
            using BinaryReader metadataReader = new(metadataStream);
            int version = metadataReader.ReadInt32();
            int count = metadataReader.ReadInt32();
            metadataReader.ReadInt32(); // atlas group image count
            metadataReader.ReadInt32(); // atlas page size
            ImageMetadata?[] images = ReadImages(metadataReader, count, version);
            return new ZlLibraryIndex(path, images, entries);
        }

        private static ImageMetadata?[] ReadImages(BinaryReader reader, int count, int version)
        {
            if (count < 0 || count > 10_000_000) throw new InvalidDataException($"贴图数量无效：{count}");
            ImageMetadata?[] images = new ImageMetadata?[count];
            for (int index = 0; index < count; index++)
            {
                if (!reader.ReadBoolean()) continue;
                int position = reader.ReadInt32();
                short width = reader.ReadInt16();
                short height = reader.ReadInt16();
                reader.BaseStream.Position += 17; // offsets, shadow type and shadow/overlay dimensions

                ImageCodec codec = version == 0 ? ImageCodec.Dxt1 : ImageCodec.Dxt5;
                int storedSize = 0;
                if (version >= 2)
                {
                    reader.ReadInt32(); // atlas page
                    reader.BaseStream.Position += 16; // source and visible rectangles
                    codec = (ImageCodec)reader.ReadByte();
                    reader.BaseStream.Position += 5; // shadow/overlay codecs and runtime preferences
                    storedSize = reader.ReadInt32();
                    reader.BaseStream.Position += 8; // image BC7 and fallback sizes
                    reader.BaseStream.Position += 24; // shadow and overlay segment sizes
                }

                int primarySize = storedSize > 0 ? storedSize : GetDataSize(width, height, codec);
                images[index] = new ImageMetadata(position, width, height, codec, primarySize);
            }
            return images;
        }

        private static byte[] Decompress(byte[] payload, int uncompressedSize, Compression compression)
        {
            if (compression == Compression.None) return payload;
            if (compression is not (Compression.DeflateFast or Compression.DeflateBest))
                throw new InvalidDataException($"不支持的 ZL 压缩方式：{(byte)compression}");
            using MemoryStream input = new(payload, writable: false);
            using DeflateStream deflate = new(input, CompressionMode.Decompress);
            using MemoryStream output = new(Math.Max(0, uncompressedSize));
            deflate.CopyTo(output);
            return output.ToArray();
        }

        private static byte[] EncodePng(byte[] payload, ImageCodec codec, int width, int height)
        {
            if (codec == ImageCodec.Png && payload.AsSpan().StartsWith(PngSignature)) return payload;

            byte[] pixels;
            SKColorType colorType;
            if (codec == ImageCodec.Bgra32)
            {
                pixels = payload;
                colorType = SKColorType.Bgra8888;
            }
            else
            {
                CompressionFormat format = codec switch
                {
                    ImageCodec.Dxt1 => CompressionFormat.Bc1WithAlpha,
                    ImageCodec.Dxt5 => CompressionFormat.Bc3,
                    ImageCodec.Bc7 => CompressionFormat.Bc7,
                    _ => throw new InvalidDataException($"不支持的地图贴图编码：{codec}")
                };
                ColorRgba32[] decoded = new BcDecoder().DecodeRaw(payload, width, height, format);
                pixels = new byte[checked(width * height * 4)];
                for (int index = 0; index < decoded.Length && index * 4 + 3 < pixels.Length; index++)
                {
                    int offset = index * 4;
                    pixels[offset] = decoded[index].r;
                    pixels[offset + 1] = decoded[index].g;
                    pixels[offset + 2] = decoded[index].b;
                    pixels[offset + 3] = decoded[index].a;
                }
                colorType = SKColorType.Rgba8888;
            }

            int expected = checked(width * height * 4);
            if (pixels.Length < expected) throw new InvalidDataException("地图贴图像素数据被截断。");
            SKImageInfo info = new(width, height, colorType, SKAlphaType.Unpremul);
            using SKBitmap bitmap = new(info);
            Marshal.Copy(pixels, 0, bitmap.GetPixels(), expected);
            using SKImage image = SKImage.FromBitmap(bitmap);
            using SKData encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            return encoded.ToArray();
        }

        private static int GetDataSize(short width, short height, ImageCodec codec)
        {
            int blocks = width <= 0 || height <= 0 ? 0 : Math.Max(1, (width + 3) / 4) * Math.Max(1, (height + 3) / 4);
            return codec switch
            {
                ImageCodec.Dxt1 => blocks * 8,
                ImageCodec.Dxt5 or ImageCodec.Bc7 => blocks * 16,
                ImageCodec.Bgra32 => Math.Max(0, (int)width) * Math.Max(0, (int)height) * 4,
                _ => 0
            };
        }

        private sealed record ImageMetadata(int Position, short Width, short Height, ImageCodec Codec, int PrimarySize);
        private sealed record PayloadEntry(int UncompressedSize, int CompressedSize, long Offset, Compression Compression);
        private enum ImageCodec : byte { Dxt1, Dxt5, Bgra32, Bc7, Png }
        private enum Compression : byte { None, DeflateFast, DeflateBest }
    }
}

public sealed record MapImageResult(byte[] Content, string ETag);
public sealed record MapAssetStatus(bool Available, int AvailableLibraries, int ExpectedLibraries, string Version, string Message);
