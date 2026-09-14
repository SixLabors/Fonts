// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// Reusable shaping pipeline state: the per-font-run workspace buffer, the accumulated
/// result buffer, and their shared feature map. Buffer storage grows to the workload's
/// high-water mark and is reused across calls, so steady-state shaping performs no
/// per-call allocation for pipeline state.
/// </summary>
/// <remarks>
/// A scratch is exclusively owned by one shaping call at a time, enforced by
/// <see cref="ObjectPool{T}"/> ownership in <see cref="TextShaper"/>. Reuse is safe
/// because every public shaping result is materialized by value before the scratch is
/// returned; nothing the pipeline pools can escape a call.
/// </remarks>
internal sealed class ShapingScratch
{
    /// <summary>
    /// The per-font-run workspace buffer glyphs are substituted in.
    /// </summary>
    private ShapingBuffer? workspace;

    /// <summary>
    /// The options the shaping pipeline is driven through, held for the lifetime of
    /// the pooled scratch so shaping a run never builds them.
    /// </summary>
    private TextOptions? shapingOptions;

    /// <summary>
    /// The accumulated result buffer glyphs are seeded and positioned in.
    /// </summary>
    private ShapingBuffer? result;

    /// <summary>
    /// The synthesized run covering the whole text when the options carry no
    /// user-defined runs, reused across passes together with its single-element
    /// list.
    /// </summary>
    private readonly TextRun[] defaultRun = [new()];

    /// <summary>
    /// The reusable bidi algorithm instance backing <see cref="BidiAlgorithm"/>.
    /// </summary>
    private BidiAlgorithm? bidiAlgorithm;

    /// <summary>
    /// The reusable bidi analysis data backing <see cref="BidiData"/>.
    /// </summary>
    private BidiData? bidiData;

    /// <summary>
    /// The resolved bidi runs of the current pass, grown to the high-water count.
    /// </summary>
    private BidiRun[] bidiRuns = new BidiRun[4];

    /// <summary>
    /// The contiguous visual glyph range of each resolved bidi run, parallel to
    /// <see cref="bidiRuns"/> and grown to the same high-water run count.
    /// </summary>
    private ShapedGlyphRange[] bidiGlyphRanges = new ShapedGlyphRange[4];

    /// <summary>
    /// The per-glyph identity records of the current pass's projection, grown to
    /// the high-water glyph count.
    /// </summary>
    private ShapedGlyphInfo[] infos = [];

    /// <summary>
    /// The per-glyph geometry records of the current pass's projection, parallel
    /// to <see cref="infos"/>.
    /// </summary>
    private ShapedGlyphPosition[] positions = [];

    /// <summary>
    /// The run table of the current pass's projection, grown to the high-water
    /// run count.
    /// </summary>
    private ShapedTextRun[] runs = new ShapedTextRun[4];

    /// <summary>
    /// Gets the reusable bidi algorithm instance. Its work buffers grow to the
    /// workload's high-water mark and are reused across passes.
    /// </summary>
    public BidiAlgorithm BidiAlgorithm => this.bidiAlgorithm ??= new BidiAlgorithm();

    /// <summary>
    /// Gets the reusable bidi analysis data. Its builders grow to the workload's
    /// high-water mark and <see cref="BidiData.Init"/> resets them per pass.
    /// </summary>
    public BidiData BidiData => this.bidiData ??= new BidiData();

    /// <summary>
    /// Gets the resolved bidi runs of the current pass. Only the first
    /// <see cref="BidiRunCount"/> entries are live.
    /// </summary>
    public BidiRun[] BidiRuns => this.bidiRuns;

    /// <summary>
    /// Gets the contiguous visual glyph range of each resolved bidi run. Only the
    /// first <see cref="BidiRunCount"/> entries are live.
    /// </summary>
    public ShapedGlyphRange[] BidiGlyphRanges => this.bidiGlyphRanges;

    /// <summary>
    /// Gets the number of live entries in <see cref="BidiRuns"/>.
    /// </summary>
    public int BidiRunCount { get; private set; }

    /// <summary>
    /// Gets the run table of the current pass's projection. Only the first
    /// <see cref="RunCount"/> entries are live.
    /// </summary>
    public ShapedTextRun[] Runs => this.runs;

    /// <summary>
    /// Gets the number of live entries in <see cref="Runs"/>.
    /// </summary>
    public int RunCount { get; private set; }

    /// <summary>
    /// Gets the codepoint index to bidi-run index mapping storage of the current
    /// pass, as last prepared by <see cref="GetBidiMap"/>.
    /// </summary>
    public int[] BidiMap { get; private set; } = [];

    /// <summary>
    /// Empties the bidi run storage for a new pass.
    /// </summary>
    public void ClearBidiRuns() => this.BidiRunCount = 0;

