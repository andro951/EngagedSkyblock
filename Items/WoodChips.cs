using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using androLib.Common;
using androLib.Common.Utility;
using Terraria.ID;
using Terraria;
using androLib.Items;

namespace EngagedSkyblock.Items {
	public class WoodChips : ES_ModItem {
		public override string Texture => (GetType().Namespace + ".Sprites." + Name).Replace('.', '/');
		public override List<WikiTypeID> WikiItemTypes => new() { WikiTypeID.GrabBag };
		public override string Artist => "andro951";
		public override string Designer => "andro951";

		public override void SetStaticDefaults() {
			ItemID.Sets.OpenableBag[Type] = true;
			ItemSets.Sets.ContinuousRightClickItems.Add(Type);
			base.SetStaticDefaults();
		}
		public override void SetDefaults() {
			Item.width = 20;
			Item.height = 16;
			Item.value = ItemID.Wood.CSI().value / 2;
			Item.consumable = true;
			Item.maxStack = Terraria.Item.CommonMaxStack;
		}
		private static float ConfigChance = 1f;
		private static float AcornChance = 0.1f;
		private static float DropChance = 0.25f;
		public override void ModifyItemLoot(ItemLoot itemLoot) {
			//Acorn
			itemLoot.Add(new BasicDropRule(ItemID.Acorn, AcornChance, ConfigChance));

			//Bugs
			IEnumerable<Item> bugs = RecipeGroup.recipeGroups[RecipeGroupID.Bugs].ValidItems.Select(t => t.CSI());
			IEnumerable<DropData> bugDrops = bugs.Select(i => new DropData(i.type, 1f / i.bait));
			itemLoot.Add(new OneFromWeightedOptionsNotScaledWithLuckDropRule(DropChance, bugDrops, null));
		}
		public override string LocalizationTooltip => 
			$"Right click to break apart the Wood Chips to see what you can find.\n" +
			$"{DropChance.PercentString()} chance to drop bugs.\n" +
			$"{AcornChance.PercentString()} chance to drop acorns.";
	}
}
