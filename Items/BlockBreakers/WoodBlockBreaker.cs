using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;

namespace EngagedSkyblock.Items {
	public class WoodBlockBreaker : BlockBreaker {
		protected override int PrimaryMaterial => ItemID.Wood;
		protected override List<(int, int)> craftingMaterials => new() {
			(ItemID.StoneBlock, 50)
		};

		protected override int pickaxePower => new Tiles.WoodBlockBreaker().pickaxePower;
		protected override int miningCooldown => new Tiles.WoodBlockBreaker().miningCooldown;
		protected override Func<int> createTile => ModContent.TileType<Tiles.WoodBlockBreaker>;
	}
}
