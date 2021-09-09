// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace SixLabors.Fonts.Tests
{
    public static class TestFonts
    {
        private static readonly Dictionary<string, Stream> Cache = new Dictionary<string, Stream>();

        public static string TwemojiMozillaFile => GetFullPath("Twemoji Mozilla.ttf");

        public static string CarterOneFile => GetFullPath("Carter_One/CarterOne.ttf");

        public static string WendyOneFile => GetFullPath("Wendy_One/WendyOne-Regular.ttf");

        public static string OpenSansFile => GetFullPath("OpenSans-Regular.ttf");

        public static string OpenSansFileWoff2 => GetFullPath("OpenSans-Regular.woff2");

        public static string SimpleFontFile => GetFullPath("SixLaborsSampleAB.ttf");

        public static string SimpleFontFileWoff => GetFullPath("SixLaborsSampleAB.woff");

        public static string ArabicFontFile => GetFullPath("Dubai-regular.ttf");

        public static string SimpleTrueTypeCollection => GetFullPath("Sample.ttc");

        public static string WhitneyBookFile => GetFullPath("whitney-book.ttf");

        public static string Version1Font => GetFullPath("Font-Version1.ttf");

        public static string NotoSansSCThinFile => GetFullPath("NotoSansSC-Thin.ttf");

        public static string HelveticaTTCFile => GetFullPath("Helvetica.ttc");

        public static Stream TwemojiMozillaData() => OpenStream(TwemojiMozillaFile);

        public static Stream WendyOneFileData() => OpenStream(WendyOneFile);

        public static Stream CarterOneFileData() => OpenStream(CarterOneFile);

        public static Stream SimpleFontFileData() => OpenStream(SimpleFontFile);

        public static Stream ArabicFontFileData() => OpenStream(ArabicFontFile);

        public static Stream OpenSansData() => OpenStream(OpenSansFile);

        public static Stream SimpleFontFileWoffData() => OpenStream(SimpleFontFileWoff);

        public static Stream SSimpleTrueTypeCollectionData() => OpenStream(SimpleTrueTypeCollection);

        public static Stream FontFileWoff2Data() => OpenStream(OpenSansFileWoff2);

        public static class Issues
        {
            public static string Issue96File => GetFullPath("Issues/Issue96.fuzz");

            public static string Issue97File => GetFullPath("Issues/Issue97.fuzz");
        }

        private static Stream OpenStream(string path)
        {
            if (Cache.ContainsKey(path))
            {
                return Cache[path].Clone();
            }

            lock (Cache)
            {
                if (Cache.ContainsKey(path))
                {
                    return Cache[path].Clone();
                }

                using (FileStream fs = File.OpenRead(path))
                {
                    Cache.Add(path, fs.Clone());
                    return Cache[path].Clone();
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
