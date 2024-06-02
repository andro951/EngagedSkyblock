using Terraria.ModLoader;
using EngagedSkyblock.Tiles;
using Terraria.ID;

namespace EngagedSkyblock.Tiles.TileEntities
{
    public class WoodAutoExtractinatorTE : AutoExtractorTE {
        protected override int Timer => 600;
        protected override int ConsumeMultiplier => 1;
        protected override int TileToBeValidOn => ModContent.TileType<WoodAutoExtractinatorTile>();
    }

    public class VanillaAutoExtractinatorTE : AutoExtractorTE {
        protected override int Timer => 60;
        protected override int ConsumeMultiplier => 1;
        protected override int TileToBeValidOn => TileID.Extractinator;
    }

    public class HellstoneAutoExtractinatorTE : AutoExtractorTE {
        protected override int Timer => 60;
        protected override int ConsumeMultiplier => 2;
        protected override int TileToBeValidOn => ModContent.TileType<HellstoneAutoExtractinatorTile>();
    }

    public class ChlorophyteAutoExtractinatorTE : AutoExtractorTE {
        protected override int Timer => 60;
        protected override int ConsumeMultiplier => 4;
        protected override int TileToBeValidOn => TileID.ChlorophyteExtractinator;
    }

    public class LuminiteAutoExtractinatorTE : AutoExtractorTE {
        protected override int Timer => 60;
        protected override int ConsumeMultiplier => 10;
        protected override int TileToBeValidOn => ModContent.TileType<LuminiteAutoExtractinatorTile>();
    }
}
