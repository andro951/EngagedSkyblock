using androLib.Common.Utility;
using androLib.Common.Utility.Compairers;
using androLib.Common.Utility.PathFinders;
using EngagedSkyblock.Content.Liquids;
using EngagedSkyblock.Weather;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
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

namespace EngagedSkyblock.Common.Globals
{
    public class ES_GlobalTile : GlobalTile {
		public override void Load() {
			On_Player.ItemCheck_UseMiningTools_ActuallyUseMiningTool += On_Player_ItemCheck_UseMiningTools_ActuallyUseMiningTool;
			IL_WorldGen.hardUpdateWorld += IL_WorldGen_hardUpdateWorld;
		}

		internal static void OnWorldLoad() {
			if (!ES_WorldGen.SkyblockWorld)
				return;

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

			CheckForTileUpdates(i, j, type);

			ES_WorldGen.TileRandomUpdate(i, j, type);
		}
		private static bool CheckForTileUpdates(int i, int j, int type) {
			if (!Main.tile[i, j].HasTile)
				return false;

			int tileToPlace = defaultTileToPlace;
			if (MudThatCanConvertToChlorophyte(i, j, type)) {
				tileToPlace = TileID.Chlorophyte;
				goto PlaceTile;
			}

			if (MudThatCanConvertToDirt(i, j, type)) {
				if (mudToDirt.TryGetValue(type, out tileToPlace))
					goto PlaceTile;
			}

			if (DirtThatCanConvertToMud(i, j, type)) {
				if (dirtToMud.TryGetValue(type, out tileToPlace))
					goto PlaceTile;
			}

			if (SandThatCanHarden(i, j, type, out int hardSandType)) {
				tileToPlace = hardSandType;
				goto PlaceTile;
			}

			if (OrganicCanBecomeFossil(i, j, type)) {
				tileToPlace = TileID.DesertFossil;

				goto PlaceTile;
			}

			if (IceCanSpread(ref i, ref j, type, out int iceType)) {
				tileToPlace = iceType;

				goto PlaceTile;
			}

			ES_Liquid.TryUpdateCombineInfo(i, j);

		PlaceTile:
			if (tileToPlace != defaultTileToPlace) {
				PlaceTile(i, j, tileToPlace);
				return true;
			}

			return false;
		}

		public static void PlaceTile(int i, int j, int tileToPlace, bool growDust = true) {
			if (!WorldGen.PlaceTile(i, j, tileToPlace, true, true)) {
				Tile tile = Main.tile[i, j];
				switch (tileToPlace) {
					case TileID.Grass:
					case TileID.CorruptGrass:
					case TileID.CrimsonGrass:
					case TileID.HallowedGrass:
						tile.HasTile = true;
						tile.TileType = TileID.Dirt;
						break;
					case TileID.JungleGrass:
					case TileID.CorruptJungleGrass:
					case TileID.CrimsonJungleGrass:
					case TileID.MushroomGrass:
						tile.HasTile = true;
						tile.TileType = TileID.Mud;
						break;
					case TileID.AshGrass:
						tile.HasTile = true;
						tile.TileType = TileID.Ash;
						break;
				}

				WorldGen.PlaceTile(i, j, tileToPlace, true, true);
			}

			if (growDust)
				ES_ModPlayer.GrowDust(new Point(i, j));
			
			WorldGen.SquareTileFrame(i, j);
			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendTileSquare(-1, i - 1, j - 1, 3);
		}

		#endregion

		#region Ice Spread

