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
	public abstract class BlockBreaker : ES_ModItem {
		public override List<WikiTypeID> WikiItemTypes => new() { WikiTypeID.Furniture };
		public override string Artist => "andro951";
		public override string Designer => "andro951";
		protected const int primaryMateriayStack = 8;
		protected abstract int pickaxePower { get; }
		protected abstract int miningCooldown { get; }
		protected abstract int PrimaryMaterial { get; }
		protected virtual string PrimaryRecipeGroup => null;
		protected abstract List<(int, int)> craftingMaterials { get; }
		public List<(int, int)> CraftingMaterials {
			get {
				List<(int, int)> materials = new() {
					(PrimaryMaterial, primaryMateriayStack)
				};

				materials.AddRange(craftingMaterials);

				return materials;
			}
		}
		protected abstract Func<int> createTile { get; }
		public override void SetDefaults() {
			Item.createTile = createTile();
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 10;
			Item.useAnimation = 15;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.value = CraftingMaterials.Select(m => m.Item1.CSI().value * m.Item2).Sum();
			Item.maxStack = Item.CommonMaxStack;
			Item.consumable = true;
		}
		protected virtual void ModifyRecipe(Recipe recipe) { }
		public override void AddRecipes() {
			Recipe recipe = CreateRecipe().AddTile(TileID.Anvils);
			foreach ((int type, int stack) material in CraftingMaterials) {
				recipe.AddIngredient(material.type, material.stack);
			}

			ModifyRecipe(recipe);
			recipe.Register();
		}

		public override string LocalizationTooltip =>
			$"Breaks blocks that is points to when receiving a wiring signal.\n" +
			$"Pickaxe Power: {pickaxePower}\n" +
			$"Cooldown: {(miningCooldown / 60f).S(2)} seconds\n" +
			$"Can be used to break blocks that require {Lang.GetItemNameValue(PrimaryMaterial)} to break." +
			$"If wire is placed on this block, it will only try to mine each time it receives a wiring signal.";
	}
}
