// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// The FeatureVariations table is used in variable fonts to provide alternate sets of
/// feature table lookups for different regions of the variation space.
/// Shared by both GPOS and GSUB tables (version 1.1).
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#featurevariations-table"/>
/// </summary>
internal sealed class FeatureVariationsTable
{
    private FeatureVariationsTable(FeatureVariationRecord[] records)
        => this.Records = records;

    public FeatureVariationRecord[] Records { get; }

    /// <summary>
    /// Loads the FeatureVariations table.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">Absolute offset to the beginning of the FeatureVariations table.</param>
    /// <param name="featureList">The FeatureListTable, used to resolve feature tags for substitutions.</param>
    /// <returns>The FeatureVariationsTable, or null if the offset is 0.</returns>
    public static FeatureVariationsTable? Load(BigEndianBinaryReader reader, long offset, FeatureListTable featureList)
    {
        if (offset == 0)
        {
            return null;
        }

        // FeatureVariations table
        // +----------+------------------------------------------------------+---------------------------------------------------------------+
        // | Type     | Name                                                 | Description                                                   |
        // +==========+======================================================+===============================================================+
        // | uint16   | majorVersion                                         | Major version — set to 1                                      |
        // +----------+------------------------------------------------------+---------------------------------------------------------------+
        // | uint16   | minorVersion                                         | Minor version — set to 0                                      |
        // +----------+------------------------------------------------------+---------------------------------------------------------------+
        // | uint32   | featureVariationRecordCount                          | Number of FeatureVariationRecords                             |
        // +----------+------------------------------------------------------+---------------------------------------------------------------+
        // | FeatureVariationRecord | featureVariationRecords[count]          | Array of FeatureVariationRecords                              |
        // +----------+------------------------------------------------------+---------------------------------------------------------------+
        reader.Seek(offset, SeekOrigin.Begin);

        ushort majorVersion = reader.ReadUInt16();
        ushort minorVersion = reader.ReadUInt16();
        uint recordCount = reader.ReadUInt32();

        // Read all record offsets first, then load data to avoid excessive seeking.
        int count = (int)recordCount;
        using Buffer<uint> conditionSetOffsetsBuffer = new(count);
        using Buffer<uint> substitutionOffsetsBuffer = new(count);
        Span<uint> conditionSetOffsets = conditionSetOffsetsBuffer.GetSpan();
        Span<uint> substitutionOffsets = substitutionOffsetsBuffer.GetSpan();
        for (int i = 0; i < count; i++)
        {
            conditionSetOffsets[i] = reader.ReadOffset32();
            substitutionOffsets[i] = reader.ReadOffset32();
        }

        FeatureVariationRecord[] records = new FeatureVariationRecord[count];
        for (int i = 0; i < count; i++)
        {
            ConditionSetTable conditionSet = ConditionSetTable.Load(reader, offset + conditionSetOffsets[i]);
            FeatureTableSubstitutionRecord[] substitutions = LoadFeatureTableSubstitution(reader, offset + substitutionOffsets[i], featureList);
            records[i] = new FeatureVariationRecord(conditionSet, substitutions);
        }

        return new FeatureVariationsTable(records);
    }

    /// <summary>
    /// Finds the first matching <see cref="FeatureVariationRecord"/> whose conditions are satisfied
    /// by the given normalized coordinates, and returns its feature substitutions.
    /// Returns null if no record matches or no variation coordinates are available.
    /// </summary>
    /// <param name="normalizedCoords">The normalized variation coordinates.</param>
    /// <returns>The matching substitution records, or null.</returns>
    public FeatureTableSubstitutionRecord[]? FindMatchingSubstitutions(ReadOnlySpan<float> normalizedCoords)
    {
        if (normalizedCoords.IsEmpty)
        {
            return null;
        }

        for (int i = 0; i < this.Records.Length; i++)
        {
            if (this.Records[i].ConditionSet.Evaluate(normalizedCoords))
            {
                return this.Records[i].Substitutions;
            }
        }

        return null;
    }

