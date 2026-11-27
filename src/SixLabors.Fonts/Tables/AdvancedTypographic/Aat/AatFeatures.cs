// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Aat;

/// <summary>
/// Resolves the Apple feature selectors requested through OpenType feature tags
/// and the font's language-tag table.
/// </summary>
internal sealed class AatFeatures
{
    private readonly Dictionary<ushort, bool> features;
    private readonly (ushort Offset, ushort Length)[] languages;
    private readonly byte[] languageData;

    /// <summary>
    /// Initializes a new instance of the <see cref="AatFeatures"/> class.
    /// </summary>
    /// <param name="features">The feature types and their exclusivity flags.</param>
    /// <param name="languages">The language tag ranges and bytes.</param>
    private AatFeatures(Dictionary<ushort, bool> features, ((ushort Offset, ushort Length)[] Tags, byte[] Data) languages)
    {
        this.features = features;
        (this.languages, this.languageData) = languages;
    }

    /// <summary>
    /// Loads optional feature and language metadata for the substitution tables.
    /// </summary>
    /// <param name="reader">The font reader.</param>
    /// <returns>The feature types and language tags in the font.</returns>
    public static AatFeatures Load(FontReader reader) => new(LoadFeatureTypes(reader), LoadLanguages(reader));

    /// <summary>
    /// Determines whether the run requests the given AAT feature selector.
    /// </summary>
    /// <param name="feature">The AAT feature type.</param>
    /// <param name="selector">The AAT feature selector.</param>
    /// <param name="featureTags">The OpenType feature tags that the run requests.</param>
    /// <param name="language">The language tag of the run.</param>
    /// <param name="vertical">Whether the run uses vertical features.</param>
    /// <returns><see langword="true"/> if the selector applies to the run.</returns>
    public bool IsEnabled(ushort feature, ushort selector, IReadOnlyList<Tag> featureTags, string language, bool vertical)
    {
        // Language selectors start at 1. Match a complete language subtag, so
        // en matches en-US but cannot match an unrelated longer language code.
        if (feature == 39)
        {
            int index = selector - 1;
            if ((uint)index >= (uint)this.languages.Length)
            {
                return false;
            }

            (ushort offset, ushort length) = this.languages[index];
            return MatchesLanguage(this.languageData.AsSpan(offset, length), language);
        }

        // Use vertical forms for an upright run.
        if (vertical && feature == 4 && selector == 0)
        {
            return true;
        }

        // Translate the deprecated Letter Case small caps selector if the font
        // advertises this older feature in place of the Lower Case feature.
        ushort exposed = feature;
        if (feature == 3 && selector == 3)
        {
            feature = 37;
            selector = 1;
        }

        if (!this.features.TryGetValue(exposed, out bool exclusive))
        {
            return false;
        }

        for (int i = featureTags.Count - 1; i >= 0; i--)
        {
            (ushort type, ushort setting) = MapFeature(featureTags[i]);
            if (type == feature && (exclusive || setting == selector))
            {
                // The last request for an exclusive feature wins.
                return setting == selector;
            }
        }

        return false;
    }

    /// <summary>
    /// Reads the feature type and exclusivity fields of each feature-name record.
    /// </summary>
    /// <param name="fontReader">The font reader.</param>
    /// <returns>The feature types and their exclusivity flags.</returns>
    private static Dictionary<ushort, bool> LoadFeatureTypes(FontReader fontReader)
    {
        Dictionary<ushort, bool> result = [];
        if (!fontReader.TryGetBoundedReaderAtTablePosition("feat", out BigEndianBinaryReader? reader))
        {
            return result;
        }

        using (reader)
        {
            uint tableLength = (uint)reader.BaseStream.Length;

            // +---------------+--------------+------------------------------------+
            // | Type          | Name         | Description                        |
            // +---------------+--------------+------------------------------------+
            // | Fixed         | version      | 1.0.                               |
            // | uint16        | featureCount | Number of feature-name records.    |
            // | uint16        | reserved1    | Reserved.                          |
            // | uint32        | reserved2    | Reserved.                          |
            // | FeatureName[] | features     | Records for exposed feature types. |
            // +---------------+--------------+------------------------------------+
            // The 'feat' table uses a 16.16 version field. Version 1.0 sets bit 16.
            if (tableLength < 12 || reader.ReadUInt32() != 0x00010000)
            {
                return result;
            }

            int count = reader.ReadUInt16();
            if (12 + (count * 12) > tableLength)
            {
                return result;
            }

            reader.Seek(12, SeekOrigin.Begin);

            // +----------+--------------+-----------------------------------------------+
            // | Type     | Name         | Description                                   |
            // +----------+--------------+-----------------------------------------------+
            // | uint16   | feature      | Feature type.                                 |
            // | uint16   | nSettings    | Number of named selectors.                    |
            // | Offset32 | settingTable | Offset from the start of feat.                |
            // | uint16   | featureFlags | Bit 15 indicates mutually exclusive settings. |
            // | int16    | nameIndex    | Name table ID.                                |
            // +----------+--------------+-----------------------------------------------+
            for (int i = 0; i < count; i++)
            {
                ushort feature = reader.ReadUInt16();
                _ = reader.ReadUInt16();
                _ = reader.ReadOffset32();
                ushort flags = reader.ReadUInt16();
                _ = reader.ReadInt16();

                // Bit 15 of featureFlags allows only one setting for this feature.
                result[feature] = (flags & 0x8000) != 0;
            }
        }

        return result;
    }

