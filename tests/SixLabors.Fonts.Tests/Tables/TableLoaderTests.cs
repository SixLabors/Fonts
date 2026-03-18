// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Reflection;
using SixLabors.Fonts.Tables;

namespace SixLabors.Fonts.Tests.Tables;

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
                    .Select(x => new object[] { x.AsType(), x.GetField("TableName", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(null) })
                    .Where(x => x[1] != null);
        }
    }

    [Theory]
    [MemberData(nameof(RegisterableTableTypes))]
    public void AllNamedTablesAreRegistered(Type type, string name)
    {
        TableLoader tl = new();
        Assert.Contains(type, tl.RegisteredTypes());
        Assert.Equal(name, tl.GetTag(type));
    }

    [Fact]
    public void DefaultIsNotNull() => Assert.NotNull(TableLoader.Default);

    [Fact]
    public void TryingToLoadUnregisteredTagReturnsUnknownTable()
    {
        TableLoader loader = new();

        string tag = Guid.NewGuid().ToString();
        Table result = loader.Load(tag, null);

        UnknownTable table = Assert.IsType<UnknownTable>(result);
        Assert.Equal(tag, table.Name);
    }

    [Fact]
    public void NullForUnknownTypes()
    {
        TableLoader loader = new();
        string tag = loader.GetTag(typeof(TableLoaderTests));
        Assert.Null(tag);
    }
}
