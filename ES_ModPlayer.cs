using Microsoft.Xna.Framework;
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
		private int growthRate = 1000;
		private float growthChance => 0.05f;
		private float growthParticleChance => Math.Min(growthChance * 5f, 1f);
		private int growthCheckDelay => 10;
		private uint nextGrowthCheck = 0;
		private static bool numPad0 = false;
		public override void PostUpdate() {
			//SpreadGrassAndGrowTrees();
			if (Debugger.IsAttached) {
				bool newNumPad0 = Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.NumPad0);
				bool numPad0Clicked = newNumPad0 && !numPad0;
				numPad0 = newNumPad0;
				if (numPad0Clicked) {
					Point cursor = Main.MouseWorld.ToTileCoordinates();
					ES_WorldGen.SkyblockPass.CreateSkyblock(cursor.X, cursor.Y);
				}
			}
		}

		public void SpreadGrassAndGrowTrees(bool ignoreGrowthDelay = false) {
			if (!ES_WorldGen.SkyblockWorld)
				return;

			//if (!ignoreGrowthDelay) {
			//	uint updateCount = Main.GameUpdateCount;
			//	if (updateCount < nextGrowthCheck)
			//		return;

			//	nextGrowthCheck = updateCount + (uint)growthCheckDelay;
			//}

			float speed = Player.velocity.Length();
			if (speed > 2.6f) {
				UpdateWorldTiles();
				////Main.NewText($"Player.Center: {Player.Center}, World Coord: {Player.Center.ToTileCoordinates()}");
				//Point playerCenter = Player.Center.ToTileCoordinates();
				//SortedSet<int> tileTypes = new() { TileID.Dirt };
				//List<List<Point>> surfaceBlocks = MakeCircleArraySurfaceLayerOnly(playerCenter, growthRadius, tileTypes);
				//FindDirtBlocksAndSapplings(surfaceBlocks, out List<Point> dirtBlocks, out List<Point> sapplings);
				//foreach (Point dirtBlock in dirtBlocks) {
				//	float rand = Main.rand.NextFloat();
				//	if (rand <= growthChance) {
				//		Main.tile[dirtBlock.X, dirtBlock.Y].TileType = TileID.Grass;
				//	}
				//	else if (rand <= growthParticleChance) {
				//		Main.NewText($"Growth Particle: {dirtBlock}");
				//	}
				//}
			}
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
			double num = 3E-05 * worldUpdateRate * growthRate;
			double num4 = (double)(growthRadius * growthRadius * 4) * num;
			int num5 = 151;
			int num6 = (int)Utils.Lerp(num5, (double)num5 * 2.8, Utils.Clamp((double)Main.maxTilesX / 4200.0 - 1.0, 0.0, 1.0));
			for (int j = 0; (double)j < num4; j++) {
				if (Main.rand.Next(num6 * 100) == 0)
					WorldGen.PlantAlch();

				//WorldGen.UpdateWorld_OvergroundTile(i2, j2, false, wallDist);
				GetRandomPointToUpdate(out int x, out int y);
				if (y < (int)Main.worldSurface) {
					Main.NewText($"UpdateOvergroundTile ({x}, {y})");
					UpdateWorld_OvergroundTile(x, y, false, wallDist);
					//updateWorld_OvergroundTileMethodInfo.Invoke(null, new object[] { x, y, false, wallDist });
				}
				else {
					Main.NewText($"UpdateUndergroundTile ({x}, {y})");
					UpdateWorld_UndergroundTile(x, y, false, wallDist);
					//updateWorld_UndergroundTileMethodInfo.Invoke(null, new object[] { x, y, false, wallDist });
				}
			}
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
		private void FindDirtBlocksAndSapplings(List<List<Point>> surfaceBlocks, out List<Point> dirtBlocks, out List<Point> sapplings) {
			dirtBlocks = new();
			sapplings = new();
			for (int i = 0; i < surfaceBlocks.Count; i++) {
				List<Point> column = surfaceBlocks[i];
				for (int j = 0; j < column.Count; j++) {
					Point point = column[j];
					Tile tile = Main.tile[point.X, point.Y];
					int tileType = tile.TileType;
					if (tileType == TileID.Dirt) {
						bool found = false;
						for (int x = -1; x <= 1; x += 2) {
							for (int y = -1; y <= 1; y++) {
								Tile adjTile = Main.tile[point.X + x, point.Y + y];
								if (!adjTile.HasTile)
									continue;

								if (adjTile.TileType == TileID.Grass) {
									dirtBlocks.Add(point);
									found = true;
									//Main.NewText($"Dirt: {point}");
									break;
								}
							}

							if (found)
								break;
						}
					}
					else if (TileID.Sets.CommonSapling[tileType]) {
						Main.NewText($"Sapling: {point}");
					}
				}
			}
		}

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

		public static List<List<Point>> MakeCircleArraySurfaceLayerOnly(Point center, float radius, SortedSet<int> types = null) {
			List<List<Point>> circleArray = new();
			for (int x = center.X - (int)radius; x <= center.X + (int)radius; x++) {
				List<Point> column = new();
				for (int y = center.Y - (int)radius; y <= center.Y + (int)radius; y++) {
					if (new Vector2(x, y).Distance(center.ToVector2()) <= radius) {
						Tile tile = Main.tile[x, y];
						if (!tile.HasTile || y == 0 || Main.tile[x, y - 1].HasTile || types != null && !types.Contains(tile.TileType))
							continue;

						column.Add(new Point(x, y));
					}
				}

				circleArray.Add(column);
			}

			return circleArray;
		}
	}
}