    /// <summary>
    /// Reads the language tag strings selected by language feature records.
    /// </summary>
    /// <param name="fontReader">The font reader.</param>
    /// <returns>The language tag ranges and the bytes that they reference.</returns>
    private static ((ushort Offset, ushort Length)[] Tags, byte[] Data) LoadLanguages(FontReader fontReader)
    {
        if (!fontReader.TryGetBoundedReaderAtTablePosition("ltag", out BigEndianBinaryReader? reader))
        {
            return ([], []);
        }

        using (reader)
        {
            uint tableLength = (uint)reader.BaseStream.Length;

            // +----------+---------+----------------------------------------------+
            // | Type     | Name    | Description                                  |
            // +----------+---------+----------------------------------------------+
            // | uint32   | version | 1.                                           |
            // | uint32   | flags   | Reserved.                                    |
            // | uint32   | numTags | Number of tag ranges.                        |
            // | Offset16 | offset  | Tag byte offset from ltag, repeated per tag. |
            // | uint16   | length  | Tag byte length, repeated per tag.           |
            // +----------+---------+----------------------------------------------+
            if (tableLength < 12 || reader.ReadUInt32() != 1)
            {
                return ([], []);
            }

            _ = reader.ReadUInt32();
            uint count = reader.ReadUInt32();
            if (count > ushort.MaxValue || 12 + (4L * count) > tableLength)
            {
                return ([], []);
            }

            (ushort Offset, ushort Length)[] tags = new (ushort, ushort)[count];
            int dataEnd = 0;
            for (int i = 0; i < tags.Length; i++)
            {
                ushort offset = reader.ReadOffset16();
                ushort length = reader.ReadUInt16();
                if (!AatStateTable.Contains(offset, length, tableLength))
                {
                    return ([], []);
                }

                tags[i] = (offset, length);
                dataEnd = Math.Max(dataEnd, offset + length);
            }

            // Ranges can alias. Keep a single copy of the referenced bytes
            // and compare each tag in place once a run requests its selector.
            reader.Seek(0, SeekOrigin.Begin);
            return (tags, reader.ReadBytes(dataEnd));
        }
    }

    /// <summary>
    /// Compares the canonical ASCII prefix of an AAT language tag to a run language.
    /// </summary>
    /// <param name="tag">The language tag bytes from the font.</param>
    /// <param name="language">The language tag of the run.</param>
    /// <returns><see langword="true"/> if the font tag matches the run language or starts it at a subtag boundary.</returns>
    private static bool MatchesLanguage(ReadOnlySpan<byte> tag, string language)
    {
        int i = 0;
        for (; i < tag.Length; i++)
        {
            char value = (char)tag[i];
            if (!char.IsAsciiLetterOrDigit(value) && value is not ('_' or '-'))
            {
                // Some fonts carry a locale suffix, such as SR.UTF-8.
                break;
            }

            if (i >= language.Length || char.ToLowerInvariant(value == '_' ? '-' : value) != char.ToLowerInvariant(language[i]))
            {
                return false;
            }
        }

        return i > 0 && (i == language.Length || language[i] == '-');
    }

