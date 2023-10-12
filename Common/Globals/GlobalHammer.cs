using androLib.Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EngagedSkyblock.Common.Globals {
	public class GlobalHammer : GlobalItem {
		public override bool AppliesToEntity(Item entity, bool lateInstantiation) {
			return entity.hammer > 0;
		}
		public override void Load() {
			On_Player.ItemCheck_ManageRightClickFeatures += On_Player_ItemCheck_ManageRightClickFeatures;
			On_Player.ItemCheck_Inner += On_Player_ItemCheck_Inner;
		}

		private void On_Player_ItemCheck_Inner(On_Player.orig_ItemCheck_Inner orig, Player self) {
			orig(self);
			if (PostUseActions != null) {
				PostUseActions();
				PostUseActions = null;
			}
		}

		public static Action PostUseActions;
		private void On_Player_ItemCheck_ManageRightClickFeatures(On_Player.orig_ItemCheck_ManageRightClickFeatures orig, Player self) {
			if (!ES_WorldGen.SkyblockWorld) {
				orig(self);
				return;
			}

			orig(self);

			Item heldItem = self.HeldItem;
			if (Main.mouseRight && !heldItem.NullOrAir() && heldItem.TryGetGlobalItem(out GlobalHammer _)) {
				self.controlUseItem = true;
				int hammer = heldItem.hammer;
				int pick = heldItem.pick;
				heldItem.pick = Math.Max(pick, hammer);
				heldItem.hammer = 0;
				PostUseActions += () => {
					heldItem.hammer = hammer;
					heldItem.pick = pick;
				};
			}
		}
	}
}
