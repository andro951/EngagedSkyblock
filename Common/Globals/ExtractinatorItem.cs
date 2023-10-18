using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EngagedSkyblock.Common.Globals {
	public class ExtractinatorItem : GlobalItem {
		public override void ExtractinatorUse(int extractType, int extractinatorBlockType, ref int resultType, ref int resultStack) {
			if (!ES_WorldGen.SkyblockWorld)
				return;

			if (extractType == ItemID.DesertFossil) {
				if (Main.rand.Next(100) == 0) {
					resultStack = 1;
					resultType = ItemID.LifeCrystal;
				}
			}
			else {
				if (Main.rand.Next(50) == 0) {
					resultStack = 1;
					if (Main.rand.Next(20) == 0)
						resultStack += Main.rand.Next(0, 2);

					if (Main.rand.Next(30) == 0)
						resultStack += Main.rand.Next(0, 3);

					if (Main.rand.Next(40) == 0)
						resultStack += Main.rand.Next(0, 4);

					if (Main.rand.Next(50) == 0)
						resultStack += Main.rand.Next(0, 5);

					if (Main.rand.Next(60) == 0)
						resultStack += Main.rand.Next(0, 6);

					resultType = Main.rand.NextBool() ? ItemID.DemoniteOre : ItemID.CrimtaneOre;
				}
			}
		}
	}
}
