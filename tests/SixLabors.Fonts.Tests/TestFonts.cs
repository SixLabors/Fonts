using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SixLabors.Fonts.Tests
{
    using System.IO;
    using System.Reflection;

    using Xunit;

    public static class TestFonts
    {
        private static Dictionary<string, Stream> cache=  new Dictionary<string, Stream>();
        public static string SimpleFontFile => GetFullPath("SixLaborsSamplesAB.ttf");
        public static Stream SimpleFontFileData() => OpenStream(SimpleFontFile);

        private static Stream OpenStream(string path)
        {
            if (cache.ContainsKey(path))
            {
                return cache[path].Clone();
            }

            lock (cache)
            {
                if (cache.ContainsKey(path))
                {
                    return cache[path].Clone();
                }

                using (var fs = File.OpenRead(path))
                {
                    cache.Add(path, fs.Clone());
                    return cache[path].Clone();
                }
            }
        }

        private static Stream Clone(this Stream src)
        {
            var ms = new MemoryStream();
            src.Position = 0;
            src.CopyTo(ms);
            ms.Position = 0;
            return ms;
        }

        private static string GetFullPath(string path)
        {
            var root = new Uri(typeof(TestFonts).GetTypeInfo().Assembly.CodeBase).LocalPath;

            var paths = new[] { "Fonts", @"..\..\Fonts", @"..\..\..\..\Fonts" };
            var fullPaths = paths.Select(x => Path.GetFullPath(Path.Combine(root, x)));
            var rootPath = fullPaths
                                .Where(x => Directory.Exists(x))
                                .FirstOrDefault();

            Assert.True(rootPath != null, $"could not find the font folder in any of these location, \n{string.Join("\n", fullPaths)}");

            return Path.Combine(rootPath, path);
        }
    }
}
