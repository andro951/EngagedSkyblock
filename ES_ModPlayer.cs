using androLib.Common.Utility;
using EngagedSkyblock.Common.Globals;
using EngagedSkyblock.Content.Dusts;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EngagedSkyblock {
	public class ES_ModPlayer : ModPlayer {
		private int growthRadius => 10;
		private int growthRate => 1000;
		private float speedThreshold => 2.6f;
		public override void PostUpdate() {
			if (Debugger.IsAttached) {
				if (Main.netMode == NetmodeID.Server)
					return;

				PlayerInput.Update();

				if (Keys.NumPad0.Clicked()) {
					Point cursor = Main.MouseWorld.ToTileCoordinates();
					ES_WorldGen.SkyblockPass.CreateSkyblock(cursor.X, cursor.Y);
				}

				if (Keys.NumPad1.Clicked()) {
					Item item = Player.HeldItem;
					if (!item.NullOrAir()) {
						ES_ModSystem.PrintNPCsThatDropItem(item.type);
					}
				}
			}
		}



		public void SpreadGrassAndGrowTrees() {
			if (!ES_WorldGen.SkyblockWorld)
				return;

			float speed = Player.velocity.Length();
			if (speed > speedThreshold)
				UpdateWorldTiles();
		}
		private static readonly MethodInfo updateWorld_OvergroundTileMethodInfo = typeof(WorldGen).GetMethod("UpdateWorld_OvergroundTile", BindingFlags.NonPublic | BindingFlags.Static);
		private delegate void UpdateWorld_OvergroundTileDelegate(int i, int j, bool checkNPCSpawns, int wallDist);
		UpdateWorld_OvergroundTileDelegate UpdateWorld_OvergroundTile = (UpdateWorld_OvergroundTileDelegate)Delegate.CreateDelegate(typeof(UpdateWorld_OvergroundTileDelegate), null, updateWorld_OvergroundTileMethodInfo);

		private static readonly MethodInfo updateWorld_UndergroundTileMethodInfo = typeof(WorldGen).GetMethod("UpdateWorld_UndergroundTile", BindingFlags.NonPublic | BindingFlags.Static);
		private delegate void UpdateWorld_UndergroundTileDelegate(int i, int j, bool checkNPCSpawns, int wallDist);
		UpdateWorld_UndergroundTileDelegate UpdateWorld_UndergroundTile = (UpdateWorld_UndergroundTileDelegate)Delegate.CreateDelegate(typeof(UpdateWorld_UndergroundTileDelegate), null, updateWorld_UndergroundTileMethodInfo);
		
		private void UpdateWorldTiles() {
			double worldUpdateRate = WorldGen.GetWorldUpdateRate();
			if (worldUpdateRate == 0)
				return;

			int wallDist = 3;
			double percentOfTiles = 3E-05 * worldUpdateRate * growthRate;
			double tilesCount = growthRadius * growthRadius * 4;
			double tilesToUpdateDouble = tilesCount * percentOfTiles;
			int tilesInt = (int)tilesToUpdateDouble;
			int tilesToUpdate = tilesInt + (Main.rand.NextDouble() <= tilesToUpdateDouble - (double)tilesInt ? 1 : 0);
			int num5 = 151;
			int num6 = (int)Utils.Lerp(num5, (double)num5 * 2.8, Utils.Clamp((double)Main.maxTilesX / 4200.0 - 1.0, 0.0, 1.0));
			List<Point> surfaceBlocks = GetTilesOfType(Player.Center.ToTileCoordinates(), growthRadius, BlockCanGrow);
			double pointsChance = tilesToUpdate > 0 ? (double)surfaceBlocks.Count / tilesCount : 0d;
			for (int j = 0; j < tilesToUpdate; j++) {
				if (Main.rand.Next(5) == 0) {
					GetRandomPointToUpdate(out int x, out int y);
					if (Main.rand.Next(num6 * 100) == 0)
						WorldGen.PlantAlch();

					if (y < (int)Main.worldSurface) {
						UpdateWorld_OvergroundTile(x, y, false, wallDist);
					}
					else {
						UpdateWorld_UndergroundTile(x, y, false, wallDist);
					}
				}
				else if (surfaceBlocks.Count > 0 && Main.rand.NextDouble() <= pointsChance) {
					Point rand = GetRandomFromList(surfaceBlocks);
					GrowDust(rand);
				}
			}
		}
		public static void GrowDust(Point point) {
			Dust.NewDust(point.ToWorldCoordinates(Main.rand.NextFloat(8f), Main.rand.NextFloat(8f)), 1, 1, ModContent.DustType<GrowthDust>());
		}
		private static bool BlockCanGrow(int x, int y) {
			Tile tile = Main.tile[x, y];
			int tileType = tile.TileType;
			bool surfaceBlockThatCangrow = tile.HasTile && !Main.tile[x, y -1].HasTile && (BlocksThatCanGrow.Contains(tileType) || WallsThatCanGrow.Contains(tile.WallType));

			return surfaceBlockThatCangrow || ES_GlobalTile.MudThatCanConvertToClay(x, y);
		}
		public static Point GetRandomFromList(List<Point> surfaceBlocks) {
			if (surfaceBlocks.Count == 0)
				return Point.Zero;

			return surfaceBlocks[Main.rand.Next(surfaceBlocks.Count)];
		}
		private void GetRandomPointToUpdate(out int x, out int y) {
			Point playerCenter = Player.Center.ToTileCoordinates();
			x = -1;
			int xMin = playerCenter.X - growthRadius;
			int xMax = playerCenter.X + growthRadius;
			while (x < 0 || x > Main.maxTilesX) {
				x = WorldGen.genRand.Next(xMin, xMax);
			}

			y = -1;
			int yMin = playerCenter.Y - growthRadius;
			int yMax = playerCenter.Y + growthRadius;
			while (y < 0 || y > Main.maxTilesY) {
				y = WorldGen.genRand.Next(yMin, yMax);
			}
		}
		private static SortedSet<int> BlocksThatCanGrow {
			get {
				if (blocksThatCanGrow == null) {
					blocksThatCanGrow = new() {
						TileID.Tombstones,
						TileID.Bamboo,
						TileID.ClayPot,
						TileID.Cactus,
						TileID.Chlorophyte,
						TileID.Pumpkins,
						TileID.ChlorophyteBrick,
						TileID.PlanterBox,
						TileID.RockGolemHead,
					};

					for (int i = 0; i < TileLoader.TileCount; i++) {
						if (Main.tileAlch[i]
							|| TileID.Sets.CommonSapling[i]
							|| TileID.Sets.TreeSapling[i]
							|| TileID.Sets.Conversion.Dirt[i]
							|| TileID.Sets.Conversion.Sand[i]
							|| TileID.Sets.Conversion.Grass[i]
							|| TileID.Sets.Conversion.Stone[i]
							|| TileID.Sets.IsATreeTrunk[i]
							|| TileID.Sets.Conversion.JungleGrass[i]
							|| TileID.Sets.SpreadOverground[i]
							|| Main.tileMoss[i]
							|| TileID.Sets.tileMossBrick[i]
							|| TileID.Sets.CanGrowCrystalShards[i]
							|| TileID.Sets.SpreadUnderground[i]
							|| TileID.Sets.TileCutIgnore.Regrowth[i]
							|| TileID.Sets.Corrupt[i]
							|| TileID.Sets.Hallow[i]
							|| TileID.Sets.Crimson[i]
							|| TileID.Sets.Conversion.Thorn[i]
							|| TileID.Sets.Conversion.MushroomGrass[i]
							|| TileID.Sets.Conversion.Snow[i]
							|| TileID.Sets.Conversion.Ice[i]
							|| TileID.Sets.Leaves[i]
							|| TileID.Sets.IsVine[i]
							|| TileID.Sets.Conversion.Sandstone[i]
						)
							blocksThatCanGrow.Add(i);
					}
				}

				return blocksThatCanGrow;
			}
		}
		private static SortedSet<int> blocksThatCanGrow = null;
		private static SortedSet<int> WallsThatCanGrow = new() {
			WallID.EbonstoneUnsafe,
			WallID.SpiderUnsafe,
			WallID.CorruptGrassUnsafe,
			WallID.HallowedGrassUnsafe,
			WallID.CrimsonGrassUnsafe,
			WallID.CrimstoneUnsafe,
		};
		//private void FindBlocksThatCanGrow(List<List<Point>> surfaceBlocks, out List<Point> blocksThatCanGrow) {
		//	blocksThatCanGrow = new();
		//	for (int i = 0; i < surfaceBlocks.Count; i++) {
		//		List<Point> column = surfaceBlocks[i];
		//		for (int j = 0; j < column.Count; j++) {
		//			Point point = column[j];
		//			Tile tile = Main.tile[point.X, point.Y];
		//			if (BlocksThatCanGrow.Contains(tile.TileType) || WallsThatCanGrow.Contains(tile.WallType))
		//				blocksThatCanGrow.Add(point);
		//		}
		//	}
		//}
		public static List<List<Point>> MakeCircleArray(Point center, float radius, bool excludeAir = false) {
			List<List<Point>> circleArray = new();
			for (int x = center.X - (int)radius; x <= center.X + (int)radius; x++) {
				List<Point> column = new();
				for (int y = center.Y - (int)radius; y <= center.Y + (int)radius; y++) {
					if (new Vector2(x, y).Distance(center.ToVector2()) <= radius) {
						if (excludeAir && !Main.tile[x, y].HasTile)
							continue;

						column.Add(new Point(x, y));
					}
				}

				circleArray.Add(column);
			}

			return circleArray;
		}
		public static List<Point> GetTilesOfType(Point center, float radius, Func<int, int, bool> condition = null) {
			List<Point> tiles = new();
			for (int x = center.X - (int)radius; x <= center.X + (int)radius; x++) {
				for (int y = center.Y - (int)radius; y <= center.Y + (int)radius; y++) {
					if (new Vector2(x, y).Distance(center.ToVector2()) <= radius) {
						if (condition(x, y))
							tiles.Add(new Point(x, y));
					}
				}
			}

			return tiles;
		}
		
		public Item miningTool = null;
		public bool PostBreakTileShouldDoVanillaDrop(int x, int y, int type) {
			if (miningTool.NullOrAir())
				return true;

			if (miningTool.TryGetGlobalItem(out GlobalHammer _)) {
				int dropItemType = -1;
				int stack = 1;
				switch (type) {
					case TileID.Stone:
						dropItemType = ItemID.SandBlock;
						break;
					default:
						if (TileID.Sets.IsShakeable[type]) {
							Main.NewText("Drop Wood Chips");
							//dropItemType = ModContent.ItemType<WoodChipps>();
						}

						break;
				}

				if (dropItemType >= 0) {
					int num = Item.NewItem(WorldGen.GetItemSource_FromTileBreak(x, y), x * 16, y * 16, 16, 16, dropItemType, stack, noBroadcast: false, -1);
					Main.item[num].TryCombiningIntoNearbyItems(num);
					return false;
				}
			}

			return true;
		}
	}
}
