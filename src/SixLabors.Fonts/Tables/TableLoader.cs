// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;
using SixLabors.Fonts.Tables.Cff;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Tables.General.Colr;
using SixLabors.Fonts.Tables.General.Kern;
using SixLabors.Fonts.Tables.General.Name;
using SixLabors.Fonts.Tables.General.Post;
using SixLabors.Fonts.Tables.General.Svg;
using SixLabors.Fonts.Tables.TrueType;
using SixLabors.Fonts.Tables.TrueType.Glyphs;
using SixLabors.Fonts.Tables.TrueType.Hinting;

namespace SixLabors.Fonts.Tables;

/// <summary>
/// Provides registration and loading of font tables by tag or CLR type.
/// Maps four-byte table tags (e.g. "cmap", "head") to their static <c>Load</c> factory methods.
/// </summary>
internal class TableLoader
{
    private readonly Dictionary<string, Func<FontReader, Table?>> loaders = [];
    private readonly Dictionary<Type, string> types = [];
    private readonly Dictionary<Type, Func<FontReader, Table?>> typesLoaders = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="TableLoader"/> class
    /// with all known table parsers registered.
    /// </summary>
    public TableLoader()
    {
        // We will hard code mapping registration in here for all the tables
        this.Register(NameTable.TableName, NameTable.Load);
        this.Register(CMapTable.TableName, CMapTable.Load);
        this.Register(HeadTable.TableName, HeadTable.Load);
        this.Register(HorizontalHeadTable.TableName, HorizontalHeadTable.Load);
        this.Register(HorizontalMetricsTable.TableName, HorizontalMetricsTable.Load);
        this.Register(VerticalHeadTable.TableName, VerticalHeadTable.Load);
        this.Register(VerticalMetricsTable.TableName, VerticalMetricsTable.Load);
        this.Register(MaximumProfileTable.TableName, MaximumProfileTable.Load);
        this.Register(OS2Table.TableName, OS2Table.Load);
        this.Register(IndexLocationTable.TableName, IndexLocationTable.Load);
        this.Register(GlyphTable.TableName, GlyphTable.Load);
        this.Register(KerningTable.TableName, KerningTable.Load);
        this.Register(ColrTable.TableName, ColrTable.Load);
        this.Register(CpalTable.TableName, CpalTable.Load);
        this.Register(GPosTable.TableName, GPosTable.Load);
        this.Register(GSubTable.TableName, GSubTable.Load);
        this.Register(CvtTable.TableName, CvtTable.Load);
        this.Register(FpgmTable.TableName, FpgmTable.Load);
        this.Register(PrepTable.TableName, PrepTable.Load);
        this.Register(GlyphDefinitionTable.TableName, GlyphDefinitionTable.Load);
        this.Register(PostTable.TableName, PostTable.Load);
        this.Register(Cff1Table.TableName, Cff1Table.Load);
        this.Register(Cff2Table.TableName, Cff2Table.Load);
        this.Register(AVarTable.TableName, AVarTable.Load);
        this.Register(GVarTable.TableName, GVarTable.Load);
        this.Register(FVarTable.TableName, FVarTable.Load);
        this.Register(HVarTable.TableName, HVarTable.Load);
        this.Register(VVarTable.TableName, VVarTable.Load);
        this.Register(MVarTable.TableName, MVarTable.Load);
        this.Register<CVarTable>(CVarTable.TableName, _ => null);
        this.Register(SvgTable.TableName, SvgTable.Load);
    }

    /// <summary>
    /// Gets the default shared <see cref="TableLoader"/> instance with all standard tables registered.
    /// </summary>
    public static TableLoader Default { get; } = new();

    /// <summary>
    /// Gets the four-byte tag string associated with the given table type.
    /// </summary>
    /// <param name="type">The CLR type of the table.</param>
    /// <returns>The tag string, or <see langword="null"/> if the type is not registered.</returns>
    public string? GetTag(Type type)
    {
        this.types.TryGetValue(type, out string? value);

        return value;
    }

    /// <summary>
    /// Gets the four-byte tag string associated with the given table type.
    /// </summary>
    /// <typeparam name="TType">The CLR type of the table.</typeparam>
    /// <returns>The tag string.</returns>
    public string GetTag<TType>()
    {
        this.types.TryGetValue(typeof(TType), out string? value);
        return value!;
    }

    /// <summary>
    /// Gets all registered table CLR types.
    /// </summary>
    internal IEnumerable<Type> RegisteredTypes() => this.types.Keys;

    /// <summary>
    /// Gets all registered four-byte table tags.
    /// </summary>
    internal IEnumerable<string> RegisteredTags() => this.types.Values;

    private void Register<T>(string tag, Func<FontReader, T?> createFunc)
        where T : Table
    {
        lock (this.loaders)
        {
            if (!this.loaders.ContainsKey(tag))
            {
                this.loaders.Add(tag, createFunc);
                this.types.Add(typeof(T), tag);
                this.typesLoaders.Add(typeof(T), createFunc);
            }
        }
    }

    /// <summary>
    /// Loads a table by its four-byte tag string.
    /// Returns an <see cref="UnknownTable"/> if no parser is registered for the tag.
    /// </summary>
    /// <param name="tag">The four-byte table tag.</param>
    /// <param name="reader">The font reader.</param>
    /// <returns>The loaded table, or an <see cref="UnknownTable"/> for unrecognized tags.</returns>
    internal Table? Load(string tag, FontReader reader)

         // loader missing? register an unknown type loader and carry on
         => this.loaders.TryGetValue(tag, out Func<FontReader, Table?>? func)
            ? func.Invoke(reader)
            : new UnknownTable(tag);

    /// <summary>
    /// Loads a table by its CLR type.
    /// </summary>
    /// <typeparam name="TTable">The table type to load.</typeparam>
    /// <param name="reader">The font reader.</param>
    /// <returns>The loaded table instance, or <see langword="null"/>.</returns>
    /// <exception cref="MissingFontTableException">Thrown when the table type has not been registered.</exception>
    internal TTable? Load<TTable>(FontReader reader)
        where TTable : Table
    {
        // loader missing register an unknown type loader and carry on
        if (this.typesLoaders.TryGetValue(typeof(TTable), out Func<FontReader, Table?>? func))
        {
            return (TTable?)func.Invoke(reader);
        }

        throw new MissingFontTableException("Font table not registered.", nameof(TTable));
    }
}
