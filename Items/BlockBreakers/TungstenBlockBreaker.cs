using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;

namespace EngagedSkyblock.Items {
	public class TungstenBlockBreaker : BlockBreaker {
		protected override int PrimaryMaterial => ItemID.TungstenBar;
		protected override List<(int, int)> craftingMaterials => new() {
			(ItemID.StoneBlock, 50)
		};

		protected override int pickaxePower => new Tiles.TungstenBlockBreaker().pickaxePower;
		protected override int miningCooldown => new Tiles.TungstenBlockBreaker().miningCooldown;
		protected override Func<int> createTile => ModContent.TileType<Tiles.TungstenBlockBreaker>;
	}
}
