using System;
using System.Collections.Generic;
using System.Linq;

namespace SixLabors.Fonts.Tests
{
    using System.IO;
    using System.Reflection;

    using Xunit;

    public static class TestFonts
    {
        private static Dictionary<string, Stream> cache = new Dictionary<string, Stream>();
        public static string CarterOneFile => GetFullPath("Carter_One/CarterOne.ttf");
        public static string WendyOneFile => GetFullPath("Wendy_One/WendyOne-Regular.ttf");
        public static string OpenSansFile => GetFullPath("OpenSans-Regular.ttf");
        public static string SimpleFontFile => GetFullPath("SixLaborsSampleAB.ttf");
        public static string SimpleFontFileWoff => GetFullPath("SixLaborsSampleAB.woff");

        public static string SimpleTrueTypeCollection => GetFullPath("Sample.ttc");

        public static Stream WendyOneFileData() => OpenStream(WendyOneFile);
        public static Stream CarterOneFileData() => OpenStream(CarterOneFile);
        public static Stream SimpleFontFileData() => OpenStream(SimpleFontFile);
        public static Stream OpenSansData() => OpenStream(OpenSansFile);
        public static Stream SimpleFontFileWoffData() => OpenStream(SimpleFontFileWoff);
        public static Stream SSimpleTrueTypeCollectionData() => OpenStream(SimpleTrueTypeCollection);

        public static class Issues
        {
            public static string Issue96File => GetFullPath("Issues/Issue96.fuzz");
            public static string Issue97File => GetFullPath("Issues/Issue97.fuzz");
        }

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

                using (FileStream fs = File.OpenRead(path))
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
            string root = Path.GetDirectoryName(new Uri(typeof(TestFonts).GetTypeInfo().Assembly.CodeBase).LocalPath);

            string[] paths = new[]
            {
                "Fonts",
                @"..\..\Fonts",
                @"..\..\..\..\Fonts",
                @"..\..\..\..\..\Fonts"
            };

            IEnumerable<string> fullPaths = paths.Select(x => Path.GetFullPath(Path.Combine(root, x)));
            string rootPath = fullPaths
                                .Where(x => Directory.Exists(x))
                                .FirstOrDefault();

            Assert.True(rootPath != null, $"could not find the font folder in any of these location, \n{string.Join("\n", fullPaths)}");

            return Path.Combine(rootPath, path);
        }
    }
}
