using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;

namespace EngagedSkyblock.Items {
	public class ChlorophyteBlockPlacer : BlockPlacer {
		protected override int PrimaryMaterial => ItemID.ChlorophyteBar;
		protected override List<(int, int)> craftingMaterials => new() {
			(ItemID.StoneBlock, 50)
		};

		protected override int cooldown => new Tiles.ChlorophyteBlockPlacer().cooldown;
		protected override Func<int> createTile => ModContent.TileType<Tiles.ChlorophyteBlockPlacer>;
	}
}
