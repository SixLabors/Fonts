// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using SixLabors.Fonts.Exceptions;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Encapsulated logic or laying out text.
    /// </summary>
    internal class TextLayout
    {
        internal static TextLayout Default { get; set; } = new TextLayout();

        /// <summary>
        /// Generates the layout.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The style.</param>
        /// <returns>A collection of layout that describe all thats needed to measure or render a series of glyphs.</returns>
        public IReadOnlyList<GlyphLayout> GenerateLayout(ReadOnlySpan<char> text, RendererOptions options)
        {
            if (text.IsEmpty)
            {
                return Array.Empty<GlyphLayout>();
            }

            var dpi = new Vector2(options.DpiX, options.DpiY);
            Vector2 origin = options.Origin / dpi;

            float maxWidth = float.MaxValue;
            float originX = 0;
            if (options.WrappingWidth > 0)
            {
                // trim trailing white spaces from the text
                text = text.TrimEnd(null);
                maxWidth = options.WrappingWidth / options.DpiX;
            }

            // lets convert the text into codepoints
            Memory<int> codePointsMemory = LineBreaker.ToUtf32(text);
            if (codePointsMemory.IsEmpty)
            {
                return Array.Empty<GlyphLayout>();
            }

            Span<int> codepoints = codePointsMemory.Span;
            var lineBreaker = new LineBreaker();
            lineBreaker.Reset(codePointsMemory);

            AppliedFontStyle spanStyle = options.GetStyle(0, codepoints.Length);
            var layout = new List<GlyphLayout>(codepoints.Length);

            float unscaledLineHeight = 0f;
            float lineHeight = 0f;
            float unscaledLineMaxAscender = 0f;
            float unscaledLineMaxDescender = 0f;
            float lineMaxAscender = 0f;
            float lineMaxDescender = 0f;
            Vector2 location = Vector2.Zero;
            float lineHeightOfFirstLine = 0;

            // Remember where the top of the layouted text is for accurate vertical alignment.
            // This is important because there is considerable space between the lineHeight at the glyph's ascender.
            float top = 0;

            bool firstLine = true;
            GlyphInstance? previousGlyph = null;
            float scale = 0;
            int lastWrappableLocation = -1;
            int nextWrappableLocation = codepoints.Length;
            bool nextWrappableRequired = false;
            bool startOfLine = true;
            float totalHeight = 0;
            int graphemeIndex = 0;

            if (lineBreaker.TryGetNextBreak(out LineBreak b))
            {
                nextWrappableLocation = b.PositionWrap - 1;
                nextWrappableRequired = b.Required;
            }

            for (int i = 0; i < codepoints.Length; i++)
            {
                if (spanStyle.End < i)
                {
                    spanStyle = options.GetStyle(i, codepoints.Length);
                    previousGlyph = null;
                }

                int codePoint = codepoints[i];

                GlyphInstance[] glyphs = spanStyle.GetGlyphLayers(codePoint, options.ColorFontSupport);
                if (glyphs.Length == 0)
                {
                    return FontsThrowHelper.ThrowGlyphMissingException<IReadOnlyList<GlyphLayout>>(codePoint);
                }

                GlyphInstance? glyph = glyphs[0];
                float fontHeight = glyph.Font.LineHeight * options.LineSpacing;
                if (fontHeight > unscaledLineHeight)
                {
                    // get the larget lineheight thus far
                    unscaledLineHeight = fontHeight;
                    scale = glyph.Font.EmSize * 72;
                    lineHeight = unscaledLineHeight * spanStyle.PointSize / scale;
                }

                if (glyph.Font.Ascender > unscaledLineMaxAscender)
                {
                    unscaledLineMaxAscender = glyph.Font.Ascender;
                    scale = glyph.Font.EmSize * 72;
                    lineMaxAscender = unscaledLineMaxAscender * spanStyle.PointSize / scale;
                }

                if (Math.Abs(glyph.Font.Descender) > unscaledLineMaxDescender)
                {
                    unscaledLineMaxDescender = Math.Abs(glyph.Font.Descender);
                    scale = glyph.Font.EmSize * 72;
                    lineMaxDescender = unscaledLineMaxDescender * spanStyle.PointSize / scale;
                }

                if (firstLine)
                {
                    // Reset the line height for the first line to prevent initial lead.
                    float unspacedLineHeight = lineHeight / options.LineSpacing;
                    if (unspacedLineHeight > lineHeightOfFirstLine)
                    {
                        lineHeightOfFirstLine = unspacedLineHeight;
                    }

                    switch (options.VerticalAlignment)
                    {
                        case VerticalAlignment.Top:
                            top = lineMaxAscender;
                            break;
                        case VerticalAlignment.Center:
                            top = (lineMaxAscender / 2F) - (lineMaxDescender / 2F);
                            break;
                        case VerticalAlignment.Bottom:
                            top = -lineMaxDescender;
                            break;
                    }
                }

                if ((options.WrappingWidth > 0 && nextWrappableLocation == i) || nextWrappableRequired)
                {
                    // keep a record of where to wrap text and ensure that no line starts with white space
                    for (int j = layout.Count - 1; j >= 0; j--)
                    {
                        if (!layout[j].IsWhiteSpace)
                        {
                            lastWrappableLocation = j + 1;
                            break;
                        }
                    }
                }

                if (nextWrappableLocation == i)
                {
                    if (lineBreaker.TryGetNextBreak(out b))
                    {
                        nextWrappableLocation = b.PositionWrap - 1;
                        nextWrappableRequired = b.Required;
                    }
                }

                float glyphWidth = glyph.AdvanceWidth * spanStyle.PointSize / scale;
                float glyphHeight = glyph.Height * spanStyle.PointSize / scale;

                if (codepoints[i] != '\r' && codepoints[i] != '\n' && codepoints[i] != '\t' && codepoints[i] != ' ')
                {
                    Vector2 glyphLocation = location;
                    if (spanStyle.ApplyKerning && previousGlyph != null)
                    {
                        // if there is special instructions for this glyph pair use that width
                        Vector2 scaledOffset = spanStyle.GetOffset(glyph, previousGlyph) * spanStyle.PointSize / scale;

                        glyphLocation += scaledOffset;

                        // only fix the 'X' of the current tracked location but use the actual 'X'/'Y' of the offset
                        location.X = glyphLocation.X;
                    }

                    foreach (GlyphInstance? g in glyphs)
                    {
                        float w = g.AdvanceWidth * spanStyle.PointSize / scale;
                        float h = g.Height * spanStyle.PointSize / scale;
                        layout.Add(new GlyphLayout(graphemeIndex, codePoint, new Glyph(g, spanStyle.PointSize), glyphLocation, w, h, lineHeight, startOfLine, false, false));

                        if (w > glyphWidth)
                        {
                            glyphWidth = w;
                        }
                    }

                    // Increment the index to signify we have moved on the a new cluster.
                    graphemeIndex++;
                    startOfLine = false;

                    // move forward the actual width of the glyph, we are retaining the baseline
                    location.X += glyphWidth;

                    // if the word extended pass the end of the box, wrap it
                    if (location.X >= maxWidth && lastWrappableLocation > 0)
                    {
                        if (lastWrappableLocation < layout.Count)
                        {
                            float wrappingOffset = layout[lastWrappableLocation].Location.X;
                            startOfLine = true;

                            // move the characters to the next line
                            for (int j = lastWrappableLocation; j < layout.Count; j++)
                            {
                                if (layout[j].IsWhiteSpace)
                                {
                                    wrappingOffset += layout[j].Width;
                                    layout.RemoveAt(j);
                                    j--;
                                    continue;
                                }

                                Vector2 current = layout[j].Location;
                                layout[j] = new GlyphLayout(layout[j].GraphemeIndex, layout[j].CodePoint, layout[j].Glyph, new Vector2(current.X - wrappingOffset, current.Y + lineHeight), layout[j].Width, layout[j].Height, layout[j].LineHeight, startOfLine, layout[j].IsWhiteSpace, layout[j].IsControlCharacter);
                                startOfLine = false;

                                location.X = layout[j].Location.X + layout[j].Width;
                            }

                            location.Y += lineHeight;
                            totalHeight += lineHeight;
                            firstLine = false;
                            lastWrappableLocation = -1;
                        }
                    }

                    previousGlyph = glyph;
                }
                else if (codepoints[i] == '\r')
                {
                    // carriage return resets the XX coordinate to 0
                    location.X = 0;
                    previousGlyph = null;
                    startOfLine = true;

                    layout.Add(new GlyphLayout(-1, codePoint, new Glyph(glyph, spanStyle.PointSize), location, 0, glyphHeight, lineHeight, startOfLine, true, true));
                    startOfLine = false;
                }
                else if (codepoints[i] == '\n')
                {
                    // carriage return resets the XX coordinate to 0
                    layout.Add(new GlyphLayout(-1, codePoint, new Glyph(glyph, spanStyle.PointSize), location, 0, glyphHeight, lineHeight, startOfLine, true, true));
                    location.X = 0;
                    location.Y += lineHeight;
                    totalHeight += lineHeight;
                    unscaledLineHeight = 0;
                    unscaledLineMaxAscender = 0;
                    previousGlyph = null;
                    firstLine = false;
                    lastWrappableLocation = -1;
                    startOfLine = true;
                }
                else if (codepoints[i] == '\t')
                {
                    float tabStop = glyphWidth * spanStyle.TabWidth;
                    float finalWidth = 0;

                    if (tabStop > 0)
                    {
                        finalWidth = tabStop - (location.X % tabStop);
                    }

                    if (finalWidth < glyphWidth)
                    {
                        // if we are not going to tab atleast a glyph width add another tabstop to it ???
                        // should I be doing this?
                        finalWidth += tabStop;
                    }

                    layout.Add(new GlyphLayout(-1, codePoint, new Glyph(glyph, spanStyle.PointSize), location, finalWidth, glyphHeight, lineHeight, startOfLine, true, false));
                    startOfLine = false;

                    // advance to a position > width away that
                    location.X += finalWidth;
                    previousGlyph = null;
                }
                else if (codepoints[i] == ' ')
                {
                    layout.Add(new GlyphLayout(-1, codePoint, new Glyph(glyph, spanStyle.PointSize), location, glyphWidth, glyphHeight, lineHeight, startOfLine, true, false));
                    startOfLine = false;
                    location.X += glyphWidth;
                    previousGlyph = null;
                }
            }

            var offsetY = new Vector2(0, top);
            switch (options.VerticalAlignment)
            {
                case VerticalAlignment.Center:
                    offsetY += new Vector2(0, -(totalHeight / 2F));
                    break;
                case VerticalAlignment.Bottom:
                    offsetY += new Vector2(0, -totalHeight);
                    break;
            }

            Vector2 offsetX = Vector2.Zero;
            for (int i = 0; i < layout.Count; i++)
            {
                GlyphLayout glyphLayout = layout[i];
                graphemeIndex = glyphLayout.GraphemeIndex;

                // Scan ahead getting the width.
                if (glyphLayout.StartOfLine)
                {
                    float width = 0;
                    for (int j = i; j < layout.Count; j++)
                    {
                        GlyphLayout current = layout[j];
                        int currentGraphemeIndex = current.GraphemeIndex;
                        if (current.StartOfLine && (currentGraphemeIndex != graphemeIndex))
                        {
                            // Leading graphemes are made up of multiple glyphs all marked as 'StartOfLine so we only
                            // break when we are sure we have entered a new cluster or previously defined break.
                            break;
                        }

                        width = current.Location.X + current.Width;
                    }

                    switch (options.HorizontalAlignment)
                    {
                        case HorizontalAlignment.Left:
                            offsetX = new Vector2(originX, 0) + offsetY;
                            break;
                        case HorizontalAlignment.Right:
                            offsetX = new Vector2(originX - width, 0) + offsetY;
                            break;
                        case HorizontalAlignment.Center:
                            offsetX = new Vector2(originX - (width / 2F), 0) + offsetY;
                            break;
                    }
                }

                // TODO calculate an offset from the 'origin' based on TextAlignment for each line
                layout[i] = new GlyphLayout(
                    glyphLayout.GraphemeIndex,
                    glyphLayout.CodePoint,
                    glyphLayout.Glyph,
                    glyphLayout.Location + offsetX + origin,
                    glyphLayout.Width,
                    glyphLayout.Height,
                    glyphLayout.LineHeight,
                    glyphLayout.StartOfLine,
                    glyphLayout.IsWhiteSpace,
                    glyphLayout.IsControlCharacter);
            }

            return layout;
        }
    }
}
