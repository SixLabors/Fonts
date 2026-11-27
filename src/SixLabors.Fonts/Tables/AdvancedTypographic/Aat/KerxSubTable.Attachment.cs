// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Tables.AdvancedTypographic.GPos;
using SixLabors.Fonts.Tables.TrueType;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Aat;

internal abstract partial class KerxSubTable
{
    /// <summary>
    /// Positions glyphs relative to the marked glyph in a format 4 state table.
    /// </summary>
    internal sealed class AttachmentSubTable : KerxSubTable
    {
        private readonly AatStateTable machine;
        private readonly short[] actions;
        private readonly AnchorAction anchorAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttachmentSubTable"/> class.
        /// </summary>
        /// <param name="coverage">The coverage flags for the subtable.</param>
        /// <param name="machine">The state table.</param>
        /// <param name="actions">The words that specify the anchors to align.</param>
        /// <param name="anchorAction">The source of the anchor coordinates.</param>
        public AttachmentSubTable(byte coverage, AatStateTable machine, short[] actions, byte anchorAction)
            : base(coverage)
        {
            this.machine = machine;
            this.actions = actions;
            this.anchorAction = (AnchorAction)anchorAction;
        }

        /// <summary>
        /// Flags specific to a kerx format 4 transition.
        /// </summary>
        [Flags]
        private enum AttachmentFlags : ushort
        {
            /// <summary>
            /// Records the current glyph as the attachment base.
            /// </summary>
            Mark = 0x8000
        }

        /// <summary>
        /// The two-bit selector for a kerx format 4 attachment record.
        /// </summary>
        private enum AnchorAction : byte
        {
            /// <summary>
            /// A pair of outline control point indices.
            /// </summary>
            ControlPoint,

            /// <summary>
            /// A pair of ankr point indices.
            /// </summary>
            AnchorPoint,

            /// <summary>
            /// Two explicit design-unit coordinates.
            /// </summary>
            Coordinates
        }

        /// <summary>
        /// Gets a value indicating whether the subtable references ankr points.
        /// </summary>
        public bool UsesAnchorTable => this.anchorAction == AnchorAction.AnchorPoint;

        /// <inheritdoc />
        public override bool Apply(FontMetrics font, ShapingBuffer buffer, AatStateTable.Traversal traversal, bool requestedKerning, AatAnchorTable? anchors, scoped Span<(int Value, bool Reset)> crossStream)
        {
            bool updated = false;
            int state = 0;
            int marked = -1;
            while (buffer.CountOperations(1))
            {
                int i = traversal.Index;
                bool atEnd = traversal.AtEnd;
                int current = atEnd ? -1 : AatStateTable.LiveIndex(buffer, i);
                ushort glyph = current < 0 ? AatStateTable.DeletedGlyph : buffer[current].GlyphId;
                if (!this.machine.TryTransition(ref state, glyph, atEnd, out AatStateTable.StateEntry entry))
                {
                    break;
                }

                if (marked >= 0 && current >= 0 && entry.First != ushort.MaxValue
                    && this.Attach(buffer, marked, current, entry.First, anchors))
                {
                    updated = true;
                    if (!crossStream.IsEmpty)
                    {
                        // The anchor replaces the child's previous offset. Keep
                        // any reset that already ended cross-stream carry here.
                        crossStream[i - traversal.StartIndex].Value = 0;
                    }
                }

                if (!atEnd && ((AttachmentFlags)entry.Flags & AttachmentFlags.Mark) != 0)
                {
                    marked = current;
                }

                if (atEnd)
                {
                    break;
                }

                if (entry.Advances)
                {
                    traversal.MoveNext();
                }
            }

            return updated;
        }

