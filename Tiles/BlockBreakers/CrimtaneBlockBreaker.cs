using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Terraria;

namespace EngagedSkyblock.Tiles {
	public class CrimtaneBlockBreaker : BlockBreaker {
		public override int pickaxePower => PickaxePowerID.Deathbringer;
		public override int miningCooldown => 54;
		public override IEnumerable<Item> GetItemDrops(int i, int j) {
			return new Item[] { new Item(ModContent.ItemType<Items.CrimtaneBlockBreaker>()) };
		}
	}
}
