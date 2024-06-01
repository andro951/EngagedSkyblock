using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;

namespace EngagedSkyblock.Items {
	public class HellstoneBlockPlacer : BlockPlacer {
		protected override int PrimaryMaterial => ItemID.HellstoneBar;
		protected override List<(int, int)> craftingMaterials => new() {
			(ItemID.StoneBlock, 50)
		};

		protected override int cooldown => new Tiles.HellstoneBlockPlacer().cooldown;
		protected override Func<int> createTile => ModContent.TileType<Tiles.HellstoneBlockPlacer>;
	}
}
