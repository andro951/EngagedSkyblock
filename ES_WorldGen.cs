using EngagedSkyblock.Utility;
using log4net;
using MonoMod.RuntimeDetour;
using ReLogic.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Text;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using Terraria.WorldBuilding;
using static tModPorter.ProgressUpdate;

namespace EngagedSkyblock {
	public static class ES_WorldGen {

		public static bool SkyblockWorld { get; private set; } = false;
		public static bool testingInNormalWorld => true && Debugger.IsAttached;
		public const string SkyblockSeedString = "skyblock";
		public static int SkyblockSeed {
			get {
				if (skyblockSeed == null)
					skyblockSeed = CalculateSeed(SkyblockSeedString);

				return skyblockSeed.Value;
			}
		}
		private static int? skyblockSeed = null;

		public const string ForTheWorthySeedString = "fortheworthyskyblock";
		public static int ForTheWorthySeed {
			get {
				if (forTheWorthySeed == null)
					forTheWorthySeed = CalculateSeed(ForTheWorthySeedString);

				return forTheWorthySeed.Value;
			}
		}
		private static int? forTheWorthySeed = null;
		private static int CalculateSeed(string seedString) {
			int seed = Crc32.Calculate(seedString);
			seed = seed == int.MinValue ? int.MaxValue : Math.Abs(seed);
			return seed;
		}
		public static bool IsSkyblockSeed(string seed) {
			return seed == SkyblockSeedString || seed == ForTheWorthySeedString;
		}
		public static bool IsSkyblockSeed(int seed) {
			return seed == SkyblockSeed || seed == ForTheWorthySeed;
		}
		private static List<Hook> hooks = new();
		public static void Load() {
			hooks.Add(new(ModLoaderModSystemModifyWorldGenTasks, ModSystem_ModifyWorldGenTasks_Detour));
			foreach (Hook hook in hooks) {
				hook.Apply();
			}

			On_UIWorldCreation.OnFinishedSettingSeed += On_UIWorldCreation_OnFinishedSettingSeed;
			On_UIWorldCreation.ProcessSpecialWorldSeeds += On_UIWorldCreation_ProcessSpecialWorldSeeds;
			On_WorldGen.UpdateWorld_Inner += On_WorldGen_UpdateWorld_Inner;
			MoveSpawnPass.AddPass();
			ClearEverythingPass.AddPass();
			SkyblockPass.AddPass();
		}

		private static void On_WorldGen_UpdateWorld_Inner(On_WorldGen.orig_UpdateWorld_Inner orig) {
			orig();
			if (!SkyblockWorld)
				return;

			for (int i = 0; i < Main.player.Length; i++) {
				Player player = Main.player[i];
				if (!player.active || player.dead || player.ghost)
					continue;

				if (player.TryGetModPlayer(out ES_ModPlayer eS_ModPlayer))
					eS_ModPlayer.SpreadGrassAndGrowTrees();
			}
		}

		private delegate void orig_ModSystem_ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight);
		private delegate void hook_ModSystem_ModifyWorldGenTasks(orig_ModSystem_ModifyWorldGenTasks orig, List<GenPass> tasks, ref double totalWeight);
		private static readonly MethodInfo ModLoaderModSystemModifyWorldGenTasks = typeof(SystemLoader).GetMethod("ModifyWorldGenTasks", BindingFlags.Public | BindingFlags.Static);
		private static void ModSystem_ModifyWorldGenTasks_Detour(orig_ModSystem_ModifyWorldGenTasks orig, List<GenPass> tasks, ref double totalWeight) {
			orig(tasks, ref totalWeight);
			ModifyWorldGenTasks(tasks, ref totalWeight);
		}
		private static void On_UIWorldCreation_ProcessSpecialWorldSeeds(On_UIWorldCreation.orig_ProcessSpecialWorldSeeds orig, string processedSeed) {
			if (processedSeed == ForTheWorthySeedString) {
				orig("fortheworthy");
			}
			else {
				orig(processedSeed);
			}
		}

		internal static Dictionary<string, GenPass> skyblockGenPasses = new();

