// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Runtime.InteropServices;
using System.Text;
using SixLabors.Fonts.Tables.TrueType;
using SixLabors.Fonts.Tables.TrueType.Glyphs;

namespace SixLabors.Fonts.Tests;

/// <summary>
/// Compares our hinted TrueType outline against the outline GDI hints for the same glyph and
/// size, both in outline space with no rasterizer involved, so the interpreter is measured on
/// its own. GDI is the parity target, and GGO_NATIVE returns its hinted outline directly.
/// </summary>
public class GdiOutlineProbe
{
    private const uint GgoNative = 2;
    private const uint GdiError = 0xFFFFFFFF;

    /// <summary>
    /// The largest outline deviation the comparison accepts, in device pixels. Half a pixel is
    /// the largest error that cannot move a centre sampled pixel, so a smaller difference cannot
    /// change the rendered result.
    /// </summary>
    private const double MaxDeviationPx = 0.5;

    [Fact]
    public void CompareHintedOutlineToGdi()
    {
        // GDI is a Windows component. The comparison runs only there.
        if (!TestEnvironment.IsWindows)
        {
            return;
        }

        FontCollection collection = new();
        FontFamily family = collection.Add(TestFonts.Tahoma);

        StringBuilder sb = new();
        double worstOverall = 0;
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        foreach (int ppem in new[] { 8, 10, 12, 16, 24 })
        {
            IntPtr hdc = CreateCompatibleDC(IntPtr.Zero);
            IntPtr hFont = CreateFontW(-ppem, 0, 0, 0, 400, 0, 0, 0, 1, 0, 0, 4, 0, "Tahoma");
            IntPtr old = SelectObject(hdc, hFont);
            Font font = family.CreateFont(ppem);

            double total = 0;
            double worst = 0;
            char worstChar = ' ';
            (double X, double Y) worstOurs = default;
            (double X, double Y) worstTheirs = default;
            int counted = 0;

            foreach (char c in chars)
            {
                font.FontMetrics.TryGetGlyphMetrics(new SixLabors.Fonts.Unicode.CodePoint(c), TextAttributes.None, TextDecorations.None, LayoutMode.HorizontalTopBottom, ColorFontSupport.None, null, out FontGlyphMetrics? metrics);
                if (metrics is not TrueTypeGlyphMetrics tt)
                {
                    continue;
                }

                List<(double X, double Y)> gdi = GdiOutline(hdc, c);
                if (gdi.Count == 0)
                {
                    continue;
                }

                GlyphVector ours = tt.GetScaledOutline(ppem * 72f, HintingMode.Full);
                List<(double X, double Y)> mine = new();
                foreach (ControlPoint cp in ours.ControlPoints)
                {
                    // Only on curve vertices are compared. Off curve control points sit farther
                    // from the outline as the size grows and represent the same curve two ways,
                    // and the four phantom points are marked off curve, so the on curve filter
                    // excludes both without any positional assumption.
                    if (cp.OnCurve)
                    {
                        mine.Add((cp.Point.X, cp.Point.Y));
                    }
                }

                if (mine.Count == 0)
                {
                    continue;
                }

                // The two origin conventions differ by the left side bearing, an integer
                // translation, so the comparison removes it by aligning the minimum X and Y of
                // each point cloud before measuring shape. That isolates hinting from origin
                // placement without a free per glyph best fit.
                Align(mine);
                Align(gdi);

                double d = Hausdorff(mine, gdi, out (double X, double Y) mineAt, out (double X, double Y) gdiAt);
                total += d;
                counted++;
                if (d > worst)
                {
                    worst = d;
                    worstChar = c;
                    worstOurs = mineAt;
                    worstTheirs = gdiAt;
                }
            }

            SelectObject(hdc, old);
            DeleteObject(hFont);
            DeleteDC(hdc);

            double mean = counted > 0 ? total / counted : 0;
            worstOverall = Math.Max(worstOverall, worst);
            sb.AppendLine(FormattableString.Invariant($"ppem {ppem,2}: mean Hausdorff {mean:0.000}px, worst '{worstChar}' {worst:0.000}px at ours ({worstOurs.X:0.000}, {worstOurs.Y:0.000}) gdi ({worstTheirs.X:0.000}, {worstTheirs.Y:0.000})"));
        }

        Assert.True(worstOverall <= MaxDeviationPx, sb.ToString());
    }

    private static void Align(List<(double X, double Y)> points)
    {
        double minX = double.MaxValue;
        double minY = double.MaxValue;
        foreach ((double x, double y) in points)
        {
            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
        }

        for (int i = 0; i < points.Count; i++)
        {
            points[i] = (points[i].X - minX, points[i].Y - minY);
        }
    }

