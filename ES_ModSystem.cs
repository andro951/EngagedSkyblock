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
using EngagedSkyblock.Content;
using EngagedSkyblock.Content.Liquids;
using EngagedSkyblock.Tiles;
using EngagedSkyblock.Items;
using static EngagedSkyblock.Tiles.ExtractionItem;

namespace EngagedSkyblock
{
    public class ES_ModSystem : ModSystem {
		public override void OnWorldLoad() {
			ES_WorldGen.OnWorldLoad();//Should always be first here.  get's world seed
		}
		public const string skyblockWorldKey = "ES_SkyblockWorld";
		public override void SaveWorldHeader(TagCompound tag) {
			tag[skyblockWorldKey] = ES_WorldGen.CheckSkyblockSeed();
		}
		private static SortedDictionary<int, SortedSet<int>> AllItemDrops {
			get {
				if (allItemDrops == null)
					SetupAllItemDrops();

				return allItemDrops;
			}
		}
		private static SortedDictionary<int, SortedSet<int>> allItemDrops = null;
		public override void PostSetupContent() {
			ExtractionItem.PostSetupContent();
			ExtractTypeSet.PostSetupContent();
			GlobalAutoExtractor.PostSetupContent();
		}
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
				if (recipe.createItem.type == ItemID.ChlorophyteExtractinator) {
					recipe.DisableRecipe();
					disabledRecipes.Add(recipe.createItem.type);
				}

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
					Recipe.Create(i).AddTile(TileID.HeavyWorkBench).AddIngredient(blockType, 50).Register();
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

			//Recipe.Create(ItemID.MudBlock, 2).AddTile(TileID.Sinks).AddIngredient(ItemID.DirtBlock, 1).AddIngredient(ItemID.Hay, 1).Register();
			Recipe.Create(ItemID.SiltBlock, 2).AddTile(TileID.Furnaces).AddIngredient(ItemID.ClayBlock, 1).AddIngredient(ItemID.SandBlock, 1).Register();
			Recipe.Create(ItemID.SnowBlock).AddIngredient(ItemID.Snowball, 15).Register();
			Recipe.Create(ItemID.DirtBlock).AddIngredient(ModContent.ItemType<LeafBlock>()).AddIngredient(ItemID.SiltBlock).Register();
			Recipe.Create(ItemID.LihzahrdBrick, 50).AddTile(TileID.Hellforge).AddIngredient(ItemID.ClayBlock, 40).AddRecipeGroup($"{AndroMod.ModName}:{AndroModSystem.GoldOrPlatinumBar}", 10).AddCondition(Condition.DownedPlantera).Register();
			(int, int[])[] allDyes = new (int, int[])[] {
				(ItemID.BlueBrick, new int[] { ItemID.CyanDye, ItemID.SkyBlueDye, ItemID.BlueDye, ItemID.BlackDye }),
				(ItemID.GreenBrick, new int[] { ItemID.LimeDye, ItemID.GreenDye, ItemID.YellowDye, ItemID.TealDye }),
				(ItemID.PinkBrick, new int[] { ItemID.PinkDye, ItemID.RedDye, ItemID.VioletDye, ItemID.OrangeDye, ItemID.PurpleDye }),
			};

			foreach ((int brickType, int[] dyes) in allDyes) {
				foreach (int dye in dyes) {
					Recipe.Create(brickType, 50).AddIngredient(ItemID.StoneBlock, 40).AddIngredient(ItemID.Obsidian, 10).AddIngredient(dye).Register();
				}
			}

			(int brick, int wall, int tiledWall, int slabWall, int wallSafe, int tiledWallSafe, int slabWallSafe)[] brickSets = new (int brick, int wall, int tiledWall, int slabWall, int wallSafe, int tiledWallSafe, int slabWallSafe)[] {
				(ItemID.BlueBrick, ItemID.BlueBrickWallUnsafe, ItemID.BlueTiledWallUnsafe, ItemID.BlueSlabWallUnsafe, ItemID.BlueBrickWall, ItemID.BlueTiledWall, ItemID.BlueSlabWall),
				(ItemID.GreenBrick, ItemID.GreenBrickWallUnsafe, ItemID.GreenTiledWallUnsafe, ItemID.GreenSlabWallUnsafe, ItemID.GreenBrickWall, ItemID.GreenTiledWall, ItemID.GreenSlabWall),
				(ItemID.PinkBrick, ItemID.PinkBrickWallUnsafe, ItemID.PinkTiledWallUnsafe, ItemID.PinkSlabWallUnsafe, ItemID.PinkBrickWall, ItemID.PinkTiledWall, ItemID.PinkSlabWall)
			};

