using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using EngagedSkyblock.Tiles;
using System.Collections.Generic;

namespace EngagedSkyblock.Items {
	[Autoload(false)]
	public class HellstoneAutoExtractinator : AutoExtractinator {
		public override int CreateTile => ModContent.TileType<HellstoneAutoExtractinatorTile>();
		public override int Rarity => ItemRarityID.LightRed;
		public override int Tier => 2;
		public override int RecipeRequiredTile => TileID.Anvils;
		public override List<(int, int)> Ingredients => new() {
			(ItemID.HellstoneBar, 20),
			(ItemID.Extractinator, 1)
		};
	}
}
