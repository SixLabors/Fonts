// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace SixLabors.Fonts;

/// <summary>
/// Represents a collection of font families.
/// </summary>
public sealed class FontCollection : IFontCollection, IFontMetricsCollection
{
    private readonly HashSet<string> searchDirectories = [];
    private readonly HashSet<FontCollectionEntry> metricsCollection = [];

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
    public ReadOnlyMemory<FontFamily> AddCollection(string path)
        => this.AddCollection(path, out _);

    /// <inheritdoc/>
    public ReadOnlyMemory<FontFamily> AddCollection(string path, out ReadOnlyMemory<FontDescription> descriptions)
        => this.AddCollectionImpl(path, CultureInfo.InvariantCulture, out descriptions);

    /// <inheritdoc/>
    public ReadOnlyMemory<FontFamily> AddCollection(Stream stream)
        => this.AddCollection(stream, out _);

    /// <inheritdoc/>
    public ReadOnlyMemory<FontFamily> AddCollection(Stream stream, out ReadOnlyMemory<FontDescription> descriptions)
        => this.AddCollectionImpl(stream, CultureInfo.InvariantCulture, out descriptions);

    /// <inheritdoc/>
    public FontFamily Get(string name)
        => this.GetByCulture(name, CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public bool TryGet(string name, out FontFamily family)
        => this.TryGetByCulture(name, CultureInfo.InvariantCulture, out family);

    /// <inheritdoc/>
    public FontFamily AddWithCulture(string path, CultureInfo culture)
        => this.AddImpl(path, culture, out _);

    /// <inheritdoc/>
    public FontFamily AddWithCulture(string path, CultureInfo culture, out FontDescription description)
        => this.AddImpl(path, culture, out description);

    /// <inheritdoc/>
    public FontFamily AddWithCulture(Stream stream, CultureInfo culture)
        => this.AddImpl(stream, culture, out _);

    /// <inheritdoc/>
    public FontFamily AddWithCulture(Stream stream, CultureInfo culture, out FontDescription description)
        => this.AddImpl(stream, culture, out description);

    /// <inheritdoc/>
    public ReadOnlyMemory<FontFamily> AddCollection(string path, CultureInfo culture)
        => this.AddCollection(path, culture, out _);

    /// <inheritdoc/>
    public ReadOnlyMemory<FontFamily> AddCollection(
        string path,
        CultureInfo culture,
        out ReadOnlyMemory<FontDescription> descriptions)
        => this.AddCollectionImpl(path, culture, out descriptions);

    /// <inheritdoc/>
    public ReadOnlyMemory<FontFamily> AddCollection(Stream stream, CultureInfo culture)
        => this.AddCollection(stream, culture, out _);

    /// <inheritdoc/>
    public ReadOnlyMemory<FontFamily> AddCollection(
        Stream stream,
        CultureInfo culture,
        out ReadOnlyMemory<FontDescription> descriptions)
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
        => this.AddMetrics(metrics, familyName: null);

    /// <inheritdoc/>
    void IFontMetricsCollection.AddMetrics(FontMetrics metrics, string familyName)
    {
        Guard.NotNull(familyName, nameof(familyName));
        this.AddMetrics(metrics, familyName, style: null);
    }

    /// <inheritdoc/>
    void IFontMetricsCollection.AddMetrics(FontMetrics metrics, string familyName, FontStyle style)
    {
        Guard.NotNull(familyName, nameof(familyName));
        this.AddMetrics(metrics, familyName, style);
    }

    /// <summary>
    /// Adds the font metrics to this collection.
    /// </summary>
    /// <param name="metrics">The font metrics to add.</param>
    /// <param name="familyName">The explicit family name to use for collection lookups, or <see langword="null"/> to use the font description.</param>
    /// <param name="style">The explicit style to use for collection lookups, or <see langword="null"/> to use the font description.</param>
    private void AddMetrics(FontMetrics metrics, string? familyName, FontStyle? style = null)
    {
        Guard.NotNull(metrics, nameof(metrics));

        if (metrics.Description is null)
        {
            throw new ArgumentException($"{nameof(FontMetrics)} must have a Description.", nameof(metrics));
        }

        lock (this.metricsCollection)
        {
            this.metricsCollection.Add(new FontCollectionEntry(metrics, familyName, style));
        }
    }

    /// <inheritdoc/>
    bool IReadOnlyFontMetricsCollection.TryGetMetrics(string name, CultureInfo culture, FontStyle style, [NotNullWhen(true)] out FontMetrics? metrics)
    {
        Guard.NotNull(name, nameof(name));
        StringComparer comparer = StringComparerHelpers.GetCaseInsensitiveStringComparer(culture);

        foreach (FontCollectionEntry entry in this.metricsCollection)
        {
            if (entry.GetStyle() == style && comparer.Equals(entry.GetFamilyName(culture), name))
            {
                metrics = entry.Metrics;
                return true;
            }
        }

        metrics = null;
        return false;
    }

    /// <inheritdoc/>
    IEnumerable<FontMetrics> IReadOnlyFontMetricsCollection.GetAllMetrics(string name, CultureInfo culture)
    {
        Guard.NotNull(name, nameof(name));
        StringComparer comparer = StringComparerHelpers.GetCaseInsensitiveStringComparer(culture);

        return this.metricsCollection
            .Where(x => comparer.Equals(x.GetFamilyName(culture), name))
            .Select(x => x.Metrics)
            .ToArray();
    }

    /// <inheritdoc/>
    ReadOnlyMemory<FontStyle> IReadOnlyFontMetricsCollection.GetAllStyles(string name, CultureInfo culture)
    {
        Guard.NotNull(name, nameof(name));
        StringComparer comparer = StringComparerHelpers.GetCaseInsensitiveStringComparer(culture);

        return this.metricsCollection
            .Where(x => comparer.Equals(x.GetFamilyName(culture), name))
            .Select(x => x.GetStyle())
            .ToArray();
    }

    /// <inheritdoc/>
    IEnumerator<FontMetrics> IReadOnlyFontMetricsCollection.GetEnumerator()
        => this.metricsCollection.Select(x => x.Metrics).GetEnumerator();

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
        MemoryFontMetrics metrics = new(stream);
        description = metrics.Description;

        return ((IFontMetricsCollection)this).AddMetrics(metrics, culture);
    }

