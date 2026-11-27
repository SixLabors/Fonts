// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// Positions combining marks from glyph extents when a script permits fallback positioning and the font has no positioning table.
/// </summary>
internal static class FallbackMarkPositioner
{
    /// <summary>
    /// The first canonical combining class whose value directly describes an attachment position.
    /// </summary>
    private const int FirstPositioningClass = 200;

    /// <summary>
    /// The attached-below-left combining class.
    /// </summary>
    private const int AttachedBelowLeft = 200;

    /// <summary>
    /// The attached-below combining class.
    /// </summary>
    private const int AttachedBelow = 202;

    /// <summary>
    /// The attached-above combining class.
    /// </summary>
    private const int AttachedAbove = 214;

    /// <summary>
    /// The attached-above-right combining class.
    /// </summary>
    private const int AttachedAboveRight = 216;

    /// <summary>
    /// The below-left combining class.
    /// </summary>
    private const int BelowLeft = 218;

    /// <summary>
    /// The below combining class.
    /// </summary>
    private const int Below = 220;

    /// <summary>
    /// The below-right combining class.
    /// </summary>
    private const int BelowRight = 222;

    /// <summary>
    /// The above-left combining class.
    /// </summary>
    private const int AboveLeft = 228;

    /// <summary>
    /// The above combining class.
    /// </summary>
    private const int Above = 230;

    /// <summary>
    /// The above-right combining class.
    /// </summary>
    private const int AboveRight = 232;

    /// <summary>
    /// The double-below combining class.
    /// </summary>
    private const int DoubleBelow = 233;

    /// <summary>
    /// The double-above combining class.
    /// </summary>
    private const int DoubleAbove = 234;

    /// <summary>
    /// The divisor defining the vertical gap between a base and an unattached mark.
    /// </summary>
    private const int VerticalGapDivisor = 16;

    /// <summary>
    /// The shared high-byte prefix of the Thai and Lao blocks.
    /// </summary>
    private const int ThaiLaoBlockPrefix = 0x0E00;

    /// <summary>
    /// The Thai character MAI HAN-AKAT.
    /// </summary>
    private const int ThaiMaiHanAkat = 0x0E31;

    /// <summary>
    /// The Thai character PHINTHU.
    /// </summary>
    private const int ThaiPhinthu = 0x0E3A;

    /// <summary>
    /// The Thai character SARA I.
    /// </summary>
    private const int ThaiSaraI = 0x0E34;

    /// <summary>
    /// The Thai character SARA II.
    /// </summary>
    private const int ThaiSaraIi = 0x0E35;

    /// <summary>
    /// The Thai character SARA UE.
    /// </summary>
    private const int ThaiSaraUe = 0x0E36;

    /// <summary>
    /// The Thai character SARA UEE.
    /// </summary>
    private const int ThaiSaraUee = 0x0E37;

    /// <summary>
    /// The Thai character MAITAIKHU.
    /// </summary>
    private const int ThaiMaiTaikhu = 0x0E47;

    /// <summary>
    /// The Thai character THANTHAKHAT.
    /// </summary>
    private const int ThaiThanthakhat = 0x0E4C;

    /// <summary>
    /// The Thai character NIKHAHIT.
    /// </summary>
    private const int ThaiNikhahit = 0x0E4D;

    /// <summary>
    /// The Thai character YAMAKKAN.
    /// </summary>
    private const int ThaiYamakkan = 0x0E4E;

    /// <summary>
    /// The Lao vowel sign MAI KAN.
    /// </summary>
    private const int LaoMaiKan = 0x0EB1;

    /// <summary>
    /// The Lao vowel sign I.
    /// </summary>
    private const int LaoVowelSignI = 0x0EB4;

    /// <summary>
    /// The Lao vowel sign II.
    /// </summary>
    private const int LaoVowelSignIi = 0x0EB5;

    /// <summary>
    /// The Lao vowel sign Y.
    /// </summary>
    private const int LaoVowelSignY = 0x0EB6;

    /// <summary>
    /// The Lao vowel sign YY.
    /// </summary>
    private const int LaoVowelSignYy = 0x0EB7;

    /// <summary>
    /// The Lao vowel sign MAI KON.
    /// </summary>
    private const int LaoMaiKon = 0x0EBB;

    /// <summary>
    /// The Lao semivowel sign LO.
    /// </summary>
    private const int LaoSemivowelLo = 0x0EBC;

    /// <summary>
    /// The Lao cancellation mark.
    /// </summary>
    private const int LaoCancellationMark = 0x0ECC;

