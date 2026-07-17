// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts;

/// <summary>
/// Provides a collection of fonts.
/// </summary>
internal sealed class SystemFontCollection : IReadOnlySystemFontCollection, IReadOnlyFontMetricsCollection
{
    private readonly Lazy<string[]> familyNames;
    private readonly Lazy<Native.NativeSystemFontFace[]> familyFaces;
    private readonly Lazy<string[]> fontPaths;
    private readonly Lazy<string[]> searchDirectories;
    private readonly Dictionary<string, SystemFontFamilyMetrics[]> familyMetrics = new(StringComparer.OrdinalIgnoreCase);
    private readonly object familyMetricsLock = new();

    /// <summary>
    /// Name IDs that identify the family grouping exposed by font platforms.
    /// </summary>
    private static readonly KnownNameIds[] FamilyNameIds =
    [
        KnownNameIds.FontFamilyName,
        KnownNameIds.TypographicFamilyName,
        KnownNameIds.WwsFamilyName,
    ];

    /// <summary>
    /// Gets the default set of locations we probe for System Fonts.
    /// </summary>
    private static readonly IReadOnlyCollection<string> StandardFontLocations;

    static SystemFontCollection()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            StandardFontLocations = new[]
            {
                @"%SYSTEMROOT%\Fonts",
                @"%APPDATA%\Microsoft\Windows\Fonts",
                @"%LOCALAPPDATA%\Microsoft\Windows\Fonts",
            };
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            StandardFontLocations = new[]
            {
                "%HOME%/.fonts/",
                "%HOME%/.local/share/fonts/",
                "/usr/local/share/fonts/",
                "/usr/share/fonts/",
            };
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            StandardFontLocations = new[]
            {
                // As documented on "Mac OS X: Font locations and their purposes"
                // https://web.archive.org/web/20191015122508/https://support.apple.com/en-us/HT201722
                "%HOME%/Library/Fonts/",
                "/Library/Fonts/",
                "/System/Library/Fonts/",
                "/Network/Library/Fonts/",
            };
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("Android")))
        {
            StandardFontLocations = new[]
            {
                "/system/fonts/"
            };
        }
        else
        {
            StandardFontLocations = Array.Empty<string>();
        }
    }

    public SystemFontCollection()
    {
        this.familyNames = new Lazy<string[]>(this.GetSystemFamilyNames, true);
        this.familyFaces = new Lazy<Native.NativeSystemFontFace[]>(this.GetSystemFamilyFaces, true);
        this.fontPaths = new Lazy<string[]>(this.GetFontPaths, true);
        this.searchDirectories = new Lazy<string[]>(this.GetSearchDirectories, true);
    }

    /// <inheritdoc/>
    public IEnumerable<FontFamily> Families
    {
        get
        {
            string[] names = this.familyNames.Value;
            FontFamily[] families = new FontFamily[names.Length];

            for (int i = 0; i < names.Length; i++)
            {
                families[i] = new FontFamily(names[i], this, CultureInfo.InvariantCulture);
            }

            return families;
        }
    }

    /// <inheritdoc/>
    public IEnumerable<string> SearchDirectories => this.searchDirectories.Value;

    /// <inheritdoc/>
    public FontFamily Get(string name) => this.GetByCulture(name, CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public bool TryGet(string name, out FontFamily family)
        => this.TryGetByCulture(name, CultureInfo.InvariantCulture, out family);

    /// <inheritdoc/>
    public IEnumerable<FontFamily> GetByCulture(CultureInfo culture)
    {
        string[] names = this.familyNames.Value;
        FontFamily[] families = new FontFamily[names.Length];

        for (int i = 0; i < names.Length; i++)
        {
            families[i] = new FontFamily(names[i], this, culture);
        }

        return families;
    }

    /// <inheritdoc/>
    public FontFamily GetByCulture(string name, CultureInfo culture)
    {
        if (this.TryGetByCulture(name, culture, out FontFamily family))
        {
            return family;
        }

        throw new FontFamilyNotFoundException(name, this.searchDirectories.Value);
    }

    /// <inheritdoc/>
    public bool TryGetByCulture(string name, CultureInfo culture, out FontFamily family)
    {
        Guard.NotNull(name, nameof(name));

        if (!this.TryGetKnownFamilyName(name, culture, out string familyName))
        {
            family = default;
            return false;
        }

        SystemFontFamilyMetrics[] metrics = this.GetOrLoadFamilyMetrics(familyName, culture);
        if (metrics.Length == 0)
        {
            family = default;
            return false;
        }

        family = new FontFamily(familyName, this, culture);
        return true;
    }

    /// <inheritdoc/>
    public bool TryMatchCharacter(
        CodePoint codePoint,
        FontStyle style,
        string? familyName,
        CultureInfo? culture,
        out FontMatch match)
    {
        if (Native.SystemFontMatcher.TryMatchCharacter(codePoint, style, familyName, culture, out string? matchedFamilyName, out FontStyle matchedStyle)
            && matchedFamilyName is not null
            && this.TryGetMatchedFamily(matchedFamilyName, culture, out FontFamily family))
        {
            if (!family.TryGetMetrics(matchedStyle, out _))
            {
                matchedStyle = family.TryGetMetrics(style, out _)
                    ? style
                    : family.GetAvailableStyles().Span[0];
            }

            match = new FontMatch(family, matchedStyle);
            return true;
        }

        match = default;
        return false;
    }

    /// <inheritdoc/>
    bool IReadOnlyFontMetricsCollection.TryGetMetrics(string name, CultureInfo culture, FontStyle style, [NotNullWhen(true)] out FontMetrics? metrics)
    {
        SystemFontFamilyMetrics[] familyMetrics = this.GetOrLoadFamilyMetrics(name, culture);

        foreach (SystemFontFamilyMetrics fontMetrics in familyMetrics)
        {
            if (fontMetrics.Style == style)
            {
                metrics = fontMetrics.Metrics;
                return true;
            }
        }

        metrics = null;
        return false;
    }

    /// <inheritdoc/>
    IEnumerable<FontMetrics> IReadOnlyFontMetricsCollection.GetAllMetrics(string name, CultureInfo culture)
        => this.GetOrLoadFamilyMetrics(name, culture).Select(x => x.Metrics).ToArray();

    /// <inheritdoc/>
    ReadOnlyMemory<FontStyle> IReadOnlyFontMetricsCollection.GetAllStyles(string name, CultureInfo culture)
    {
        SystemFontFamilyMetrics[] familyMetrics = this.GetOrLoadFamilyMetrics(name, culture);
        FontStyle[] styles = new FontStyle[familyMetrics.Length];

        for (int i = 0; i < familyMetrics.Length; i++)
        {
            styles[i] = familyMetrics[i].Style;
        }

        return styles;
    }

    /// <inheritdoc/>
    IEnumerator<FontMetrics> IReadOnlyFontMetricsCollection.GetEnumerator()
    {
        foreach (string familyName in this.familyNames.Value)
        {
            foreach (SystemFontFamilyMetrics metrics in this.GetOrLoadFamilyMetrics(familyName, CultureInfo.InvariantCulture))
            {
                yield return metrics.Metrics;
            }
        }
    }

    /// <summary>
    /// Gets all system font metrics with their platform family names.
    /// </summary>
    /// <returns>The system font family metrics.</returns>
    internal IEnumerable<SystemFontFamilyMetrics> GetAllFamilyMetrics()
    {
        foreach (string familyName in this.familyNames.Value)
        {
            foreach (SystemFontFamilyMetrics metrics in this.GetOrLoadFamilyMetrics(familyName, CultureInfo.InvariantCulture))
            {
                yield return metrics;
            }
        }
    }

    /// <inheritdoc/>
    bool IReadOnlyFontMetricsCollection.TryGetMetrics(
        string name,
        CultureInfo culture,
        FontStyle style,
        FontWeight weight,
        [NotNullWhen(true)] out FontMetrics? metrics)
    {
        bool italic = (style & FontStyle.Italic) == FontStyle.Italic;

        return TryFindWeight(this.GetOrLoadFamilyMetrics(name, culture), italic, weight, out metrics);
    }

    /// <summary>
    /// Attempts to resolve a native fallback family name using the requested culture before falling back to invariant lookup.
    /// </summary>
    /// <param name="familyName">The family name returned by the native matcher.</param>
    /// <param name="culture">The requested culture, or <see langword="null"/> to use invariant lookup.</param>
    /// <param name="family">The matched family.</param>
    /// <returns><see langword="true"/> when the family is known and its metrics can be loaded.</returns>
    private bool TryGetMatchedFamily(string familyName, CultureInfo? culture, out FontFamily family)
    {
        if (culture is not null && this.TryGetByCulture(familyName, culture, out family))
        {
            return true;
        }

        if (this.TryGet(familyName, out family))
        {
            return true;
        }

        return this.TryGetByCulture(familyName, CultureInfo.InvariantCulture, out family);
    }

    /// <summary>
    /// Gets the metrics for a single family, loading them from disk the first time that family is requested.
    /// </summary>
    /// <param name="name">The family name.</param>
    /// <param name="culture">The culture used to compare localized family names.</param>
    /// <returns>The metrics belonging to the requested family.</returns>
    private SystemFontFamilyMetrics[] GetOrLoadFamilyMetrics(string name, CultureInfo culture)
    {
        string key = string.Concat(culture.Name, '\0', name);

        lock (this.familyMetricsLock)
        {
            if (this.familyMetrics.TryGetValue(key, out SystemFontFamilyMetrics[]? metrics))
            {
                return metrics;
            }

            metrics = this.LoadFamilyMetrics(name, culture);
            this.familyMetrics.Add(key, metrics);
            return metrics;
        }
    }

    /// <summary>
    /// Loads the metrics for one family from the system font files.
    /// </summary>
    /// <param name="name">The family name.</param>
    /// <param name="culture">The culture used to compare localized family names.</param>
    /// <returns>The metrics belonging to the requested family.</returns>
    private SystemFontFamilyMetrics[] LoadFamilyMetrics(string name, CultureInfo culture)
    {
        Native.NativeSystemFontFace[] nativeFaces = this.familyFaces.Value;
        if (nativeFaces.Length > 0)
        {
            return LoadNativeFamilyMetrics(name, culture, nativeFaces);
        }

        List<SystemFontFamilyMetrics> metrics = [];
        StringComparer comparer = StringComparerHelpers.GetCaseInsensitiveStringComparer(culture);

        foreach (string path in this.fontPaths.Value)
        {
            try
            {
                if (IsFontCollectionPath(path))
                {
                    foreach (FileFontMetrics fontMetrics in FileFontMetrics.LoadFontCollection(path).Span)
                    {
                        if (MatchesFamilyName(fontMetrics.Description, name, culture, comparer))
                        {
                            metrics.Add(new SystemFontFamilyMetrics(name, fontMetrics.Description.Style, fontMetrics));
                        }
                    }
                }
                else
                {
                    FileFontMetrics fontMetrics = new(path);
                    if (MatchesFamilyName(fontMetrics.Description, name, culture, comparer))
                    {
                        metrics.Add(new SystemFontFamilyMetrics(name, fontMetrics.Description.Style, fontMetrics));
                    }
                }
            }
            catch
            {
                // We swallow exceptions installing system fonts as we hold no guarantees about permissions etc.
            }
        }

        return [.. metrics];
    }

    /// <summary>
    /// Loads metrics for one native system family from resolved platform font faces.
    /// </summary>
    /// <param name="name">The family name.</param>
    /// <param name="culture">The culture used to compare localized family names.</param>
    /// <param name="nativeFaces">The resolved native font faces.</param>
    /// <returns>The metrics belonging to the requested family.</returns>
    private static SystemFontFamilyMetrics[] LoadNativeFamilyMetrics(string name, CultureInfo culture, Native.NativeSystemFontFace[] nativeFaces)
    {
        List<Native.NativeSystemFontFace> familyFaces = [];
        StringComparer comparer = StringComparerHelpers.GetCaseInsensitiveStringComparer(culture);

        foreach (Native.NativeSystemFontFace face in nativeFaces)
        {
            if (comparer.Equals(face.FamilyName, name))
            {
                familyFaces.Add(face);
            }
        }

        // Style-only lookup returns the first face in its FontStyle bucket. Keep every numeric
        // weight in the family, but place the platform's closest 400/700 face first so adding
        // numeric lookup does not change the established regular, bold, italic, or bold-italic face.
        familyFaces.Sort(static (left, right) =>
        {
            int styleComparison = left.Style.CompareTo(right.Style);
            if (styleComparison != 0)
            {
                return styleComparison;
            }

            return left.StyleScore.CompareTo(right.StyleScore);
        });

        List<SystemFontFamilyMetrics> metrics = [];
        foreach (Native.NativeSystemFontFace face in familyFaces)
        {
            AddNativeFamilyMetrics(metrics, face);
        }

        return [.. metrics];
    }

    /// <summary>
    /// Finds the browser-preferred face for an OpenType weight and slant.
    /// </summary>
    /// <param name="familyMetrics">The OS-enumerated faces in the family.</param>
    /// <param name="italic">Whether an italic or oblique face is required.</param>
    /// <param name="weight">The requested OpenType weight.</param>
    /// <param name="metrics">The matching metrics.</param>
    /// <returns><see langword="true"/> when a face with the requested slant is available.</returns>
    private static bool TryFindWeight(
        SystemFontFamilyMetrics[] familyMetrics,
        bool italic,
        FontWeight weight,
        [NotNullWhen(true)] out FontMetrics? metrics)
    {
        int requestedWeight = (int)weight;
        int bestDistance = int.MaxValue;
        int bestWeight = int.MinValue;
        FontMetrics? bestMatch = null;

        foreach (SystemFontFamilyMetrics candidate in familyMetrics)
        {
            bool candidateItalic = (candidate.Style & FontStyle.Italic) == FontStyle.Italic;
            if (candidateItalic != italic)
            {
                continue;
            }

            int candidateWeight = (int)candidate.Metrics.Description.Weight;
            int distance = Math.Abs(candidateWeight - requestedWeight);

            if (distance == 0)
            {
                metrics = candidate.Metrics;
                return true;
            }

            // Windows system-family selection asks DirectWrite for the face that best matches the
            // requested numeric weight:
            // https://learn.microsoft.com/windows/win32/api/dwrite/nf-dwrite-idwritefontfamily-getfirstmatchingfont
            // Select the nearest installed weight and prefer the heavier face when two weights are
            // equally distant. The tie rule reproduces Windows browser selection for Segoe UI 500,
            // which resolves to Semibold 600 rather than Regular 400.
            if (distance < bestDistance || (distance == bestDistance && candidateWeight > bestWeight))
            {
                bestDistance = distance;
                bestWeight = candidateWeight;
                bestMatch = candidate.Metrics;
            }
        }

        metrics = bestMatch;
        return metrics is not null;
    }

    /// <summary>
    /// Adds metrics for a resolved native face.
    /// </summary>
    /// <param name="metrics">The metrics collection to update.</param>
    /// <param name="face">The native face to load.</param>
    private static void AddNativeFamilyMetrics(List<SystemFontFamilyMetrics> metrics, Native.NativeSystemFontFace face)
    {
        try
        {
            if (TryLoadFontFace(face.Path, face.FaceIndex, out FileFontMetrics? fontMetrics))
            {
                metrics.Add(new SystemFontFamilyMetrics(face.FamilyName, face.Style, fontMetrics));
            }
        }
        catch
        {
            // We swallow exceptions installing system fonts as we hold no guarantees about permissions etc.
        }
    }

    /// <summary>
    /// Attempts to resolve a requested name against the native family-name list.
    /// </summary>
    /// <param name="name">The requested family name.</param>
    /// <param name="culture">The culture used to compare localized family names.</param>
    /// <param name="familyName">The known family name to use for metric loading.</param>
    /// <returns><see langword="true"/> when the name is known, or when no native family-name list is available.</returns>
    private bool TryGetKnownFamilyName(string name, CultureInfo culture, out string familyName)
    {
        string[] names = this.familyNames.Value;
        if (names.Length == 0)
        {
            familyName = name;
            return true;
        }

        StringComparer comparer = StringComparerHelpers.GetCaseInsensitiveStringComparer(culture);

        foreach (string knownFamilyName in names)
        {
            if (comparer.Equals(knownFamilyName, name))
            {
                familyName = knownFamilyName;
                return true;
            }
        }

        familyName = string.Empty;
        return false;
    }

    /// <summary>
    /// Gets the installed family names from the native matcher, falling back to font-file probing when native names are unavailable.
    /// </summary>
    /// <returns>The installed system font family names.</returns>
    private string[] GetSystemFamilyNames()
    {
        Native.NativeSystemFontFace[] nativeFaces = this.familyFaces.Value;
        if (nativeFaces.Length > 0)
        {
            HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);

            foreach (Native.NativeSystemFontFace face in nativeFaces)
            {
                _ = names.Add(face.FamilyName);
            }

            return [.. names];
        }

        return this.GetFamilyNamesFromFontPaths();
    }

    /// <summary>
    /// Gets the installed system font faces from the native matcher.
    /// </summary>
    /// <returns>The resolved native font faces, or an empty array when native face mapping is unavailable.</returns>
    private Native.NativeSystemFontFace[] GetSystemFamilyFaces()
        => Native.SystemFontMatcher.TryGetFamilyFaces(false, out Native.NativeSystemFontFace[]? faces)
            ? faces
            : [];

    /// <summary>
    /// Loads family names by probing font descriptions from the system font files.
    /// </summary>
    /// <returns>The installed system font family names.</returns>
    private string[] GetFamilyNamesFromFontPaths()
    {
        HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);

        foreach (string path in this.fontPaths.Value)
        {
            try
            {
                if (IsFontCollectionPath(path))
                {
                    foreach (FileFontMetrics fontMetrics in FileFontMetrics.LoadFontCollection(path).Span)
                    {
                        AddFamilyNames(names, fontMetrics.Description);
                    }
                }
                else
                {
                    FileFontMetrics fontMetrics = new(path);
                    AddFamilyNames(names, fontMetrics.Description);
                }
            }
            catch
            {
                // We swallow exceptions installing system fonts as we hold no guarantees about permissions etc.
            }
        }

        return [.. names];
    }

    /// <summary>
    /// Adds every family-grouping name exposed by the font description.
    /// </summary>
    /// <param name="names">The family names.</param>
    /// <param name="description">The font description.</param>
    private static void AddFamilyNames(HashSet<string> names, FontDescription description)
    {
        foreach (KnownNameIds nameId in FamilyNameIds)
        {
            string familyName = description.GetNameById(CultureInfo.InvariantCulture, nameId);
            if (familyName.Length > 0)
            {
                _ = names.Add(familyName);
            }
        }
    }

    /// <summary>
    /// Returns whether the description exposes the requested family name through any family-grouping name ID.
    /// </summary>
    /// <param name="description">The font description.</param>
    /// <param name="name">The requested family name.</param>
    /// <param name="culture">The culture used to compare localized family names.</param>
    /// <param name="comparer">The string comparer.</param>
    /// <returns><see langword="true"/> when the description contains the requested family name.</returns>
    private static bool MatchesFamilyName(FontDescription description, string name, CultureInfo culture, StringComparer comparer)
    {
        foreach (KnownNameIds nameId in FamilyNameIds)
        {
            if (comparer.Equals(description.GetNameById(culture, nameId), name))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the system font file paths used for lazy family metric loading.
    /// </summary>
    /// <returns>The installed system font file paths.</returns>
    private string[] GetFontPaths()
    {
        Native.NativeSystemFontFace[] nativeFaces = this.familyFaces.Value;
        if (nativeFaces.Length > 0)
        {
            HashSet<string> paths = [];

            foreach (Native.NativeSystemFontFace face in nativeFaces)
            {
                _ = paths.Add(face.Path);
            }

            return [.. paths];
        }

        // We do this to provide a consistent experience with case sensitive file systems.
        return [.. this.searchDirectories.Value
            .SelectMany(x => Directory.EnumerateFiles(x, "*.*", SearchOption.AllDirectories))
            .Where(IsSupportedFontFile)];
    }

    /// <summary>
    /// Gets the system font directories used for directory-based probing.
    /// </summary>
    /// <returns>The existing system font directories.</returns>
    private string[] GetSearchDirectories()
    {
        Native.NativeSystemFontFace[] nativeFaces = this.familyFaces.Value;
        if (nativeFaces.Length > 0)
        {
            HashSet<string> directories = new(StringComparer.OrdinalIgnoreCase);

            foreach (Native.NativeSystemFontFace face in nativeFaces)
            {
                string? directory = Path.GetDirectoryName(face.Path);
                if (directory is { Length: > 0 })
                {
                    _ = directories.Add(directory);
                }
            }

            return [.. directories];
        }

        return GetStandardSearchDirectories();
    }

    /// <summary>
    /// Gets the existing fallback directories used for font-file probing.
    /// </summary>
    /// <returns>The existing fallback directories.</returns>
    private static string[] GetStandardSearchDirectories()
    {
        string[] expanded = [.. StandardFontLocations.Select(Environment.ExpandEnvironmentVariables)];
        return [.. expanded.Where(x => Directory.Exists(x))];
    }

    /// <summary>
    /// Returns whether the path uses a supported system font file extension.
    /// </summary>
    /// <param name="path">The path to inspect.</param>
    /// <returns><see langword="true"/> when the file extension is supported.</returns>
    private static bool IsSupportedFontFile(string path)
    {
        string extension = Path.GetExtension(path);
        return extension.Equals(".ttf", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".ttc", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".otc", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".otf", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets whether a path identifies an OpenType font collection.
    /// </summary>
    /// <param name="path">The font file path.</param>
    /// <returns><see langword="true"/> for TTC and OTC files; otherwise, <see langword="false"/>.</returns>
    private static bool IsFontCollectionPath(string path)
    {
        string extension = Path.GetExtension(path);
        return extension.Equals(".ttc", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".otc", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tries to load a font face by file path and collection face index.
    /// </summary>
    /// <param name="path">The font file path.</param>
    /// <param name="faceIndex">The zero-based face index within the font file.</param>
    /// <param name="metrics">The font metrics.</param>
    /// <returns><see langword="true"/> if the font face was loaded; otherwise, <see langword="false"/>.</returns>
    private static bool TryLoadFontFace(string path, int faceIndex, [NotNullWhen(true)] out FileFontMetrics? metrics)
    {
        if (faceIndex == 0 && !IsFontCollectionPath(path))
        {
            metrics = new FileFontMetrics(path);
            return true;
        }

        ReadOnlyMemory<FileFontMetrics> collection = FileFontMetrics.LoadFontCollection(path);
        if ((uint)faceIndex < (uint)collection.Length)
        {
            metrics = collection.Span[faceIndex];
            return true;
        }

        metrics = null;
        return false;
    }
}
