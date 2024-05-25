using System;
using EngagedSkyblock.Items.AutoExtractors;
using EngagedSkyblock.Tiles.TileEntities;
using Terraria.ModLoader;

namespace EngagedSkyblock.Tiles.AutoExtractors
{
    public class AutoExtractorTier1Tile : AutoExtractor_BaseTile
    {
        protected override int ExtractorTile => Type;
        protected override string TilesheetPath => "EngagedSkyblock/Tiles/AutoExtractors/AutoExtractorTier1Tile";
        protected override ModTileEntity Entity => ModContent.GetInstance<AutoExtractorTier1Entity>();
        protected override Func<int> MyItemType => ModContent.ItemType<AutoExtractor>;
	}

    public class AutoExtractorTier2Tile : AutoExtractor_BaseTile
    {
        protected override int ExtractorTile => Type;
        protected override string TilesheetPath => "EngagedSkyblock/Tiles/AutoExtractors/AutoExtractorTier2Tile";
        protected override ModTileEntity Entity => ModContent.GetInstance<AutoExtractorTier2Entity>();
		protected override Func<int> MyItemType => ModContent.ItemType<AutoExtractorTier2>;
	}

    public class AutoExtractorTier3Tile : AutoExtractor_BaseTile
    {
        protected override int ExtractorTile => Type;
        protected override string TilesheetPath => "EngagedSkyblock/Tiles/AutoExtractors/AutoExtractorTier3Tile";
        protected override ModTileEntity Entity => ModContent.GetInstance<AutoExtractorTier3Entity>();
		protected override Func<int> MyItemType => ModContent.ItemType<AutoExtractorTier3>;
	}
    public class AutoExtractorTier4Tile : AutoExtractor_BaseTile
    {
        protected override int ExtractorTile => Type;
        protected override string TilesheetPath => "EngagedSkyblock/Tiles/AutoExtractors/AutoExtractorTier4Tile";
        protected override ModTileEntity Entity => ModContent.GetInstance<AutoExtractorTier4Entity>();
		protected override Func<int> MyItemType => ModContent.ItemType<AutoExtractorTier4>;
	}

    public class AutoExtractorTier5Tile : AutoExtractor_BaseTile
    {
        protected override int ExtractorTile => Type;
        protected override string TilesheetPath => "EngagedSkyblock/Tiles/AutoExtractors/AutoExtractorTier5Tile";
        protected override ModTileEntity Entity => ModContent.GetInstance<AutoExtractorTier5Entity>();
		protected override Func<int> MyItemType => ModContent.ItemType<AutoExtractorTier5>;
	}
}
