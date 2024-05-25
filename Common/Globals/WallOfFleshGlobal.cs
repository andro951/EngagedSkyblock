using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EngagedSkyblock.Common.Globals {
	public class WallOfFleshGlobal : GlobalNPC {
		private static bool downedWallOfFlesh = false;
		public static void OnWorldLoad() {
			downedWallOfFlesh = Main.hardMode;
		}
		public override void OnKill(NPC npc) {
			 if (npc.netID != NPCID.WallofFlesh)
				return;

			if (!downedWallOfFlesh) {
				downedWallOfFlesh = true;
				ES_WorldGen.TrySpawnChlorophyteKilldedWallOfFlesh();
			}
		}
	}
}
