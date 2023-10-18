using androLib.Common.Utility;
using androLib.Common.Utility.Compairers;
using EngagedSkyblock.Weather;
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
using static EngagedSkyblock.Common.Globals.ES_GlobalTile;
using static EngagedSkyblock.EngagedSkyblock;

namespace EngagedSkyblock.Common.Globals {
	public class ES_GlobalTile : GlobalTile {
		public override void Load() {
			On_Player.ItemCheck_UseMiningTools_ActuallyUseMiningTool += On_Player_ItemCheck_UseMiningTools_ActuallyUseMiningTool;
		}
		internal static void OnWorldLoad() {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			SetupDesertFossils();
		}

		#region Tiles Hit by Hammer

		public delegate bool orig_TileLoader_Drop(int x, int y, int type, bool includeLargeObjectDrops);
		public delegate bool hook_TileLoader_Drop(orig_TileLoader_Drop orig, int x, int y, int type, bool includeLargeObjectDrops);
		public static readonly MethodInfo TileLoaderDrop = typeof(TileLoader).GetMethod("Drop", BindingFlags.Public | BindingFlags.Static);
		public static bool TileLoader_Drop_Detour(orig_TileLoader_Drop orig, int x, int y, int type, bool includeLargeObjectDrops) {
			if (!ES_WorldGen.SkyblockWorld)
				return orig(x, y, type, includeLargeObjectDrops);

			foreach (KeyValuePair<int, Point> p in tilesJustHit) {
				if (p.Value.X == x && p.Value.Y == y) {
					bool returnFalse = !GlobalHammer.BreakTileWithHammerShouldDoVanillaDrop(x, y, type);
                    Tile tile = Main.tile[x, y];
					if (tile.HasTile && TileID.Sets.IsATreeTrunk[tile.TileType] && y < Main.maxTilesY - 1) {
						tilesJustHit[p.Key] = p.Value + new Point(0, -1);
					}
					else {
						tilesJustHit.Remove(p.Key);
					}

					if (returnFalse)
						return false;

					break;
				}
			}
			
			return orig(x, y, type, includeLargeObjectDrops);
		}
		private static SortedDictionary<int, Point> tilesJustHit = new();
		private void On_Player_ItemCheck_UseMiningTools_ActuallyUseMiningTool(On_Player.orig_ItemCheck_UseMiningTools_ActuallyUseMiningTool orig, Player self, Item sItem, out bool canHitWalls, int x, int y) {
			if (!ES_WorldGen.SkyblockWorld) {
				orig(self, sItem, out canHitWalls, x, y);
				return;
			}
			
			if (sItem.TryGetGlobalItem(out GlobalHammer _) && GlobalHammer.IsHammerableTileType(x, y))
				HitTile(x, y, self.whoAmI);

			orig(self, sItem, out canHitWalls, x, y);
		}
		public static void HitTile(int x, int y, int playerWhoAmI) {
			tilesJustHit.AddOrSet(playerWhoAmI, new(x, y));
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				SendHitBlockPacket(x, y, playerWhoAmI);
			}
		}
		private static void SendHitBlockPacket(int x, int y, int playerWhoAmI) {
			ModPacket modPacket = EngagedSkyblock.Instance.GetPacket();
			modPacket.Write((byte)ModPacketID.HitTile);
			modPacket.Write(x);
			modPacket.Write(y);
			modPacket.Write(playerWhoAmI);
			modPacket.Send();
		}

		#endregion

		#region Random Update and Place Tile

