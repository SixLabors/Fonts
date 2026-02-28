// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using SixLabors.Fonts.Tables;

namespace SixLabors.Fonts;

/// <summary>
/// Represents a collection of font families.
/// </summary>
public sealed class FontCollection : IFontCollection, IFontMetricsCollection
{
    private readonly HashSet<string> searchDirectories = [];
    private readonly HashSet<FontMetrics> metricsCollection = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="FontCollection"/> class.
    /// </summary>
    public FontCollection()
        : this(Array.Empty<string>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FontCollection"/> class.
    /// </summary>
    /// <param name="searchDirectories">The collection of directories used to search for font families.</param>
    /// <remarks>
    /// Use this constructor instead of the parameterless constructor if the fonts added to that collection
    /// are actually added after searching inside physical file system directories. The message of the
    /// <see cref="FontFamilyNotFoundException"/> will include the searched directories.
    /// </remarks>
    internal FontCollection(IReadOnlyCollection<string> searchDirectories)
    {
        Guard.NotNull(searchDirectories, nameof(searchDirectories));
        foreach (string? dir in searchDirectories)
        {
            this.searchDirectories.Add(dir);
        }
    }

    /// <inheritdoc/>
    public IEnumerable<FontFamily> Families => this.FamiliesByCultureImpl(CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public FontFamily Add(string path)
        => this.Add(path, out _);

    /// <inheritdoc/>
    public FontFamily Add(string path, out FontDescription description)
        => this.AddImpl(path, CultureInfo.InvariantCulture, out description);

    /// <inheritdoc/>
    public FontFamily Add(Stream stream)
        => this.Add(stream, out _);

    /// <inheritdoc/>
    public FontFamily Add(Stream stream, out FontDescription description)
        => this.AddImpl(stream, CultureInfo.InvariantCulture, out description);

    /// <inheritdoc/>
    public IEnumerable<FontFamily> AddCollection(string path)
        => this.AddCollection(path, out _);

    /// <inheritdoc/>
    public IEnumerable<FontFamily> AddCollection(string path, out IEnumerable<FontDescription> descriptions)
        => this.AddCollectionImpl(path, CultureInfo.InvariantCulture, out descriptions);

    /// <inheritdoc/>
    public IEnumerable<FontFamily> AddCollection(Stream stream)
        => this.AddCollection(stream, out _);

    /// <inheritdoc/>
    public IEnumerable<FontFamily> AddCollection(Stream stream, out IEnumerable<FontDescription> descriptions)
        => this.AddCollectionImpl(stream, CultureInfo.InvariantCulture, out descriptions);

    /// <inheritdoc/>
    public FontFamily Get(string name)
        => this.GetByCulture(name, CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public bool TryGet(string name, out FontFamily family)
        => this.TryGetByCulture(name, CultureInfo.InvariantCulture, out family);

    /// <inheritdoc/>
    public FontFamily Add(string path, CultureInfo culture)
        => this.AddImpl(path, culture, out _);

    /// <inheritdoc/>
    public FontFamily Add(string path, CultureInfo culture, out FontDescription description)
        => this.AddImpl(path, culture, out description);

    /// <inheritdoc/>
    public FontFamily Add(Stream stream, CultureInfo culture)
        => this.AddImpl(stream, culture, out _);

    /// <inheritdoc/>
    public FontFamily Add(Stream stream, CultureInfo culture, out FontDescription description)
        => this.AddImpl(stream, culture, out description);

    /// <inheritdoc/>
    public IEnumerable<FontFamily> AddCollection(string path, CultureInfo culture)
        => this.AddCollection(path, culture, out _);

    /// <inheritdoc/>
    public IEnumerable<FontFamily> AddCollection(
        string path,
        CultureInfo culture,
        out IEnumerable<FontDescription> descriptions)
        => this.AddCollectionImpl(path, culture, out descriptions);

    /// <inheritdoc/>
    public IEnumerable<FontFamily> AddCollection(Stream stream, CultureInfo culture)
        => this.AddCollection(stream, culture, out _);

    /// <inheritdoc/>
    public IEnumerable<FontFamily> AddCollection(
        Stream stream,
        CultureInfo culture,
        out IEnumerable<FontDescription> descriptions)
        => this.AddCollectionImpl(stream, culture, out descriptions);

    /// <inheritdoc/>
    public IEnumerable<FontFamily> GetByCulture(CultureInfo culture)
        => this.FamiliesByCultureImpl(culture);

    /// <inheritdoc/>
    public FontFamily GetByCulture(string name, CultureInfo culture)
        => this.GetImpl(name, culture);

    /// <inheritdoc/>
    public bool TryGetByCulture(string name, CultureInfo culture, out FontFamily family)
        => this.TryGetImpl(name, culture, out family);

    /// <inheritdoc/>
    FontFamily IFontMetricsCollection.AddMetrics(FontMetrics metrics, CultureInfo culture)
    {
        ((IFontMetricsCollection)this).AddMetrics(metrics);
        return new FontFamily(metrics.Description.FontFamily(culture), this, culture);
    }

    /// <inheritdoc/>
    void IFontMetricsCollection.AddMetrics(FontMetrics metrics)
    {
        Guard.NotNull(metrics, nameof(metrics));

        if (metrics.Description is null)
        {
            throw new ArgumentException($"{nameof(FontMetrics)} must have a Description.", nameof(metrics));
        }

        lock (this.metricsCollection)
        {
            this.metricsCollection.Add(metrics);
        }
    }

    /// <inheritdoc/>
    bool IReadOnlyFontMetricsCollection.TryGetMetrics(string name, CultureInfo culture, FontStyle style, [NotNullWhen(true)] out FontMetrics? metrics)
    {
        metrics = ((IReadOnlyFontMetricsCollection)this).GetAllMetrics(name, culture)
            .FirstOrDefault(x => x.Description.Style == style);

        return metrics != null;
    }

    /// <inheritdoc/>
    IEnumerable<FontMetrics> IReadOnlyFontMetricsCollection.GetAllMetrics(string name, CultureInfo culture)
    {
        Guard.NotNull(name, nameof(name));
        StringComparer comparer = StringComparerHelpers.GetCaseInsensitiveStringComparer(culture);

        return this.metricsCollection
            .Where(x => comparer.Equals(x.Description.FontFamily(culture), name))
            .ToArray();
    }

    /// <inheritdoc/>
    IEnumerable<FontStyle> IReadOnlyFontMetricsCollection.GetAllStyles(string name, CultureInfo culture)
        => ((IReadOnlyFontMetricsCollection)this).GetAllMetrics(name, culture).Select(x => x.Description.Style).ToArray();

    /// <inheritdoc/>
    IEnumerator<FontMetrics> IReadOnlyFontMetricsCollection.GetEnumerator()
        => this.metricsCollection.GetEnumerator();

    internal void AddSearchDirectories(IEnumerable<string> directories)
    {
        foreach (string? directory in directories)
        {
            this.searchDirectories.Add(directory);
        }
    }

    private FontFamily AddImpl(string path, CultureInfo culture, out FontDescription description)
    {
        FileFontMetrics instance = new(path);
        description = instance.Description;
        return ((IFontMetricsCollection)this).AddMetrics(instance, culture);
    }

    private FontFamily AddImpl(Stream stream, CultureInfo culture, out FontDescription description)
    {
        StreamFontMetrics metrics = StreamFontMetrics.LoadFont(stream);
        description = metrics.Description;

        return ((IFontMetricsCollection)this).AddMetrics(metrics, culture);
    }

    private HashSet<FontFamily> AddCollectionImpl(
        string path,
        CultureInfo culture,
        out IEnumerable<FontDescription> descriptions)
    {
        FileFontMetrics[] fonts = FileFontMetrics.LoadFontCollection(path);

        FontDescription[] description = new FontDescription[fonts.Length];
        HashSet<FontFamily> families = [];
        for (int i = 0; i < fonts.Length; i++)
        {
            description[i] = fonts[i].Description;
            FontFamily family = ((IFontMetricsCollection)this).AddMetrics(fonts[i], culture);
            families.Add(family);
        }

        descriptions = description;
        return families;
    }

    private HashSet<FontFamily> AddCollectionImpl(
        Stream stream,
        CultureInfo culture,
        out IEnumerable<FontDescription> descriptions)
    {
        long startPos = stream.Position;
        using BigEndianBinaryReader reader = new(stream, true);
        TtcHeader ttcHeader = TtcHeader.Read(reader);
        List<FontDescription> result = new((int)ttcHeader.NumFonts);
        HashSet<FontFamily> installedFamilies = [];
        for (int i = 0; i < ttcHeader.NumFonts; ++i)
        {
            stream.Position = startPos + ttcHeader.OffsetTable[i];
            StreamFontMetrics instance = StreamFontMetrics.LoadFont(stream);
            installedFamilies.Add(((IFontMetricsCollection)this).AddMetrics(instance, culture));
            FontDescription fontDescription = instance.Description;
            result.Add(fontDescription);
        }

        descriptions = result;
        return installedFamilies;
    }

    private FontFamily[] FamiliesByCultureImpl(CultureInfo culture)
        => [.. this.metricsCollection
        .Select(x => x.Description.FontFamily(culture))
        .Distinct()
        .Select(x => new FontFamily(x, this, culture))];

    private bool TryGetImpl(string name, CultureInfo culture, out FontFamily family)
    {
        Guard.NotNull(name, nameof(name));
        StringComparer comparer = StringComparerHelpers.GetCaseInsensitiveStringComparer(culture);

        string? match = this.metricsCollection
            .Select(x => x.Description.FontFamily(culture))
            .FirstOrDefault(x => comparer.Equals(name, x));

        if (match != null)
        {
            family = new FontFamily(match, this, culture);
            return true;
        }

        family = default;
        return false;
    }

    private FontFamily GetImpl(string name, CultureInfo culture)
    {
        if (this.TryGetImpl(name, culture, out FontFamily family))
        {
            return family;
        }

        throw new FontFamilyNotFoundException(name, this.searchDirectories);
    }
}
