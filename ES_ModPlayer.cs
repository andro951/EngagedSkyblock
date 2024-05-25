using androLib.Common.Utility;
using EngagedSkyblock.Common.Globals;
using EngagedSkyblock.Content.Dusts;
using EngagedSkyblock.Items;
using EngagedSkyblock.Weather;
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
using Terraria.UI;

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

				if (Keys.NumPad2.Clicked()) {
					Point cursor = Main.MouseWorld.ToTileCoordinates();
					ES_WorldGen.GenerateTestingDesert(cursor.X, cursor.Y);
				}

				if (Keys.NumPad3.Clicked()) {
					Point cursor = Main.MouseWorld.ToTileCoordinates();
					Tile left = Main.tile[cursor.X - 1, cursor.Y];
					Tile leftUp = Main.tile[cursor.X - 1, cursor.Y - 1];
					if (!left.HasTile && !leftUp.HasTile) {
						//WorldGen.PlaceTile(cursor.X, cursor.Y, TileID.LargePiles, true, true, style: 24);
						ES_GlobalWall.PlaceLargePile(cursor.X, cursor.Y, 24);
					}
					else {
						//WorldGen.PlaceSmallPile(cursor.X, cursor.Y, 31, 1);
						//ES_GlobalWall.PlaceSmallPile(cursor.X, cursor.Y, 32, true);
					}
					//Main.tile[cursor.X, cursor.Y].TileFrameX = 54 * 2;
					//Main.tile[cursor.X, cursor.Y].TileFrameY = 36 * 7;
				}
			}
		}

		public void DoPlayerMovementTileGrowth() {
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
			Point playerCenterTile = Player.Center.ToTileCoordinates();
			List<Point> surfaceBlocks = GetTilesOfType(playerCenterTile, growthRadius, BlockCanGrow);
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
			bool canGrow = tile.HasTile && y != 0 && !Main.tile[x, y - 1].HasTile && BlocksThatCanGrow.Contains(tileType)
			//|| WallsThatCanGrow.Contains(tile.WallType)
			|| tile.WallType == WallID.SpiderUnsafe && NearbyTile(x, y, 4, (i, j) => WorldGen.SolidTile(i, j))
			|| ES_GlobalTile.MudThatCanConvertToChlorophyte(x, y, tileType)
			|| ES_GlobalTile.MudThatCanConvertToClay(x, y, tileType)
			|| ES_GlobalTile.SandThatCanHarden(x, y, tileType, out _)
			|| ES_GlobalTile.OrganicCanBecomeFossil(x, y, tileType)
			;

			return canGrow;
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
		//public static SortedSet<int> WallsThatCanGrow = new() {
		//	WallID.EbonstoneUnsafe,
		//	//WallID.SpiderUnsafe,//Covered on it's own.
		//	WallID.CorruptGrassUnsafe,
		//	WallID.HallowedGrassUnsafe,
		//	WallID.CrimsonGrassUnsafe,
		//	WallID.CrimstoneUnsafe,
		//};

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
		private static bool NearbyTile(int x, int y, float radius, Func<int, int, bool> condition = null) {
			int xStart = Math.Max(0, x - (int)radius);
			int xEnd = Math.Min(Main.maxTilesX - 1, x + (int)radius);
			int yStart = Math.Max(0, y - (int)radius);
			int yEnd = Math.Min(Main.maxTilesY - 1, y + (int)radius);
			for (int i = xStart; i <= xEnd; i++) {
				for (int j = yStart; j <= yEnd; j++) {
					if (condition(i, j))
						return true;
				}
			}

			return false;
		}
		public override void OnEnterWorld() {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				ES_WorldGen.RequestSeedFromServer();
		}
	}
}