    /// <summary>
    /// Returns the options carrying the public shaping request, refreshed in place.
    /// </summary>
    /// <param name="font">The font being shaped against.</param>
    /// <param name="direction">The line base direction or directional-run direction.</param>
    /// <param name="language">The language the text is written in.</param>
    /// <param name="script">The script applied to the whole request, or <see langword="null"/> to infer scripts from the text.</param>
    /// <param name="layoutMode">The horizontal or vertical layout mode.</param>
    /// <param name="kerningMode">The kerning mode.</param>
    /// <param name="features">The feature tags to turn on.</param>
    /// <param name="bidiMode">Whether the request is a logical line or one directional run.</param>
    /// <returns>The options.</returns>
    public TextOptions GetShapingOptions(Font font, TextDirection direction, CultureInfo language, ScriptClass? script, LayoutMode layoutMode, KerningMode kerningMode, Tag[] features, TextBidiMode bidiMode)
    {
        TextOptions current = this.shapingOptions ??= new TextOptions(font);

        // The object is retained with the pooled scratch state to avoid allocating
        // TextOptions per call. Overwrite every value the public shaping API can
        // control so no state leaks from the previous use of the pool entry.
        current.Font = font;
        current.TextDirection = direction;
        current.TextBidiMode = bidiMode;
        current.Culture = language;
        current.Script = script;
        current.LayoutMode = layoutMode;
        current.KerningMode = kerningMode;
        current.FeatureTags = features;

        return current;
    }

    /// <summary>
    /// Appends a resolved bidi run.
    /// </summary>
    /// <param name="run">The run to append.</param>
    public void AddBidiRun(in BidiRun run)
    {
        if (this.BidiRunCount == this.bidiRuns.Length)
        {
            Array.Resize(ref this.bidiRuns, this.bidiRuns.Length * 2);
            Array.Resize(ref this.bidiGlyphRanges, this.bidiGlyphRanges.Length * 2);
        }

        this.bidiRuns[this.BidiRunCount++] = run;
    }

    /// <summary>
    /// Records the contiguous visual glyph range owned by one resolved bidi run.
    /// </summary>
    /// <param name="runIndex">The zero-based resolved bidi-run index.</param>
    /// <param name="range">The glyph range over the projected shaping arrays.</param>
    public void SetBidiGlyphRange(int runIndex, in ShapedGlyphRange range)
        => this.bidiGlyphRanges[runIndex] = range;

    /// <summary>
    /// Empties the projected run table for a new pass.
    /// </summary>
    public void ClearRuns() => this.RunCount = 0;

    /// <summary>
    /// Appends a projected run table entry.
    /// </summary>
    /// <param name="run">The entry to append.</param>
    public void AddRun(in ShapedTextRun run)
    {
        if (this.RunCount == this.runs.Length)
        {
            Array.Resize(ref this.runs, this.runs.Length * 2);
        }

        this.runs[this.RunCount++] = run;
    }

    /// <summary>
    /// Gets the parallel projection storage for the given glyph count.
    /// </summary>
    /// <param name="count">The glyph capacity required.</param>
    /// <returns>The identity and geometry storage; entries beyond the count are undefined.</returns>
    public (ShapedGlyphInfo[] Infos, ShapedGlyphPosition[] Positions) GetProjection(int count)
    {
        if (this.infos.Length < count)
        {
            int capacity = Math.Max(count, Math.Max(64, this.infos.Length * 2));
            this.infos = new ShapedGlyphInfo[capacity];
            this.positions = new ShapedGlyphPosition[capacity];
        }

        return (this.infos, this.positions);
    }

    /// <summary>
    /// Gets the codepoint index to bidi-run index mapping for the given text
    /// length, every entry reset to the unvisited -1 sentinel.
    /// </summary>
    /// <param name="length">The codepoint capacity required.</param>
    /// <returns>The mapping storage; entries beyond the length are undefined.</returns>
    public int[] GetBidiMap(int length)
    {
        if (this.BidiMap.Length < length)
        {
            this.BidiMap = new int[Math.Max(length, Math.Max(64, this.BidiMap.Length * 2))];
        }

        Array.Fill(this.BidiMap, -1, 0, length);
        return this.BidiMap;
    }

    /// <summary>
    /// Gets the reusable single-run list configured for the given options. The
    /// shaping pass sets its exclusive end after enumerating the text.
    /// </summary>
    /// <param name="options">The text options supplying the font.</param>
    /// <returns>The run list.</returns>
    public IReadOnlyList<TextRun> GetDefaultTextRuns(TextOptions options)
    {
        TextRun run = this.defaultRun[0];
        run.Start = 0;
        run.End = 0;
        run.Font = options.Font;
        run.ResolveFontWeight(options.FontWeight);
        return this.defaultRun;
    }

    /// <summary>
    /// Gets the reusable shaping buffers, reset for a new pass over the given options.
    /// </summary>
    /// <param name="options">The text options for the pass.</param>
    /// <returns>The reusable buffers, sharing one feature map.</returns>
    public (ShapingBuffer Workspace, ShapingBuffer Result) Prepare(TextOptions options)
    {
        ShapingBuffer? workspace = this.workspace;
        ShapingBuffer? result = this.result;
        if (workspace is null || result is null)
        {
            workspace = new ShapingBuffer(options, ShapingBufferRole.Substitution);
            result = new ShapingBuffer(options, ShapingBufferRole.Positioning);
            this.workspace = workspace;
            this.result = result;
        }
        else
        {
            workspace.Reset(options);
            result.Reset(options);

            // The single-run fast path flips the workspace to the positioning role in
            // place; a pooled scratch must hand out buffers in their home roles.
            workspace.SetRole(ShapingBufferRole.Substitution);
            result.SetRole(ShapingBufferRole.Positioning);
        }

        return (workspace, result);
    }
}
