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
                try
                {
                    var tableTypeInfo = typeof(Table).GetTypeInfo();

                    return
                        typeof(Table).GetTypeInfo()
                            .Assembly.DefinedTypes
                            .Where(x => tableTypeInfo.IsAssignableFrom(x))
                            .Select(x => new object[] { x.AsType(), x.GetCustomAttribute<TableNameAttribute>()?.Name })
                            .Where(x => x[1] != null);
                }catch(Exception ex)
                {
                    Assert.False(true, "Failed laoding stuff");
                    throw;
                }
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
            var result = loader.Load(tag, null);

            var table = Assert.IsType<UnknownTable>(result);
            Assert.Equal(tag, table.Name);
        }


        [Fact]
        public void NullForUnknownTypes()
        {
            var loader = new TableLoader();
            var tag = loader.GetTag(typeof(TableLoaderTests));
            Assert.Null(tag);
        }
    }
}
