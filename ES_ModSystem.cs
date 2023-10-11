using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.WorldBuilding;
using androLib.Common.Utility;
using androLib.Common.Globals;

namespace EngagedSkyblock {
	public class ES_ModSystem : ModSystem {
		public override void OnWorldLoad() {
			ES_WorldGen.OnWorldLoad();
		}
		private static SortedDictionary<int, SortedSet<int>> AllItemDrops {
			get {
				if (allItemDrops == null)
					SetupAllItemDrops();

				return allItemDrops;
			}
		}
		private static SortedDictionary<int, SortedSet<int>> allItemDrops = null;
		private static void SetupAllItemDrops() {
			allItemDrops = new();
			foreach (KeyValuePair<int, NPC> npcPair in ContentSamples.NpcsByNetId) {
				int netID = npcPair.Key;
				NPC npc = npcPair.Value;
				List<IItemDropRule> dropRules = Main.ItemDropsDB.GetRulesForNPCID(netID, false).ToList();
				foreach (IItemDropRule dropRule in dropRules) {
					List<DropRateInfo> dropRates = new();
					DropRateInfoChainFeed dropRateInfoChainFeed = new(1f);
					dropRule.ReportDroprates(dropRates, dropRateInfoChainFeed);
					foreach (DropRateInfo dropRate in dropRates) {
						int itemType = dropRate.itemId;
						allItemDrops.AddOrCombine(itemType, netID);
					}
				}
			}
		}
		public override void AddRecipes() {
			Recipe.Create(ItemID.MudBlock, 2).AddTile(TileID.Sinks).AddIngredient(ItemID.DirtBlock, 1).AddIngredient(ItemID.Hay, 1).Register();
			Recipe.Create(ItemID.SiltBlock, 2).AddTile(TileID.Furnaces).AddIngredient(ItemID.ClayBlock, 1).AddIngredient(ItemID.SandBlock, 1).Register();
		}

		internal static void PrintNPCsThatDropItem(int itemType) {
			if (itemType <= 0 || itemType >= ItemLoader.ItemCount)
				return;

			Item item = itemType.CSI();
			string label = $"NPCs that drop {item.S()}";
			if (!AllItemDrops.TryGetValue(itemType, out SortedSet<int> npcs)) {
				$"{label}None\n".LogSimpleNT();
			}
			else {
				npcs.EnumerableToStringBlock(label, (netID) => netID.CSNPC().S());
			}
		}
	}
}