		private static int defaultTileToPlace = -1;
		public override void RandomUpdate(int i, int j, int type) {
			if (!ES_WorldGen.SkyblockWorld)
				return;

			int tileToPlace = defaultTileToPlace;

			if (MudThatCanConvertToClay(i, j)) {
				tileToPlace = TileID.ClayBlock;
				goto PlaceTile;
			}

			if (SandThatCanHarden(i, j, out int hardSandType)) {
				tileToPlace = hardSandType;
				goto PlaceTile;
			}

			if (OrganicCanBecomeFossil(i, j)) {
				tileToPlace = TileID.DesertFossil;

				goto PlaceTile;
			}

			if (IceCanSpread(ref i, ref j, out int iceType)) {
				tileToPlace = iceType;

				goto PlaceTile;
			}

			ES_Liquid.TryUpdateCombineInfo(i, j);

			PlaceTile:
			if (tileToPlace != defaultTileToPlace) {
				PlaceTile(i, j, tileToPlace);
			}
		}
		public static void PlaceTile(int i, int j, int tileToPlace, bool growDust = true) {
			WorldGen.PlaceTile(i, j, tileToPlace, true, true);
			if (growDust)
				ES_ModPlayer.GrowDust(new Point(i, j));

			WorldGen.SquareTileFrame(i, j);
			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendTileSquare(-1, i - 1, j - 1, 3);
		}

		#endregion

		#region Ice Spread

		private bool IceCanSpread(ref int i, ref int j, out int iceType) {
			iceType = -1;
			Tile tile = Main.tile[i, j];
			if (!tile.HasTile || !TileID.Sets.Ices[tile.TileType])
				return false;

			if (!WorldGen.InWorld(i, j, 1))
				return false;

			Tile[] directions = DirectionID.GetTiles(i, j);
			List<int> ices = new();
			for (int k = DirectionID.None + 1; k < DirectionID.Count; k++) {
				Tile waterTile = directions[k];
				if (waterTile.LiquidAmount <= 0 || waterTile.LiquidType != LiquidID.Water)
					continue;

				int x = i;
				int y = j;
				DirectionID.ApplyDirection(ref x, ref y, k);
				if (!WorldGen.InWorld(x, y, 1))
					continue;

				Tile[] iceDirections = DirectionID.GetTiles(x, y);
				for (int l = DirectionID.None + 1; l < DirectionID.Count; l++) {
					Tile iceTile = iceDirections[l];
					if (!iceTile.HasTile || !TileID.Sets.Ices[iceTile.TileType])
						continue;

					ices.Add(iceTile.TileType);
				}

				if (ices.Count > 1) {
					i = x;
					j = y;
					iceType = ices[Main.rand.Next(ices.Count)];
					return true;
				}

				ices.Clear();
			}

			return false;
		}

		#endregion

		#region Desert Fossils

