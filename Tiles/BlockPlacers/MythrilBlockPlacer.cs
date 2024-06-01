using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Terraria;

namespace EngagedSkyblock.Tiles {
	public class MythrilBlockPlacer : BlockPlacer {
		public override int cooldown => 30;
		public override IEnumerable<Item> GetItemDrops(int i, int j) {
			return new Item[] { new Item(ModContent.ItemType<Items.MythrilBlockPlacer>()) };
		}
	}
}