    /// <summary>
    /// Maps an enabled OpenType feature tag to its Apple feature type and selector.
    /// </summary>
    /// <param name="tag">The OpenType feature tag.</param>
    /// <returns>
    /// The AAT feature type and selector. If the tag has no mapping, both values are <see cref="ushort.MaxValue"/>.
    /// </returns>
    private static (ushort Type, ushort Selector) MapFeature(Tag tag)
    {
        // Tags ss01 through ss20 store 'ss' in the two high bytes.
        const uint stylisticSetPrefix = ('s' << 8) | 's';
        uint value = tag.Value;
        if ((value >> 16) == stylisticSetPrefix)
        {
            // The low two bytes hold the ASCII digits of the set number.
            int tens = (byte)(value >> 8) - '0';
            int ones = (byte)value - '0';
            int index = (tens * 10) + ones;
            if ((uint)tens <= 2 && (uint)ones <= 9 && index is >= 1 and <= 20)
            {
                // Each set has an enable and a disable selector. The even one enables it.
                return (35, (ushort)(2 * index));
            }
        }

        // Apple Feature Registry mappings for enabled OpenType features.
        return ((KnownFeatureTags)value) switch
        {
            KnownFeatureTags.AlternativeFractions => (11, 1),
            KnownFeatureTags.PetiteCapitalsFromCapitals => (38, 2),
            KnownFeatureTags.SmallCapitalsFromCapitals => (38, 1),
            KnownFeatureTags.ContextualAlternates => (36, 0),
            KnownFeatureTags.CaseSensitiveForms => (33, 0),
            KnownFeatureTags.ContextualLigatures => (1, 18),
            KnownFeatureTags.CapitalSpacing => (33, 2),
            KnownFeatureTags.ContextualSwash => (36, 4),
            KnownFeatureTags.DiscretionaryLigatures => (1, 4),
            KnownFeatureTags.ExpertForms => (20, 10),
            KnownFeatureTags.Fractions => (11, 2),
            KnownFeatureTags.FullWidths => (22, 1),
            KnownFeatureTags.AlternateHalfWidths => (22, 6),
            KnownFeatureTags.HistoricalForms => (40, 0),
            KnownFeatureTags.HorizontalKanaAlternates => (34, 0),
            KnownFeatureTags.HistoricalLigatures => (1, 20),
            KnownFeatureTags.Hangul => (23, 1),
            KnownFeatureTags.HojoKanjiForms => (20, 12),
            KnownFeatureTags.HalfWidths => (22, 2),
            KnownFeatureTags.Italics => (32, 2),
            KnownFeatureTags.Jis2004 => (20, 11),
            KnownFeatureTags.Jis78Forms => (20, 2),
            KnownFeatureTags.Jis83Forms => (20, 3),
            KnownFeatureTags.Jis90Forms => (20, 4),
            KnownFeatureTags.Ligatures => (1, 2),
            KnownFeatureTags.LiningFigures => (21, 1),
            KnownFeatureTags.MathematicalGreek => (15, 10),
            KnownFeatureTags.NlcKanjiForms => (20, 13),
            KnownFeatureTags.OldstyleFigures => (21, 0),
            KnownFeatureTags.Ordinals => (10, 3),
            KnownFeatureTags.ProportionalAlternateWidths => (22, 5),
            KnownFeatureTags.PetiteCapitals => (37, 2),
            KnownFeatureTags.ProportionalKana => (22, 0),
            KnownFeatureTags.ProportionalFigures => (6, 1),
            KnownFeatureTags.ProportionalWidths => (22, 0),
            KnownFeatureTags.QuarterWidths => (22, 4),
            KnownFeatureTags.RequiredLigatures => (1, 0),
            KnownFeatureTags.RubyNotationForms => (28, 2),
            KnownFeatureTags.ScientificInferiors => (10, 4),
            KnownFeatureTags.SmallCapitals => (37, 1),
            KnownFeatureTags.SimplifiedForms => (20, 1),
            KnownFeatureTags.Subscript => (10, 2),
            KnownFeatureTags.Superscript => (10, 1),
            KnownFeatureTags.Swash => (36, 2),
            KnownFeatureTags.Titling => (19, 4),
            KnownFeatureTags.TraditionalNameForms => (20, 14),
            KnownFeatureTags.TabularFigures => (6, 0),
            KnownFeatureTags.TraditionalForms => (20, 0),
            KnownFeatureTags.ThirdWidths => (22, 3),
            KnownFeatureTags.Unicase => (3, 14),
            KnownFeatureTags.AlternateVerticalMetrics => (22, 5),
            KnownFeatureTags.VerticalAlternates => (4, 0),
            KnownFeatureTags.AlternateVerticalHalfMetrics => (22, 6),
            KnownFeatureTags.VerticalKanaAlternates => (34, 2),
            KnownFeatureTags.ProportionalAlternateVerticalMetrics => (22, 5),
            KnownFeatureTags.VerticalAlternatesAndRotation => (4, 0),
            KnownFeatureTags.VerticalAlternatesForRotation => (4, 2),
            KnownFeatureTags.SlashedZero => (14, 4),
            _ => (ushort.MaxValue, ushort.MaxValue)
        };
    }
}