			foreach (var brickSet in brickSets) {
				Recipe.Create(brickSet.wall, 4).AddIngredient(brickSet.brick).AddTile(TileID.HeavyWorkBench).AddCondition(Condition.DownedSkeletron).DisableDecraft().Register();
				Recipe.Create(brickSet.tiledWall, 4).AddIngredient(brickSet.brick).AddTile(TileID.HeavyWorkBench).AddCondition(Condition.DownedSkeletron).DisableDecraft().Register();
				Recipe.Create(brickSet.slabWall, 4).AddIngredient(brickSet.brick).AddTile(TileID.HeavyWorkBench).AddCondition(Condition.DownedSkeletron).DisableDecraft().Register();

				Recipe.Create(brickSet.brick).AddIngredient(brickSet.wall, 4).AddTile(TileID.HeavyWorkBench).AddCondition(Condition.DownedSkeletron).DisableDecraft().Register();
				Recipe.Create(brickSet.brick).AddIngredient(brickSet.tiledWall, 4).AddTile(TileID.HeavyWorkBench).AddCondition(Condition.DownedSkeletron).DisableDecraft().Register();
				Recipe.Create(brickSet.brick).AddIngredient(brickSet.slabWall, 4).AddTile(TileID.HeavyWorkBench).AddCondition(Condition.DownedSkeletron).DisableDecraft().Register();

				Recipe.Create(brickSet.wall).AddIngredient(brickSet.wallSafe).AddTile(TileID.HeavyWorkBench).AddCondition(Condition.DownedSkeletron).DisableDecraft().Register();
				Recipe.Create(brickSet.tiledWall).AddIngredient(brickSet.tiledWallSafe).AddTile(TileID.HeavyWorkBench).AddCondition(Condition.DownedSkeletron).DisableDecraft().Register();
				Recipe.Create(brickSet.slabWall).AddIngredient(brickSet.slabWallSafe).AddTile(TileID.HeavyWorkBench).AddCondition(Condition.DownedSkeletron).DisableDecraft().Register();
			}

			int[] templeTraps = new int[] {
				ItemID.SpikyBallTrap,
				ItemID.SpearTrap,
				ItemID.SuperDartTrap,
				ItemID.FlameTrap,
			};

			Recipe.Create(ItemID.WoodenSpike, 10).AddTile(TileID.HeavyWorkBench).AddIngredient(ItemID.RichMahogany, 10).AddIngredient(ItemID.Vine).AddIngredient(ItemID.VialofVenom, 1).Register();
			Recipe.Create(ItemID.DartTrap).AddTile(TileID.HeavyWorkBench).AddIngredient(ItemID.StoneBlock, 100).Register();
			Recipe.Create(ItemID.GeyserTrap).AddTile(TileID.HeavyWorkBench).AddIngredient(ItemID.StoneBlock, 100).Register();
			Recipe.Create(ItemID.Spike).AddTile(TileID.HeavyWorkBench).AddIngredient(ItemID.StoneBlock, 10).Register();

			Recipe.Create(ItemID.Extractinator).AddTile(TileID.Anvils).AddIngredient(ModContent.ItemType<WoodAutoExtractinator>()).AddRecipeGroup($"{AndroMod.ModName}:{AndroModSystem.AnyIronBar}", 20).Register();
			Recipe.Create(ItemID.ChlorophyteExtractinator).AddTile(TileID.MythrilAnvil).AddIngredient(ModContent.ItemType<HellstoneAutoExtractinator>()).AddIngredient(ItemID.ChlorophyteBar, 20).Register();
		}
		public static bool IsActivatableStatue(Item item) => !item.NullOrAir() && item.createTile == TileID.Statues || item.createTile == TileID.MushroomStatue || item.createTile == TileID.BoulderStatue;
		public static void SwitchDisabledRecipes() {
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
			ReflectionHelper.CallNonPublicStaticMethod(typeof(Recipe), "UpdateMaterialFieldForAllRecipes");
			ReflectionHelper.CallNonPublicStaticMethod(typeof(Recipe), "CreateRequiredItemQuickLookups");
			ShimmerTransforms.UpdateRecipeSets();
		}
		public override void PostAddRecipes() {
			GlobalHammer.PostSetupRecipes();
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
			if (!ES_WorldGen.SkyblockWorld)
				return;

			if (PreUpdateWorldActions != null) {
				PreUpdateWorldActions();
				PreUpdateWorldActions = null;
			}
		}
		public override void PostUpdateEverything() {
			if (!ES_WorldGen.SkyblockWorld)
				return;

			ES_Liquid.Update();
			ES_Weather.Update();
			ES_GlobalWall.Update();
			SpawnManager.Update();
			AutoFisherTE.UpdateAll();
		}
		public override void Load() {
			On_WorldFile.SaveWorld += On_WorldFile_SaveWorld;
		}

		private void On_WorldFile_SaveWorld(On_WorldFile.orig_SaveWorld orig) {
			ES_Liquid.PreSaveWorld();
			orig();
		}
	}
}
