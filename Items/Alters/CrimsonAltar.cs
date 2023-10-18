using androLib.Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace EngagedSkyblock.Items {
	public class CrimsonAltar : Altar {
		public override void SetDefaults() {
			Item.createTile = ModContent.TileType<Tiles.CrimsonAltar>();
			Item.width = 44;
			Item.height = 30;
			base.SetDefaults();
		}
		public override void ModifyRecipe(Recipe recipe) {
			recipe.AddCondition(Condition.CrimsonWorld);
		}
		public override string LocalizationTooltip =>
			$"Works the same as vanilla Demon Altar except is craftable.";
	}
}
