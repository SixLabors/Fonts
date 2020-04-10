using System.Globalization;
using System.Linq;
using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class TrueTypeCollectionTests
    {
        [Fact]
        public void InstallViaPathReturnsDecription()
        {
            var sut = new FontCollection();
            var collectionFromPath = sut.InstallCollection(TestFonts.SimpleTrueTypeCollection, out var descriptions);

            Assert.Equal(2, descriptions.Count());
            var openSans = Assert.Single(collectionFromPath, x => x.Name == "Open Sans");
            var abFont = Assert.Single(collectionFromPath, x => x.Name == "SixLaborsSampleAB");

            Assert.Equal(2, descriptions.Count());
            var openSansDescription = Assert.Single(descriptions, x => x.FontNameInvariantCulture == "Open Sans");
            var abFontDescription = Assert.Single(descriptions, x => x.FontNameInvariantCulture == "SixLaborsSampleAB regular");
        }

        [Fact]
        public void InstallViaPathInstallFontFileInstances()
        {
            var sut = new FontCollection();
            var collectionFromPath = sut.InstallCollection(TestFonts.SimpleTrueTypeCollection, out var descriptions);

            var allInstances = sut.Families.SelectMany(x => sut.FindAll(x.Name, CultureInfo.InvariantCulture));

            Assert.All(allInstances, i =>
            {
                var font = Assert.IsType<FileFontInstance>(i);
            });
        }

        [Fact]
        public void InstallViaStreamhReturnsDecription()
        {
            var sut = new FontCollection();
            var collectionFromPath = sut.InstallCollection(TestFonts.SSimpleTrueTypeCollectionData(), out var descriptions);

            Assert.Equal(2, collectionFromPath.Count());
            var openSans = Assert.Single(collectionFromPath, x => x.Name == "Open Sans");
            var abFont = Assert.Single(collectionFromPath, x => x.Name == "SixLaborsSampleAB");

            Assert.Equal(2, descriptions.Count());
            var openSansDescription = Assert.Single(descriptions, x => x.FontNameInvariantCulture == "Open Sans");
            var abFontDescription = Assert.Single(descriptions, x => x.FontNameInvariantCulture == "SixLaborsSampleAB regular");
        }
    }
}