		private static SortedSet<Point> burriedOrganicTiles = new(comparer: new Compairers.PointComparer());
		private static float onDistanceThreshold = 16f * 30f;
		private static float offDistanceThreshold = 16f * 20f;
		private static SortedSet<int> organicTiles = new() {
			TileID.BoneBlock,
			TileID.FleshBlock,
			TileID.PoopBlock,
			TileID.MushroomBlock,
			TileID.LivingMahoganyLeaves,
			TileID.Cactus,
			TileID.BambooBlock,
			TileID.LargeBambooBlock,
			TileID.PumpkinBlock,
			TileID.HayBlock,
			TileID.WoodBlock,
			TileID.BorealWood,
			TileID.PalmWood,
			TileID.RichMahogany,
			TileID.Shadewood,
			TileID.Pearlwood,
			TileID.LivingWood,
			TileID.LeafBlock,
			TileID.LivingMahogany,
			TileID.Hive,
			TileID.HoneyBlock,
			TileID.CrispyHoneyBlock,
			TileID.BubblegumBlock,
			TileID.DynastyWood,
			TileID.SlimeBlock,
			TileID.SpookyWood,
			TileID.PinkSlimeBlock,
			TileID.LesionBlock,
			TileID.RockGolemHead,
			TileID.AshWood,
			TileID.FrozenSlimeBlock,
		};
		public static bool OrganicCanBecomeFossil(int i, int j) {
			Tile tile = Main.tile[i, j];
			if (!tile.HasTile) {
				return false;
			}

			int chanceDenom;
			switch (tile.TileType) {
				case TileID.RockGolemHead:
					chanceDenom = 1;
					break;
				case TileID.BoneBlock:
				case TileID.FleshBlock:
				case TileID.LesionBlock:
					chanceDenom = 5;
					break;
				case TileID.PoopBlock:
					chanceDenom = 10;
					break;
				case TileID.MushroomBlock:
					chanceDenom = 25;
					break;
				case TileID.LivingWood:
				case TileID.LivingMahogany:
				case TileID.LeafBlock:
				case TileID.LivingMahoganyLeaves:
				case TileID.Hive:
				case TileID.HoneyBlock:
				case TileID.CrispyHoneyBlock:
					chanceDenom = 50;
					break;
				case TileID.Cactus:
				case TileID.BambooBlock:
				case TileID.LargeBambooBlock:
				case TileID.PumpkinBlock:
					chanceDenom = 66;
					break;
				case TileID.HayBlock:
					chanceDenom = 100;
					break;
				case TileID.SlimeBlock:
				case TileID.FrozenSlimeBlock:
				case TileID.PinkSlimeBlock:
				case TileID.BubblegumBlock:
					chanceDenom = 200;
					break;
				case TileID.WoodBlock:
				case TileID.BorealWood:
				case TileID.PalmWood:
				case TileID.RichMahogany:
				case TileID.Shadewood:
				case TileID.Pearlwood:
				case TileID.AshWood:
				case TileID.DynastyWood:
				case TileID.SpookyWood:
					chanceDenom = 1000;
					break;
				default:
					return false;
			}

			if (Main.rand.Next(chanceDenom) != 0)
				return false;

			Point point = new Point(i, j);
			bool desert = burriedOrganicTiles.Contains(point);
			bool playerDesert = false;
			float minDistance = float.MaxValue;
			foreach (Player player in Main.player) {
				if (player.NullOrNotActive())
					continue;

				float distance = player.Distance(point.ToWorldCoordinates());
				if (distance > onDistanceThreshold)
					continue;

				if (minDistance > distance)
					minDistance = distance;

				if (player.ZoneDesert) {
					if (!desert)
						burriedOrganicTiles.Add(point);

					playerDesert = true;
					break;
				}
			}

			if (!playerDesert) {
				if (minDistance <= offDistanceThreshold) {
					desert = playerDesert;
					burriedOrganicTiles.Remove(point);
				}
			}

			if (!desert)
				return false;

			return CheckOrganicTileSurrounded(i, j);
		}
		private static bool CheckOrganicTileSurrounded(int i, int j) {
			if (i <= 0 || i >= Main.maxTilesX - 1 || j <= 0 || j >= Main.maxTilesY - 1)
				return false;

			Tile[] directions = DirectionID.GetTiles(i, j);
			for (int k = DirectionID.None + 1; k < DirectionID.Count; k++) {
				Tile direction = directions[k];
				if (!direction.HasTile)
					return false;

				if (!organicTiles.Contains(direction.TileType) && TileID.Sets.SandBiome[direction.TileType] <= 0)
					return false;
			}

			return true;
		}
		public static void SetupDesertFossils() {
			burriedOrganicTiles = new(comparer: new Compairers.PointComparer());
			List<Point> organicTilesToCheck = new();
			for (int x = 0; x < Main.maxTilesX; x++) {
				for (int y = 0; y < Main.maxTilesY; y++) {
					Tile tile = Main.tile[x, y];
					if (!tile.HasTile)
						continue;

					if (!CheckOrganicTileSurrounded(x, y))
						continue;

					if (organicTiles.Contains(tile.TileType))
						organicTilesToCheck.Add(new Point(x, y));
				}
			}

			if (organicTilesToCheck.Count <= 0)
				return;

			int groupSize = 10;
			int xEnd = Main.maxTilesX.CeilingDivide(groupSize);
			int yEnd = Main.maxTilesY.CeilingDivide(groupSize);
			int[,] tileCounts = new int[xEnd, yEnd];
			for (int xGroup = 0; xGroup < xEnd; xGroup++) {
				for (int yGroup = 0; yGroup < yEnd; yGroup++) {
					int xStart = xGroup * groupSize;
					int yStart = yGroup * groupSize;
					int xLimit = Math.Min(xStart + groupSize, Main.maxTilesX);
					int yLimit = Math.Min(yStart + groupSize, Main.maxTilesY);
					for (int x = xStart; x < xLimit; x++) {
						for (int y = yStart; y < yLimit; y++) {
							Tile tile = Main.tile[x, y];
							if (!tile.HasTile)
								continue;

							tileCounts[xGroup, yGroup] += TileID.Sets.SandBiome[tile.TileType];
						}
					}
				}
			}

			int scanX = (Main.buffScanAreaWidth / 2).CeilingDivide(groupSize);
			int scanY = (Main.buffScanAreaHeight / 2).CeilingDivide(groupSize);
			foreach (Point point in organicTilesToCheck) {
				int xGroup = point.X / groupSize;
				int yGroup = point.Y / groupSize;
				int xStart = Math.Max(xGroup - scanX, 0);
				int yStart = Math.Max(yGroup - scanY, 0);
				int xLimit = Math.Min(xGroup + scanX, xEnd);
				int yLimit = Math.Min(yGroup + scanY, yEnd);
				int count = 0;
				for (int x = xStart; x < xLimit; x++) {
					for (int y = yStart; y < yLimit; y++) {
						count += tileCounts[x, y];
					}
				}

				if (count < SceneMetrics.DesertTileThreshold)
					continue;

				burriedOrganicTiles.Add(point);
			}
		}

