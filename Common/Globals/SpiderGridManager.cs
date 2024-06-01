using androLib.Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria;
using androLib.Common.Utility.PathFinders;
using static EngagedSkyblock.Common.Globals.SpiderGridManager;
using Terraria.UI;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xna.Framework;

namespace EngagedSkyblock.Common.Globals {
	public class SpiderGridManager {
		private SortedDictionary<int, SpiderGrid> spiderGrids = new();
		public SpiderGrid Active;
		internal void SetOrCreateActiveGrid(int chestNum) {
			Chest chest = Main.chest[chestNum];
			if (chest == null) {
				throw new Exception($"SetOrCreateActiveGrid({chestNum}): chest was null.");
			}

			int x = chest.x;
			int y = chest.y;
			if (Active != null && Active.chest == chestNum && Active.x == x && Active.y == y)
				return;

			foreach (SpiderGrid grid in spiderGrids.Values) {
				if (grid.chest == chestNum) {
					if (grid.x == x && grid.y == y) {
						Active = grid;
						return;
					}
					else {
						spiderGrids.Remove(chestNum);
						break;
					}
				}
			}

			spiderGrids.Add(chestNum, new(chestNum, x, y));
			Active = spiderGrids[chestNum];
		}
		public void CleanUp(SortedDictionary<int, int> bugFilledChests) {
			int[] remove = new int[spiderGrids.Count];
			int index = 0;
			foreach (SpiderGrid grid in spiderGrids.Values) {
				remove[index++] = grid.chest;
			}

			foreach (KeyValuePair<int, int> pair in bugFilledChests) {
				index = 0;
				Chest chest = Main.chest[pair.Key];
				if (chest == null)
					continue;

				foreach (int spiderChestNum in spiderGrids.Keys) {
					if (pair.Key == spiderChestNum) {
						remove[index] = -1;
						break;
					}

					index++;
				}
			}

			for (int i = remove.Length - 1; i >= 0; i--) {
				int chestNum = remove[i];
				if (chestNum >= 0)
					spiderGrids.Remove(chestNum);
			}
		}

		internal bool PlaceNextTarget(int chestNum) {
			SetOrCreateActiveGrid(chestNum);
			return Active.TryPlaceNextTarget();
		}

		public static bool CanSpreadToTile(Tile tile) {
			if (tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
				return false;

			return tile.WallType != WallID.SpiderUnsafe;
		}
		public static bool CanContinuePathAtTile(Tile tile) => tile.WallType == WallID.SpiderUnsafe && (!tile.HasTile || !Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);

		internal void ProcessNewGrid(int chestNum, int tilesToCheck) {
			SetOrCreateActiveGrid(chestNum);
			Active.ProcessNewGrid(tilesToCheck);
		}
	}

	public class SpiderGrid {
		public int chest;
		public int x;
		public int y;
		public SpiderGrid(int chest, int x, int y) {
			this.chest = chest;
			this.x = x;
			this.y = y;
			SpiderPath = new();
			Targets = new();
			SpiderPathNew = new();
			TargetsNew = new();
		}

		public DictionaryGrid<int> SpiderPath;
		public DictionaryGrid<int> Targets;
		public DictionaryGrid<int> SpiderPathNew;
		public DictionaryGrid<int> TargetsNew;
		private int pathCheckID = AllConnectedPathFinder.PathCheckDefaultID;
		private bool CheckPathShouldContinueNew(int newX, int newY, int distance) =>
			CheckPathShouldContinue(newX, newY, distance, SpiderPathNew, TargetsNew);
		private bool CheckPathShouldContinue(int newX, int newY, int distance) =>
			CheckPathShouldContinue(newX, newY, distance, SpiderPath, Targets);
		private bool CheckPathShouldContinue(int newX, int newY, int distance, DictionaryGrid<int> spiderPath, DictionaryGrid<int> targets) {
			Tile checkTile = Main.tile[newX, newY];
			if (CanContinuePathAtTile(checkTile)) {
				if (spiderPath.TryGetValue(newX, newY, out int lastDistance)) {
					if (distance < lastDistance) {
						spiderPath.Set(newX, newY, distance);
						return true;
					}
					else {
						return false;
					}
				}
				else {
					spiderPath.Add(newX, newY, distance);
					return true;
				}
			}
			else if (CanSpreadToTile(checkTile)) {
				if (targets.TryGetValue(newX, newY, out int lastDistance)) {
					if (distance < lastDistance) {
						targets.Set(newX, newY, distance);
						return true;
					}
					else {
						return false;
					}
				}
				else {
					targets.Add(newX, newY, distance);
					return false;
				}
			}

			return false;
		}
		public bool TryPlaceNextTarget() {
			int placeWallX = -1;
			int placeWallY = -1;
			int value = -1;
			Tile chestTile = Main.tile[x, y];
			if (chestTile.WallType != WallID.SpiderUnsafe) {
				if (CanSpreadToTile(chestTile)) {
					placeWallX = x;
					placeWallY = y;
					value = 0;
					goto PlaceWall;
				}
				else {
					return false;
				}
			}

			if (Targets == null)
				return false;

			if (Targets.Any()) {
				Targets.GetRandomAndRemove(out placeWallX, out placeWallY, out value);
				Vector2 targetPosition = new Vector2(placeWallX, placeWallY).ToWorldCoordinates();
				foreach (Vector2 playerCenter in ES_GlobalWall.PlayerPositions) {
					float xDist = Math.Abs(playerCenter.X - targetPosition.X);
					float yDist = Math.Abs(playerCenter.Y - targetPosition.Y);
					if (xDist < ES_GlobalWall.PlayerDistanceToDisableX && yDist < ES_GlobalWall.PlayerDistanceToDisableY)
						return  false;
				}

				bool validForPlacingWall = false;
				Tile tile = Main.tile[placeWallX, placeWallY];
				if (CanSpreadToTile(tile)) {
					for (int directionID = 0; directionID < 4; directionID++) {
						PathDirectionID.GetDirection(directionID, out int i, out int j);
						Tile checkTile = Main.tile[placeWallX + i, placeWallY + j];
						if (CanContinuePathAtTile(checkTile)) {
							validForPlacingWall = true;
							break;
						}
					}
				}

				if (!validForPlacingWall)
					return false;

				goto PlaceWall;
			}
			else {
				return false;
			}

		PlaceWall:
			WorldGen.PlaceWall(placeWallX, placeWallY, WallID.SpiderUnsafe, true);
			if (Main.netMode == NetmodeID.Server && Main.tile[placeWallX, placeWallY].WallType == WallID.SpiderUnsafe)
				NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 3, placeWallX, placeWallY, WallID.SpiderUnsafe);

			AllConnectedPathFinder.FindAll(placeWallX, placeWallY, CheckPathShouldContinue, 10, AllConnectedPathFinder.PathCheckDefaultID, Main.maxTilesX - 1, Main.maxTilesY - 1, distance: value);

			return true;
		}
		private void FinishedCreatingNewPaths() {
			SpiderPath = SpiderPathNew;
			Targets = TargetsNew;
			SpiderPathNew = new();
			TargetsNew = new();
		}

		internal void ProcessNewGrid(int tilesToCheck) {
			pathCheckID = AllConnectedPathFinder.FindAll(x, y, CheckPathShouldContinueNew, tilesToCheck, pathCheckID, Main.maxTilesX - 1, Main.maxTilesY - 1);
			if (pathCheckID == AllConnectedPathFinder.PathCheckDefaultID)
				FinishedCreatingNewPaths();
		}
	}
}
