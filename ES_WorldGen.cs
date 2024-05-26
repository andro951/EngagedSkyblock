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
using androLib.Common.Utility;
using KokoLib;
using System.Threading;
using static EngagedSkyblock.EngagedSkyblock;
using EngagedSkyblock.Common.Globals;
using Microsoft.Xna.Framework;

namespace EngagedSkyblock {
	public static class ES_WorldGen {
		public static void Load() {
			On_UIWorldCreation.OnFinishedSettingSeed += On_UIWorldCreation_OnFinishedSettingSeed;
			On_UIWorldCreation.ProcessSpecialWorldSeeds += On_UIWorldCreation_ProcessSpecialWorldSeeds;
			On_WorldGen.UpdateWorld_Inner += On_WorldGen_UpdateWorld_Inner;
			MoveSpawnPass.AddPass();
			ClearEverythingPass.AddPass();
			SkyblockPass.AddPass();
		}

		#region Seeds

		public static bool SkyblockWorld => skyblockWorld || testingInNormalWorld;
		private static bool skyblockWorld = true;
		public static void SetSkyblockWorld(bool value) => skyblockWorld = value;
		public static bool CheckSkyblockSeed() => IsSkyblockSeed(Main.netMode == NetmodeID.Server ? Main.ActiveWorldFileData.SeedText : WorldGen.currentWorldSeed);
		public static bool testingInNormalWorld => false && Debugger.IsAttached;
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
		private static void GetWorldSeed() {
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				bool gotData = Main.ActiveWorldFileData.TryGetHeaderData<ES_ModSystem>(out TagCompound data);
				if (gotData)
					skyblockWorld = data.GetBool(ES_ModSystem.skyblockWorldKey);

				if (!skyblockWorld) {
					if (CheckSkyblockSeed()) {
						skyblockWorld = true;
					}
					else if (!gotData) {
						$"Failed to get header data.  Unable to determine if the world is a skyblock or not. Main.netmode: {Main.netMode}".LogSimple();
					}
				}
			}
		}
		public static void RequestSeedFromServer() {
			ModPacket modPacket = EngagedSkyblock.Instance.GetPacket();
			modPacket.Write((byte)ModPacketID.RequestWorldSeedFromClient);
			modPacket.Write(Main.myPlayer);
			modPacket.Send();
		}
		internal static void RecieveWorldSeed(string seed) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				WorldGen.currentWorldSeed = seed;
				Main.ActiveWorldFileData.SetSeed(seed);
				SetSkyblockWorld(CheckSkyblockSeed());
				PostSeedSetup();
				Main.NewText($"Recieved World Seed: {seed}");
			}
			else {
				throw new Exception($"RecieveWorldSeed called.  Main.netMode: {Main.netMode}");
			}
		}

		#endregion

		#region World Creation

		public delegate void orig_ModSystem_PostWorldGen();
		public delegate void hook_ModSystem_PostWordGen(orig_ModSystem_PostWorldGen orig);
		public static readonly MethodInfo ModLoaderModSystemPostWorldGen = typeof(SystemLoader).GetMethod("PostWorldGen", BindingFlags.Public | BindingFlags.Static);
		public static void ModSystem_PostWorldGen_Detour(orig_ModSystem_PostWorldGen orig) {
			orig();
			PostWorldGenDetour();
		}
		private static void PostWorldGenDetour() {
			if (!SkyblockWorld)
				return;

			//If spawn point changed, another mod probably did something in PostWorldGen(), so recreate the skyblock.
			if (MoveSpawnPass.SpawnChanged()) {
				foreach (var pass in skyblockGenPasses) {
					pass.Value.Apply(null, null);
				}
			}
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
					eS_ModPlayer.DoPlayerMovementTileGrowth();
			}
		}

		public delegate void orig_ModSystem_ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight);
		public delegate void hook_ModSystem_ModifyWorldGenTasks(orig_ModSystem_ModifyWorldGenTasks orig, List<GenPass> tasks, ref double totalWeight);
		public static readonly MethodInfo ModLoaderModSystemModifyWorldGenTasks = typeof(SystemLoader).GetMethod("ModifyWorldGenTasks", BindingFlags.Public | BindingFlags.Static);
		public static void ModSystem_ModifyWorldGenTasks_Detour(orig_ModSystem_ModifyWorldGenTasks orig, List<GenPass> tasks, ref double totalWeight) {
			orig(tasks, ref totalWeight);
			ModifyWorldGenTasks(tasks, ref totalWeight);
		}
		private static void On_UIWorldCreation_ProcessSpecialWorldSeeds(On_UIWorldCreation.orig_ProcessSpecialWorldSeeds orig, string processedSeed) {
			CheckUpdateSeed(ref processedSeed);

			Main.ActiveWorldFileData.SetSeed(processedSeed);
			SetSkyblockWorld(CheckSkyblockSeed());
			if (processedSeed == ForTheWorthySeedString) {
				orig("fortheworthy");
			}
			else {
				orig(processedSeed);
			}
		}

		internal static Dictionary<string, GenPass> skyblockGenPasses = new();

		private static void On_UIWorldCreation_OnFinishedSettingSeed(On_UIWorldCreation.orig_OnFinishedSettingSeed orig, UIWorldCreation self, string seed) {
			CheckUpdateSeed(ref seed);

			orig(self, seed);
		}
		private static void CheckUpdateSeed(ref string seed) {
			if (seed.ToLower() == SkyblockSeedString)
				seed = SkyblockSeedString;

			if (seed.ToLower().Replace(" ", "") == ForTheWorthySeedString)
				seed = ForTheWorthySeedString;
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

				postActions?.Invoke();
				totalWeight = tasks.Sum(x => x.Weight);
			}
		}
		public class MoveSpawnPass : GenPass {
			public MoveSpawnPass() : base("Move Spawn to Skyblock Spawn", 1) { }
			private static int spawnX;
			private static int spawnY;
			public static bool SpawnChanged() => spawnX != Main.spawnTileX || spawnY != Main.spawnTileY;
			protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration) {
				if (progress != null)
					progress.Message = "Moving Spawn to Skyblock Spawn";

				Main.spawnTileX = Main.maxTilesX / 2 - 5;
				Main.spawnTileY = (int)Main.worldSurface - 55;

				Vector2 npcSapwn = new Point(Main.spawnTileX, Main.spawnTileY - 2).ToWorldCoordinates();
				foreach (NPC npc in Main.npc) {
					if (npc.active && npc.townNPC) {
						npc.position = npcSapwn;
						if (Main.getGoodWorld && npc.netID != NPCID.Demolitionist || !Main.getGoodWorld && npc.netID != NPCID.Guide) {
							npc.active = false;
						}
					}
				}
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
				if (progress != null)
					progress.Message = "Voiding The World";

				for (int x = 0; x < Main.maxTilesX; x++) {
					for (int y = 0; y < Main.maxTilesY; y++) {
						Tile tile = Main.tile[x, y];
						tile.ClearEverything();
					}
				}

				for (int i = 0; i < Main.chest.Length; i++) {
					Main.chest[i] = null;
				}
			}
		}
		public class SkyblockPass : GenPass {
			public SkyblockPass() : base("Skyblock", 5) { }

			internal static void AddPass() {
				SkyblockPass pass = new();
				skyblockGenPasses.Add(pass.Name, pass);
			}
			private static int RandomNext(int min, int max) => GenBase._random.Next(min, max + 1);
			protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration) {
				if (progress != null)
					progress.Message = "Blocking the Sky";

				CreateSkyblock();
			}
			public static void CreateSkyblock(int? SkyblockX = null, int? SkyblockY = null) {
				int islandSurfaceHeight = SkyblockY ?? Main.spawnTileY + 1;
				int islandCenterX = SkyblockX ?? Main.spawnTileX;
				int totalBlocks = RandomNext(35, 48);
				int minSizeX = 7;
				int sizeX = RandomNext(minSizeX, 12);
				int sizeY = totalBlocks.CeilingDivide(sizeX);
				totalBlocks = sizeX * sizeY;
				int dirtBlocks = RandomNext(30, totalBlocks - 5);
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
					for (; ; ) {
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
		}

		#endregion

		internal static void OnWorldLoad() {
			GetWorldSeed();
			if (Main.netMode != NetmodeID.MultiplayerClient)
				PostSeedSetup();
		}
		private static void PostSeedSetup() {
			ES_ModSystem.SwitchDisabledRecipes();
			ES_GlobalTile.OnWorldLoad();
			GlobalHammer.UpdateHammersAllowRepeatedRightclick();
			WallOfFleshGlobal.OnWorldLoad();
		}

		public static void GenerateTestingDesert(int x, int y) {
			int sizeX = 25;
			int sizeY = 15;
			int xMin = sizeX + 2;
			x = Math.Max(x, xMin);
			int xMax = Main.maxTilesX - xMin;
			x = Math.Min(x, xMax);
			int yMin = sizeY + 4;
			y = Math.Max(y, yMin);
			int yMax = Main.maxTilesY - yMin;
			y = Math.Min(y, yMax);
			for (int i = x - sizeX; i < x + sizeX; i++) {
				for (int j = y + sizeY - 2; j >= y - sizeY - 1; j--) {
					WorldGen.PlaceTile(i, j, TileID.Sand, true, true);
				}

				WorldGen.PlaceTile(i, y + sizeY - 1, TileID.Sandstone, true, true);
			}
		}
		public static void TrySpawnChlorophyteKilldedWallOfFlesh() {
			List<Point> mud = new();
			List<Point> jungleGrass = new();
			bool anyMudFound = false;
			for (int y = 0; y < Main.maxTilesY; y++) {
				for (int x = 0; x < Main.maxTilesX; x++) {
					Tile tile = Main.tile[x, y];
					if (tile.HasTile) {
						int type = tile.TileType;
						if (type == TileID.Mud) {
							mud.Add(new Point(x, y));
							anyMudFound = true;
						}
						else if (!anyMudFound && type == TileID.JungleGrass) {
							jungleGrass.Add(new Point(x, y));
						}
					}
				}
			}

			Point chosenPoint = new Point(-1, -1);
			if (anyMudFound) {
				for (int requiredAdjacent = 4; requiredAdjacent >= 0; requiredAdjacent--) {
					int attempts = 100;
					//Try to find a suitable mud block surrounded by all mud, jungle grass or clay tiles.
					for (int i = 0; i < attempts; i++) {
						Point point = mud[Main.rand.Next(mud.Count)];
						if (CheckSuitableForChlorophyteSpawn(point, requiredAdjacent)) {
							chosenPoint = point;
							goto PlaceTile;
						}
					}
				}
			}
			else {
				if (jungleGrass.Count <= 0)
					return;

				chosenPoint = jungleGrass[Main.rand.Next(jungleGrass.Count)];
			}

			if (chosenPoint.X < 0 || chosenPoint.Y < 0)
				return;

			PlaceTile:
			ES_GlobalTile.PlaceTile(chosenPoint.X, chosenPoint.Y, TileID.Chlorophyte, false);
		}
		private static bool CheckSuitableForChlorophyteSpawn(Point point, int requiredAdjacent) {
			int adjacent = 0;
			for (int directionID = 0; directionID < 4; directionID++) {
				PathDirectionID.GetDirection(directionID, out int x, out int y);

				Tile tile = Main.tile[point.X + x, point.Y + y];
				if (!tile.HasTile || tile.TileType != TileID.Mud && tile.TileType != TileID.JungleGrass && tile.TileType != TileID.ClayBlock) {
					if (3 - directionID < requiredAdjacent - adjacent)
						return false;

					break;
				}
				else {
					adjacent++;
					if (adjacent >= requiredAdjacent)
						return true;
				}
			}

			return false;
		}
	}
}
