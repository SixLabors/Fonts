// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Xml;
using System.Xml.Linq;
using SixLabors.Fonts.Rendering;

#pragma warning disable SA1201 // Elements should appear in the correct order
namespace SixLabors.Fonts.Tables.Svg;

/// <summary>
/// Supplies painted glyphs (layers + commands + paints) and canvas metadata for OT-SVG glyphs.
/// Geometry coordinates are kept in SVG user space; all transforms are carried as matrices
/// on the canvas (root) and on each layer. No point-transforming is performed here.
/// </summary>
internal sealed class SvgGlyphSource : IPaintedGlyphSource
{
    private readonly SvgTable svgTable;

    // Cache parsed docs by (start, length) slice.
    private static readonly Dictionary<(int Start, int Length), ParsedDoc> DocCache = [];

    private sealed class ParsedDoc
    {
        public required XDocument Doc { get; init; }

        public required ConcurrentDictionary<string, XElement> IdMap { get; init; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SvgGlyphSource"/> class.
    /// </summary>
    /// <param name="svgTable">The SVG table.</param>
    public SvgGlyphSource(SvgTable svgTable) => this.svgTable = svgTable;

    /// <inheritdoc/>
    public bool TryGetPaintedGlyph(ushort glyphId, out PaintedGlyph glyph, out PaintedCanvas canvas)
    {
        glyph = default;
        canvas = default;

        if (!this.TryGetParsedDoc(glyphId, out ParsedDoc? parsed))
        {
            return false;
        }

        XElement root = parsed.Doc.Root!;
        FontRectangle viewBox = GetViewBox(root);
        Matrix3x2 rootTransform = ParseTransform(root.Attribute("transform")?.Value);

        // Prefer a dedicated group with id="glyph{gid}", else fall back to the root.
        string wantedId = "glyph" + glyphId.ToString(CultureInfo.InvariantCulture);
        XElement glyphRoot = parsed.IdMap.TryGetValue(wantedId, out XElement? ge) ? ge : root;

        List<PaintedLayer> layers = [];
        Walk(
            glyphRoot,
            rootTransform,
            inheritedPaint: null,
            outputLayers: layers,
            idMap: parsed.IdMap);

        if (layers.Count == 0)
        {
            return false;
        }

        // TODO: Use IEnumerable.
        glyph = new PaintedGlyph(layers.ToArray());
        canvas = new PaintedCanvas(viewBox, true, rootTransform);
        return true;
    }

    // ---------------------------------------------------------------------
    // Parse & cache
    // ---------------------------------------------------------------------
    private bool TryGetParsedDoc(ushort glyphId, [NotNullWhen(true)] out ParsedDoc? parsed)
    {
        parsed = default;

        if (!this.svgTable.TryGetDocumentSpan(glyphId, out int start, out int length))
        {
            return false;
        }

        (int Start, int Length) key = (start, length);
        if (DocCache.TryGetValue(key, out parsed))
        {
            return true;
        }

        if (!this.svgTable.TryOpenDecodedDocumentStream(glyphId, out Stream stream))
        {
            return false;
        }

        using (stream)
        {
            XDocument doc = LoadXml(stream);
            if (doc.Root is null)
            {
                return false;
            }

            // TODO: How large is this likely to get? If large, consider a more memory-efficient structure.
            ConcurrentDictionary<string, XElement> idMap = new(Environment.ProcessorCount, capacity: 65536, comparer: StringComparer.Ordinal);

            foreach (XElement e in doc.Root.DescendantsAndSelf())
            {
                XAttribute? id = e.Attribute("id");
                if (id is not null)
                {
                    idMap[id.Value] = e; // last-wins
                }
            }

            parsed = new ParsedDoc
            {
                Doc = doc,
                IdMap = idMap
            };

            DocCache[key] = parsed;
            return true;
        }
    }

    private static XDocument LoadXml(Stream stream)
    {
        XmlReaderSettings settings = new()
        {
            DtdProcessing = DtdProcessing.Ignore,
            XmlResolver = null,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true
        };

        using XmlReader reader = XmlReader.Create(stream, settings);
        return XDocument.Load(reader, LoadOptions.None);
    }

    private static FontRectangle GetViewBox(XElement svg)
    {
        if (TryParseViewBox(svg.Attribute("viewBox")?.Value, out float x, out float y, out float w, out float h))
        {
            return new FontRectangle(x, y, w, h);
        }

        // No viewBox; return an empty rect. Metrics layer must decide fallback mapping.
        return FontRectangle.Empty;
    }

    // ---------------------------------------------------------------------
    // Traversal (no point transforms; carry matrices)
    // ---------------------------------------------------------------------
    private static void Walk(
        XElement node,
        Matrix3x2 parentLocalTransform,
        Paint? inheritedPaint,
        List<PaintedLayer> outputLayers,
        ConcurrentDictionary<string, XElement> idMap)
    {
        Matrix3x2 localTransform = parentLocalTransform * ParseTransform(node.Attribute("transform")?.Value);

        FillRule fillRule = ResolveFillRule(node, FillRule.NonZero);
        Paint? paint = ResolvePaint(node, inheritedPaint, idMap, out bool fillNone, out float opacityMul);

        string name = node.Name.LocalName;
        switch (name)
        {
            case "svg":
            case "g":
            {
                foreach (XElement child in node.Elements())
                {
                    Walk(child, localTransform, fillNone ? null : paint, outputLayers, idMap);
                }

                break;
            }

            case "use":
            {
                string? href = GetHref(node);
                if (href is null)
                {
                    break;
                }

                float ux = ParseFloat(node.Attribute("x")?.Value);
                float uy = ParseFloat(node.Attribute("y")?.Value);
                Matrix3x2 xf = localTransform * Matrix3x2.CreateTranslation(ux, uy);

                Paint? usePaint = ResolvePaint(node, paint, idMap, out bool useNone, out float _);
                Paint? childInherited = useNone ? null : usePaint;

                XElement? target = LookupById(idMap, href);
                if (target is not null)
                {
                    Walk(target, xf, childInherited, outputLayers, idMap);
                }

                break;
            }

            case "path":
            {
                if (fillNone)
                {
                    break;
                }

                string? d = node.Attribute("d")?.Value;
                if (string.IsNullOrWhiteSpace(d))
                {
                    break;
                }

                List<PathCommand> cmds = BuildCommandsFromPathData(d);
                if (cmds.Count > 0)
                {
                    Paint? layerPaint = ApplyOpacityToPaint(paint, opacityMul);
                    outputLayers.Add(new(layerPaint, fillRule, localTransform, cmds));
                }

                break;
            }

            case "polygon":
            case "polyline":
            {
                if (fillNone)
                {
                    break;
                }

                string pts = node.Attribute("points")?.Value ?? string.Empty;
                float[] coords = ParseFloatList(pts);
                if (coords.Length >= 4)
                {
                    bool close = string.Equals(node.Name.LocalName, "polygon", StringComparison.Ordinal);
                    List<PathCommand> cmds = BuildCommandsFromPoly(coords, close);
                    if (cmds.Count > 0)
                    {
                        Paint? layerPaint = ApplyOpacityToPaint(paint, opacityMul);
                        outputLayers.Add(new(layerPaint, fillRule, localTransform, cmds));
                    }
                }

                break;
            }

            case "rect":
            {
                if (fillNone)
                {
                    break;
                }

                float x = ParseFloat(node.Attribute("x")?.Value);
                float y = ParseFloat(node.Attribute("y")?.Value);
                float w = ParseFloat(node.Attribute("width")?.Value);
                float h = ParseFloat(node.Attribute("height")?.Value);

                // Rounded corners (rx/ry) not handled here (could be approximated later if needed).
                if (w > 0f && h > 0f)
                {
                    float[] coords =
                    [
                        x, y,
                        x + w, y,
                        x + w, y + h,
                        x, y + h
                    ];

                    List<PathCommand> cmds = BuildCommandsFromPoly(coords, close: true);
                    if (cmds.Count > 0)
                    {
                        Paint? layerPaint = ApplyOpacityToPaint(paint, opacityMul);
                        outputLayers.Add(new(layerPaint, fillRule, localTransform, cmds));
                    }
                }

                break;
            }

            case "circle":
            {
                if (fillNone)
                {
                    break;
                }

                float cx = ParseFloat(node.Attribute("cx")?.Value);
                float cy = ParseFloat(node.Attribute("cy")?.Value);
                float r = ParseFloat(node.Attribute("r")?.Value);
                if (r > 0f)
                {
                    List<PathCommand> cmds = BuildCommandsForEllipse(cx, cy, r, r);
                    if (cmds.Count > 0)
                    {
                        Paint? layerPaint = ApplyOpacityToPaint(paint, opacityMul);
                        outputLayers.Add(new(layerPaint, fillRule, localTransform, cmds));
                    }
                }

                break;
            }

            case "ellipse":
            {
                if (fillNone)
                {
                    break;
                }

                float cx = ParseFloat(node.Attribute("cx")?.Value);
                float cy = ParseFloat(node.Attribute("cy")?.Value);
                float rx = ParseFloat(node.Attribute("rx")?.Value);
                float ry = ParseFloat(node.Attribute("ry")?.Value);
                if (rx > 0f && ry > 0f)
                {
                    List<PathCommand> cmds = BuildCommandsForEllipse(cx, cy, rx, ry);
                    if (cmds.Count > 0)
                    {
                        Paint? layerPaint = ApplyOpacityToPaint(paint, opacityMul);
                        outputLayers.Add(new(layerPaint, fillRule, localTransform, cmds));
                    }
                }

                break;
            }

            default:
            {
                // Unhandled (image, text, mask, clipPath, etc.) in v1.
                break;
            }
        }
    }

    // ---------------------------------------------------------------------
    // Paint resolution (solid + linear/radial/sweep). No transforms applied.
    // ---------------------------------------------------------------------
    private static Paint? ApplyOpacityToPaint(Paint? basePaint, float opacityMul)
    {
        if (basePaint is null)
        {
            return null;
        }

        float effective = Math.Clamp(basePaint.Opacity * opacityMul, 0f, 1f);
        if (effective <= 0f)
        {
            return null;
        }

        return basePaint switch
        {
            SolidPaint s => new SolidPaint { Color = s.Color, Opacity = effective },
            LinearGradientPaint lg => new LinearGradientPaint
            {
                Units = lg.Units,
                P0 = lg.P0,
                P1 = lg.P1,
                Spread = lg.Spread,
                Stops = lg.Stops,
                Transform = lg.Transform,
                Opacity = effective
            },
            RadialGradientPaint rg => new RadialGradientPaint
            {
                Units = rg.Units,
                Center0 = rg.Center0,
                Radius0 = rg.Radius0,
                Center1 = rg.Center1,
                Radius1 = rg.Radius1,
                Spread = rg.Spread,
                Stops = rg.Stops,
                Transform = rg.Transform,
                Opacity = effective
            },
            SweepGradientPaint sg => new SweepGradientPaint
            {
                Units = sg.Units,
                Center = sg.Center,
                StartAngle = sg.StartAngle,
                EndAngle = sg.EndAngle,
                Spread = sg.Spread,
                Stops = sg.Stops,
                Transform = sg.Transform,
                Opacity = effective
            },
            _ => null,
        };
    }

    private static FillRule ResolveFillRule(XElement e, FillRule inheritedDefault)
    {
        string? styleRule = TryCss(e.Attribute("style")?.Value, "fill-rule");
        string? attrRule = e.Attribute("fill-rule")?.Value;
        string? value = styleRule ?? attrRule;

        if (string.Equals(value, "evenodd", StringComparison.OrdinalIgnoreCase))
        {
            return FillRule.EvenOdd;
        }

        if (string.Equals(value, "nonzero", StringComparison.OrdinalIgnoreCase))
        {
            return FillRule.NonZero;
        }

        return inheritedDefault;
    }

    private static Paint? ResolvePaint(
        XElement e,
        Paint? inherited,
        ConcurrentDictionary<string, XElement> idMap,
        out bool fillNone,
        out float opacityMul)
    {
        fillNone = false;
        opacityMul = 1f;

        string? style = e.Attribute("style")?.Value;
        string? fillAttr = e.Attribute("fill")?.Value;
        string? opacityAttr = e.Attribute("opacity")?.Value;
        string? fillOpacityAttr = e.Attribute("fill-opacity")?.Value;

        string? styleFill = TryCss(style, "fill");
        string? styleOpacity = TryCss(style, "opacity");
        string? styleFillOpacity = TryCss(style, "fill-opacity");

        string? fill = styleFill ?? fillAttr;
        string? op = styleOpacity ?? opacityAttr;
        string? fop = styleFillOpacity ?? fillOpacityAttr;

        if (!string.IsNullOrEmpty(op) && float.TryParse(op, NumberStyles.Float, CultureInfo.InvariantCulture, out float o))
        {
            opacityMul *= Math.Clamp(o, 0f, 1f);
        }

        if (!string.IsNullOrEmpty(fop) && float.TryParse(fop, NumberStyles.Float, CultureInfo.InvariantCulture, out float fo))
        {
            opacityMul *= Math.Clamp(fo, 0f, 1f);
        }

        if (string.IsNullOrEmpty(fill))
        {
            return inherited;
        }

        if (string.Equals(fill, "none", StringComparison.OrdinalIgnoreCase))
        {
            fillNone = true;
            return null;
        }

        if (TryParseColor(fill, out GlyphColor color))
        {
            return new SolidPaint { Color = color };
        }

        if (TryExtractUrlId(fill, out string? paintId) && paintId is not null)
        {
            return ResolvePaintServer(paintId, idMap) ?? inherited;
        }

        return inherited;
    }

    private static Paint? ResolvePaintServer(string id, ConcurrentDictionary<string, XElement> idMap)
    {
        if (!idMap.TryGetValue(id, out XElement? server))
        {
            return null;
        }

        string tag = server.Name.LocalName;
        return tag switch
        {
            "linearGradient" => BuildLinearGradient(server, idMap),
            "radialGradient" => BuildRadialGradient(server, idMap),
            "sweepGradient" => BuildSweepGradient(server, idMap),
            _ => null
        };
    }

    private static LinearGradientPaint? BuildLinearGradient(XElement grad, ConcurrentDictionary<string, XElement> idMap)
    {
        GradientUnits units = GradientUnits.ObjectBoundingBox;
        SpreadMethod spread = SpreadMethod.Pad;
        Matrix3x2 gxf = Matrix3x2.Identity;

        float? x1 = null, y1 = null, x2 = null, y2 = null;
        List<(float Offset, GlyphColor Color)> stops = [];

        HashSet<string> visited = new(StringComparer.Ordinal);
        XElement? cur = grad;

        while (cur is not null)
        {
            string? u = cur.Attribute("gradientUnits")?.Value;
            if (u is not null)
            {
                units = ParseGradientUnits(u);
            }

            string? sm = cur.Attribute("spreadMethod")?.Value;
            if (sm is not null)
            {
                spread = ParseSpreadMethod(sm);
            }

            gxf = ParseTransform(cur.Attribute("gradientTransform")?.Value) * gxf;

            x1 ??= ParseCoordNullable(cur.Attribute("x1")?.Value, units);
            y1 ??= ParseCoordNullable(cur.Attribute("y1")?.Value, units);
            x2 ??= ParseCoordNullable(cur.Attribute("x2")?.Value, units);
            y2 ??= ParseCoordNullable(cur.Attribute("y2")?.Value, units);

            bool hadStops = false;
            foreach (XElement s in cur.Elements())
            {
                if (s.Name.LocalName != "stop")
                {
                    continue;
                }

                if (TryParseStop(s, out float off, out GlyphColor c))
                {
                    stops.Add((off, c));
                    hadStops = true;
                }
            }

            if (hadStops)
            {
                break;
            }

            string? href = GetHref(cur);
            if (href is null || href.Length <= 1 || href[0] != '#')
            {
                break;
            }

            string refId = href[1..];
            if (!visited.Add(refId) || !idMap.TryGetValue(refId, out cur))
            {
                break;
            }
        }

        if (!x1.HasValue)
        {
            x1 = 0f;
        }

        if (!y1.HasValue)
        {
            y1 = 0f;
        }

        if (!x2.HasValue)
        {
            x2 = units == GradientUnits.ObjectBoundingBox ? 1f : 0f;
        }

        if (!y2.HasValue)
        {
            y2 = 0f;
        }

        GradientStop[] gs = BuildStopsArray(stops);

        return new LinearGradientPaint
        {
            Units = units,
            P0 = new Vector2(x1.Value, y1.Value),
            P1 = new Vector2(x2.Value, y2.Value),
            Spread = spread,
            Stops = gs,
            Transform = gxf
        };
    }

    private static RadialGradientPaint? BuildRadialGradient(XElement grad, ConcurrentDictionary<string, XElement> idMap)
    {
        GradientUnits units = GradientUnits.ObjectBoundingBox;
        SpreadMethod spread = SpreadMethod.Pad;
        Matrix3x2 gxf = Matrix3x2.Identity;

        float? cx = null, cy = null, r = null, fx = null, fy = null, fr = null;
        List<(float Offset, GlyphColor Color)> stops = [];

        HashSet<string> visited = new(StringComparer.Ordinal);
        XElement? cur = grad;

        while (cur is not null)
        {
            string? u = cur.Attribute("gradientUnits")?.Value;
            if (u is not null)
            {
                units = ParseGradientUnits(u);
            }

            string? sm = cur.Attribute("spreadMethod")?.Value;
            if (sm is not null)
            {
                spread = ParseSpreadMethod(sm);
            }

            gxf = ParseTransform(cur.Attribute("gradientTransform")?.Value) * gxf;

            cx ??= ParseCoordNullable(cur.Attribute("cx")?.Value, units);
            cy ??= ParseCoordNullable(cur.Attribute("cy")?.Value, units);
            r ??= ParseRadiusNullable(cur.Attribute("r")?.Value, units);
            fx ??= ParseCoordNullable(cur.Attribute("fx")?.Value, units);
            fy ??= ParseCoordNullable(cur.Attribute("fy")?.Value, units);
            fr ??= ParseRadiusNullable(cur.Attribute("fr")?.Value, units);

            bool hadStops = false;
            foreach (XElement s in cur.Elements())
            {
                if (s.Name.LocalName != "stop")
                {
                    continue;
                }

                if (TryParseStop(s, out float off, out GlyphColor c))
                {
                    stops.Add((off, c));
                    hadStops = true;
                }
            }

            if (hadStops)
            {
                break;
            }

            string? href = GetHref(cur);
            if (href is null || href.Length <= 1 || href[0] != '#')
            {
                break;
            }

            string refId = href[1..];
            if (!visited.Add(refId) || !idMap.TryGetValue(refId, out cur))
            {
                break;
            }
        }

        if (!cx.HasValue)
        {
            cx = units == GradientUnits.ObjectBoundingBox ? 0.5f : 0f;
        }

        if (!cy.HasValue)
        {
            cy = units == GradientUnits.ObjectBoundingBox ? 0.5f : 0f;
        }

        if (!r.HasValue)
        {
            r = units == GradientUnits.ObjectBoundingBox ? 0.5f : 0f;
        }

        if (!fx.HasValue)
        {
            fx = cx.Value;
        }

        if (!fy.HasValue)
        {
            fy = cy.Value;
        }

        if (!fr.HasValue)
        {
            fr = 0f;
        }

        GradientStop[] gs = BuildStopsArray(stops);

        // Center0=(fx,fy), Radius0=fr; Center1=(cx,cy), Radius1=r
        return new RadialGradientPaint
        {
            Units = units,
            Center0 = new Vector2(fx.Value, fy.Value),
            Radius0 = fr.Value,
            Center1 = new Vector2(cx.Value, cy.Value),
            Radius1 = r.Value,
            Spread = spread,
            Stops = gs,
            Transform = gxf
        };
    }

    private static SweepGradientPaint? BuildSweepGradient(XElement grad, ConcurrentDictionary<string, XElement> idMap)
    {
        GradientUnits units = GradientUnits.ObjectBoundingBox;
        SpreadMethod spread = SpreadMethod.Pad;
        Matrix3x2 gxf = Matrix3x2.Identity;

        float? cx = null, cy = null, startDeg = null, endDeg = null;
        List<(float Offset, GlyphColor Color)> stops = [];

        HashSet<string> visited = new(StringComparer.Ordinal);
        XElement? cur = grad;

        while (cur is not null)
        {
            string? u = cur.Attribute("gradientUnits")?.Value;
            if (u is not null)
            {
                units = ParseGradientUnits(u);
            }

            string? sm = cur.Attribute("spreadMethod")?.Value;
            if (sm is not null)
            {
                spread = ParseSpreadMethod(sm);
            }

            gxf = ParseTransform(cur.Attribute("gradientTransform")?.Value) * gxf;

            cx ??= ParseCoordNullable(cur.Attribute("cx")?.Value, units);
            cy ??= ParseCoordNullable(cur.Attribute("cy")?.Value, units);
            startDeg ??= ParseAngleNullable(cur.Attribute("startAngle")?.Value);
            endDeg ??= ParseAngleNullable(cur.Attribute("endAngle")?.Value);

            bool hadStops = false;
            foreach (XElement s in cur.Elements())
            {
                if (s.Name.LocalName != "stop")
                {
                    continue;
                }

                if (TryParseStop(s, out float off, out GlyphColor c))
                {
                    stops.Add((off, c));
                    hadStops = true;
                }
            }

            if (hadStops)
            {
                break;
            }

            string? href = GetHref(cur);
            if (href is null || href.Length <= 1 || href[0] != '#')
            {
                break;
            }

            string refId = href[1..];
            if (!visited.Add(refId) || !idMap.TryGetValue(refId, out cur))
            {
                break;
            }
        }

        if (!cx.HasValue)
        {
            cx = units == GradientUnits.ObjectBoundingBox ? 0.5f : 0f;
        }

        if (!cy.HasValue)
        {
            cy = units == GradientUnits.ObjectBoundingBox ? 0.5f : 0f;
        }

        if (!startDeg.HasValue)
        {
            startDeg = 0f;
        }

        if (!endDeg.HasValue)
        {
            endDeg = 360f;
        }

        GradientStop[] gs = BuildStopsArray(stops);

        return new SweepGradientPaint
        {
            Units = units,
            Center = new Vector2(cx.Value, cy.Value),
            StartAngle = startDeg.Value,
            EndAngle = endDeg.Value,
            Spread = spread,
            Stops = gs,
            Transform = gxf
        };
    }

    private static SpreadMethod ParseSpreadMethod(string value)
    {
        if (string.Equals(value, "reflect", StringComparison.OrdinalIgnoreCase))
        {
            return SpreadMethod.Reflect;
        }

        if (string.Equals(value, "repeat", StringComparison.OrdinalIgnoreCase))
        {
            return SpreadMethod.Repeat;
        }

        return SpreadMethod.Pad;
    }

    private static GradientUnits ParseGradientUnits(string value)
        => string.Equals(value, "userSpaceOnUse", StringComparison.OrdinalIgnoreCase)
            ? GradientUnits.UserSpaceOnUse
            : GradientUnits.ObjectBoundingBox;

    private static float? ParseCoordNullable(string? s, GradientUnits units)
    {
        if (string.IsNullOrEmpty(s))
        {
            return null;
        }

        if (s!.EndsWith('%'))
        {
            if (float.TryParse(s.AsSpan(0, s.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out float p))
            {
                return p / 100f;
            }

            return null;
        }

        if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
        {
            return v; // In OBB this is already a fraction; in userSpace it is absolute user units.
        }

        return null;
    }

    private static float? ParseRadiusNullable(string? s, GradientUnits units)
        => ParseCoordNullable(s, units);

    private static float? ParseAngleNullable(string? s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return null;
        }

        ReadOnlySpan<char> span = s.AsSpan().Trim();

        if (span.EndsWith("deg".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            return float.TryParse(span[..^3], NumberStyles.Float, CultureInfo.InvariantCulture, out float vDeg) ? vDeg : null;
        }

        if (span.EndsWith("rad".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            return float.TryParse(span[..^3], NumberStyles.Float, CultureInfo.InvariantCulture, out float vRad)
                ? vRad * (180f / (float)Math.PI)
                : null;
        }

        if (span.EndsWith("grad".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            return float.TryParse(span[..^4], NumberStyles.Float, CultureInfo.InvariantCulture, out float vGrad)
                ? vGrad * 0.9f
                : null;
        }

        if (span.EndsWith("turn".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            return float.TryParse(span[..^4], NumberStyles.Float, CultureInfo.InvariantCulture, out float vTurn)
                ? vTurn * 360f
                : null;
        }

        return float.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : null;
    }

    private static bool TryParseStop(XElement stop, out float offset, out GlyphColor color)
    {
        offset = 0f;
        color = default;

        string? style = stop.Attribute("style")?.Value;
        string? offAttr = stop.Attribute("offset")?.Value;

        string? sc = stop.Attribute("stop-color")?.Value ?? TryCss(style, "stop-color");
        string? so = stop.Attribute("stop-opacity")?.Value ?? TryCss(style, "stop-opacity");

        if (!string.IsNullOrEmpty(offAttr))
        {
            if (offAttr.EndsWith('%'))
            {
                if (float.TryParse(offAttr.AsSpan(0, offAttr.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out float p))
                {
                    offset = Math.Clamp(p / 100f, 0f, 1f);
                }
            }
            else if (float.TryParse(offAttr, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
            {
                offset = Math.Clamp(v, 0f, 1f);
            }
        }

        GlyphColor baseColor = new(0, 0, 0, 255);
        if (!string.IsNullOrEmpty(sc) && TryParseColor(sc, out GlyphColor parsed))
        {
            baseColor = parsed;
        }

        float aMul = 1f;
        if (!string.IsNullOrEmpty(so) && float.TryParse(so, NumberStyles.Float, CultureInfo.InvariantCulture, out float soVal))
        {
            aMul = Math.Clamp(soVal, 0f, 1f);
        }

        byte a = (byte)Math.Clamp((int)Math.Round(baseColor.Alpha * aMul), 0, 255);
        color = new GlyphColor(baseColor.Red, baseColor.Green, baseColor.Blue, a);
        return true;
    }

    private static GradientStop[] BuildStopsArray(List<(float Offset, GlyphColor Color)> list)
    {
        if (list.Count == 0)
        {
            return
            [
                new GradientStop(0f, new GlyphColor(0, 0, 0, 255)),
                new GradientStop(1f, new GlyphColor(0, 0, 0, 255))
            ];
        }

        list.Sort((a, b) => a.Offset.CompareTo(b.Offset));
        GradientStop[] stops = new GradientStop[list.Count];
        for (int i = 0; i < list.Count; i++)
        {
            (float o, GlyphColor c) = list[i];
            stops[i] = new GradientStop(Math.Clamp(o, 0f, 1f), c);
        }

        return stops;
    }

    private static string? TryCss(string? style, string prop)
    {
        if (string.IsNullOrEmpty(style))
        {
            return null;
        }

        string[] parts = style.Split(';', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i];
            int c = part.IndexOf(':');
            if (c <= 0)
            {
                continue;
            }

            string name = part.AsSpan(0, c).Trim().ToString();
            if (name.Equals(prop, StringComparison.OrdinalIgnoreCase))
            {
                return part.AsSpan(c + 1).Trim().ToString();
            }
        }

        return null;
    }

    private static bool TryParseColor(string s, out GlyphColor color)
    {
        color = default;

        if (GlyphColor.TryParseHex(s, out GlyphColor? hex))
        {
            color = hex.Value;
            return true;
        }

        if (s.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
        {
            int l = s.IndexOf('(');
            int r = s.IndexOf(')');
            if (l >= 0 && r > l)
            {
                // TODO: Investigate spans to avoid allocations.
                string[] comps = s.Substring(l + 1, r - l - 1).Split(',');
                if (comps.Length >= 3)
                {
                    byte rr = ParseByte(comps[0]);
                    byte gg = ParseByte(comps[1]);
                    byte bb = ParseByte(comps[2]);
                    byte aa = 255;
                    if (comps.Length >= 4 && float.TryParse(comps[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float af))
                    {
                        aa = (byte)Math.Clamp((int)Math.Round(255f * af), 0, 255);
                    }

                    color = new GlyphColor(rr, gg, bb, aa);
                    return true;
                }
            }
        }

        return false;

        static byte ParseByte(ReadOnlySpan<char> x)
        {
            if (x.IsEmpty)
            {
                return 0;
            }

            ReadOnlySpan<char> t = x.Trim();
            if (t[^1] == '%')
            {
                if (float.TryParse(t[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out float p))
                {
                    return (byte)Math.Clamp((int)Math.Round(255f * (p / 100f)), 0, 255);
                }

                return 0;
            }

            if (int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
            {
                return (byte)Math.Clamp(v, 0, 255);
            }

            return 0;
        }
    }

    private static bool TryExtractUrlId(string s, [NotNullWhen(true)] out string? id)
    {
        id = null;

        int lp = s.IndexOf("url(", StringComparison.OrdinalIgnoreCase);
        if (lp < 0)
        {
            return false;
        }

        int rp = s.IndexOf(')', lp + 4);
        if (rp < 0)
        {
            return false;
        }

        string inner = s[(lp + 4)..rp].Trim();
        if (inner.Length > 1 && inner[0] == '#')
        {
            id = inner[1..];
            return true;
        }

        return false;
    }

    private static XElement? LookupById(ConcurrentDictionary<string, XElement> idMap, string href)
    {
        if (string.IsNullOrEmpty(href) || href[0] != '#')
        {
            return null;
        }

        return idMap.TryGetValue(href.AsSpan(1).ToString(), out XElement? e) ? e : null;
    }

    private static string? GetHref(XElement e)
    {
        XNamespace xlink = "http://www.w3.org/1999/xlink";
        return e.Attribute(xlink + "href")?.Value ?? e.Attribute("href")?.Value;
    }

    // ---------------------------------------------------------------------
    // Geometry builders (no transforms applied to points)
    // ---------------------------------------------------------------------
    private static List<PathCommand> BuildCommandsFromPoly(float[] coords, bool close)
    {
        List<PathCommand> cmds = [];

        Vector2 start = new(coords[0], coords[1]);
        cmds.Add(PathCommand.MoveTo(start));

        Vector2 prev = start;
        for (int i = 2; i + 1 < coords.Length; i += 2)
        {
            Vector2 p = new(coords[i], coords[i + 1]);
            if (!NearlyEqual(prev, p))
            {
                cmds.Add(PathCommand.LineTo(p));
                prev = p;
            }
        }

        if (close && !NearlyEqual(prev, start))
        {
            cmds.Add(PathCommand.LineTo(start));
            cmds.Add(PathCommand.Close());
        }

        return cmds;
    }

    private static List<PathCommand> BuildCommandsForEllipse(float cx, float cy, float rx, float ry)
    {
        List<PathCommand> cmds = [];

        // Start at (cx + rx, cy)
        Vector2 s = new(cx + rx, cy);
        cmds.Add(PathCommand.MoveTo(s));

        // First half to (cx - rx, cy)
        Vector2 p1 = new(cx - rx, cy);
        cmds.Add(PathCommand.ArcTo(rx, ry, 0f, true, true, p1));

        // Second half back to start
        Vector2 p2 = new(cx + rx, cy);
        cmds.Add(PathCommand.ArcTo(rx, ry, 0f, true, true, p2));

        cmds.Add(PathCommand.Close());
        return cmds;
    }

    private static List<PathCommand> BuildCommandsFromPathData(string d)
    {
        List<PathCommand> cmds = [];

        ReadOnlySpan<char> s = d.AsSpan();

        Vector2 first = default;
        Vector2 curr = default;
        Vector2 lastc = default;

        Vector2 p1, p2, p3;

        char op = '\0';
        char prevOp = '\0';
        bool rel = false;
        bool figureOpen = false;

        while (true)
        {
            s = s.TrimStart();
            if (s.Length == 0)
            {
                break;
            }

            char ch = s[0];
            if (char.IsDigit(ch) || ch == '-' || ch == '+' || ch == '.')
            {
                if (s.Length == 0 || op == 'Z')
                {
                    return [];
                }
            }
            else if (IsSeparator(ch))
            {
                s = TrimSeparator(s);
            }
            else
            {
                op = ch;
                rel = false;
                if (char.IsLower(op))
                {
                    op = char.ToUpper(op, CultureInfo.InvariantCulture);
                    rel = true;
                }

                s = TrimSeparator(s[1..]);
            }

            switch (op)
            {
                case 'M':
                {
                    s = FindPoint(s, rel, curr, out p1);

                    if (figureOpen)
                    {
                        cmds.Add(PathCommand.Close());
                    }

                    cmds.Add(PathCommand.MoveTo(p1));
                    first = curr = p1;
                    prevOp = '\0';
                    op = 'L';
                    figureOpen = true;
                    break;
                }

                case 'L':
                {
                    s = FindPoint(s, rel, curr, out p1);
                    if (!NearlyEqual(p1, curr))
                    {
                        cmds.Add(PathCommand.LineTo(p1));
                    }

                    curr = p1;
                    break;
                }

                case 'H':
                {
                    s = FindScaler(s, out float x);
                    if (rel)
                    {
                        x += curr.X;
                    }

                    p1 = new Vector2(x, curr.Y);
                    if (!NearlyEqual(p1, curr))
                    {
                        cmds.Add(PathCommand.LineTo(p1));
                    }

                    curr = p1;
                    break;
                }

                case 'V':
                {
                    s = FindScaler(s, out float y);
                    if (rel)
                    {
                        y += curr.Y;
                    }

                    p1 = new Vector2(curr.X, y);
                    if (!NearlyEqual(p1, curr))
                    {
                        cmds.Add(PathCommand.LineTo(p1));
                    }

                    curr = p1;
                    break;
                }

                case 'C':
                {
                    s = FindPoint(s, rel, curr, out p1);
                    s = FindPoint(s, rel, curr, out p2);
                    s = FindPoint(s, rel, curr, out p3);

                    cmds.Add(PathCommand.CubicTo(p1, p2, p3));

                    lastc = p2;
                    curr = p3;
                    break;
                }

                case 'S':
                {
                    s = FindPoint(s, rel, curr, out p2);
                    s = FindPoint(s, rel, curr, out p3);

                    p1 = curr;
                    if (prevOp is 'C' or 'S')
                    {
                        p1.X -= lastc.X - curr.X;
                        p1.Y -= lastc.Y - curr.Y;
                    }

                    cmds.Add(PathCommand.CubicTo(p1, p2, p3));

                    lastc = p2;
                    curr = p3;
                    break;
                }

                case 'Q':
                {
                    s = FindPoint(s, rel, curr, out p1);
                    s = FindPoint(s, rel, curr, out p2);

                    cmds.Add(PathCommand.QuadraticTo(p1, p2));

                    lastc = p1;
                    curr = p2;
                    break;
                }

                case 'T':
                {
                    s = FindPoint(s, rel, curr, out p2);

                    p1 = curr;
                    if (prevOp is 'Q' or 'T')
                    {
                        p1.X -= lastc.X - curr.X;
                        p1.Y -= lastc.Y - curr.Y;
                    }

                    cmds.Add(PathCommand.QuadraticTo(p1, p2));

                    lastc = p1;
                    curr = p2;
                    break;
                }

                case 'A':
                {
                    if (TryFindScaler(ref s, out float rx)
                        && TryTrimSeparator(ref s)
                        && TryFindScaler(ref s, out float ry)
                        && TryTrimSeparator(ref s)
                        && TryFindScaler(ref s, out float angle)
                        && TryTrimSeparator(ref s)
                        && TryFindScaler(ref s, out float largeArc)
                        && TryTrimSeparator(ref s)
                        && TryFindScaler(ref s, out float sweep)
                        && TryFindPoint(ref s, rel, curr, out p1))
                    {
                        cmds.Add(PathCommand.ArcTo(rx, ry, angle, largeArc == 1, sweep == 1, p1));
                        curr = p1;
                    }

                    break;
                }

                case 'Z':
                {
                    if (figureOpen)
                    {
                        if (!NearlyEqual(curr, first))
                        {
                            cmds.Add(PathCommand.LineTo(first));
                        }

                        cmds.Add(PathCommand.Close());
                        curr = first;
                        figureOpen = false;
                    }

                    break;
                }

                default:
                {
                    return [];
                }
            }

            if (prevOp == 0)
            {
                first = curr;
            }

            prevOp = op;
            if (op == 'M')
            {
                figureOpen = true;
            }
        }

        return cmds;
    }

    // ---------------------------------------------------------------------
    // Root + common helpers
    // ---------------------------------------------------------------------
    private static bool TryParseViewBox(string? s, out float x, out float y, out float w, out float h)
    {
        x = 0f;
        y = 0f;
        w = 0f;
        h = 0f;

        if (string.IsNullOrEmpty(s))
        {
            return false;
        }

        float[] v = ParseFloatList(s);
        if (v.Length == 4)
        {
            x = v[0];
            y = v[1];
            w = v[2];
            h = v[3];
            return true;
        }

        return false;
    }

    private static Matrix3x2 ParseTransform(string? s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return Matrix3x2.Identity;
        }

        Matrix3x2 m = Matrix3x2.Identity;
        int i = 0;
        int n = s.Length;

        while (i < n)
        {
            SkipSep(s, ref i);
            if (i >= n)
            {
                break;
            }

            int start = i;
            while (i < n && char.IsLetter(s[i]))
            {
                i++;
            }

            string op = s[start..i];

            SkipSep(s, ref i);
            if (i >= n || s[i] != '(')
            {
                break;
            }

            i++; // '('

            int argsStart = i;
            int depth = 1;
            while (i < n && depth > 0)
            {
                if (s[i] == '(')
                {
                    depth++;
                }
                else if (s[i] == ')')
                {
                    depth--;
                }

                i++;
            }

            string args = s.Substring(argsStart, (i - argsStart) - 1);
            float[] a = ParseFloatList(args);

            Matrix3x2 t = Matrix3x2.Identity;
            switch (op)
            {
                case "matrix":
                {
                    if (a.Length >= 6)
                    {
                        t = new Matrix3x2(a[0], a[1], a[2], a[3], a[4], a[5]);
                    }

                    break;
                }

                case "translate":
                {
                    if (a.Length == 1)
                    {
                        t = Matrix3x2.CreateTranslation(a[0], 0f);
                    }
                    else if (a.Length >= 2)
                    {
                        t = Matrix3x2.CreateTranslation(a[0], a[1]);
                    }

                    break;
                }

                case "scale":
                {
                    if (a.Length == 1)
                    {
                        t = Matrix3x2.CreateScale(a[0], a[0]);
                    }
                    else if (a.Length >= 2)
                    {
                        t = Matrix3x2.CreateScale(a[0], a[1]);
                    }

                    break;
                }

                case "rotate":
                {
                    if (a.Length >= 1)
                    {
                        t = Matrix3x2.CreateRotation(a[0] * (float)(Math.PI / 180.0));
                    }

                    break;
                }

                case "skewX":
                {
                    if (a.Length >= 1)
                    {
                        t = new Matrix3x2(1f, 0f, MathF.Tan(a[0] * (float)(Math.PI / 180.0)), 1f, 0f, 0f);
                    }

                    break;
                }

                case "skewY":
                {
                    if (a.Length >= 1)
                    {
                        t = new Matrix3x2(1f, MathF.Tan(a[0] * (float)(Math.PI / 180.0)), 0f, 1f, 0f, 0f);
                    }

                    break;
                }
            }

            m *= t;
            SkipSep(s, ref i);
        }

        return m;

        static void SkipSep(string s, ref int i)
        {
            int n = s.Length;
            while (i < n)
            {
                char c = s[i];
                if (char.IsWhiteSpace(c) || c == ',')
                {
                    i++;
                }
                else
                {
                    break;
                }
            }
        }
    }

    private static ReadOnlySpan<char> FindPoint(ReadOnlySpan<char> str, bool rel, Vector2 current, out Vector2 value)
    {
        str = FindScaler(str, out float x);
        str = FindScaler(str, out float y);

        if (rel)
        {
            x += current.X;
            y += current.Y;
        }

        value = new Vector2(x, y);
        return str;
    }

    private static ReadOnlySpan<char> FindScaler(ReadOnlySpan<char> str, out float scaler)
    {
        str = TrimSeparator(str);
        scaler = 0f;

        for (int i = 0; i < str.Length; i++)
        {
            if (IsSeparator(str[i]))
            {
                scaler = ParseFloat(str[..i]);
                return str[i..];
            }
        }

        if (str.Length > 0)
        {
            scaler = ParseFloat(str);
        }

        return [];
    }

    private static bool TryTrimSeparator(ref ReadOnlySpan<char> str)
    {
        ReadOnlySpan<char> result = TrimSeparator(str);
        if (str[^result.Length..].StartsWith(result))
        {
            str = result;
            return true;
        }

        return false;
    }

    private static bool TryFindScaler(ref ReadOnlySpan<char> str, out float value)
    {
        ReadOnlySpan<char> result = FindScaler(str, out float v);
        if (str[^result.Length..].StartsWith(result))
        {
            value = v;
            str = result;
            return true;
        }

        value = default;
        return false;
    }

    private static bool TryFindPoint(ref ReadOnlySpan<char> str, bool relative, Vector2 current, out Vector2 value)
    {
        ReadOnlySpan<char> result = FindPoint(str, relative, current, out Vector2 v);
        if (str[^result.Length..].StartsWith(result))
        {
            value = v;
            str = result;
            return true;
        }

        value = default;
        return false;
    }

    private static bool IsSeparator(char ch)
        => char.IsWhiteSpace(ch) || ch == ',';

    private static ReadOnlySpan<char> TrimSeparator(ReadOnlySpan<char> s)
    {
        int idx = 0;
        for (; idx < s.Length; idx++)
        {
            if (!IsSeparator(s[idx]))
            {
                break;
            }
        }

        return s[idx..];
    }

    private static float ParseFloat(ReadOnlySpan<char> str)
        => str.IsEmpty ? 0 : float.Parse(str, CultureInfo.InvariantCulture);

    private static float[] ParseFloatList(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return [];
        }

        List<float> vals = [];
        int i = 0;
        int n = s.Length;

        while (i < n)
        {
            while (i < n && (char.IsWhiteSpace(s[i]) || s[i] == ','))
            {
                i++;
            }

            if (i >= n)
            {
                break;
            }

            int start = i;

            if (s[i] is '+' or '-')
            {
                i++;
            }

            bool dot = false;
            while (i < n)
            {
                char c = s[i];
                if (char.IsDigit(c))
                {
                    i++;
                    continue;
                }

                if (c == '.' && !dot)
                {
                    dot = true;
                    i++;
                    continue;
                }

                break;
            }

            if (i < n && (s[i] == 'e' || s[i] == 'E'))
            {
                i++;
                if (i < n && (s[i] == '+' || s[i] == '-'))
                {
                    i++;
                }

                while (i < n && char.IsDigit(s[i]))
                {
                    i++;
                }
            }

            if (float.TryParse(s.AsSpan(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
            {
                vals.Add(v);
            }
        }

        return [.. vals];
    }

    private static bool NearlyEqual(in Vector2 a, in Vector2 b, float eps = 1e-3f)
        => MathF.Abs(a.X - b.X) <= eps && MathF.Abs(a.Y - b.Y) <= eps;
}
