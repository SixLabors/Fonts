// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Resolves the Font DICT index for a given glyph using the FDSelect data from a CIDFont.
/// Supports FDSelect format 0 (per-glyph map) and formats 3/4 (range-based).
/// </summary>
internal struct FDRangeProvider
{
    private readonly int format;
    private readonly FDRange[] ranges;
    private readonly Dictionary<int, byte> fdSelectMap;
    private uint currentGlyphIndex;
    private uint endGlyphIndexMax;
    private FDRange currentRange;
    private int currentSelectedRangeIndex;

    public FDRangeProvider(CidFontInfo cidFontInfo)
    {
        this.format = cidFontInfo.FdSelectFormat;
        this.ranges = cidFontInfo.FdRanges;
        this.fdSelectMap = cidFontInfo.FdSelectMap;
        this.currentGlyphIndex = 0;
        this.currentSelectedRangeIndex = 0;

        if (this.ranges.Length is not 0)
        {
            this.currentRange = this.ranges[0];
            this.endGlyphIndexMax = this.ranges[1].First;
        }
        else
        {
            // empty
            this.currentRange = default;
            this.endGlyphIndexMax = 0;
        }

        this.SelectedFDArray = 0;
    }

    public ushort SelectedFDArray { get; private set; }

    public void SetCurrentGlyphIndex(ushort index)
    {
        switch (this.format)
        {
            case 0:
                this.currentGlyphIndex = this.fdSelectMap[index];
                break;

            case 3:
            case 4:
                // Find proper range for selected index.
                if (index >= this.currentRange.First && index < this.endGlyphIndexMax)
                {
                    // Ok, in current range.
                    this.SelectedFDArray = this.currentRange.FontDictionary;
                }
                else
                {
                    // Move to next range.
                    this.currentSelectedRangeIndex++;
                    this.currentRange = this.ranges[this.currentSelectedRangeIndex];

                    this.endGlyphIndexMax = this.ranges[this.currentSelectedRangeIndex + 1].First;
                    if (index >= this.currentRange.First && index < this.endGlyphIndexMax)
                    {
                        this.SelectedFDArray = this.currentRange.FontDictionary;
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }

                this.currentGlyphIndex = index;

                break;

            default:
                throw new NotSupportedException();
        }
    }
}
