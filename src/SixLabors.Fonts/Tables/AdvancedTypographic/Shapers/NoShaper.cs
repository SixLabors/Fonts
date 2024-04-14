// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts;
using SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;

internal class NoShaper : BaseShaper
{
    private readonly HashSet<ShapingStage> shapingStages = new();

    internal NoShaper()
    {
    }

    protected override void PlanFeatures(IGlyphShapingCollection collection, int index, int count)
    {
    }

    protected override void PlanPreprocessingFeatures(IGlyphShapingCollection collection, int index, int count)
    {
    }

    protected override void PlanPostprocessingFeatures(IGlyphShapingCollection collection, int index, int count)
    {
    }

    /// <inheritdoc />
    protected override void AssignFeatures(IGlyphShapingCollection collection, int index, int count)
    {
    }

    public override IEnumerable<ShapingStage> GetShapingStages() => this.shapingStages;
}
