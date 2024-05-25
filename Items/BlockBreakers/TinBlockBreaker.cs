using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;

namespace EngagedSkyblock.Items {
	public class TinBlockBreaker : BlockBreaker {
		protected override int PrimaryMaterial => ItemID.TinBar;
		protected override List<(int, int)> craftingMaterials => new() {
			(ItemID.StoneBlock, 50)
		};

		protected override int pickaxePower => new Tiles.TinBlockBreaker().pickaxePower;
		protected override int miningCooldown => new Tiles.TinBlockBreaker().miningCooldown;
		protected override Func<int> createTile => ModContent.TileType<Tiles.TinBlockBreaker>;
	}
}