		private static void On_UIWorldCreation_OnFinishedSettingSeed(On_UIWorldCreation.orig_OnFinishedSettingSeed orig, UIWorldCreation self, string seed) {
			if (seed.ToLower() == SkyblockSeedString)
				seed = SkyblockSeedString;

			if (seed.ToLower().Replace(" ", "") == ForTheWorthySeedString)
				seed = ForTheWorthySeedString;

			orig(self, seed);
		}

		internal static void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight) {
			if (IsSkyblockSeed(WorldGen.currentWorldSeed)) {
				Action postActions = null;
				bool spawnPointFound = false;
				bool insertedSpawnPoint = false;
				bool passedGrassWall = false;
				for (int i = 0; i < tasks.Count; i++) {
					GenPass pass = tasks[i];
					
					if (!insertedSpawnPoint) {
						if (!spawnPointFound && pass.Name == "Spawn Point") {
							if (true || passedGrassWall) {
								tasks.Insert(++i, new MoveSpawnPass());
								insertedSpawnPoint = true;
							}

							spawnPointFound = true;
						}

						if (!passedGrassWall && pass.Name == "Grass Wall") {
							if (spawnPointFound) {
								tasks.Insert(i++, new MoveSpawnPass());
								insertedSpawnPoint = true;
							}

							passedGrassWall = true;
						}
					}
				}

				foreach (var pass in skyblockGenPasses) {
					tasks.Add(pass.Value);
				}

				postActions?.Invoke();
				totalWeight = tasks.Sum(x => x.Weight);
			}
		}

		internal static void OnWorldLoad() {
			SkyblockWorld = IsSkyblockSeed(WorldGen.currentWorldSeed) || testingInNormalWorld;
		}
		public class MoveSpawnPass : GenPass {
			public MoveSpawnPass() : base("Move Spawn to Skyblock Spawn", 1) {}

			protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration) {
				progress.Message = "Moving Spawn to Skyblock Spawn";
				Main.spawnTileX = Main.maxTilesX / 2 - 5;
				Main.spawnTileY = (int)Main.worldSurface - 55;
			}
			internal static void AddPass() {
				MoveSpawnPass pass = new();
				skyblockGenPasses.Add(pass.Name, pass);
			}
		}
		public class ClearEverythingPass : GenPass {
			public ClearEverythingPass() : base("Clear Everything", 10) {
				 
			}
			internal static void AddPass() {
				ClearEverythingPass pass = new();
				skyblockGenPasses.Add(pass.Name, pass);
			}
			protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration) {
				progress.Message = "Voiding The World";
				for (int x = 0; x < Main.maxTilesX; x++) {
					for (int y = 0; y < Main.maxTilesY; y++) {
						Tile tile = Main.tile[x, y];
						tile.ClearEverything();
					}
				}
			}
		}
		public class SkyblockPass : GenPass {
			public SkyblockPass() : base ("Skyblock", 5) {}

			internal static void AddPass() {
				SkyblockPass pass = new();
				skyblockGenPasses.Add(pass.Name, pass);
			}
			private static int RandomNext(int min, int max) => GenBase._random.Next(min, max + 1);
			protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration) {
				progress.Message = "Blocking the Sky";
				CreateSkyblock();
			}
			public static void CreateSkyblock(int? SkyblockX = null, int? SkyblockY = null) {
				int islandSurfaceHeight = SkyblockY ?? Main.spawnTileY + 1;
				int islandCenterX = SkyblockX ?? Main.spawnTileX;
				int totalBlocks = RandomNext(21, 48);
				int minSizeX = 7;
				int sizeX = RandomNext(minSizeX, 12);
				int sizeY = totalBlocks.CeilingDivide(sizeX);
				totalBlocks = sizeX * sizeY;
				int dirtBlocks = RandomNext(15, totalBlocks - 5);
				int dirtCount = dirtBlocks;
				int islandLeft = islandCenterX - sizeX / 2;
				int islandRight = islandLeft + sizeX - 1;
				int yLayer = islandSurfaceHeight;
				int minTopCut = 1;
				int maxTopCut = 2;
				int leftTopCut;
				int rightTopCut;
				if (sizeX == minSizeX + 1) {
					bool left = RandomNext(0, 1) == 0;
					if (left) {
						leftTopCut = sizeX > minSizeX ? RandomNext(minTopCut, maxTopCut) : minTopCut;
						rightTopCut = leftTopCut == minTopCut ? RandomNext(minTopCut, maxTopCut) : minTopCut;
					}
					else {
						rightTopCut = sizeX > minSizeX ? RandomNext(minTopCut, maxTopCut) : minTopCut;
						leftTopCut = rightTopCut == minTopCut ? RandomNext(minTopCut, maxTopCut) : minTopCut;
					}
				}
				else {
					leftTopCut = sizeX > minSizeX ? RandomNext(minTopCut, maxTopCut) : minTopCut;
					rightTopCut = sizeX > minSizeX ? RandomNext(minTopCut, maxTopCut) : minTopCut;
				}

				//Top layer
				int islandSurfaceLayerLeft = islandLeft + leftTopCut;
				int islandSurfaceLayerRight = islandRight - rightTopCut;
				for (int x = islandSurfaceLayerLeft; x <= islandSurfaceLayerRight; x++) {
					dirtCount--;
					WorldGen.PlaceTile(x, yLayer, TileID.Dirt, true, true);
					WorldGen.PlaceTile(x, yLayer, TileID.Grass, true, true);
				}

				yLayer++;

				if (leftTopCut > 1)
					leftTopCut--;

				if (rightTopCut > 1)
					rightTopCut--;

				leftTopCut = leftTopCut > 0 ? RandomNext(0, leftTopCut) : 0;
				rightTopCut = rightTopCut > 0 ? RandomNext(0, rightTopCut) : 0;

				//2nd layer
				int xStart = islandLeft + leftTopCut;
				int xEnd = islandRight - rightTopCut;
				for (int x = xStart; x <= xEnd; x++) {
					WorldGen.PlaceTile(x, yLayer, TileID.Dirt, true, true);
					if (!Main.tile[x, yLayer - 1].HasTile)
						WorldGen.PlaceTile(x, yLayer, TileID.Grass, true, true);

					dirtCount--;
				}

				yLayer++;

				//3rd layer
				for (int x = islandLeft; x <= islandRight; x++) {
					WorldGen.PlaceTile(x, yLayer, TileID.Dirt, true, true);
					if (!Main.tile[x, yLayer - 1].HasTile)
						WorldGen.PlaceTile(x, yLayer, TileID.Grass, true, true);

					dirtCount--;
				}

				//Remaining dirt layers
				while (dirtCount > 0) {

					yLayer++;
					for (int x = islandLeft; x <= islandRight; x++) {
						WorldGen.PlaceTile(x, yLayer, TileID.Dirt, true, true);
						if (!Main.tile[x, yLayer - 1].HasTile)
							WorldGen.PlaceTile(x, yLayer, TileID.Grass, true, true);

						dirtCount--;
					}
				}

				//Replace dirt with stone
				while (dirtCount < 0) {
					int stoneX;
					int stoneLeft = 0;
					int stoneRight = sizeX % 2 == 0 ? 1 : 0;
					for(;;) {
						stoneX = RandomNext(stoneLeft - 1, stoneRight + 1);
						bool doAgain = stoneX < stoneLeft || stoneX > stoneRight || Main.tile[stoneX + islandCenterX, islandSurfaceHeight + 1].HasTile && Main.tile[stoneX + islandCenterX, islandSurfaceHeight + 1].TileType == TileID.Stone;
						if (!doAgain)
							break;

						if (stoneLeft + islandCenterX - 1 > islandLeft)
							stoneLeft--;

						if (stoneRight + islandCenterX + 1 < islandRight)
							stoneRight++;

					}

					int stoneY = yLayer;
					while (Main.tile[stoneX + islandCenterX, stoneY].HasTile && Main.tile[stoneX + islandCenterX, stoneY].TileType == TileID.Stone) {
						stoneY--;
					}

					WorldGen.PlaceTile(stoneX + islandCenterX, stoneY, TileID.Stone, true, true);
					dirtCount++;
				}

				yLayer++;

				//1st stone layer
				for (int x = islandLeft; x <= islandRight; x++) {
					WorldGen.PlaceTile(x, yLayer, TileID.Stone, true, true);
				}

				yLayer++;

				//2nd stone layer
				int leftCut = RandomNext(0, (islandCenterX - islandLeft).CeilingDivide(2));
				int rightCut = RandomNext(0, (islandRight - islandCenterX).CeilingDivide(2));
				xStart = islandLeft + leftCut;
				xEnd = islandRight - rightCut;
				for (int x = xStart; x <= xEnd; x++) {
					WorldGen.PlaceTile(x, yLayer, TileID.Stone, true, true);
				}

				yLayer++;

				//Last stone layer
				leftCut = RandomNext(leftCut + 1, islandCenterX - islandLeft);
				rightCut = RandomNext(rightCut + 1, islandRight - islandCenterX);
				xStart = islandLeft + leftCut;
				xEnd = islandRight - rightCut;
				for (int x = xStart; x <= xEnd; x++) {
					WorldGen.PlaceTile(x, yLayer, TileID.Stone, true, true);
				}

				//Chest
				for (int x = islandSurfaceLayerLeft; x <= islandSurfaceLayerRight; x++) {
					int chestNum = WorldGen.PlaceChest(x, islandSurfaceHeight - 1);
					if (chestNum < 0)
						continue;

					Item[] inv = Main.chest[chestNum].item;
					int index = 0;
					inv[index++] = new(ItemID.Acorn, 10);
					inv[index++] = new(ItemID.WaterBucket);
					inv[index++] = new(ItemID.LavaBucket);
					break;
				}

				//Tree
				for (int y = islandSurfaceHeight - 2; y <= islandSurfaceHeight + 4; y++) {
					for (int x = islandSurfaceLayerRight; x >= islandSurfaceLayerLeft; x--) {
						//Try grow a tree
						if (WorldGen.GrowEpicTree(x, y))
							break;
					}
				}
			}
			public static void CreateSkyblockOld(int? SkyblockX = null, int? SkyblockY = null) {
				int islandSurfaceHeight = SkyblockY ?? Main.spawnTileY + 1;
				int islandCenterX = SkyblockX ?? Main.spawnTileX;
				int minBlocks = 20;
				int maxBlocks = 40;
				int totalDirt = RandomNext(15, 30);
				int totalStone = RandomNext(minBlocks - totalDirt, maxBlocks - totalDirt);
				int totalBlocks = totalDirt + totalStone;
				int minSizeX = 7;
				int maxSizeX = 10;
				int islandSizeX = RandomNext(minSizeX, maxSizeX);
				int minSizeY = totalBlocks.CeilingDivide(islandSizeX);
				int islandSizeY = RandomNext(minSizeY, minSizeY + 2);
				int dirtCount = totalDirt;
				int stoneCount = totalStone;
				int blockCount = dirtCount + stoneCount;
				int emptySpaceToFill = islandSizeX * islandSizeY - blockCount;
				int islandLeft = islandCenterX - islandSizeX / 2;
				int islandRight = islandLeft + islandSizeX - 1;
				int yLayer = islandSurfaceHeight;
				int minTopCut = 1;
				int maxTopCut = 2;
				int leftTopCut = emptySpaceToFill > 0 && islandSizeX > minSizeX ? RandomNext(minTopCut, maxTopCut) : minTopCut;
				emptySpaceToFill -= leftTopCut - minTopCut;
				int rightTopCut = emptySpaceToFill > 0 && islandSizeX > minSizeX ? RandomNext(minTopCut, maxTopCut) : minTopCut;
				emptySpaceToFill -= rightTopCut - 1;
				int islandSurfaceLayerLeft = islandLeft + leftTopCut;
				int islandSurfaceLayerRight = islandRight - rightTopCut;
				for (int x = islandSurfaceLayerLeft; x <= islandSurfaceLayerRight && blockCount > 0; x++) {
					ushort tileType = dirtCount / 2 > x - islandSurfaceLayerLeft || dirtCount > islandSurfaceLayerRight - x ? TileID.Dirt : TileID.Stone;
					blockCount--;
					if (tileType == TileID.Dirt) {
						dirtCount--;
						WorldGen.PlaceTile(x, yLayer, tileType, true, true);
						tileType = TileID.Grass;
					}
					else {
						stoneCount--;
					}

					WorldGen.PlaceTile(x, yLayer, tileType, true, true);
				}

				yLayer++;

				int yMiddleLayer = islandSurfaceHeight + islandSizeY / 2;
				if (leftTopCut > 1)
					leftTopCut--;

				if (rightTopCut > 1)
					rightTopCut--;

				for (; yLayer <= yMiddleLayer; yLayer++) {
					leftTopCut = leftTopCut > 0 && emptySpaceToFill > 0 && islandSizeX > minSizeX ? RandomNext(0, leftTopCut) : 0;
					emptySpaceToFill -= leftTopCut;
					rightTopCut = rightTopCut > 0 && emptySpaceToFill > 0 && islandSizeX > minSizeX ? RandomNext(0, rightTopCut) : 0;
					emptySpaceToFill -= rightTopCut;

					int xStart = islandLeft + leftTopCut;
					int xEnd = islandRight - rightTopCut;
					for (int x = xStart; x <= xEnd && blockCount > 0; x++) {
						ushort tileType = dirtCount / 2 > x - xStart || dirtCount > xEnd - x ? TileID.Dirt : TileID.Stone;
						blockCount--;
						if (tileType == TileID.Dirt) {
							if (!Main.tile[x, yLayer - 1].HasTile) {
								WorldGen.PlaceTile(x, yLayer, tileType, true, true);
								tileType = TileID.Grass;
							}

							dirtCount--;
						}
						else {
							stoneCount--;
						}

						WorldGen.PlaceTile(x, yLayer, tileType, true, true);
					}
				}

				int startBottomYlayer = yLayer;
				int yBottomLayer = islandSurfaceHeight + islandSizeY - 1;
				int bottomLayersCount = yBottomLayer - startBottomYlayer + 1;
				int emptyToFillBefore = emptySpaceToFill;
				for (; yLayer <= yBottomLayer; yLayer++) {
					int layersLeft = yBottomLayer - yLayer;
					//int cut = (bottomLayersCount - layersLeft) * (islandSizeX / 2 - 1) / bottomLayersCount;
					int cut = (bottomLayersCount - layersLeft) * emptyToFillBefore / bottomLayersCount / 2;
					int leftLowerCut = layersLeft > 0 ? cut : emptySpaceToFill / 2;
					int rightLowerCut = layersLeft > 0 ? cut : emptySpaceToFill - leftLowerCut;
					emptySpaceToFill -= leftLowerCut + rightLowerCut;
					int xStart = islandLeft + leftLowerCut;
					int xEnd = islandRight - rightLowerCut;
					for (int x = xStart; x <= xEnd && blockCount > 0; x++) {
						ushort tileType = dirtCount > 0 && (dirtCount / 2 > x - xStart || dirtCount > xEnd - x) ? TileID.Dirt : TileID.Stone;
						blockCount--;
						if (tileType == TileID.Dirt) {
							dirtCount--;
						}
						else {
							stoneCount--;
						}

						WorldGen.PlaceTile(x, yLayer, tileType, true, true);
					}
				}

				for (int x = islandSurfaceLayerLeft; x <= islandSurfaceLayerRight; x++) {
					int chestNum = WorldGen.PlaceChest(x, islandSurfaceHeight - 1);
					if (chestNum < 0)
						continue;

					Item[] inv = Main.chest[chestNum].item;
					int index = 0;
					//inv[index++].SetDefaults(ItemID.Acorn);
					inv[index++].SetDefaults(ItemID.WaterBucket);
					inv[index++].SetDefaults(ItemID.LavaBucket);
					break;
				}

				for (int y = islandSurfaceHeight - 2; y <= islandSurfaceHeight + 4; y++) {
					for (int x = islandSurfaceLayerRight; x >= islandSurfaceLayerLeft; x--) {
						//Try grow a tree
						if (WorldGen.GrowEpicTree(x, y))
							break;
					}
				}
			}
		}
	}
}
