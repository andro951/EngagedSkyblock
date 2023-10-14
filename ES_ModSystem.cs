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
using androLib;
using System.Reflection;
using Terraria.GameContent;
using EngagedSkyblock.Common.Globals;
using EngagedSkyblock.Weather;
using Terraria.IO;

namespace EngagedSkyblock {
	public class ES_ModSystem : ModSystem {
		public override void OnWorldLoad() {
			WorldLoadSetup();
		}
		public const string skyblockWorldKey = "ES_SkyblockWorld";
		public override void SaveWorldHeader(TagCompound tag) {
			tag[skyblockWorldKey] = ES_WorldGen.CheckSkyblockSeed();
		}
		public static void WorldLoadSetup() {
			ES_WorldGen.OnWorldLoad();//Should always be first here.  get's world seed
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
		public override void AddRecipeGroups() {
			(string, IEnumerable<int>)[] recipeGroups = new (string, IEnumerable<int>)[] {

			};

			foreach ((string name, IEnumerable<int> itemTypes) in recipeGroups) {
				RecipeGroup group = new(() => name.AddSpaces(), itemTypes.ToArray());
				RecipeGroup.RegisterGroup(name, group);
			}
		}
		private static SortedSet<int> statuesNotDisabled = new();
		private static SortedSet<int> disabledRecipes = new();
		public override void AddRecipes() {
			for (int i = 0; i < AndroMod.VanillaRecipeCount; i++) {
				Recipe recipe = Main.recipe[i];
				if (!IsActivatableStatue(recipe.createItem))
					continue;

				if (recipe.requiredItem.Count > 1) {
					recipe.DisableRecipe();
					disabledRecipes.Add(recipe.createItem.type);
				}
				else {
					statuesNotDisabled.Add(recipe.createItem.type);
				}
			}

			for (int i = 0; i < ItemID.Count; i++) {
				Item item = i.CSI();
				if (item.NullOrAir())
					continue;

				if (IsActivatableStatue(item) && !statuesNotDisabled.Contains(i)) {
					int blockType = i >= ItemID.LihzahrdStatue && i <= ItemID.LihzahrdGuardianStatue ? ItemID.LihzahrdBrick : ItemID.StoneBlock;
					Recipe recipe = Recipe.Create(i).AddTile(TileID.HeavyWorkBench).AddIngredient(blockType, 50).Register();
				}
			}

			/*
			(int createItem, int requiredItem, int requiredItemStack)[] statuesRequireItems = new (int createItem, int requiredItem, int requiredItemStack)[] {

			};

			foreach ((int createItem, int requiredItem, int requiredItemStack) in statuesRequireItems) {
				Recipe.Create(createItem).AddTile(TileID.HeavyWorkBench).AddIngredient(ItemID.StoneBlock, 50).AddIngredient(requiredItem, requiredItemStack).AddCondition(SkyblockCondition).Register();
			}

			(int createItem, int group, int requiredItemStack)[] statuesRequireVanillaGroup = new (int createItem, int group, int requiredItemStack)[] {
				//(ItemID.BuggyStatue, RecipeGroupID.Bugs, 5),
			};

			foreach ((int createItem, int group, int requiredItemStack) in statuesRequireVanillaGroup) {
				Recipe.Create(createItem).AddTile(TileID.HeavyWorkBench).AddIngredient(ItemID.StoneBlock, 50).AddRecipeGroup(group, requiredItemStack).AddCondition(SkyblockCondition).Register();
			}

			(int createItem, string group, int requiredItemStack)[] statuesRequireGroup = new (int createItem, string group, int requiredItemStack)[] {

			};

			foreach ((int createItem, string group, int requiredItemStack) in statuesRequireGroup) {
				Recipe.Create(createItem).AddTile(TileID.HeavyWorkBench).AddIngredient(ItemID.StoneBlock, 50).AddRecipeGroup(group, requiredItemStack).AddCondition(SkyblockCondition).Register();
			}
			*/

			Recipe.Create(ItemID.MudBlock, 2).AddTile(TileID.Sinks).AddIngredient(ItemID.DirtBlock, 1).AddIngredient(ItemID.Hay, 1).Register();
			Recipe.Create(ItemID.SiltBlock, 2).AddTile(TileID.Furnaces).AddIngredient(ItemID.ClayBlock, 1).AddIngredient(ItemID.SandBlock, 1).Register();
			Recipe.Create(ItemID.SnowBlock).AddIngredient(ItemID.Snowball, 15).Register();
		}
		public static bool IsActivatableStatue(Item item) => !item.NullOrAir() && item.createTile == TileID.Statues || item.createTile == TileID.MushroomStatue || item.createTile == TileID.BoulderStatue;
		public static void SwitchStatueRecipesDisabled() {
			bool skyblockWorld = ES_WorldGen.SkyblockWorld;
			for (int i = 0; i < Recipe.numRecipes; i++) {
				Recipe recipe = Main.recipe[i];
				if (recipe.createItem.NullOrAir())
					continue;

				if (recipe.createItem.type >= ItemID.Count)
					continue;


				if (recipe.Mod == null || recipe.Mod.Name != EngagedSkyblock.ModName) {
					if (skyblockWorld) {
						if (recipe.Disabled)
							continue;

						if (disabledRecipes.Contains(recipe.createItem.type)) {
							ReflectionHelper recipeHelper = new(recipe);
							recipeHelper.SetProperty("Disabled", true, BindingFlags.Public | BindingFlags.Instance);
						}
					}
					else {
						if (!recipe.Disabled)
							continue;

						if (disabledRecipes.Contains(recipe.createItem.type)) {
							ReflectionHelper recipeHelper = new(recipe);
							recipeHelper.SetProperty("Disabled", false, BindingFlags.Public | BindingFlags.Instance);
						}
					}
				}
				else {
					if (skyblockWorld) {
						if (!recipe.Disabled)
							continue;

						ReflectionHelper recipeHelper = new(recipe);
						recipeHelper.SetProperty("Disabled", false, BindingFlags.Public | BindingFlags.Instance);
					}
					else {
						if (recipe.Disabled)
							continue;

						ReflectionHelper recipeHelper = new(recipe);
						recipeHelper.SetProperty("Disabled", true, BindingFlags.Public | BindingFlags.Instance);
					}
				}
			}

			Recipe.UpdateWhichItemsAreMaterials();
			Recipe.UpdateWhichItemsAreCrafted();
			ReflectionHelper.CallNonPublicStaticMethod("Terraria", typeof(Recipe).Name, "UpdateMaterialFieldForAllRecipes");
			ReflectionHelper.CallNonPublicStaticMethod("Terraria", typeof(Recipe).Name, "CreateRequiredItemQuickLookups");
			ShimmerTransforms.UpdateRecipeSets();
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
		public static Action PreUpdateWorldActions = null;
		public override void PreUpdateWorld() {
			if (PreUpdateWorldActions != null) {
				PreUpdateWorldActions();
				PreUpdateWorldActions = null;
			}
		}
		public override void PostUpdateEverything() {
			ES_Liquid.Update();
			ES_Weather.Update();
		}
		public override void PostAddRecipes() {
			GlobalHammer.PostSetupRecipes();
		}
	}
}