    private ReadOnlyMemory<FontFamily> AddCollectionImpl(
        string path,
        CultureInfo culture,
        out ReadOnlyMemory<FontDescription> descriptions)
    {
        ReadOnlyMemory<FileFontMetrics> fontMetrics = FileFontMetrics.LoadFontCollection(path);
        ReadOnlySpan<FileFontMetrics> fonts = fontMetrics.Span;

        FontDescription[] description = new FontDescription[fonts.Length];
        FontFamily[] families = new FontFamily[fonts.Length];
        int familyCount = 0;
        for (int i = 0; i < fonts.Length; i++)
        {
            description[i] = fonts[i].Description;
            FontFamily family = ((IFontMetricsCollection)this).AddMetrics(fonts[i], culture);

            if (!families.AsSpan(0, familyCount).Contains(family))
            {
                families[familyCount++] = family;
            }
        }

        descriptions = description;
        return new ReadOnlyMemory<FontFamily>(families, 0, familyCount);
    }

    private ReadOnlyMemory<FontFamily> AddCollectionImpl(
        Stream stream,
        CultureInfo culture,
        out ReadOnlyMemory<FontDescription> descriptions)
    {
        ReadOnlyMemory<MemoryFontMetrics> fontMetrics = MemoryFontMetrics.LoadFontCollection(stream);
        ReadOnlySpan<MemoryFontMetrics> fonts = fontMetrics.Span;

        FontDescription[] result = new FontDescription[fonts.Length];
        FontFamily[] installedFamilies = new FontFamily[fonts.Length];
        int familyCount = 0;
        for (int i = 0; i < fonts.Length; ++i)
        {
            result[i] = fonts[i].Description;
            FontFamily family = ((IFontMetricsCollection)this).AddMetrics(fonts[i], culture);

            if (!installedFamilies.AsSpan(0, familyCount).Contains(family))
            {
                installedFamilies[familyCount++] = family;
            }
        }

        descriptions = result;
        return new ReadOnlyMemory<FontFamily>(installedFamilies, 0, familyCount);
    }

    private FontFamily[] FamiliesByCultureImpl(CultureInfo culture)
        => [.. this.metricsCollection
        .Select(x => x.GetFamilyName(culture))
        .Distinct()
        .Select(x => new FontFamily(x, this, culture))];

    private bool TryGetImpl(string name, CultureInfo culture, out FontFamily family)
    {
        Guard.NotNull(name, nameof(name));
        StringComparer comparer = StringComparerHelpers.GetCaseInsensitiveStringComparer(culture);

        string? match = this.metricsCollection
            .Select(x => x.GetFamilyName(culture))
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

    /// <summary>
    /// Stores font metrics with an optional collection family name override.
    /// </summary>
    private readonly struct FontCollectionEntry : IEquatable<FontCollectionEntry>
    {
        private readonly string? familyName;
        private readonly FontStyle? style;

        /// <summary>
        /// Initializes a new instance of the <see cref="FontCollectionEntry"/> struct.
        /// </summary>
        /// <param name="metrics">The font metrics.</param>
        /// <param name="familyName">The explicit family name, or <see langword="null"/> to use the font description.</param>
        /// <param name="style">The explicit style, or <see langword="null"/> to use the font description.</param>
        public FontCollectionEntry(FontMetrics metrics, string? familyName, FontStyle? style)
        {
            this.Metrics = metrics;
            this.familyName = familyName;
            this.style = style;
        }

        /// <summary>
        /// Gets the font metrics.
        /// </summary>
        public FontMetrics Metrics { get; }

        /// <summary>
        /// Gets the family name to use for collection lookups.
        /// </summary>
        /// <param name="culture">The culture used to read font-description family names.</param>
        /// <returns>The collection family name.</returns>
        public string GetFamilyName(CultureInfo culture)
            => this.familyName ?? this.Metrics.Description.FontFamily(culture);

        /// <summary>
        /// Gets the style to use for collection lookups.
        /// </summary>
        /// <returns>The collection style.</returns>
        public FontStyle GetStyle()
            => this.style ?? this.Metrics.Description.Style;

        /// <inheritdoc/>
        public bool Equals(FontCollectionEntry other)
            => EqualityComparer<FontMetrics>.Default.Equals(this.Metrics, other.Metrics)
            && StringComparer.Ordinal.Equals(this.familyName, other.familyName)
            && this.style == other.style;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
            => obj is FontCollectionEntry entry && this.Equals(entry);

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(this.Metrics, this.familyName, this.style);
    }
}
