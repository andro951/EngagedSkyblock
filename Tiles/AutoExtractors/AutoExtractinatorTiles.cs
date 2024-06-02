using System;
using EngagedSkyblock.Items;
using EngagedSkyblock.Tiles.TileEntities;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;

namespace EngagedSkyblock.Tiles
{
    public class WoodAutoExtractinatorTile : AutoExtractorTile {
        public override AutoExtractorTE NewEntity => ModContent.GetInstance<WoodAutoExtractinatorTE>();
		public override Func<int> MyItemType => ModContent.ItemType<WoodAutoExtractinator>;
        public override int Tier => 0;
		public override float UseSpeedMultiplier => 0.2f;
	}
	
    public class HellstoneAutoExtractinatorTile : AutoExtractorTile {
		public override AutoExtractorTE NewEntity => ModContent.GetInstance<HellstoneAutoExtractinatorTE>();
		public override Func<int> MyItemType => ModContent.ItemType<HellstoneAutoExtractinator>;
		public override int Tier => 2;
		public override float UseSpeedMultiplier => 1f / 0.6f;
	}

    public class LuminiteAutoExtractinatorTile : AutoExtractorTile {
		public override AutoExtractorTE NewEntity => ModContent.GetInstance<LuminiteAutoExtractinatorTE>();
		public override Func<int> MyItemType => ModContent.ItemType<LuminiteAutoExtractinator>;
		public override int Tier => 4;
		public override float UseSpeedMultiplier => 5f;
		public override void AnimateTile(ref int frame, ref int frameCounter) {
			frame = Main.tileFrame[TileID.ChlorophyteExtractinator];
			frameCounter = Main.tileFrameCounter[TileID.ChlorophyteExtractinator];
		}
	}
}