    /// <summary>
    /// The Lao NIGGAHITA.
    /// </summary>
    private const int LaoNiggahita = 0x0ECD;

    /// <summary>
    /// Applies fallback mark positioning to the active span.
    /// </summary>
    /// <param name="fontMetrics">The font metrics supplying extents and advances.</param>
    /// <param name="buffer">The positioned shaping buffer.</param>
    public static void Apply(FontMetrics fontMetrics, ShapingBuffer buffer)
    {
        // In this order the pen advances past the base before it
        // reaches the mark, even when the run reads right to left.
        int index = buffer.SpanStart;
        bool preserveGraphemeOrder = buffer.HorizontalScriptDirection == ScriptHorizontalDirection.LeftToRight;
        TextDirection direction = preserveGraphemeOrder ? TextDirection.LeftToRight : buffer[index].Direction;
        PositionSegment(buffer, index, buffer.SpanEnd - index, fontMetrics.UnitsPerEm, direction);
    }

    /// <summary>
    /// Divides a segment at each visible non-mark and positions the marks following each base.
    /// </summary>
    /// <param name="buffer">The positioned shaping buffer.</param>
    /// <param name="index">The zero-based index of the first record.</param>
    /// <param name="count">The number of records in the segment.</param>
    /// <param name="unitsPerEm">The font's units per em.</param>
    /// <param name="direction">The order of the base and marks in the segment.</param>
    private static void PositionSegment(ShapingBuffer buffer, int index, int count, int unitsPerEm, TextDirection direction)
    {
        int start = index;
        int end = index + count;
        for (int i = index + 1; i < end; i++)
        {
            ref GlyphShapingData data = ref buffer[i];
            if (!CodePoint.IsMark(data.CodePoint) && !data.IsHidden && !data.IsDefaultIgnorable)
            {
                PositionCluster(buffer, start, i, unitsPerEm, direction);
                start = i;
            }
        }

        PositionCluster(buffer, start, end, unitsPerEm, direction);
    }

    /// <summary>
    /// Finds each base in a character cluster and positions its following marks.
    /// </summary>
    /// <param name="buffer">The positioned shaping buffer.</param>
    /// <param name="start">The zero-based cluster start.</param>
    /// <param name="end">The exclusive cluster end.</param>
    /// <param name="unitsPerEm">The font's units per em.</param>
    /// <param name="direction">The order of the base and marks in the cluster.</param>
    private static void PositionCluster(ShapingBuffer buffer, int start, int end, int unitsPerEm, TextDirection direction)
    {
        if (end - start < 2)
        {
            return;
        }

        for (int i = start; i < end; i++)
        {
            if (CodePoint.IsMark(buffer[i].CodePoint))
            {
                continue;
            }

            int markEnd = i + 1;
            while (markEnd < end)
            {
                ref GlyphShapingData data = ref buffer[markEnd];
                if (!data.IsHidden && !data.IsDefaultIgnorable && !CodePoint.IsMark(data.CodePoint))
                {
                    break;
                }

                markEnd++;
            }

            PositionAroundBase(buffer, i, markEnd, unitsPerEm, direction);
            i = markEnd - 1;
        }
    }

