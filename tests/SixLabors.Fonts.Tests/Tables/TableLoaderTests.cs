using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using SixLabors.Fonts.Tables;

using Xunit;

namespace SixLabors.Fonts.Tests.Tables
{
    public class TableLoaderTests
    {
        public static IEnumerable<object[]> RegisterableTableTypes
        {
            get
            {
                TypeInfo tableTypeInfo = typeof(Table).GetTypeInfo();

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
            Assert.Equal(name, tl.GetTag(type));
        }

        [Fact]
        public void DefaultIsnotNull()
        {
            Assert.NotNull(TableLoader.Default);
        }

        [Fact]
        public void TryingToLoadUnregisteredTagReturnsUnknownTable()
        {
            var loader = new TableLoader();

            string tag = Guid.NewGuid().ToString();
            Table result = loader.Load(tag, null);

            UnknownTable table = Assert.IsType<UnknownTable>(result);
            Assert.Equal(tag, table.Name);
        }


        [Fact]
        public void NullForUnknownTypes()
        {
            var loader = new TableLoader();
            string tag = loader.GetTag(typeof(TableLoaderTests));
            Assert.Null(tag);
        }
    }
}
