using androLib;
using androLib.Common.Utility;
using EngagedSkyblock.Items;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;

namespace EngagedSkyblock.Weather {
	public static class ES_Weather {
		public static void Load() {
			On_SceneMetrics.ScanAndExportToMain += On_SceneMetrics_ScanAndExportToMain;
		}

		public static bool CanRain => Main.atmo >= 0.4f;//From Rain.NewRain() if ((double)Main.atmo < 0.4) num2 = 0;
		public static int SnowStartHeight => Main.maxTilesY / 5;
		private static void On_SceneMetrics_ScanAndExportToMain(On_SceneMetrics.orig_ScanAndExportToMain orig, SceneMetrics self, SceneMetricsScanSettings settings) {
			orig(self, settings);

			if (!ES_WorldGen.SkyblockWorld)
				return;

			if (Main.raining && settings.BiomeScanCenterPositionInWorld.HasValue) {
				Point point = settings.BiomeScanCenterPositionInWorld.Value.ToTileCoordinates();
				int snowStartHeight = SnowStartHeight;
				if (point.Y <= snowStartHeight) {
					if (!self.EnoughTilesForDesert) {
						int snowToAdd = (int)Math.Round(((snowStartHeight - point.Y) / (float)snowStartHeight) * (SceneMetrics.SnowTileMax - SceneMetrics.SnowTileThreshold)) + SceneMetrics.SnowTileThreshold;
						self.SnowTileCount += snowToAdd;
					}
				}
			}
		}

		public static void Update() {
			SnowFlake.TrySpawnSnowFlake();
		}

		public static bool Snowing => Main.SceneMetrics.EnoughTilesForSnow;
		private static float snowMultiplier = 1f;
		public static float SnowMultiplier => Main.SceneMetrics.SnowTileCount * snowMultiplier / SceneMetrics.SnowTileThreshold;
		//public static bool CanSpawnSnow(ref int x, ref int y) {
		//	bool checkBlock = Main.tile[x, y + 1].TileType == TileID.RedBrick;
		//	if (!Main.raining)
		//		return false;

		//	if (!CanRain)
		//		return false;

		//	if (!Tiles.RainTotem.TotemActive())
		//		return false;

		//	if (!Snowing)
		//		return false;

		//	if (y == Main.maxTilesY - 1)
		//		return false;

		//	if (y > SnowStartHeight)
		//		return false;

		//	float multiplier = SnowMultiplier * 10f;
		//	if (multiplier <= 0f)
		//		return false;

		//	if (Main.rand.NextFloat() > multiplier)
		//		return false;

		//	Tile above = Main.tile[x, y - 1];
		//	if (above.HasTile)
		//		return false;

		//	if (checkBlock)
		//		Main.NewText($"On top of ash ({x}, {y})");

		//	Tile tile = Main.tile[x, y];
		//	if (!tile.HasTile)
		//		return false;

		//	if (tile.Slope != SlopeType.Solid)
		//		return false;

		//	int tileType = tile.TileType;
		//	if (!Main.tileSolid[tileType] && !Main.tileSolidTop[tileType])
		//		return false;

		//	for (int i = 0; i < Main.player.Length; i++) {
		//		Player player = Main.player[i];
		//		if (player.NullOrNotActive())
		//			continue;

		//		Point playerPosition = player.Center.ToTileCoordinates();
		//		int minDistanceFromPlayer = 2;
		//		if (Math.Abs(playerPosition.X - x) < minDistanceFromPlayer)
		//			return false;
		//	}

		//	//TODO: change this to check for tiles blocking it instead of all the way up.
		//	for (int y2 = y - 1; y2 >= 0; y2--) {
		//		Tile above2 = Main.tile[x, y2];
		//		if (above2.HasTile)
		//			return false;
		//	}

		//	//RandomUpdate() isn't called on air tiles, so need to move up to the air tile on top of this one.
		//	y--;

		//	return true;
		//}
	}
}