    /// <summary>
    /// Measures the Hausdorff distance between two point clouds and reports the pair of points
    /// that produced it, so a failure names the vertex that moved.
    /// </summary>
    /// <param name="a">The first point cloud.</param>
    /// <param name="b">The second point cloud.</param>
    /// <param name="fromA">The point of <paramref name="a"/> at the worst distance.</param>
    /// <param name="fromB">Its nearest point in <paramref name="b"/>.</param>
    /// <returns>The Hausdorff distance in device pixels.</returns>
    private static double Hausdorff(List<(double X, double Y)> a, List<(double X, double Y)> b, out (double X, double Y) fromA, out (double X, double Y) fromB)
    {
        double forward = Directed(a, b, out (double X, double Y) fa, out (double X, double Y) fb);
        double backward = Directed(b, a, out (double X, double Y) ba, out (double X, double Y) bb);
        if (forward >= backward)
        {
            fromA = fa;
            fromB = fb;
            return forward;
        }

        fromA = bb;
        fromB = ba;
        return backward;
    }

    private static double Directed(List<(double X, double Y)> from, List<(double X, double Y)> to, out (double X, double Y) source, out (double X, double Y) target)
    {
        double worst = 0;
        source = default;
        target = default;
        foreach ((double fx, double fy) in from)
        {
            double nearest = double.MaxValue;
            (double X, double Y) nearestPoint = default;
            foreach ((double tx, double ty) in to)
            {
                double dx = fx - tx;
                double dy = fy - ty;
                double squared = (dx * dx) + (dy * dy);
                if (squared < nearest)
                {
                    nearest = squared;
                    nearestPoint = (tx, ty);
                }
            }

            if (nearest > worst)
            {
                worst = nearest;
                source = (fx, fy);
                target = nearestPoint;
            }
        }

        return Math.Sqrt(worst);
    }

    private static List<(double X, double Y)> GdiOutline(IntPtr hdc, char c)
    {
        Mat2 identity = default;
        identity.EM11Value = 1;
        identity.EM22Value = 1;

        uint size = GetGlyphOutlineW(hdc, c, GgoNative, out _, 0, IntPtr.Zero, ref identity);
        if (size == GdiError || size == 0)
        {
            return new List<(double X, double Y)>();
        }

        IntPtr buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            if (GetGlyphOutlineW(hdc, c, GgoNative, out _, size, buffer, ref identity) == GdiError)
            {
                return new List<(double X, double Y)>();
            }

            List<(double X, double Y)> points = new();
            int offset = 0;
            while (offset < size)
            {
                // TTPOLYGONHEADER: cb, dwType, pfxStart. The contour's on curve start point,
                // then a run of TTPOLYCURVE records filling the header's cb bytes.
                int cb = Marshal.ReadInt32(buffer, offset);
                double startX = ReadFixed(buffer, offset + 8);
                double startY = ReadFixed(buffer, offset + 12);
                points.Add((startX, startY));

                int curveOffset = offset + 16;
                int end = offset + cb;
                while (curveOffset < end)
                {
                    // TTPOLYCURVE: wType, cpfx, then cpfx POINTFX values. In a line record every
                    // point is on curve; in a quadratic spline record the control points are off
                    // curve and only the final point is on curve. Only on curve points are kept.
                    short wType = Marshal.ReadInt16(buffer, curveOffset);
                    short cpfx = Marshal.ReadInt16(buffer, curveOffset + 2);
                    int p = curveOffset + 4;
                    for (int i = 0; i < cpfx; i++)
                    {
                        bool onCurve = wType == 1 || i == cpfx - 1;
                        if (onCurve)
                        {
                            points.Add((ReadFixed(buffer, p), ReadFixed(buffer, p + 4)));
                        }

                        p += 8;
                    }

                    curveOffset = p;
                }

                offset += cb;
            }

            return points;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static double ReadFixed(IntPtr buffer, int offset)
    {
        // FIXED is 16.16 as {WORD fract; short value}.
        ushort fract = (ushort)Marshal.ReadInt16(buffer, offset);
        short value = Marshal.ReadInt16(buffer, offset + 2);
        return value + (fract / 65536.0);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Mat2
    {
        public short EM11Frac, EM11Value, EM12Frac, EM12Value, EM21Frac, EM21Value, EM22Frac, EM22Value;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GlyphMetricsGdi
    {
        public uint BlackBoxX;
        public uint BlackBoxY;
        public int OriginX;
        public int OriginY;
        public short CellIncX;
        public short CellIncY;
    }

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateFontW(int height, int width, int escapement, int orientation, int weight, uint italic, uint underline, uint strikeOut, uint charSet, uint outPrecision, uint clipPrecision, uint quality, uint pitchAndFamily, string faceName);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr obj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
    private static extern uint GetGlyphOutlineW(IntPtr hdc, uint ch, uint format, out GlyphMetricsGdi metrics, uint bufferSize, IntPtr buffer, ref Mat2 mat);
}