    private static FeatureTableSubstitutionRecord[] LoadFeatureTableSubstitution(
        BigEndianBinaryReader reader,
        long offset,
        FeatureListTable featureList)
    {
        // FeatureTableSubstitution table
        // +----------+------------------------------------------------------+---------------------------------------------------------------+
        // | Type     | Name                                                 | Description                                                   |
        // +==========+======================================================+===============================================================+
        // | uint16   | majorVersion                                         | Major version — set to 1                                      |
        // +----------+------------------------------------------------------+---------------------------------------------------------------+
        // | uint16   | minorVersion                                         | Minor version — set to 0                                      |
        // +----------+------------------------------------------------------+---------------------------------------------------------------+
        // | uint16   | substitutionCount                                    | Number of FeatureTableSubstitutionRecords                     |
        // +----------+------------------------------------------------------+---------------------------------------------------------------+
        // | FeatureTableSubstitutionRecord | substitutions[count]           | Array of records                                              |
        // +----------+------------------------------------------------------+---------------------------------------------------------------+
        reader.Seek(offset, SeekOrigin.Begin);

        ushort majorVersion = reader.ReadUInt16();
        ushort minorVersion = reader.ReadUInt16();
        ushort substitutionCount = reader.ReadUInt16();

        // Read record headers (featureIndex + offset pairs).
        using Buffer<ushort> featureIndicesBuffer = new(substitutionCount);
        using Buffer<uint> featureTableOffsetsBuffer = new(substitutionCount);
        Span<ushort> featureIndices = featureIndicesBuffer.GetSpan();
        Span<uint> featureTableOffsets = featureTableOffsetsBuffer.GetSpan();
        for (int i = 0; i < substitutionCount; i++)
        {
            featureIndices[i] = reader.ReadUInt16();
            featureTableOffsets[i] = reader.ReadOffset32();
        }

        // Load each alternate Feature table.
        FeatureTableSubstitutionRecord[] records = new FeatureTableSubstitutionRecord[substitutionCount];
        for (int i = 0; i < substitutionCount; i++)
        {
            ushort featureIndex = featureIndices[i];

            // Resolve the original feature tag from the FeatureList so the substitute
            // carries the same tag.
            Tag featureTag = featureIndex < featureList.FeatureTables.Length
                ? featureList.FeatureTables[featureIndex].FeatureTag
                : default;

            FeatureTable alternateFeatureTable = FeatureTable.Load(featureTag, reader, offset + featureTableOffsets[i]);
            records[i] = new FeatureTableSubstitutionRecord(featureIndex, alternateFeatureTable);
        }

        return records;
    }
}

/// <summary>
/// A set of conditions that must all be true for a FeatureVariationRecord to match.
/// </summary>
internal sealed class ConditionSetTable
{
    private ConditionSetTable(ConditionTable[] conditions)
        => this.Conditions = conditions;

    public ConditionTable[] Conditions { get; }

    public static ConditionSetTable Load(BigEndianBinaryReader reader, long offset)
    {
        // ConditionSet table
        // +----------+----------------------------+------------------------------------------+
        // | Type     | Name                       | Description                              |
        // +==========+============================+==========================================+
        // | uint16   | conditionCount             | Number of conditions                     |
        // +----------+----------------------------+------------------------------------------+
        // | Offset32 | conditionOffsets[count]     | Offsets to Condition tables, from         |
        // |          |                            | beginning of ConditionSet table           |
        // +----------+----------------------------+------------------------------------------+
        reader.Seek(offset, SeekOrigin.Begin);

        ushort conditionCount = reader.ReadUInt16();
        using Buffer<uint> conditionOffsetsBuffer = new(conditionCount);
        Span<uint> conditionOffsets = conditionOffsetsBuffer.GetSpan();
        for (int i = 0; i < conditionCount; i++)
        {
            conditionOffsets[i] = reader.ReadOffset32();
        }

        ConditionTable[] conditions = new ConditionTable[conditionCount];
        for (int i = 0; i < conditionCount; i++)
        {
            conditions[i] = ConditionTable.Load(reader, offset + conditionOffsets[i]);
        }

        return new ConditionSetTable(conditions);
    }

    /// <summary>
    /// Evaluates whether all conditions in this set are satisfied by the given normalized coordinates.
    /// </summary>
    /// <param name="normalizedCoords">The normalized variation coordinates.</param>
    /// <returns>True if all conditions match.</returns>
    public bool Evaluate(ReadOnlySpan<float> normalizedCoords)
    {
        for (int i = 0; i < this.Conditions.Length; i++)
        {
            if (!this.Conditions[i].Evaluate(normalizedCoords))
            {
                return false;
            }
        }

        return true;
    }
}