        /// <summary>
        /// Positions the current glyph's anchor relative to the marked glyph.
        /// </summary>
        /// <param name="buffer">The shaping buffer.</param>
        /// <param name="parent">The live index of the base glyph.</param>
        /// <param name="current">The live index of the glyph to attach.</param>
        /// <param name="action">The index of the anchor action.</param>
        /// <param name="anchors">The anchor table, if present.</param>
        /// <returns>Whether the action set the glyph's anchor offset.</returns>
        private bool Attach(ShapingBuffer buffer, int parent, int current, int action, AatAnchorTable? anchors)
        {
            ref GlyphShapingPosition position = ref buffer.PositionAt(current);

            // The link points from the mark to its parent in draw order.
            // Horizontal right-to-left runs draw in reverse buffer order.
            int distance = !this.IsVertical && buffer[current].Direction == TextDirection.RightToLeft ? current - parent : parent - current;
            if (distance is < short.MinValue or > short.MaxValue)
            {
                // Keep the signed 16-bit link limit used by HarfBuzz.
                // A link beyond this range clears the earlier link and keeps its offsets.
                position.CursiveAttachment = GlyphShapingPosition.NoCursiveAttachment;
                position.MarkAttachment = -1;
                return false;
            }

            int xOffset;
            int yOffset;
            switch (this.anchorAction)
            {
                case AnchorAction.ControlPoint:
                    // The two unsigned words index points on the marked glyph and the current glyph.
                    if (action >= this.actions.Length / 2)
                    {
                        return false;
                    }

                    // The action uses outline coordinates in font design units.
                    // The TrueType metrics supply these points with either hinting setting.
                    if (buffer.MetricsAt(parent).Metrics is not TrueTypeGlyphMetrics parentMetrics ||
                        buffer.MetricsAt(current).Metrics is not TrueTypeGlyphMetrics currentMetrics)
                    {
                        return false;
                    }

                    // The metrics already contain the outline. Charge for
                    // reading its two points before testing the indices.
                    if (!buffer.CountOperations(2) ||
                        !parentMetrics.TryGetControlPoint((ushort)this.actions[action * 2], out Vector2 basePoint) ||
                        !currentMetrics.TryGetControlPoint((ushort)this.actions[(action * 2) + 1], out Vector2 markPoint))
                    {
                        return false;
                    }

                    xOffset = (int)(basePoint.X - markPoint.X);
                    yOffset = (int)(basePoint.Y - markPoint.Y);
                    break;
                case AnchorAction.AnchorPoint:
                    // The two unsigned words index the marked and current glyphs' ankr lists.
                    if (action >= this.actions.Length / 2)
                    {
                        return false;
                    }

                    AnchorXY baseAnchor = anchors?.GetAnchor(buffer[parent].GlyphId, (ushort)this.actions[action * 2]) ?? default;
                    AnchorXY markAnchor = anchors?.GetAnchor(buffer[current].GlyphId, (ushort)this.actions[(action * 2) + 1]) ?? default;

                    // The anchor point table stores integer design units.
                    xOffset = (int)(baseAnchor.XCoordinate - markAnchor.XCoordinate);
                    yOffset = (int)(baseAnchor.YCoordinate - markAnchor.YCoordinate);
                    break;
                case AnchorAction.Coordinates:
                    // The four signed words give X and Y for the marked glyph, then X and Y for the current glyph.
                    if (action >= this.actions.Length / 4)
                    {
                        return false;
                    }

                    int offset = action * 4;
                    xOffset = this.actions[offset] - this.actions[offset + 2];
                    yOffset = this.actions[offset + 1] - this.actions[offset + 3];
                    break;
                default:
                    return false;
            }

            // Reserve the work needed to sum all intervening advances before
            // installing the link. Every accepted link can then be resolved.
            if (!buffer.CountOperations(Math.Abs(distance)))
            {
                return false;
            }

            position.Bounds.X = xOffset;
            position.Bounds.Y = yOffset;
            position.CursiveAttachment = GlyphShapingPosition.NoCursiveAttachment;

            // A nonadvancing transition can adjust two points on the same glyph.
            // Apply the delta without creating an attachment to the glyph itself.
            position.MarkAttachment = parent == current ? -1 : parent;
            return true;
        }
    }
}