		#endregion

		#region Sand Hardening

		public static int sandAboveToHarden = 4;
		private static SortedDictionary<int, int> SandToHardendedSand = new() {
			{ TileID.Sand, TileID.HardenedSand },
			{ TileID.Crimsand, TileID.CrimsonHardenedSand },
			{ TileID.Ebonsand, TileID.CorruptHardenedSand },
			{ TileID.Pearlsand, TileID.HallowHardenedSand },
		};
		public static bool SandThatCanHarden(int i, int j, out int hardSandType) {
			hardSandType = -1;
			if (j < sandAboveToHarden)
				return false;

			Tile tile = Main.tile[i, j];
			if (!tile.HasTile)
				return false;

			if (!TileID.Sets.Conversion.Sand[tile.TileType])
				return false;

			for (int y = 1; y <= sandAboveToHarden; y++) {
				Tile above = Main.tile[i, j - y];
				if (!above.HasTile)
					return false;

				if (!TileID.Sets.Falling[above.TileType])
					return false;
			}

			hardSandType = SandToHardendedSand.TryGetValue(tile.TileType, out int hardSand) ? hardSand : TileID.HardenedSand;

			return true;
		}

		#endregion

		#region Mud Conversion

		public static bool MudThatCanConvertToClay(int i, int j) {
			if (Main.raining)
				return false;

			Tile tile = Main.tile[i, j];
			if (tile.HasTile) {
				int tileType = tile.TileType;
				if (tileType == TileID.Mud) {
					if (TouchingAir(i, j)) {
						int radius = 10;
						return !HasMudPathToWater(i, j, radius);
					}
				}
			}

			return false;
		}
		private static SortedSet<int> mudPathAllowedTiles = new() {
			TileID.Mud,
			TileID.JungleGrass,
			TileID.Grass,
			TileID.Dirt
		};
		private static bool HasMudPathToWater(int i, int j, int radius) {
			return HasPath(i, j, radius, CountsAsMudPath, CountsAsMudWaterSource, Main.maxTilesX, Main.maxTilesY);
		}
		private static bool CountsAsMudPath(int x, int y) {
			Tile tile = Main.tile[x, y];
			return tile.HasTile && mudPathAllowedTiles.Contains(tile.TileType);
		}
		private static bool CountsAsMudWaterSource(int x, int y) {
			Tile tile = Main.tile[x, y];
			return tile.LiquidAmount > 0 && tile.LiquidType == LiquidID.Water;
		}
		private static void GetBoundaries(int x, int y, int xMin, int xMax, int yMin, int yMax, int radius, out int left, out int up, out int right, out int down) {
			left = Math.Max(-radius, xMin - x);
			up = Math.Max(-radius, yMin - y);
			right = Math.Min(radius, xMax - x);
			down = Math.Min(radius, yMax - y);
		}
		public static bool HasPath(int x, int y, int MaxDistance, Func<int, int, bool> CountsAsPath, Func<int, int, bool> CountsAsTarget, int XMax, int YMax, int XMin = 0, int YMin = 0) {
			countsAsPath = CountsAsPath;
			countsAsTarget = CountsAsTarget;
			maxDistance = MaxDistance;
			//Create a rectangle that is either smaller than or equal to min and max limits provided, minimizing the size of PathGrid.
			GetBoundaries(x, y, XMin, XMax, YMin, YMax, maxDistance, out int left, out int up, out int right, out int down);
			//If not limited by the min or max values, left will be negative maxDistance, right positive maxDistance, up negative maxDistance, down positive maxDistance.
			int gridSizeX = -left + right + 1;
			int gridSizeY = -up + down + 1;
			//centerX and centerY are the converted x and y coordinates in PathGrid.
			centerX = -left;
			centerY = -up;
			//xStart and yStart are the difference between the provided x and y and the center of PathGrid.
			//In FindPath(), x and y are in PathGrid coordinates.  x + xStart, y + yStart will be in the original coordinates.
			xStart = x - centerX;
			yStart = y - centerY;
			xMax = gridSizeX - 1;
			xMin = 0;
			yMax = gridSizeY - 1;
			yMin = 0;
			//Resizes PathGrid to gridSizeX by gridSizeY, and sets all values to int.MaxValue
			FillArray(ref PathGrid, gridSizeX, gridSizeY, int.MaxValue);
			//Set the starting point to 0 to prevent paths from trying to go back through it.
			PathGrid[centerX, centerY] = 0;

			bool hasPath = FindPath(centerX, centerY, 0);
			//if (Debugger.IsAttached) PrintPathGrid();

			PathGrid = null;
			resultPath = null;
			countsAsPath = null;
			countsAsTarget = null;

			return hasPath;
		}
		private static void PrintPathGrid() {
			string path = "\n";
			int longest = 0;
			for (int x = xMin; x <= xMax; x++) {
				for (int y = yMin; y <= yMax; y++) {
					int pathGridValue = PathGrid[x, y];
					string pathGridString = pathGridValue switch {
						int.MaxValue => "X",
						0 => "S",
						_ => pathGridValue.ToString()
					};

					if (pathGridString.Length > longest)
						longest = pathGridString.Length;
				}
			}

			for (int y = yMin; y <= yMax; y++) {
				bool first = true;
				for (int x = xMin; x <= xMax; x++) {
					if (first) {
						first = false;
					}
					else {
						path += ", ";
					}

					int pathGridValue = PathGrid[x, y];
					string pathGridString = pathGridValue switch {
						int.MaxValue => "X",
						0 => "S",
						_ => pathGridValue.ToString()
					};

					path += pathGridString.PadLeft(longest);
				}

				path += "\n";
			}

			path += "\n";
			path.LogSimple();
		}
		private static int[,] PathGrid;
		private static string resultPath;
		private static int xStart;
		private static int yStart;
		private static int centerX;
		private static int centerY;
		private static Func<int, int, bool> countsAsPath;
		private static Func<int, int, bool> countsAsTarget;
		private static int maxDistance;
		private static int xMax;
		private static int yMax;
		private static int xMin;
		private static int yMin;
		/// <summary>
		/// Searches for a path to a position satisfied by countsAsTarget only through positions satisfied by countsAsPath.<br/>
		/// </summary>
		private static bool FindPath(int x, int y, int currentDistance, int fromDirection = -1, int previousFrom = -1) {
			//$"{x}, {y}, ({x + xStart}, {y + yStart}), currentDistance: {currentDistance}, fromDirection: {fromDirection}, previousFrom: {previousFrom}".LogSimple();
			//opposite and previousOpposite are the opposite directionIDs for the path taken to get to this point.
			//For instance, if the path taken to get here was directionID 0 (down), opposite will be 2 (up).
			//previousFrom is used to track the previous x if fromDirection is tracking y or vice versa because the path should generally go away from previous paths.
			int opposite = fromDirection >= 0 ? (fromDirection + 2) % 4 : -1;
			int previousOpposite = previousFrom >= 0 ? (previousFrom + 2) % 4 : -1;
			List<Func<bool>> directionsToCheck = new();
			for (int directionID = 0; directionID < 4; directionID++) {
				int i = directionID % 2;
				int j = 1 - i;
				if (directionID > 1) {
					i *= -1;
					j *= -1;
				}

				//directionID:  i,  j
				//down		0:  0,  1
				//right		1:  1,  0
				//up		2:  0, -1
				//left		3: -1,  0

				if (opposite == directionID)
					continue;

				int x2 = x + i;
				//Prevent out of bounds
				if (x2 < xMin || x2 > xMax)
					continue;

				int y2 = y + j;
				//Prevent out of bounds
				if (y2 < yMin || y2 > yMax)
					continue;

				//Increment the distance every time we move to a new position.
				int distance = currentDistance + 1;
				//PathGrid stores the distance required to get to this point by previous paths or int.MaxValue if not yet reached.
				//There is no reason to continue checking a path on a tile that has been reached by a shorter path.
				if (PathGrid[x2, y2] <= distance)
					continue;

				if (distance > maxDistance)
					continue;

				//realX and realY are converting the search area coordinates to coordinates of the original FindPath() request.
				int realX = x2 + xStart;
				int realY = y2 + yStart;

				//Base case
				if (countsAsTarget(realX, realY))
					return true;

				//Mark the grid with the distance required to get to this point on this path.
				PathGrid[x2, y2] = distance;

				directionsToCheck.Add(() => {
					if (!countsAsPath(realX, realY))
						return false;

					//Usually, the searches should only go out in 2 directions, an x direction and a y direction.
					//However, there are some situations where backtracking is the only option, so only skip the backtrack path if the
					//	backTrackX, backTrackY position isn't the starting point and doesn't count as a path.
					if (previousOpposite == directionID) {
						GetPreviousDirection(x + xStart, y + yStart, fromDirection, previousFrom, out int backTrackX, out int backTrackY);
						if (backTrackX == centerX && backTrackY == centerY || countsAsPath(backTrackX, backTrackY))
							return false;
					}

					return FindPath(x2, y2, distance, directionID, fromDirection == directionID ? previousFrom : fromDirection);
				});
			}

			foreach (Func<bool> directionToCheck in directionsToCheck) {
				if (directionToCheck())
					return true;
			}

			return false;
		}
		/// <summary>
		/// Prevents the path from going almost in a circle by doing something such as down 1, left 1, up 1 if the previous coordinates count as a path.<br/>
		/// x0 and y0 are the coordinates of the current search.  x and y will be back 1 in both of the previous directions.<br/>
		/// . . 3 0<br/>
		/// . . 2 1<br/>
		/// . . . .<br/>
		/// . . . .<br/>
		/// Prevents the path to 3 because it can be reached by a shorter path from the starting point, 0.
		/// </summary>
		private static void GetPreviousDirection(int x0, int y0, int fromDirection, int previousFrom, out int x, out int y) {
			//directionID:  i,  j
			//down		0:  0,  1
			//right		1:  1,  0
			//up		2:  0, -1
			//left		3: -1,  0
			x = x0;
			y = y0;
			bool fromPositiveY = fromDirection == 0 || previousFrom == 0;
			bool fromPositiveX = fromDirection == 1 || previousFrom == 1;
			if (fromPositiveY) {
				y--;
			}
			else {
				y++;
			}

			if (fromPositiveX) {
				x--;
			}
			else {
				x++;
			}
		}
		public static void FillArray<T>(ref T[,] arr, int xLen, int yLen, T value) {
			arr = new T[xLen, yLen];
			for (int y = 0; y < yLen; y++) {
				for (int x = 0; x < xLen; x++) {
					arr[x, y] = value;
				}
			}
		}
		//private static bool CountsAsMudPath(Tile tile) => tile.HasTile && mudPathAllowedTiles.Contains(tile.TileType) || tile.LiquidAmount > 0 && tile.LiquidType == LiquidID.Water;
		private static bool TouchingAir(int i, int j) {
			for (int k = 0; k < DirectionID.Count; k++) {
				int x = i;
				int y = j;
				DirectionID.ApplyDirection(ref x, ref y, k);
				if (!WorldGen.InWorld(x, y))
					continue;

				Tile tile = Main.tile[x, y];
				if (!tile.HasTile)
					return true;
			}

			return false;
		}

		#endregion

	}
}
