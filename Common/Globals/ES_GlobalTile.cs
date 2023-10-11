using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static EngagedSkyblock.Common.Globals.ES_GlobalTile;

namespace EngagedSkyblock.Common.Globals {
	public class ES_GlobalTile : GlobalTile {
		public override void RandomUpdate(int i, int j, int type) {
			if (!ES_WorldGen.SkyblockWorld)// || !Main.dayTime)
				return;
			
			if (MudThatCanConvertToClay(i, j)) {
				WorldGen.PlaceTile(i, j, TileID.ClayBlock, true, true);
				ES_ModPlayer.GrowDust(new Point(i, j));
			}
		}
		public static bool MudThatCanConvertToClay(int i, int j) {
			Tile tile = Main.tile[i, j];
			if (tile.HasTile) {
				int tileType = tile.TileType;
				if (tileType == TileID.Mud) {
					if (TouchingAir(i, j)) {
						bool waterNearby = false;
						int radius = 4;
						for (int x = -radius; x <= radius; x++) {
							for (int y = -radius; y <= radius; y++) {
								int i2 = i + x;
								int j2 = j + y;
								Tile t = Main.tile[i2, j2];
								if (t.LiquidAmount > 0 && t.LiquidType == LiquidID.Water) {
									if (HasPath(i, j, i2, j2, (float)radius + 0.99f, CountsAsMudPath)) {
										waterNearby = true;
										goto finishedWaterNearbyCheck;
									}
								}
							}
						}

					finishedWaterNearbyCheck:
						return !waterNearby;
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
		private static bool CountsAsMudPath(Tile tile) => tile.HasTile && mudPathAllowedTiles.Contains(tile.TileType) || tile.LiquidAmount > 0 && tile.LiquidType == LiquidID.Water;
		private static bool TouchingAir(int i, int j) {
			for (int k = 0; k < DirectionID.Count; k++) {
				int x = i;
				int y = j;
				DirectionID.ApplyDirection(ref x, ref y, k);
				Tile tile = Main.tile[x, y];
				if (!tile.HasTile)
					return true;
			}

			return false;
		}
		public static bool HasPath(int x, int y, int targetX, int targetY, float maxDistance, Func<Tile, bool> countsAsPath) {
			if (x == targetX && y == targetY)
				return true;

			return FindPath(x, y, x, y, targetX, targetY, maxDistance, countsAsPath, currentDistance: Distance(x, y, targetX, targetY));
		}
		public static class DirectionID {
			public const int None = -1;
			public const int Up = 0;
			public const int Down = 1;
			public const int Left = 2;
			public const int Right = 3;
			public const int Count = 4;

			public static void ApplyDirection(ref int x, ref int y, int direction) {
				switch (direction) {
					case Up:
						y--;
						break;
					case Down:
						y++;
						break;
					case Left:
						x--;
						break;
					case Right:
						x++;
						break;
				}
			}

			public static int GetOppositeDirection(int direction) {
				switch (direction) {
					case Up:
						return Down;
					case Down:
						return Up;
					case Left:
						return Right;
					case Right:
						return Left;
					default:
						return None;
				}
			}
		}
		public class Element<K, T> where K : IComparable {
			public Element(K key, T value, Element<K, T> prev = null, Element<K, T> next = null) {
				this.key = key;
				this.value = value;
				this.prev = prev;
				this.next = next;
			}

			public  K key;
			public T value;
			public Element<K, T> next;
			public Element<K, T> prev;
		}
		public class OrderList<K, T> where K : IComparable {
			public OrderList() {}
			public Element<K, T> first = null;
			public Element<K, T> last = null;
			public void Add(K key, T value) {
				if (first == null) {
					first = new(key, value);
					last = first;
					return;
				}

				for (Element<K, T> current = first; current != null; current = current.next) {
					if (current.key.CompareTo(key) < 0) {
						Element<K, T> newElement = new(key, value, current.prev, current);
						current.prev = newElement;
						if (newElement.prev != null) {
							newElement.prev.next = newElement;
						}
						else {
							first = newElement;
						}

						return;
					}
				}

				last.next = new(key, value, last);
				last = last.next;
			}
		}
		public delegate bool FindPathDelegate(int xStart, int yStart, int x, int y, int targetX, int targetY, float maxDistance, Func<Tile, bool> countsAsPath);
		private static bool FindPath(int xStart, int yStart, int x, int y, int targetX, int targetY, double maxDistance, Func<Tile, bool> countsAsPath, int from = DirectionID.None, double currentDistance = 0d) {
			//Filter and put into a list and sort by distance
			double[] distances = new double[DirectionID.Count];
			OrderList<double, int> order = new();
			int acceptableDistanceCount = 0;//Used to check if any of the directions are outside the maxDistance.
			for (int i = DirectionID.Up; i < DirectionID.Count; i++) {
				//Don't check the direction it just came from.
				if (i == from)
					continue;

				int x2 = x;
				int y2 = y;
				DirectionID.ApplyDirection(ref x2, ref y2, i);
				//Base Case
				if (x2 == targetX && y2 == targetY)
					return true;

				ref double distance = ref distances[i];
				distance = Distance(x2, y2, targetX, targetY);
				if (distance <= maxDistance)
					acceptableDistanceCount++;
			}

			bool outsideBorder = acceptableDistanceCount < 3;
			for (int i = DirectionID.Up; i < DirectionID.Count; i++) {
				//Don't check the direction it just came from.
				if (i == from)
					continue;

				double distance = distances[i];
				int x2 = x;
				int y2 = y;
				DirectionID.ApplyDirection(ref x2, ref y2, i);
				//Only include ones that are further than the current distance if the current position is on the outside border.
				if (distance <= maxDistance && (distance <= currentDistance || outsideBorder) && countsAsPath(Main.tile[x2, y2]))
					order.Add(distance, i);
			}

			for (Element<double, int> current = order.first; current != null; current = current.next) {
				int x2 = x;
				int y2 = y;
				DirectionID.ApplyDirection(ref x2, ref y2, current.value);
				if (FindPath(xStart, yStart, x2, y2, targetX, targetY, maxDistance, countsAsPath, DirectionID.GetOppositeDirection(current.value), current.key))
					return true;
			}

			return false;
		}
		private static double Distance(int x1, int y1, int x2, int y2) {
			return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
		}
	}
}