#pragma warning disable SA1201 // Elements should appear in the correct order

/// <summary>
/// A single record in the FeatureVariations table, pairing a condition set with
/// a set of feature table substitutions.
/// </summary>
internal readonly struct FeatureVariationRecord
{
    public FeatureVariationRecord(ConditionSetTable conditionSet, FeatureTableSubstitutionRecord[] substitutions)
    {
        this.ConditionSet = conditionSet;
        this.Substitutions = substitutions;
    }

    public ConditionSetTable ConditionSet { get; }

    public FeatureTableSubstitutionRecord[] Substitutions { get; }
}

/// <summary>
/// A substitution record that maps a feature index to an alternate Feature table.
/// </summary>
internal readonly struct FeatureTableSubstitutionRecord
{
    public FeatureTableSubstitutionRecord(ushort featureIndex, FeatureTable alternateFeatureTable)
    {
        this.FeatureIndex = featureIndex;
        this.AlternateFeatureTable = alternateFeatureTable;
    }

    /// <summary>
    /// Gets the index into the FeatureList of the feature being substituted.
    /// </summary>
    public ushort FeatureIndex { get; }

    /// <summary>
    /// Gets the alternate Feature table to use in place of the original.
    /// </summary>
    public FeatureTable AlternateFeatureTable { get; }
}

/// <summary>
/// A condition that checks whether a normalized coordinate for a specific axis
/// falls within a given range.
/// </summary>
internal readonly struct ConditionTable
{
    public ConditionTable(ushort axisIndex, float filterRangeMinValue, float filterRangeMaxValue)
    {
        this.AxisIndex = axisIndex;
        this.FilterRangeMinValue = filterRangeMinValue;
        this.FilterRangeMaxValue = filterRangeMaxValue;
    }

    /// <summary>
    /// Gets the index of the variation axis (into fvar axes array).
    /// </summary>
    public ushort AxisIndex { get; }

    /// <summary>
    /// Gets the minimum normalized coordinate value for the condition to be true.
    /// </summary>
    public float FilterRangeMinValue { get; }

    /// <summary>
    /// Gets the maximum normalized coordinate value for the condition to be true.
    /// </summary>
    public float FilterRangeMaxValue { get; }

    public static ConditionTable Load(BigEndianBinaryReader reader, long offset)
    {
        // Condition table, Format 1 (ConditionAxisRange)
        // +----------+----------------------------+------------------------------------------+
        // | Type     | Name                       | Description                              |
        // +==========+============================+==========================================+
        // | uint16   | format                     | Format = 1                               |
        // +----------+----------------------------+------------------------------------------+
        // | uint16   | axisIndex                  | Index of variation axis                  |
        // +----------+----------------------------+------------------------------------------+
        // | F2DOT14  | filterRangeMinValue        | Minimum normalized coordinate value      |
        // +----------+----------------------------+------------------------------------------+
        // | F2DOT14  | filterRangeMaxValue        | Maximum normalized coordinate value      |
        // +----------+----------------------------+------------------------------------------+
        reader.Seek(offset, SeekOrigin.Begin);

        ushort format = reader.ReadUInt16();

        // Only Format 1 is defined.
        if (format != 1)
        {
            return default;
        }

        ushort axisIndex = reader.ReadUInt16();
        float filterRangeMinValue = reader.ReadF2Dot14();
        float filterRangeMaxValue = reader.ReadF2Dot14();

        return new ConditionTable(axisIndex, filterRangeMinValue, filterRangeMaxValue);
    }

    /// <summary>
    /// Evaluates whether the given normalized coordinates satisfy this condition.
    /// </summary>
    /// <param name="normalizedCoords">The normalized variation coordinates.</param>
    /// <returns>True if the coordinate for this axis is within the filter range.</returns>
    public bool Evaluate(ReadOnlySpan<float> normalizedCoords)
    {
        if (this.AxisIndex >= normalizedCoords.Length)
        {
            return false;
        }

        float coord = normalizedCoords[this.AxisIndex];
        return coord >= this.FilterRangeMinValue && coord <= this.FilterRangeMaxValue;
    }
}

#pragma warning restore SA1201