    /// <summary>
    /// Positions marks around one base, stacking marks with the same recategorized combining class.
    /// </summary>
    /// <param name="buffer">The positioned shaping buffer.</param>
    /// <param name="baseIndex">The zero-based base record index.</param>
    /// <param name="end">The exclusive end of the marks belonging to the base.</param>
    /// <param name="unitsPerEm">The font's units per em.</param>
    /// <param name="direction">The order of the base and its marks.</param>
    private static void PositionAroundBase(ShapingBuffer buffer, int baseIndex, int end, int unitsPerEm, TextDirection direction)
    {
        FontGlyphMetrics baseMetrics = buffer.MetricsAt(baseIndex).Metrics;
        ref GlyphShapingPosition basePosition = ref buffer.PositionAt(baseIndex);
        GlyphExtents baseExtents = GetExtents(baseMetrics);
        baseExtents.YBearing += basePosition.Bounds.Y;

        // Horizontal advance gives stable component widths even for a zero-ink glyph.
        baseExtents.XBearing = 0;
        baseExtents.Width = baseMetrics.AdvanceWidth;

        ref GlyphShapingData baseData = ref buffer[baseIndex];
        int ligatureId = baseData.LigatureId;
        int componentCount = baseData.LigatureComponentCount;
        int xOffset = 0;
        int yOffset = 0;
        bool isForward = direction == TextDirection.LeftToRight
            || buffer.TextOptions.LayoutMode.IsVertical();
        if (isForward)
        {
            xOffset -= basePosition.Bounds.Width;
            yOffset -= basePosition.Bounds.Height;
        }

        GlyphExtents componentExtents = baseExtents;
        GlyphExtents clusterExtents = baseExtents;
        int lastLigatureComponent = -1;
        int lastCombiningClass = byte.MaxValue;
        for (int i = baseIndex + 1; i < end; i++)
        {
            ref GlyphShapingData data = ref buffer[i];
            int combiningClass = RecategorizeCombiningClass(data.CodePoint, data.MarkOrderingClass);
            if (combiningClass == 0)
            {
                ref GlyphShapingPosition ordinaryPosition = ref buffer.PositionAt(i);
                if (isForward)
                {
                    xOffset -= ordinaryPosition.Bounds.Width;
                    yOffset -= ordinaryPosition.Bounds.Height;
                }
                else
                {
                    xOffset += ordinaryPosition.Bounds.Width;
                    yOffset += ordinaryPosition.Bounds.Height;
                }

                continue;
            }

            if (componentCount > 1)
            {
                int component = data.LigatureComponent - 1;
                if (ligatureId == 0 || data.LigatureId != ligatureId || component < 0 || component >= componentCount)
                {
                    component = componentCount - 1;
                }

                if (lastLigatureComponent != component)
                {
                    lastLigatureComponent = component;
                    lastCombiningClass = byte.MaxValue;
                    componentExtents = baseExtents;
                    if (direction == TextDirection.LeftToRight)
                    {
                        componentExtents.XBearing += (component * componentExtents.Width) / componentCount;
                    }
                    else
                    {
                        componentExtents.XBearing += ((componentCount - 1 - component) * componentExtents.Width) / componentCount;
                    }

                    componentExtents.Width /= componentCount;
                }
            }

            if (lastCombiningClass != combiningClass)
            {
                lastCombiningClass = combiningClass;
                clusterExtents = componentExtents;
            }

            PositionMark(buffer, i, combiningClass, unitsPerEm, ref clusterExtents);

            ref GlyphShapingPosition markPosition = ref buffer.PositionAt(i);
            markPosition.Bounds.Width = 0;
            markPosition.Bounds.Height = 0;
            markPosition.Bounds.X += xOffset;
            markPosition.Bounds.Y += yOffset;
        }
    }

    /// <summary>
    /// Positions one mark against the current stack extents.
    /// </summary>
    /// <param name="buffer">The positioned shaping buffer.</param>
    /// <param name="index">The zero-based mark index.</param>
    /// <param name="combiningClass">The recategorized combining class.</param>
    /// <param name="unitsPerEm">The font's units per em.</param>
    /// <param name="baseExtents">The extents of the base or current mark stack.</param>
    private static void PositionMark(ShapingBuffer buffer, int index, int combiningClass, int unitsPerEm, ref GlyphExtents baseExtents)
    {
        GlyphExtents markExtents = GetExtents(buffer.MetricsAt(index).Metrics);
        int verticalGap = unitsPerEm / VerticalGapDivisor;
        ref GlyphShapingPosition position = ref buffer.PositionAt(index);
        position.Bounds.X = 0;
        position.Bounds.Y = 0;

        switch (combiningClass)
        {
            case DoubleBelow:
            case DoubleAbove:
                if (buffer[index].Direction == TextDirection.LeftToRight)
                {
                    position.Bounds.X += baseExtents.XBearing + baseExtents.Width - (markExtents.Width / 2) - markExtents.XBearing;
                    break;
                }

                position.Bounds.X += baseExtents.XBearing - (markExtents.Width / 2) - markExtents.XBearing;
                break;
            case AttachedBelowLeft:
            case BelowLeft:
            case AboveLeft:
                position.Bounds.X += baseExtents.XBearing - markExtents.XBearing;
                break;
            case AttachedAboveRight:
            case BelowRight:
            case AboveRight:
                position.Bounds.X += baseExtents.XBearing + baseExtents.Width - markExtents.Width - markExtents.XBearing;
                break;
            default:
                position.Bounds.X += baseExtents.XBearing + ((baseExtents.Width - markExtents.Width) / 2) - markExtents.XBearing;
                break;
        }

        switch (combiningClass)
        {
            case DoubleBelow:
            case BelowLeft:
            case Below:
            case BelowRight:
                baseExtents.Height -= verticalGap;
                goto case AttachedBelow;
            case AttachedBelowLeft:
            case AttachedBelow:
                position.Bounds.Y = baseExtents.YBearing + baseExtents.Height - markExtents.YBearing;
                if ((verticalGap > 0) == (position.Bounds.Y > 0))
                {
                    baseExtents.Height -= position.Bounds.Y;
                    position.Bounds.Y = 0;
                }

                baseExtents.Height += markExtents.Height;
                break;
            case DoubleAbove:
            case AboveLeft:
            case Above:
            case AboveRight:
                baseExtents.YBearing += verticalGap;
                baseExtents.Height -= verticalGap;
                goto case AttachedAbove;
            case AttachedAbove:
            case AttachedAboveRight:
                position.Bounds.Y = baseExtents.YBearing - (markExtents.YBearing + markExtents.Height);
                if ((verticalGap > 0) != (position.Bounds.Y > 0))
                {
                    int correction = -position.Bounds.Y / 2;
                    baseExtents.YBearing += correction;
                    baseExtents.Height -= correction;
                    position.Bounds.Y += correction;
                }

                baseExtents.YBearing -= markExtents.Height;
                baseExtents.Height += markExtents.Height;
                break;
        }
    }

