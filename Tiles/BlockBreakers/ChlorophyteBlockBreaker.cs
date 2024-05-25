using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Terraria;

namespace EngagedSkyblock.Tiles {
	public class ChlorophyteBlockBreaker : BlockBreaker {
		public override int pickaxePower => PickaxePowerID.Chlorophyte;
		public override int miningCooldown => 4;
		public override IEnumerable<Item> GetItemDrops(int i, int j) {
			return new Item[] { new Item(ModContent.ItemType<Items.ChlorophyteBlockBreaker>()) };
		}
	}
}
