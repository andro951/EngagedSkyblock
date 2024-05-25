using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;

namespace EngagedSkyblock.Items {
	public class PlatinumBlockBreaker : BlockBreaker {
		protected override int PrimaryMaterial => ItemID.PlatinumBar;
		protected override List<(int, int)> craftingMaterials => new() {
			(ItemID.StoneBlock, 50)
		};

		protected override int pickaxePower => new Tiles.PlatinumBlockBreaker().pickaxePower;
		protected override int miningCooldown => new Tiles.PlatinumBlockBreaker().miningCooldown;
		protected override Func<int> createTile => ModContent.TileType<Tiles.PlatinumBlockBreaker>;
	}
}
