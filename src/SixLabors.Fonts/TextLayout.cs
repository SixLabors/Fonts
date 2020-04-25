// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using SixLabors.Fonts.Exceptions;

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
            var dpi = new Vector2(options.DpiX, options.DpiY);
            Vector2 origin = (Vector2)options.Origin / dpi;

            float maxWidth = float.MaxValue;
            float originX = 0;
            if (options.WrappingWidth > 0)
            {
                // trim trailing white spaces from the text
                text = text.TrimEnd(null);

                maxWidth = options.WrappingWidth / options.DpiX;

                switch (options.HorizontalAlignment)
                {
                    case HorizontalAlignment.Right:
                        originX = maxWidth;
                        break;
                    case HorizontalAlignment.Center:
                        originX = 0.5f * maxWidth;
                        break;
                    case HorizontalAlignment.Left:
                    default:
                        originX = 0;
                        break;
                }
            }

            AppliedFontStyle spanStyle = options.GetStyle(0, text.Length);
            var layout = new List<GlyphLayout>(text.Length);

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
            bool startOfLine = true;
            float totalHeight = 0;

            for (int i = 0; i < text.Length; i++)
            {
                // four-byte characters are processed on the first char
                if (char.IsLowSurrogate(text[i]))
                {
                    continue;
                }

                if (spanStyle.End < i)
                {
                    spanStyle = options.GetStyle(i, text.Length);
                    previousGlyph = null;
                }

                bool hasFourBytes = char.IsHighSurrogate(text[i]);
                int codePoint = hasFourBytes ? char.ConvertToUtf32(text[i], text[i + 1]) : text[i];

                GlyphInstance[] glyphs = spanStyle.GetGlyphLayers(codePoint, options.ColorFontSupport);
                if (glyphs.Length == 0)
                {
                    return FontsThrowHelper.ThrowGlyphMissingException<IReadOnlyList<GlyphLayout>>(codePoint);
                }

                var glyph = glyphs[0];
                if (glyph.Font.LineHeight > unscaledLineHeight)
                {
                    // get the larget lineheight thus far
                    unscaledLineHeight = glyph.Font.LineHeight;
                    scale = glyph.Font.EmSize * 72;
                    lineHeight = (unscaledLineHeight * spanStyle.PointSize) / scale;
                }

                if (glyph.Font.Ascender > unscaledLineMaxAscender)
                {
                    unscaledLineMaxAscender = glyph.Font.Ascender;
                    scale = glyph.Font.EmSize * 72;
                    lineMaxAscender = (unscaledLineMaxAscender * spanStyle.PointSize) / scale;
                }

                if (glyph.Font.Descender > unscaledLineMaxDescender)
                {
                    unscaledLineMaxDescender = glyph.Font.Descender;
                    scale = glyph.Font.EmSize * 72;
                    lineMaxDescender = (unscaledLineMaxDescender * spanStyle.PointSize) / scale;
                }

                if (firstLine)
                {
                    if (lineHeight > lineHeightOfFirstLine)
                    {
                        lineHeightOfFirstLine = lineHeight;
                    }

                    var lineGap = lineHeightOfFirstLine - lineMaxAscender - lineMaxDescender;

                    top = lineHeightOfFirstLine - lineMaxAscender - (lineGap / 2);
                }

                if (options.WrappingWidth > 0 && char.IsWhiteSpace(text[i]))
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

                float glyphWidth = (glyph.AdvanceWidth * spanStyle.PointSize) / scale;
                float glyphHeight = (glyph.Height * spanStyle.PointSize) / scale;

                if (hasFourBytes || (text[i] != '\r' && text[i] != '\n' && text[i] != '\t' && text[i] != ' '))
                {
                    Vector2 glyphLocation = location;
                    if (spanStyle.ApplyKerning && previousGlyph != null)
                    {
                        // if there is special instructions for this glyph pair use that width
                        Vector2 scaledOffset = (spanStyle.GetOffset(glyph, previousGlyph) * spanStyle.PointSize) / scale;

                        glyphLocation += scaledOffset;

                        // only fix the 'X' of the current tracked location but use the actual 'X'/'Y' of the offset
                        location.X = glyphLocation.X;
                    }

                    foreach (var g in glyphs)
                    {
                        var w = (g.AdvanceWidth * spanStyle.PointSize) / scale;
                        var h = (g.Height * spanStyle.PointSize) / scale;
                        layout.Add(new GlyphLayout(codePoint, new Glyph(g, spanStyle.PointSize), glyphLocation, w, h, lineHeight, startOfLine, false, false));

                        if (w > glyphWidth)
                        {
                            glyphWidth = w;
                        }
                    }

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
                                layout[j] = new GlyphLayout(layout[j].CodePoint, layout[j].Glyph, new Vector2(current.X - wrappingOffset, current.Y + lineHeight), layout[j].Width, layout[j].Height, layout[j].LineHeight, startOfLine, layout[j].IsWhiteSpace, layout[j].IsControlCharacter);
                                startOfLine = false;

                                location.X = layout[j].Location.X + layout[j].Width;
                            }

                            location.Y += lineHeight;
                            firstLine = false;
                            lastWrappableLocation = -1;
                        }
                    }

                    float bottom = location.Y + lineHeight;
                    if (bottom > totalHeight)
                    {
                        totalHeight = bottom;
                    }

                    previousGlyph = glyph;
                }
                else if (text[i] == '\r')
                {
                    // carriage return resets the XX coordinate to 0
                    location.X = 0;
                    previousGlyph = null;
                    startOfLine = true;

                    layout.Add(new GlyphLayout(codePoint, new Glyph(glyph, spanStyle.PointSize), location, 0, glyphHeight, lineHeight, startOfLine, true, true));
                    startOfLine = false;
                }
                else if (text[i] == '\n')
                {
                    // carriage return resets the XX coordinate to 0
                    layout.Add(new GlyphLayout(codePoint, new Glyph(glyph, spanStyle.PointSize), location, 0, glyphHeight, lineHeight, startOfLine, true, true));
                    location.X = 0;
                    location.Y += lineHeight;
                    unscaledLineHeight = 0;
                    unscaledLineMaxAscender = 0;
                    previousGlyph = null;
                    firstLine = false;
                    lastWrappableLocation = -1;
                    startOfLine = true;
                }
                else if (text[i] == '\t')
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

                    layout.Add(new GlyphLayout(codePoint, new Glyph(glyph, spanStyle.PointSize), location, finalWidth, glyphHeight, lineHeight, startOfLine, true, false));
                    startOfLine = false;

                    // advance to a position > width away that
                    location.X += finalWidth;
                    previousGlyph = null;
                }
                else if (text[i] == ' ')
                {
                    layout.Add(new GlyphLayout(codePoint, new Glyph(glyph, spanStyle.PointSize), location, glyphWidth, glyphHeight, lineHeight, startOfLine, true, false));
                    startOfLine = false;
                    location.X += glyphWidth;
                    previousGlyph = null;
                }
            }

            totalHeight -= top;
            var offset = new Vector2(0, lineHeightOfFirstLine - top);

            switch (options.VerticalAlignment)
            {
                case VerticalAlignment.Center:
                    offset += new Vector2(0, -0.5f * totalHeight);
                    break;
                case VerticalAlignment.Bottom:
                    offset += new Vector2(0, -totalHeight);
                    break;
                case VerticalAlignment.Top:
                default:
                    // no change
                    break;
            }

            Vector2 lineOffset = offset;
            for (int i = 0; i < layout.Count; i++)
            {
                GlyphLayout glyphLayout = layout[i];
                if (glyphLayout.StartOfLine)
                {
                    lineOffset = offset;

                    // scan ahead measuring width
                    float width = glyphLayout.Width;
                    for (int j = i + 1; j < layout.Count; j++)
                    {
                        if (layout[j].StartOfLine)
                        {
                            break;
                        }

                        width = layout[j].Location.X + layout[j].Width; // rhs
                    }

                    switch (options.HorizontalAlignment)
                    {
                        case HorizontalAlignment.Right:
                            lineOffset = new Vector2(originX - width, 0) + offset;
                            break;
                        case HorizontalAlignment.Center:
                            lineOffset = new Vector2(originX - (width / 2f), 0) + offset;
                            break;
                        case HorizontalAlignment.Left:
                        default:
                            lineOffset = new Vector2(originX, 0) + offset;
                            break;
                    }
                }

                // TODO calculate an offset from the 'origin' based on TextAlignment for each line
                layout[i] = new GlyphLayout(glyphLayout.CodePoint, glyphLayout.Glyph, glyphLayout.Location + lineOffset + origin, glyphLayout.Width, glyphLayout.Height, glyphLayout.LineHeight, glyphLayout.StartOfLine, glyphLayout.IsWhiteSpace, glyphLayout.IsControlCharacter);
            }

            return layout;
        }
    }
}
