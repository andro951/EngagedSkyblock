using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace EngagedSkyblock.Items {
	internal class DemonAltar : Altar {
		public override void SetDefaults() {
			Item.createTile = ModContent.TileType<Tiles.DemonAltar>();
			Item.width = 48;
			Item.height = 34;
			base.SetDefaults();
		}
		public override void ModifyRecipe(Recipe recipe) {
			recipe.AddCondition(Condition.CorruptWorld);
		}

		public override string LocalizationTooltip =>
			$"Works the same as vanilla Crimson Altar except is craftable.";
	}
}