		private static bool IceCanSpread(ref int i, ref int j, int type, out int iceType) {
			iceType = -1;
			if (!TileID.Sets.Ices[type])
				return false;

			List<int> ices = new();
			for (int directionID = 0; directionID < 4; directionID++) {
				PathDirectionID.GetDirection(directionID, out int x, out int y);
				x += i;
				y += j;

				if (!WorldGen.InWorld(x, y))
					continue;

				Tile waterTile = Main.tile[x, y];
				if (waterTile.HasTile || waterTile.LiquidAmount <= 0 || waterTile.LiquidType != LiquidID.Water)
					continue;

				for (int iceDirection = 0; iceDirection < 4; iceDirection++) {
					PathDirectionID.GetDirection(iceDirection, out int x2, out int y2);
					x2 += x;
					y2 += y;

					if (!WorldGen.InWorld(x2, y2))
						continue;

					Tile iceTile = Main.tile[x2, y2];
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
		public static bool OrganicCanBecomeFossil(int i, int j, int type) {
			int chanceDenom;
			switch (type) {
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
				case TileID.LeafBlock:
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
		public static bool SandThatCanHarden(int i, int j, int type, out int hardSandType) {
			hardSandType = -1;
			if (j < sandAboveToHarden)
				return false;

			if (!TileID.Sets.Conversion.Sand[type])
				return false;

			for (int y = 1; y <= sandAboveToHarden; y++) {
				Tile above = Main.tile[i, j - y];
				if (!above.HasTile)
					return false;

				if (!TileID.Sets.Falling[above.TileType])
					return false;
			}

			hardSandType = SandToHardendedSand.TryGetValue(type, out int hardSand) ? hardSand : TileID.HardenedSand;

			return true;
		}

		#endregion

		#region Mud and Dirt Conversion

		public static bool MudThatCanConvertToChlorophyte(int i, int j, int type) {
			if (!Main.hardMode)
				return false;

			if (type != TileID.Mud)
				return false;

			if (false && !Main.rand.NextBool(8640000))//TODO: remove false
				return false;

			for (int directionID = 0; directionID < 4; directionID++) {
				PathDirectionID.GetDirection(directionID, out int x, out int y);
				Tile checkTile = Main.tile[i + x, j + y];
				if (!checkTile.HasTile || checkTile.TileType != TileID.Mud && checkTile.TileType != TileID.JungleGrass && checkTile.TileType != TileID.ClayBlock)
					 return false;
			}

			 return true;
		}
		private void IL_WorldGen_hardUpdateWorld(ILContext il) {
			// if ((double)j > (Main.worldSurface + Main.rockLayer) / 2.0 || Main.remixWorld)
			//IL_017f: ldarg.1
			//IL_0180: conv.r8
			//IL_0181: ldsfld float64 Terraria.Main::worldSurface
			//IL_0186: ldsfld float64 Terraria.Main::rockLayer
			//IL_018b: add
			//IL_018c: ldc.r8 2
			//IL_0195: div
			//IL_0196: bgt.s IL_01a2

			//IL_0198: ldsfld bool Terraria.Main::remixWorld

			var c = new ILCursor(il);

			if (!c.TryGotoNext(MoveType.After,
				i => i.MatchLdarg(1),
				i => i.MatchConvR8(),
				i => i.MatchLdsfld(typeof(Main), nameof(Main.worldSurface)),
				i => i.MatchLdsfld(typeof(Main), nameof(Main.rockLayer)),
				i => i.MatchAdd(),
				i => i.MatchLdcR8(2),
				i => i.MatchDiv(),
				i => i.MatchBgt(out _),
				i => i.MatchLdsfld(typeof(Main), nameof(Main.remixWorld))
				)) {
				throw new Exception("Failed to find instructions IL_WorldGen_hardUpdateWorld");
			}

			c.EmitDelegate((bool remixWorld) => {
				return remixWorld || ES_WorldGen.SkyblockWorld;
			});
		}

		private const int waterRadius = 10;
		public static bool MudThatCanConvertToDirt(int i, int j, int type) {
			if (!mudToDirt.ContainsKey(type))
				return false;

			if (Main.raining)
				return false;

			if (TouchingMudWaterSource(i, j))
				return false;

			if (SurroundedBy(i, j, CountsAsMudPath))
				return false;

			return !HasMudPathToWater(i, j, waterRadius);
		}
		private static SortedSet<int> mudPathAllowedTiles = new() {
			TileID.Mud,
			TileID.JungleGrass,
			TileID.CorruptJungleGrass,
			TileID.CrimsonJungleGrass,
			TileID.MushroomGrass
		};
		private static SortedDictionary<int, int> dirtToMud = new() {
			{ TileID.Dirt, TileID.Mud },
			{ TileID.Grass, TileID.JungleGrass },
			{ TileID.CorruptGrass, TileID.CorruptJungleGrass },
			{ TileID.CrimsonGrass, TileID.CrimsonJungleGrass }
		};
		private static SortedDictionary<int, int> mudToDirt = new() {
			{ TileID.Mud, TileID.Dirt },
			{ TileID.JungleGrass, TileID.Grass },
			{ TileID.CorruptJungleGrass, TileID.CorruptGrass },
			{ TileID.CrimsonJungleGrass, TileID.CrimsonGrass },
			{ TileID.MushroomGrass, TileID.Grass }
		};
		private static bool HasMudPathToWater(int i, int j, int radius) {
			return MaxDistancePathFinder.HasPath(i, j, radius, CountsAsMudPath, CountsAsMudWaterSource, Main.maxTilesX, Main.maxTilesY);
		}
		private static bool CountsAsMudPath(int x, int y) => CountsAsMudPath(Main.tile[x, y]);
		private static bool CountsAsMudPath(Tile tile) => tile.HasTile && mudPathAllowedTiles.Contains(tile.TileType);
		private static bool CountsAsMudWaterSource(int x, int y) {
			Tile tile = Main.tile[x, y];
			if (tile.LiquidAmount > 0 && tile.LiquidType == LiquidID.Water)
				return true;

			return Main.raining && tile.HasTile && CountsAsMudPath(tile) && DirectPathToSky(x, y);
		}
		private static bool TouchingAir(int i, int j) => TouchingOtherBlock(i, j, (Tile tile) => !tile.HasTile);
		private static bool TouchingWater(int i, int j) => TouchingBlock(i, j, (Tile tile) => tile.LiquidAmount > 0 && tile.LiquidType == LiquidID.Water);
		private static bool TouchingMudWaterSource(int i, int j) => TouchingBlock(i, j, CountsAsMudWaterSource);
		private static bool TouchingOtherBlock(int i, int j, int tileType) => TouchingOtherBlock(i, j, (Tile tile) => tile.HasTile && tile.TileType == tileType);
		private static bool TouchingOtherBlock(int i, int j, Func<Tile, bool> condition) {
			for (int k = 1; k < DirectionID.Count; k++) {
				int x = i;
				int y = j;
				DirectionID.ApplyDirection(ref x, ref y, k);
				if (!WorldGen.InWorld(x, y))
					continue;

				Tile tile = Main.tile[x, y];
				if (condition(tile))
					return true;
			}

			return false;
		}
		private static bool TouchingBlock(int i, int j, Func<Tile, bool> condition) {
			for (int k = 0; k < DirectionID.Count; k++) {
				int x = i;
				int y = j;
				DirectionID.ApplyDirection(ref x, ref y, k);
				if (!WorldGen.InWorld(x, y))
					continue;

				Tile tile = Main.tile[x, y];
				if (condition(tile))
					return true;
			}

			return false;
		}
		private static bool TouchingBlock(int i, int j, Func<int, int, bool> condition) {
			for (int k = 0; k < DirectionID.Count; k++) {
				int x = i;
				int y = j;
				DirectionID.ApplyDirection(ref x, ref y, k);
				if (!WorldGen.InWorld(x, y))
					continue;

				if (condition(x, y))
					return true;
			}

			return false;
		}
		private static bool SurroundedBy(int i, int j, Func<Tile, bool> condition) {
			for (int k = 1; k < DirectionID.Count; k++) {
				int x = i;
				int y = j;
				DirectionID.ApplyDirection(ref x, ref y, k);
				if (!WorldGen.InWorld(x, y))
					continue;

				Tile tile = Main.tile[x, y];
				if (!condition(tile))
					return false;
			}

			return true;
		}
		private static bool DirectPathToSky(int i, int j) {
			for (int y = j - 1; y >= 0; y--) {
				Tile tile = Main.tile[i, y];
				if (tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType]) {
					return false;
				}
			}

			return true;
		}
		public static bool DirtThatCanConvertToMud(int i, int j, int type) {
			if (!dirtToMud.ContainsKey(type))
				return false;

			if (SurroundedBy(i, j, CountsAsMudPath))
				return true;

			if (Main.raining && DirectPathToSky(i, j))
				return true;

			if (TouchingMudWaterSource(i, j))
				return true;

			if (HasMudPathToWater(i, j, waterRadius))
				return true;

			return false;
		}

		#endregion
	}
}
