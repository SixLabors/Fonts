// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Tables.TrueType.Glyphs;

/// <summary>
/// Implements loading of composite (compound) glyph descriptions from the 'glyf' table.
/// A composite glyph references one or more component glyphs, each with its own transformation.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf"/>
/// </summary>
internal sealed class CompositeGlyphLoader : GlyphLoader
{
    private readonly Bounds bounds;
    private readonly Composite[] composites;
    private readonly ReadOnlyMemory<byte> instructions;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeGlyphLoader"/> class.
    /// </summary>
    /// <param name="composites">The component glyph references.</param>
    /// <param name="bounds">The composite glyph bounding box.</param>
    /// <param name="instructions">The hinting instructions for this composite glyph.</param>
    public CompositeGlyphLoader(IEnumerable<Composite> composites, Bounds bounds, ReadOnlyMemory<byte> instructions)
    {
        this.composites = [.. composites];
        this.bounds = bounds;
        this.instructions = instructions;
    }

    /// <inheritdoc/>
    public override GlyphVector CreateGlyph(GlyphTable table)
    {
        List<ControlPoint> controlPoints = [];
        List<ushort> endPoints = [];
        CompositeComponent[] components = new CompositeComponent[this.composites.Length];
        for (int i = 0; i < this.composites.Length; i++)
        {
            Composite composite = this.composites[i];
            GlyphVector clone = GlyphVector.DeepClone(table.GetGlyph(composite.GlyphIndex));
            GlyphVector.TransformInPlace(ref clone, composite.Transformation);
            ushort endPointOffset = (ushort)controlPoints.Count;

            // Store original component offset and point count for gvar processing.
            components[i] = new CompositeComponent(
                composite.Transformation.Translation.X,
                composite.Transformation.Translation.Y,
                clone.ControlPoints.Count);

            controlPoints.AddRange(clone.ControlPoints);
            foreach (ushort p in clone.EndPoints)
            {
                endPoints.Add((ushort)(p + endPointOffset));
            }
        }

        return new(controlPoints, endPoints, this.bounds, this.instructions, true)
        {
            CompositeComponents = components
        };
    }

    /// <summary>
    /// Reads a composite glyph description from the binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader positioned after the glyph header.</param>
    /// <param name="bounds">The glyph bounding box.</param>
    /// <returns>A <see cref="CompositeGlyphLoader"/> containing the composite glyph data.</returns>
    public static CompositeGlyphLoader LoadCompositeGlyph(BigEndianBinaryReader reader, in Bounds bounds)
    {
        List<Composite> composites = [];
        CompositeGlyphFlags flags;
        do
        {
            flags = (CompositeGlyphFlags)reader.ReadUInt16();
            ushort glyphIndex = reader.ReadUInt16();

            LoadArguments(reader, flags, out int dx, out int dy);

            Matrix3x2 transform = Matrix3x2.Identity;
            transform.Translation = new Vector2(dx, dy);

            if ((flags & CompositeGlyphFlags.WeHaveAScale) != 0)
            {
                float scale = reader.ReadF2Dot14(); // Format 2.14
                transform.M11 = scale;
                transform.M22 = scale;
            }
            else if ((flags & CompositeGlyphFlags.WeHaveXAndYScale) != 0)
            {
                transform.M11 = reader.ReadF2Dot14();
                transform.M22 = reader.ReadF2Dot14();
            }
            else if ((flags & CompositeGlyphFlags.WeHaveATwoByTwo) != 0)
            {
                transform.M11 = reader.ReadF2Dot14();
                transform.M12 = reader.ReadF2Dot14();
                transform.M21 = reader.ReadF2Dot14();
                transform.M22 = reader.ReadF2Dot14();
            }

            composites.Add(new Composite(glyphIndex, flags, transform));
        }
        while ((flags & CompositeGlyphFlags.MoreComponents) != 0);

        byte[] instructions = [];
        if ((flags & CompositeGlyphFlags.WeHaveInstructions) != 0)
        {
            // Read the instructions if they exist.
            ushort instructionSize = reader.ReadUInt16();
            instructions = reader.ReadUInt8Array(instructionSize);
        }

        return new CompositeGlyphLoader(composites, bounds, instructions);
    }

    /// <summary>
    /// Reads the component arguments (offsets or point numbers) from the binary reader
    /// based on the specified composite glyph flags.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <param name="flags">The composite glyph flags for this component.</param>
    /// <param name="dx">When this method returns, contains the x offset or point number.</param>
    /// <param name="dy">When this method returns, contains the y offset or point number.</param>
    public static void LoadArguments(BigEndianBinaryReader reader, CompositeGlyphFlags flags, out int dx, out int dy)
    {
        // are we 16 or 8 bits values?
        if ((flags & CompositeGlyphFlags.Args1And2AreWords) != 0)
        {
            // 16 bit
            // are we int or unit?
            if ((flags & CompositeGlyphFlags.ArgsAreXYValues) != 0)
            {
                // signed
                dx = reader.ReadInt16();
                dy = reader.ReadInt16();
            }
            else
            {
                // unsigned
                dx = reader.ReadUInt16();
                dy = reader.ReadUInt16();
            }
        }
        else
        {
            // 8 bit
            // are we sbyte or byte?
            if ((flags & CompositeGlyphFlags.ArgsAreXYValues) != 0)
            {
                // signed
                dx = reader.ReadSByte();
                dy = reader.ReadSByte();
            }
            else
            {
                // unsigned
                dx = reader.ReadByte();
                dy = reader.ReadByte();
            }
        }
    }

    /// <summary>
    /// Represents a single component reference within a composite glyph,
    /// storing the referenced glyph index, flags, and transformation matrix.
    /// </summary>
    public readonly struct Composite
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Composite"/> struct.
        /// </summary>
        /// <param name="glyphIndex">The glyph index of the component.</param>
        /// <param name="flags">The composite glyph flags.</param>
        /// <param name="transformation">The transformation matrix to apply to the component.</param>
        public Composite(ushort glyphIndex, CompositeGlyphFlags flags, Matrix3x2 transformation)
        {
            this.GlyphIndex = glyphIndex;
            this.Flags = flags;
            this.Transformation = transformation;
        }

        /// <summary>
        /// Gets the glyph index of the component.
        /// </summary>
        public ushort GlyphIndex { get; }

        /// <summary>
        /// Gets the composite glyph flags for this component.
        /// </summary>
        public CompositeGlyphFlags Flags { get; }

        /// <summary>
        /// Gets the transformation matrix to apply to the component's outline.
        /// </summary>
        public Matrix3x2 Transformation { get; }
    }
}
