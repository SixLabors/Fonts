// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.Unicode.Resources;

namespace SixLabors.Fonts;

/// <summary>
/// Contains supplementary data that allows the shaping of glyphs.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
internal class GlyphShapingData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GlyphShapingData"/> class.
    /// </summary>
    /// <param name="textRun">The text run.</param>
    public GlyphShapingData(TextRun textRun) => this.TextRun = textRun;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlyphShapingData"/> class.
    /// </summary>
    /// <param name="data">The data to copy properties from.</param>
    /// <param name="clearFeatures">Whether to clear features.</param>
    public GlyphShapingData(GlyphShapingData data, bool clearFeatures = false)
    {
        this.GlyphId = data.GlyphId;
        this.CodePoint = data.CodePoint;
        this.CodePointCount = data.CodePointCount;
        this.Direction = data.Direction;
        this.TextRun = data.TextRun;
        this.LigatureId = data.LigatureId;
        this.IsLigated = data.IsLigated;
        this.LigatureComponent = data.LigatureComponent;
        this.MarkAttachment = data.MarkAttachment;
        this.CursiveAttachment = data.CursiveAttachment;
        this.IsSubstituted = data.IsSubstituted;
        this.IsDecomposed = data.IsDecomposed;
        if (data.UniversalShapingEngineInfo != null)
        {
            this.UniversalShapingEngineInfo = new(
                data.UniversalShapingEngineInfo.Category,
                data.UniversalShapingEngineInfo.SyllableType,
                data.UniversalShapingEngineInfo.Syllable);
        }

        if (data.IndicShapingEngineInfo != null)
        {
            this.IndicShapingEngineInfo = new(
                data.IndicShapingEngineInfo.Category,
                data.IndicShapingEngineInfo.Position,
                data.IndicShapingEngineInfo.SyllableType,
                data.IndicShapingEngineInfo.Syllable);
        }

        if (!clearFeatures)
        {
            this.Features.AddRange(data.Features);
        }

        this.AppliedFeatures.AddRange(data.AppliedFeatures);

        this.Bounds = data.Bounds;
    }

    /// <summary>
    /// Gets or sets the glyph id.
    /// </summary>
    public ushort GlyphId { get; set; }

    /// <summary>
    /// Gets or sets the leading codepoint.
    /// </summary>
    public CodePoint CodePoint { get; set; }

    /// <summary>
    /// Gets or sets the codepoint count represented by this glyph.
    /// </summary>
    public int CodePointCount { get; set; } = 1;

    /// <summary>
    /// Gets or sets the text direction.
    /// </summary>
    public TextDirection Direction { get; set; }

    /// <summary>
    /// Gets or sets the text run this glyph belongs to.
    /// </summary>
    public TextRun TextRun { get; set; }

    /// <summary>
    /// Gets or sets the id of any ligature this glyph is a member of.
    /// </summary>
    public int LigatureId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the glyph is ligated.
    /// </summary>
    public bool IsLigated { get; set; }

    /// <summary>
    /// Gets or sets the ligature component index of the glyph.
    /// </summary>
    public int LigatureComponent { get; set; } = -1;

    /// <summary>
    /// Gets or sets the index of any mark attachment.
    /// </summary>
    public int MarkAttachment { get; set; } = -1;

    /// <summary>
    /// Gets or sets the index of any cursive attachment.
    /// </summary>
    public int CursiveAttachment { get; set; } = -1;

    /// <summary>
    /// Gets or sets the collection of features.
    /// </summary>
    public List<TagEntry> Features { get; set; } = new();

    /// <summary>
    /// Gets or sets the collection of applied features.
    /// </summary>
    public List<Tag> AppliedFeatures { get; set; } = new();

    /// <summary>
    /// Gets or sets the shaping bounds.
    /// </summary>
    public GlyphShapingBounds Bounds { get; set; } = new(0, 0, 0, 0);

    /// <summary>
    /// Gets or sets a value indicating whether this glyph is the result of a substitution.
    /// </summary>
    public bool IsSubstituted { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this glyph is the result of a decomposition substitution
    /// </summary>
    public bool IsDecomposed { get; set; }

    /// <summary>
    /// Gets or sets the universal shaping information.
    /// </summary>
    public UniversalShapingEngineInfo? UniversalShapingEngineInfo { get; set; }

    /// <summary>
    /// Gets or sets the Indic shaping information.
    /// </summary>
    public IndicShapingEngineInfo? IndicShapingEngineInfo { get; set; }

    private string DebuggerDisplay
        => FormattableString
        .Invariant($" {this.GlyphId} : {this.CodePoint.ToDebuggerDisplay()} : {CodePoint.GetScriptClass(this.CodePoint)} : {this.Direction} : {this.TextRun.TextAttributes} : {this.LigatureId} : {this.LigatureComponent} : {this.IsDecomposed}");

    internal string ToDebuggerDisplay() => this.DebuggerDisplay;
}

/// <summary>
/// Represents information required for universal shaping.
/// </summary>
internal class UniversalShapingEngineInfo
{
    public UniversalShapingEngineInfo(string category, string syllableType, int syllable)
    {
        this.Category = category;
        this.SyllableType = syllableType;
        this.Syllable = syllable;
    }

    public string Category { get; set; }

    public string SyllableType { get; }

    public int Syllable { get; }
}

internal class IndicShapingEngineInfo
{
    public IndicShapingEngineInfo(
        IndicShapingData.Categories category,
        IndicShapingData.Positions position,
        string syllableType,
        int syllable)
    {
        this.Category = category;
        this.Position = position;
        this.SyllableType = syllableType;
        this.Syllable = syllable;
    }

    public IndicShapingData.Categories Category { get; set; }

    public IndicShapingData.Positions Position { get; set; }

    public string SyllableType { get; }

    public int Syllable { get; }
}
