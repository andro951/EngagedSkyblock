using androLib.Common.Globals;
using androLib.Common.Utility;
using EngagedSkyblock.Common.ItemDropRules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EngagedSkyblock.Common.Globals {
	public class ES_GlobalNPC : GlobalNPC {
		float ConfigDropChance => 1f;
		public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot) {
			if (npc.aiStyle == NPCAIStyleID.Slime) {
				npcLoot.Add(new CommonSkyblockDropRule(ItemID.JungleGrassSeeds, 0.02f, ConfigDropChance));
				npcLoot.Add(new CommonSkyblockDropRule(ItemID.GrassSeeds, 0.02f, ConfigDropChance));
				npcLoot.Add(new CommonSkyblockDropRule(ItemID.Acorn, 0.05f, ConfigDropChance));
			}
		}
	}
}
