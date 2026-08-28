using System;
using System.IO;

namespace Library
{
    public static class PlatformPath
    {
        public static string Normalize(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return path;

            char separator = Path.DirectorySeparatorChar;
            return path.Replace('\\', separator).Replace('/', separator);
        }

        public static string Resolve(string path, string baseDirectory = null)
        {
            string normalized = Normalize(path);
            if (Path.IsPathRooted(normalized)) return Path.GetFullPath(normalized);

            return Path.GetFullPath(Path.Combine(baseDirectory ?? AppDomain.CurrentDomain.BaseDirectory, normalized));
        }
    }
}