    /// <summary>
    /// Recategorizes script-specific mark-ordering classes into geometric attachment classes.
    /// </summary>
    /// <param name="codePoint">The mark character.</param>
    /// <param name="combiningClass">The mark-ordering class.</param>
    /// <returns>The geometric attachment class.</returns>
    private static int RecategorizeCombiningClass(CodePoint codePoint, int combiningClass)
    {
        if (combiningClass >= FirstPositioningClass)
        {
            return combiningClass;
        }

        if ((codePoint.Value & ~byte.MaxValue) == ThaiLaoBlockPrefix)
        {
            if (combiningClass == 0)
            {
                combiningClass = codePoint.Value switch
                {
                    ThaiMaiHanAkat or ThaiSaraI or ThaiSaraIi or ThaiSaraUe or ThaiSaraUee or ThaiMaiTaikhu or ThaiThanthakhat or ThaiNikhahit or ThaiYamakkan => AboveRight,
                    LaoMaiKan or LaoVowelSignI or LaoVowelSignIi or LaoVowelSignY or LaoVowelSignYy or LaoMaiKon or LaoCancellationMark or LaoNiggahita => Above,
                    LaoSemivowelLo => Below,
                    _ => 0
                };
            }
            else if (codePoint.Value == ThaiPhinthu)
            {
                combiningClass = BelowRight;
            }
        }

        return combiningClass switch
        {
            22 or 15 or 16 or 17 or 23 or 18 or 19 or 20 or 21 or 24 or 25 => Below,
            13 => AttachedAbove,
            10 => AboveRight,
            11 or 14 => AboveLeft,
            26 => Above,
            28 or 29 or 31 or 32 or 27 or 34 or 35 or 36 => Above,
            30 or 33 => Below,
            3 => BelowRight,
            107 => AboveRight,
            118 or 131 or 129 => Below,
            122 or 132 => Above,
            _ => combiningClass
        };
    }

    /// <summary>
    /// Converts glyph bounds into bearing, width, and downward-height extents.
    /// </summary>
    /// <param name="metrics">The glyph metrics.</param>
    /// <returns>The glyph extents.</returns>
    private static GlyphExtents GetExtents(FontGlyphMetrics metrics)
    {
        Bounds bounds = metrics.Bounds;
        return new GlyphExtents((int)bounds.Min.X, (int)bounds.Max.Y, (int)(bounds.Max.X - bounds.Min.X), (int)(bounds.Min.Y - bounds.Max.Y));
    }

    /// <summary>
    /// Stores glyph extents in font coordinates.
    /// </summary>
    private struct GlyphExtents
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphExtents"/> struct.
        /// </summary>
        /// <param name="xBearing">The horizontal bearing.</param>
        /// <param name="yBearing">The vertical bearing.</param>
        /// <param name="width">The glyph width.</param>
        /// <param name="height">The downward glyph height.</param>
        public GlyphExtents(int xBearing, int yBearing, int width, int height)
        {
            this.XBearing = xBearing;
            this.YBearing = yBearing;
            this.Width = width;
            this.Height = height;
        }

        /// <summary>
        /// Gets or sets the horizontal bearing.
        /// </summary>
        public int XBearing { get; set; }

        /// <summary>
        /// Gets or sets the vertical bearing.
        /// </summary>
        public int YBearing { get; set; }

        /// <summary>
        /// Gets or sets the glyph width.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Gets or sets the downward glyph height.
        /// </summary>
        public int Height { get; set; }
    }
}
