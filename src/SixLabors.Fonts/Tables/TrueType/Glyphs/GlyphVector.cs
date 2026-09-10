// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Tables.TrueType.Hinting;

namespace SixLabors.Fonts.Tables.TrueType.Glyphs;

/// <summary>
/// Represents the raw glyph outlines for a given glyph comprised of a collection of glyph table entries.
/// The type is mutable by design to reduce copying during transformation.
/// </summary>
internal struct GlyphVector
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GlyphVector"/> struct.
    /// </summary>
    /// <param name="controlPoints">The control points defining the glyph outline.</param>
    /// <param name="endPoints">The indices of the last point of each contour.</param>
    /// <param name="bounds">The glyph bounding box.</param>
    /// <param name="instructions">The TrueType hinting instructions.</param>
    /// <param name="isComposite">Whether this glyph is a composite glyph.</param>
    internal GlyphVector(
        IList<ControlPoint> controlPoints,
        IReadOnlyList<ushort> endPoints,
        Bounds bounds,
        ReadOnlyMemory<byte> instructions,
        bool isComposite)
    {
        this.ControlPoints = controlPoints;
        this.EndPoints = endPoints;
        this.Bounds = bounds;
        this.Instructions = instructions;
        this.IsComposite = isComposite;
    }

    /// <summary>
    /// Identifies the native scaling expression selected for a reduced ratio.
    /// </summary>
    private enum ScaleRounding
    {
        PowerOfTwo,
        Divide,
        FixedMultiply
    }

    /// <summary>
    /// Gets or sets the control points defining the glyph outline.
    /// </summary>
    public IList<ControlPoint> ControlPoints { get; set; }

    /// <summary>
    /// Gets or sets the indices of the last point of each contour.
    /// </summary>
    public IReadOnlyList<ushort> EndPoints { get; set; }

    /// <summary>
    /// Gets or sets the TrueType hinting instructions for this glyph.
    /// </summary>
    public ReadOnlyMemory<byte> Instructions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a composite glyph.
    /// </summary>
    public bool IsComposite { get; set; }

    /// <summary>
    /// Gets or sets the glyph bounding box.
    /// </summary>
    public Bounds Bounds { get; set; }

    /// <summary>
    /// Gets or sets the composite component information used for gvar variation processing.
    /// Each entry stores the original component offset and the number of control points
    /// contributed by that component, so that TransformPoints can apply per-component
    /// offset deltas to the assembled outline.
    /// Null for simple (non-composite) glyphs.
    /// </summary>
    public CompositeComponent[]? CompositeComponents { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the outline has been grid fitted, either by
    /// successful instruction execution or by geometric fitting. Only fitted outlines
    /// qualify for whole pixel origin snapping at emit time.
    /// </summary>
    public bool IsHinted { get; set; }

    /// <summary>
    /// Gets or sets the hinted advance in whole device pixels, read back from the phantom
    /// points after instruction execution. X holds the horizontal advance and Y the vertical
    /// advance. Zero when the outline has not been hinted.
    /// </summary>
    public Vector2 HintedAdvance { get; set; }

    /// <summary>
    /// Creates an empty glyph vector with no control points or contours.
    /// </summary>
    /// <param name="bounds">The optional bounds to assign to the empty glyph.</param>
    /// <returns>An empty <see cref="GlyphVector"/>.</returns>
    public static GlyphVector Empty(Bounds bounds = default)
        => new(Array.Empty<ControlPoint>(), Array.Empty<ushort>(), bounds, Array.Empty<byte>(), false);

    /// <summary>
    /// Transforms a glyph vector by a specified 3x2 matrix.
    /// </summary>
    /// <param name="src">The glyph vector to transform.</param>
    /// <param name="matrix">The transformation matrix.</param>
    public static void TransformInPlace(ref GlyphVector src, Matrix3x2 matrix)
    {
        IList<ControlPoint> controlPoints = src.ControlPoints;
        for (int i = 0; i < controlPoints.Count; i++)
        {
            ControlPoint point = controlPoints[i];
            point.Point = Vector2.Transform(point.Point, matrix);
            controlPoints[i] = point;
        }

        src.Bounds = Bounds.Transform(src.Bounds, matrix);
    }

    /// <summary>
    /// Quantizes every control point to the nearest sixty-fourth of a pixel, ties away from zero.
    /// Device space outlines live on the 26.6 fixed point grid: the interpreter's instruction set
    /// is specified over it and the classic rasterizers consume it, so a scaled outline adopts the
    /// grid whether or not it is subsequently hinted. Leaving coordinates a hair off the grid makes
    /// every later rounding decision fall on the wrong side whenever the exact value sits on a
    /// rounding boundary.
    /// </summary>
    /// <param name="src">The scaled glyph vector to quantize.</param>
    public static void QuantizeInPlace(ref GlyphVector src)
    {
        IList<ControlPoint> controlPoints = src.ControlPoints;
        for (int i = 0; i < controlPoints.Count; i++)
        {
            ControlPoint point = controlPoints[i];
            point.Point = new Vector2(MathF.Round(point.Point.X * 64F, MidpointRounding.AwayFromZero) / 64F, MathF.Round(point.Point.Y * 64F, MidpointRounding.AwayFromZero) / 64F);
            controlPoints[i] = point;
        }
    }

    /// <summary>
    /// Scales a design-unit outline onto GDI's 26.6 device grid for an integral pixels-per-em
    /// size. This is the identity-matrix path through <c>scl_ComputeScaling</c> and
    /// <c>scl_Scale</c> in fontdrvhost.
    /// </summary>
    /// <param name="src">The design-unit glyph vector to scale in place.</param>
    /// <param name="pixelsPerEm">The integral device pixels per em.</param>
    /// <param name="unitsPerEm">The font design units per em.</param>
    public static void ScaleTrueTypeInPlace(ref GlyphVector src, int pixelsPerEm, int unitsPerEm)
    {
        // scl_InitializeScaling supplies both operands as 16.16 values. ComputeScaling
        // removes their common powers of two before choosing one of three integer rounders;
        // preserving that choice is essential because FRound rounds negative half values
        // toward positive infinity while SRound rounds them away from zero.
        TrueTypeScaler scaler = new(pixelsPerEm << 16, unitsPerEm << 16);
        IList<ControlPoint> controlPoints = src.ControlPoints;
        for (int i = 0; i < controlPoints.Count; i++)
        {
            ControlPoint point = controlPoints[i];
            point.Point = new Vector2(
                TrueTypeScaler.ToFloat(scaler.Scale((int)point.Point.X)),
                TrueTypeScaler.ToFloat(scaler.Scale((int)point.Point.Y)));
            controlPoints[i] = point;
        }

        // The native scaler applies the same operation to the design-unit bounding box
        // used to form phantom points. Keep the cached bounds in the same 26.6 domain as
        // the outline rather than retaining a separately rounded floating-point transform.
        Bounds bounds = src.Bounds;
        src.Bounds = new Bounds(
            TrueTypeScaler.ToFloat(scaler.Scale((int)bounds.Min.X)),
            TrueTypeScaler.ToFloat(scaler.Scale((int)bounds.Min.Y)),
            TrueTypeScaler.ToFloat(scaler.Scale((int)bounds.Max.X)),
            TrueTypeScaler.ToFloat(scaler.Scale((int)bounds.Max.Y)));
    }

    /// <summary>
    /// Applies True Type hinting to the specified glyph vector.
    /// </summary>
    /// <param name="hintingMode">The hinting mode.</param>
    /// <param name="glyph">The glyph vector to hint.</param>
    /// <param name="unscaled">The same outline in font units, which IP interpolates from.</param>
    /// <param name="interpreter">The True Type interpreter.</param>
    /// <param name="pp1">The first phantom point.</param>
    /// <param name="pp2">The second phantom point.</param>
    /// <param name="pp3">The third phantom point.</param>
    /// <param name="pp4">The fourth phantom point.</param>
    /// <param name="unscaledPp1">The first phantom point in font units.</param>
    /// <param name="unscaledPp2">The second phantom point in font units.</param>
    /// <param name="unscaledPp3">The third phantom point in font units.</param>
    /// <param name="unscaledPp4">The fourth phantom point in font units.</param>
    /// <returns><see langword="true"/> if hinting was successfully applied; otherwise, <see langword="false"/>.</returns>
    public static bool Hint(
        HintingMode hintingMode,
        ref GlyphVector glyph,
        in GlyphVector unscaled,
        TrueTypeInterpreter interpreter,
        Vector2 pp1,
        Vector2 pp2,
        Vector2 pp3,
        Vector2 pp4,
        Vector2 unscaledPp1,
        Vector2 unscaledPp2,
        Vector2 unscaledPp3,
        Vector2 unscaledPp4)
    {
        if (hintingMode == HintingMode.None)
        {
            return false;
        }

        // The interpreter stages the outline and phantom points into its own reusable
        // zone buffers, so hinting allocates nothing per glyph.
        if (interpreter.TryHintGlyph(glyph.ControlPoints, unscaled.ControlPoints, pp1, pp2, pp3, pp4, unscaledPp1, unscaledPp2, unscaledPp3, unscaledPp4, glyph.EndPoints, glyph.Instructions, glyph.IsComposite))
        {
            ControlPoint[] hinted = interpreter.GlyphZonePoints;
            int count = interpreter.GlyphZonePointCount;
            Vector2 hintedPP1 = hinted[count - 4].Point;
            Vector2 hintedPP2 = hinted[count - 3].Point;
            Vector2 hintedPP3 = hinted[count - 2].Point;
            Vector2 hintedPP4 = hinted[count - 1].Point;

            for (int i = 0; i < count - 4; i++)
            {
                ControlPoint point = hinted[i];

                // cjFillPolygon converts every outline coordinate relative to the first
                // hinted phantom point. Apply that subtraction in the existing copy pass so
                // the cached outline has GDI's emitted coordinate origin without another
                // traversal or any per-glyph allocation.
                point.Point -= hintedPP1;
                glyph.ControlPoints[i] = point;
            }

            glyph.IsHinted = true;
            glyph.HintedAdvance = new Vector2(MathF.Floor(hintedPP2.X - hintedPP1.X + 0.5F), MathF.Floor(hintedPP3.Y - hintedPP4.Y + 0.5F));

            return true;
        }

        return false;
    }

    /// <summary>
    /// Creates a new glyph vector that is a deep copy of the specified instance.
    /// </summary>
    /// <param name="src">The source glyph vector to copy.</param>
    /// <returns>The cloned <see cref="GlyphVector"/>.</returns>
    public static GlyphVector DeepClone(GlyphVector src)
    {
        List<ControlPoint> controlPoints = [.. src.ControlPoints];
        List<ushort> endPoints = [.. src.EndPoints];

        return new GlyphVector(controlPoints, endPoints, src.Bounds, src.Instructions, src.IsComposite)
        {
            CompositeComponents = src.CompositeComponents is not null
                ? [.. src.CompositeComponents]
                : null,
            IsHinted = src.IsHinted,
            HintedAdvance = src.HintedAdvance
        };
    }

    /// <summary>
    /// Returns a value indicating whether the current instance is empty.
    /// </summary>
    /// <returns>The <see cref="bool"/> indicating the result.</returns>
    public readonly bool HasValue() => this.ControlPoints?.Count > 0;

    /// <summary>
    /// Holds the reduced integer ratio and rounding operation selected by GDI's
    /// <c>scl_ComputeScaling</c> routine.
    /// </summary>
    internal readonly struct TrueTypeScaler
    {
        private readonly int fixedScale;
        private readonly int numerator;
        private readonly int denominator;
        private readonly int shift;
        private readonly ScaleRounding rounding;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrueTypeScaler"/> struct from the
        /// 16.16 device-em and design-em values supplied by <c>scl_InitializeScaling</c>.
        /// </summary>
        /// <param name="deviceEm">The device em size in 16.16 units.</param>
        /// <param name="designEm">The design em size in 16.16 units.</param>
        public TrueTypeScaler(int deviceEm, int designEm)
        {
            int commonShift = BitOperations.TrailingZeroCount((uint)(deviceEm | designEm)) - 1;
            int reducedNumerator = commonShift >= 1 ? deviceEm >> commonShift : deviceEm;
            int reducedDenominator = commonShift >= 1 ? designEm >> commonShift : designEm;

            // ComputeScaling folds the six fractional bits required by the interpreter into
            // the numerator unless that would exceed its signed 26-bit working range.
            if (reducedNumerator < 0x2000000)
            {
                reducedNumerator <<= 6;
            }
            else
            {
                reducedDenominator >>= 6;
            }

            this.fixedScale = DivideF16Dot16(reducedNumerator, reducedDenominator);
            this.numerator = reducedNumerator;
            this.denominator = reducedDenominator;
            if (reducedNumerator < 0x8000)
            {
                if (reducedDenominator != 0 && (reducedDenominator & (reducedDenominator - 1)) == 0)
                {
                    this.shift = BitOperations.TrailingZeroCount((uint)reducedDenominator);
                    this.rounding = ScaleRounding.PowerOfTwo;
                }
                else
                {
                    this.shift = -1;
                    this.rounding = ScaleRounding.Divide;
                }
            }
            else
            {
                this.shift = -1;
                this.rounding = ScaleRounding.FixedMultiply;
            }
        }

        /// <summary>
        /// Scales one integral design coordinate to its signed 26.6 device coordinate using
        /// the rounder selected by <c>scl_ComputeScaling</c>.
        /// </summary>
        /// <param name="value">The design-unit coordinate.</param>
        /// <returns>The scaled signed 26.6 coordinate.</returns>
        public int Scale(int value)
        {
            if (this.rounding == ScaleRounding.PowerOfTwo)
            {
                // scl_FRound deliberately adds the positive half denominator for both signs
                // before its arithmetic shift, so negative ties resolve toward +infinity.
                return unchecked((value * this.numerator) + (this.denominator >> 1)) >> this.shift;
            }

            if (this.rounding == ScaleRounding.Divide)
            {
                int product = unchecked(value * this.numerator);

                // scl_SRound uses signed division, whose truncation toward zero requires the
                // mirrored expression on negative design coordinates.
                return value < 0
                    ? -unchecked(((this.denominator >> 1) - product) / this.denominator)
                    : unchecked(((this.denominator >> 1) + product) / this.denominator);
            }

            long fixedProduct = (long)value * this.fixedScale;
            long rounded = fixedProduct + 0x8000 + (fixedProduct >> 63);
            long result = rounded >> 16;

            // scl_FixRound saturates after its rounded 16.16 multiply.
            return result > int.MaxValue ? int.MaxValue : result < int.MinValue ? int.MinValue : (int)result;
        }

        /// <summary>
        /// Converts a signed 26.6 coordinate into the float storage representation used by
        /// <see cref="ControlPoint"/> without changing its exact binary value.
        /// </summary>
        /// <param name="value">The signed 26.6 coordinate.</param>
        /// <returns>The coordinate in device pixels.</returns>
        public static float ToFloat(int value) => value / 64F;

        /// <summary>
        /// Divides two integers into a saturated 16.16 quotient using GDI's
        /// <c>DWRITE_FixDiv</c> sign-aware half-denominator rounding.
        /// </summary>
        /// <param name="numerator">The quotient numerator.</param>
        /// <param name="denominator">The quotient denominator.</param>
        /// <returns>The saturated signed 16.16 quotient.</returns>
        private static int DivideF16Dot16(int numerator, int denominator)
        {
            if (denominator == 0)
            {
                return int.MaxValue;
            }

            long scaledNumerator = (long)numerator << 16;
            long halfDenominator = denominator / 2;
            bool sameSign = (uint)denominator >> 31 == (ulong)scaledNumerator >> 63;
            long quotient = (scaledNumerator + (sameSign ? halfDenominator : -halfDenominator)) / denominator;
            return quotient > int.MaxValue ? int.MaxValue : quotient < int.MinValue ? int.MinValue : (int)quotient;
        }
    }
}
