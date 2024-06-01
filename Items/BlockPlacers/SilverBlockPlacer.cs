using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;

namespace EngagedSkyblock.Items {
	public class SilverBlockPlacer : BlockPlacer {
		protected override int PrimaryMaterial => ItemID.SilverBar;
		protected override List<(int, int)> craftingMaterials => new() {
			(ItemID.StoneBlock, 50)
		};

		protected override int cooldown => new Tiles.SilverBlockPlacer().cooldown;
		protected override Func<int> createTile => ModContent.TileType<Tiles.SilverBlockPlacer>;
	}
}
