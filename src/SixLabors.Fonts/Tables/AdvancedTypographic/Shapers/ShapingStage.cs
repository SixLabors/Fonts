// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;

/// <summary>
/// An individual shaping stage.
/// Each stage must have a feature tag but can also contain pre and post feature processing operations.
/// </summary>
/// <remarks>
/// For comparison purposes we only care about the feature tag as we want to avoid duplication.
/// </remarks>
internal readonly struct ShapingStage : IEquatable<ShapingStage>
{
    /// <summary>The optional action to invoke before the feature is applied.</summary>
    private readonly Action<IGlyphShapingCollection, int, int>? preAction;

    /// <summary>The optional action to invoke after the feature is applied.</summary>
    private readonly Action<IGlyphShapingCollection, int, int>? postAction;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShapingStage"/> struct.
    /// </summary>
    /// <param name="featureTag">The OpenType feature tag for this stage.</param>
    /// <param name="preAction">An optional action to invoke before the feature is applied.</param>
    /// <param name="postAction">An optional action to invoke after the feature is applied.</param>
    public ShapingStage(Tag featureTag, Action<IGlyphShapingCollection, int, int>? preAction, Action<IGlyphShapingCollection, int, int>? postAction)
    {
        this.FeatureTag = featureTag;
        this.preAction = preAction;
        this.postAction = postAction;
    }

    /// <summary>
    /// Gets the OpenType feature tag for this stage.
    /// </summary>
    public Tag FeatureTag { get; }

    /// <summary>
    /// Invokes the pre-processing action for this shaping stage, if one was provided.
    /// </summary>
    /// <param name="collection">The glyph shaping collection.</param>
    /// <param name="index">The zero-based index of the first element.</param>
    /// <param name="count">The number of elements.</param>
    public void PreProcessFeature(IGlyphShapingCollection collection, int index, int count)
        => this.preAction?.Invoke(collection, index, count);

    /// <summary>
    /// Invokes the post-processing action for this shaping stage, if one was provided.
    /// </summary>
    /// <param name="collection">The glyph shaping collection.</param>
    /// <param name="index">The zero-based index of the first element.</param>
    /// <param name="count">The number of elements.</param>
    public void PostProcessFeature(IGlyphShapingCollection collection, int index, int count)
        => this.postAction?.Invoke(collection, index, count);

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is ShapingStage stage && this.Equals(stage);

    /// <inheritdoc />
    public bool Equals(ShapingStage other) => this.FeatureTag.Equals(other.FeatureTag);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(this.FeatureTag);
}
