using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using EngagedSkyblock.Tiles;
using System.Collections.Generic;

namespace EngagedSkyblock.Items {
	[Autoload(false)]
	public class LuminiteAutoExtractinator : AutoExtractinator {
		public override int CreateTile => ModContent.TileType<LuminiteAutoExtractinatorTile>();
		public override int Rarity => ItemRarityID.Red;
		public override int Tier => 4;
		public override int RecipeRequiredTile => TileID.MythrilAnvil;
		public override List<(int, int)> Ingredients => new() {
			(ItemID.LunarBar, 20),
			(ItemID.ChlorophyteExtractinator, 1)
		};
	}
}
