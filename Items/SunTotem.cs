using androLib;
using androLib.Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;

namespace EngagedSkyblock.Items {
	public class SunTotem : ES_ModItem {
		public override List<WikiTypeID> WikiItemTypes => new() { WikiTypeID.Furniture };
		public override string Artist => "andro951";
		public override string Designer => "andro951";
		private static int wood = 50;
		private static int gems = 2;
		public override void SetDefaults() {
			Item.createTile = ModContent.TileType<Tiles.SunTotem>();
			Item.value = ItemID.Wood.CSI().value * wood + ItemID.Topaz.CSI().value * gems;
			Item.rare = ItemRarityID.Blue;
			Item.width = 32;
			Item.height = 32;
			Item.maxStack = Terraria.Item.CommonMaxStack;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 10;
			Item.useAnimation = 15;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.consumable = true;
		}
		public override void AddRecipes() {
			CreateRecipe().AddTile(TileID.Sawmill).AddRecipeGroup(RecipeGroupID.Wood, wood).AddRecipeGroup($"{AndroMod.ModName}:{AndroModSystem.AnyCommonGem}", gems).Register();
		}
		public override string LocalizationTooltip =>
			$"Prevents rain and snow.";
	}
}
