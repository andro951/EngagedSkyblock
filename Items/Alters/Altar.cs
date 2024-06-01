using androLib;
using androLib.Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;

namespace EngagedSkyblock.Items {
	public abstract class Altar : ES_ModItem {
		public override List<WikiTypeID> WikiItemTypes => new() { WikiTypeID.CraftingStation };
		public override string Artist => "andro951";
		public override string Designer => "andro951";
		public override void SetDefaults() {
			Item.maxStack = Terraria.Item.CommonMaxStack;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 10;
			Item.useAnimation = 15;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.value = ItemID.Shackle.CSI().value * 2 + ItemID.Chain.CSI().value * 20 + ItemID.BlackLens.CSI().value;
			Item.rare = ItemRarityID.Blue;
			base.SetDefaults();
		}
		public override void AddRecipes() {
			Recipe recipe = CreateRecipe().AddTile(TileID.HeavyWorkBench).AddIngredient(ItemID.StoneBlock, 100).AddIngredient(ItemID.Shackle, 2).AddIngredient(ItemID.Chain, 20).AddIngredient(ItemID.BlackLens);
			ModifyRecipe(recipe);
			recipe.Register();
		}
		public abstract void ModifyRecipe(Recipe recipe);
	}
}
