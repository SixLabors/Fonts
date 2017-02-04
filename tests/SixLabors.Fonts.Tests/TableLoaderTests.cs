using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using SixLabors.Fonts.Tables;

using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class TableLoaderTests
    {
        public static IEnumerable<object[]> RegisterableTableTypes
        {
            get
            {
                var tableTypeInfo = typeof(Table).GetTypeInfo();

                return
                    typeof(Table).GetTypeInfo()
                        .Assembly.DefinedTypes
                        .Where(x => tableTypeInfo.IsAssignableFrom(x))
                        .Select(x => new object[] { x.AsType(), x.GetCustomAttribute<TableNameAttribute>()?.Name })
                        .Where(x => x[1] != null);
            }
        }

        [Theory]
        [MemberData(nameof(RegisterableTableTypes))]
        public void AllNamedTablesAreRegistered(Type type, string name)
        {
            var tl = new TableLoader();
            Assert.Contains(type, tl.RegisterdTypes());
            Assert.Contains(name, tl.TableTags(null));
        }

        [Fact]
        public void DefaultIsnotNull()
        {
            Assert.NotNull(TableLoader.Default);
        }

        [Fact]
        public void PassingNullToGetTagsReturnsAllRegisteredTableTags()
        {
            var loader = new TableLoader();

            Assert.NotEmpty(loader.TableTags(null));
        }

        [Fact]
        public void TryingToLoadUnregisteredTagReturnsUnknownTable()
        {
            var loader = new TableLoader();

            string tag = Guid.NewGuid().ToString();
            var result = loader.Load(tag, null);

            var table = Assert.IsType<UnknownTable>(result);
            Assert.Equal(tag, table.Name);
        }
    }
}
