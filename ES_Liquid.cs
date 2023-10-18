using androLib.Common.Utility;
using androLib.Common.Utility.Compairers;
using androLib.Common.Utility.LogSystem.WebpageComponenets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Terraria;
using Terraria.ID;
using static EngagedSkyblock.ES_Liquid;
using static Terraria.GameContent.Animations.Actions.NPCs;

namespace EngagedSkyblock {
	public static class ES_Liquid {
		public static void Load() {
			IL_Liquid.LiquidCheck += IL_Liquid_LiquidCheck;
			IL_Liquid.Update += IL_Liquid_Update;
		}

		private static void IL_Liquid_Update(ILContext il) {
			//// bool flag2 = true;
			//IL_0484: ldc.i4.1
			//IL_0485: stloc.s 9
			//// bool flag3 = true;
			//IL_0487: ldc.i4.1
			//IL_0488: stloc.s 10
			//// bool flag4 = true;
			//IL_048a: ldc.i4.1
			//IL_048b: stloc.s 11
			//// bool flag5 = true;
			//IL_048d: ldc.i4.1
			//IL_048e: stloc.s 12

			var c = new ILCursor(il);

			if (!c.TryGotoNext(MoveType.Before,
				i => i.MatchLdcI4(1),
				i => i.MatchStloc(9),
				i => i.MatchLdcI4(1),
				i => i.MatchStloc(10),
				i => i.MatchLdcI4(1),
				i => i.MatchStloc(11),
				i => i.MatchLdcI4(1),
				i => i.MatchStloc(12)
				)) {
				throw new Exception("Failed to find instructions IL_Liquid_Update 1/2");
			}

			//IL_0011: ldarg.0
			//IL_0012: ldfld int32 Terraria.Liquid::x
			//IL_0017: ldc.i4.1
			//IL_0018: sub
			//IL_0019: ldarg.0
			//IL_001a: ldfld int32 Terraria.Liquid::y

			c.EmitLdarg(0);
			c.EmitLdfld(typeof(Liquid).GetField("x"));
			c.EmitLdarg(0);
			c.EmitLdfld(typeof(Liquid).GetField("y"));

			c.EmitDelegate(CheckMoveLiquids);

			var label = c.DefineLabel();
			c.Emit(OpCodes.Br, label);

			//IL_1656: ldloca.s 4
			//IL_1658: call instance uint8 & Terraria.Tile::get_liquid()
			//IL_165d: ldind.u1
			//IL_165e: ldloc.s 5
			//IL_1660: beq.s IL_16c7

			if (!c.TryGotoNext(MoveType.Before,
				i => i.MatchLdloca(4),
				i => i.MatchCall(out _),
				i => i.MatchLdindU1(),
				i => i.MatchLdloc(5),
				i => i.MatchBeq(out _)
				)) {
				throw new Exception("Failed to find instructions IL_Liquid_Update 2/2");
			}

			c.MarkLabel(label);
		}
		private static bool MergeLiquidsShouldDoVanilla(int x, int y, int thisLiquidType) {
			if (!WorldGen.InWorld(x, y, 1))
				return true;

			Tile tile = Main.tile[x, y];
			Tile up = Main.tile[x, y - 1];
			Tile left = Main.tile[x - 1, y];
			Tile right = Main.tile[x + 1, y];
			int[] liquids = new int[4];
			liquids[thisLiquidType] += tile.LiquidAmount;
			Action onSucces = null;
			onSucces += () => tile.LiquidAmount = 0;
			if (left.LiquidAmount > 0 && left.LiquidType != thisLiquidType) {
				liquids[left.LiquidType] += left.LiquidAmount;
				onSucces += () => left.LiquidAmount = 0;
			}

			if (right.LiquidAmount > 0 && right.LiquidType != thisLiquidType) {
				liquids[right.LiquidType] += right.LiquidAmount;
				onSucces += () => right.LiquidAmount = 0;
			}

			if (up.LiquidAmount > 0 && up.LiquidType != thisLiquidType) {
				liquids[up.LiquidType] += up.LiquidAmount;
				onSucces += () => up.LiquidAmount = 0;
			}

			return MergeLiquidsShouldDoVanilla(x, y, thisLiquidType, liquids, onSucces);
		}
		private static bool MergeLiquidsShouldDoVanillaDownOnly(int x, int y, int thisLiquidType) {
			if (y == Main.maxTilesY)
				return true;

			Tile tile = Main.tile[x, y];
			Tile down = Main.tile[x, y + 1];
			int[] liquids = new int[4];
			liquids[thisLiquidType] += tile.LiquidAmount;
			Action onSucces = null;
			onSucces += () => tile.LiquidAmount = 0;
			if (down.LiquidAmount > 0 && down.LiquidType != thisLiquidType) {
				liquids[down.LiquidType] += down.LiquidAmount;
				onSucces += () => down.LiquidAmount = 0;
			}

			return MergeLiquidsShouldDoVanilla(x, y + 1, thisLiquidType, liquids, onSucces);
		}
		private static bool MergeLiquidsShouldDoVanilla(int x, int y, int thisLiquidType, int[] liquids, Action postAction) {
			Liquid.GetLiquidMergeTypes(thisLiquidType, out int liquidMergeTileType, out int liquidMergeType, liquids[LiquidID.Water] > 0, liquids[LiquidID.Lava] > 0, liquids[LiquidID.Honey] > 0, liquids[LiquidID.Shimmer] > 0);
			if (liquidMergeType == thisLiquidType)
				return false;

			int lavaCount = liquids[LiquidID.Lava];
			switch (liquidMergeTileType) {
				case TileID.Obsidian:
					int tileType = lavaCount < 240 ? lavaCount < 64 ? TileID.Stone : TileID.Silt : TileID.Obsidian;
					PlaceBlockFromLiquidMerge(x, y, tileType, thisLiquidType, liquidMergeType);
					postAction();
					return false;
			}

			return true;
		}
		private static void IL_Liquid_LiquidCheck(ILContext il) {
			//IL_009a: ldc.i4.0
			//IL_009b: stloc.s 5

			var c = new ILCursor(il);

			if (!c.TryGotoNext(MoveType.After,
				i => i.MatchLdcI4(0),
				i => i.MatchStloc(5)
				)) {
				throw new Exception("Failed to find instructions IL_Liquid_LiquidCheck 1/3");
			}

			c.EmitLdarg(0);
			c.EmitLdarg(1);
			c.EmitLdarg(2);
			c.EmitDelegate((int x, int y, int thisLiquidType) => {
				if (ES_WorldGen.SkyblockWorld)
					return MergeLiquidsShouldDoVanilla(x, y, thisLiquidType);

				return true;
			});

			var label = c.DefineLabel();
			c.Emit(OpCodes.Brtrue_S, label);
			c.Emit(OpCodes.Ret);
			c.MarkLabel(label);

			//IL_0341: ldloca.s 3
			//IL_0343: call instance bool Terraria.Tile::active()
			//IL_0348: ldc.i4.0
			//IL_0349: ceq
			//IL_034b: ldloc.s 13
			//IL_034d: or
			//IL_034e: brtrue.s IL_0351

			if (!c.TryGotoNext(MoveType.After,
				i => i.MatchLdloca(3),
				i => i.MatchCall(out _),
				i => i.MatchLdcI4(0),
				i => i.MatchCeq(),
				i => i.MatchLdloc(13),
				i => i.MatchOr()
				)) {
				throw new Exception("Failed to find instructions IL_Liquid_LiquidCheck 2/3");
			}

			var label3 = c.DefineLabel();
			c.Remove();
			c.Emit(OpCodes.Brtrue_S, label3);

			//IL_0351: ldloca.s 4
			//IL_0353: call instance uint8 & Terraria.Tile::get_liquid()
			//IL_0358: ldind.u1
			//IL_0359: ldc.i4.s 24

			if (!c.TryGotoNext(MoveType.Before,
				i => i.MatchLdloca(4),
				i => i.MatchCall(out _),
				i => i.MatchLdindU1(),
				i => i.MatchLdcI4(24)
				)) {
				throw new Exception("Failed to find instructions IL_Liquid_LiquidCheck 2/3");
			}

			c.MarkLabel(label3);
			c.EmitLdarg(0);
			c.EmitLdarg(1);
			c.EmitLdarg(2);
			c.EmitDelegate((int x, int y, int thisLiquidType) => {
				if (ES_WorldGen.SkyblockWorld)
					return MergeLiquidsShouldDoVanillaDownOnly(x, y, thisLiquidType);

				return true;
			});

			var label2 = c.DefineLabel();
			c.Emit(OpCodes.Brtrue_S, label2);
			c.Emit(OpCodes.Ret);
			c.MarkLabel(label2);
		}
		private static bool MovePrevented(Tile tile) => tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
		private static bool WillMerge(Tile tile) => tile.LiquidAmount > 0 && tile.LiquidType != tile.LiquidType;
		private static void CheckMoveLiquids(int x, int y) {
			Tile tile = Main.tile[x, y];
			if (tile.LiquidAmount > 0) {
				float num = 0f;
				int numLeft = 0;
				int numRight = 0;
				int liquidAmount = 0;
				for (int i = 1; i <= 3; i++) {
					int leftX = x - i;
					Tile tileL = Main.tile[leftX, y];
					bool leftCanMove = true;
					if (MovePrevented(tileL) || WillMerge(tileL)) {
						leftCanMove = false;
					}
					else if (i > 1 && tileL.LiquidAmount == 0) {
						leftCanMove = false;
					}
					else if (i == 2 && tile.LiquidAmount > 250) {
						leftCanMove = false;
					}

					if (CanMove(x, y, leftX, y, leftCanMove) ?? leftCanMove) {
						numLeft++;
						liquidAmount += tileL.LiquidAmount;
					}

					int rightX = x + i;
					Tile tileR = Main.tile[rightX, y];
					bool rightCanMove = true;
					if (MovePrevented(tileR) || WillMerge(tileR)) {
						rightCanMove = false;
					}
					else if (i > 1 && tileR.LiquidAmount == 0) {
						rightCanMove = false;
					}
					else if (i == 2 && tile.LiquidAmount > 250) {
						rightCanMove = false;
					}

					if (CanMove(x, y, rightX, y, rightCanMove) ?? rightCanMove) {
						numRight++;
						liquidAmount += tileR.LiquidAmount;
					}

					if (!leftCanMove || !rightCanMove)
						break;
				}

				num += tile.LiquidAmount + liquidAmount;
				if (tile.LiquidAmount < 3)
					num--;

				byte newAmount = (byte)Math.Round(num / (float)(1 + numLeft + numRight));
				if (newAmount == byte.MaxValue - 1 && WorldGen.genRand.Next(30) == 0)
					newAmount = byte.MaxValue;

				bool anyUpdated = false;
				int higherNum = Math.Max(numLeft, numRight);
				for (int i = 1; i <= higherNum; i++) {
					if (i <= numLeft) {
						int tileX = x - i;
						Tile tileL = Main.tile[tileX, y];
						tileL.LiquidType = tile.LiquidType;
						if (tileL.LiquidAmount != newAmount || tile.LiquidAmount != newAmount) {
							tileL.LiquidAmount = newAmount;
							Liquid.AddWater(tileX, y);
							anyUpdated = true;
						}
					}

					if (i <= numRight) {
						int tileX = x + i;
						Tile tileLR = Main.tile[tileX, y];
						tileLR.LiquidType = tile.LiquidType;
						if (tileLR.LiquidAmount != newAmount || tile.LiquidAmount != newAmount) {
							tileLR.LiquidAmount = newAmount;
							Liquid.AddWater(tileX, y);
							anyUpdated = true;
						}
					}
				}

				if (anyUpdated || numLeft < 2 && numRight < 2 || Main.tile[x, y - 1].LiquidAmount <= 0)
					tile.LiquidAmount = newAmount;
			}
		}
		private static List<Point> combinePoints = new();
		private static List<CombineInfo> combineInfos = new();
		public class CombineInfo {
			public int InitialDelay;
			public int Delay;
			public Action Action;
			public CombineInfo(int delay, Action action) {
				InitialDelay = delay;
				Delay = delay;
				Action = action;
			}
		}
		public static void Update() {
			for (int i = combineInfos.Count - 1; i >= 0; i--) {
				CombineInfo combineInfo = combineInfos[i];
				ref int delay = ref combineInfo.Delay;
				delay--;
				if (delay <= 0) {
					combineInfo.Action();
					combinePoints.RemoveAt(i);
					combineInfos.RemoveAt(i);
				}
			}
		}
		private const int combineDelay = 30;
		private const int convertDelay = 15;
		public class TileChangeFromLiquid {
			public Func<int, bool> CheckTileType;
			public Func<int, bool> CheckLiquidType;
			public Func<int, int> ResultTileType;
			/// <summary>
			/// The default other liquid type to combine with for sound is the liquid that was valid, so pick a different one if you want a sound, or the same if no sound.
			/// </summary>
			public int CombineLiquidForSound;
			public TileChangeFromLiquid(Func<int, bool> tileType, Func<int, bool> liquidType, Func<int, int> resultTileType, int combineLiquidForSound) {
				CheckTileType = tileType;
				CheckLiquidType = liquidType;
				ResultTileType = resultTileType;
				CombineLiquidForSound = combineLiquidForSound;
			}
			public TileChangeFromLiquid(Func<int, bool> tileType, int liquidType, Func<int, int> resultTileType, int combineLiquidForSound) {
				CheckTileType = tileType;
				CheckLiquidType = (type) => type == liquidType;
				ResultTileType = resultTileType;
				CombineLiquidForSound = combineLiquidForSound;
			}
			public TileChangeFromLiquid(int tileType, Func<int, bool> liquidType, Func<int, int> resultTileType, int combineLiquidForSound) {
				CheckTileType = (type) => type == tileType;
				CheckLiquidType = liquidType;
				ResultTileType = resultTileType;
				CombineLiquidForSound = combineLiquidForSound;
			}
			public TileChangeFromLiquid(int tileType, int liquidType, Func<int, int> resultTileType, int combineLiquidForSound) {
				CheckTileType = (type) => type == tileType;
				CheckLiquidType = (type) => type == liquidType;
				ResultTileType = resultTileType;
				CombineLiquidForSound = combineLiquidForSound;
			}
			public TileChangeFromLiquid(Func<int, bool> tileType, Func<int, bool> liquidType, int resultTileType, int combineLiquidForSound) {
				CheckTileType = tileType;
				CheckLiquidType = liquidType;
				ResultTileType = (type) => resultTileType;
				CombineLiquidForSound = combineLiquidForSound;
			}
			public TileChangeFromLiquid(Func<int, bool> tileType, int liquidType, int resultTileType, int combineLiquidForSound) {
				CheckTileType = tileType;
				CheckLiquidType = (type) => type == liquidType;
				ResultTileType = (type) => resultTileType;
				CombineLiquidForSound = combineLiquidForSound;
			}
			public TileChangeFromLiquid(int tileType, Func<int, bool> liquidType, int resultTileType, int combineLiquidForSound) {
				CheckTileType = (type) => type == tileType;
				CheckLiquidType = liquidType;
				ResultTileType = (type) => resultTileType;
				CombineLiquidForSound = combineLiquidForSound;
			}
			public TileChangeFromLiquid(int tileType, int liquidType, int resultTileType, int combineLiquidForSound) {
				CheckTileType = (type) => type == tileType;
				CheckLiquidType = (type) => type == liquidType;
				ResultTileType = (type) => resultTileType;
				CombineLiquidForSound = combineLiquidForSound;
			}
			public TileChangeFromLiquid(Func<int, bool> tileType, Func<int, bool> liquidType, int combineLiquidForSound) {
				CheckTileType = tileType;
				CheckLiquidType = liquidType;
				ResultTileType = (type) => type;
				CombineLiquidForSound = combineLiquidForSound;
			}
		}
		private static List<TileChangeFromLiquid> tilesThatChangeFromLiquids = new() {
			new((tileType) => TileID.Sets.Conversion.Sand[tileType], LiquidID.Lava, (tileType) => SandConversions.TryGetValue(tileType, out int dictType) ? dictType : TileID.Sandstone, LiquidID.Water),
			new(TileID.SnowBlock, LiquidID.Water, TileID.Slush, LiquidID.Honey),
		};
		public class TileChangeFromMultiLiquid {
			public Func<int, bool> CheckTileType;
			/// <summary>
			/// The first 2 are used to determine the sound, so make sure they are the desired ones.  Providing less than 2 will cause an error.
			/// </summary>
			public List<int> LiquidTypes;
			public Func<int, int> ResultTileType;
			public TileChangeFromMultiLiquid(Func<int, bool> tileType, IEnumerable<int> liquidTypes, Func<int, int> resultTileType) {
				CheckTileType = tileType;
				LiquidTypes = liquidTypes.ToList();
				ResultTileType = resultTileType;
			}
			public TileChangeFromMultiLiquid(int tileType, IEnumerable<int> liquidTypes, Func<int, int> resultTileType) {
				CheckTileType = (type) => type == tileType;
				LiquidTypes = liquidTypes.ToList();
				ResultTileType = resultTileType;
			}
			public TileChangeFromMultiLiquid(Func<int, bool> tileType, IEnumerable<int> liquidTypes, int resultTileType) {
				CheckTileType = tileType;
				LiquidTypes = liquidTypes.ToList();
				ResultTileType = (type) => resultTileType;
			}
			public TileChangeFromMultiLiquid(int tileType, IEnumerable<int> liquidTypes, int resultTileType) {
				CheckTileType = (type) => type == tileType;
				LiquidTypes = liquidTypes.ToList();
				ResultTileType = (type) => resultTileType;
			}

			public bool CheckLiquidTypes(Tile[] liquidTiles, int start, int endNotInclusive) {
				bool[] found = new bool[LiquidTypes.Count];
				for (int i = start; i < endNotInclusive; i++) {
					Tile liquidTile = liquidTiles[i];
					if (liquidTile.LiquidAmount <= 0)
						continue;

					for (int j = 0; j < LiquidTypes.Count; j++) {
						if (LiquidTypes[j] == liquidTile.LiquidType) {
							found[j] = true;
							break;
						}
					}
				}

				for (int i = 0; i < found.Length; i++) {
					if (!found[i])
						return false;
				}

				return true;
			}
		}
		private static List<int> LavaAndWater = new() { LiquidID.Lava, LiquidID.Water };
		private static List<int> HoneyAndWater = new() { LiquidID.Water, LiquidID.Honey };
		private static List<int> LavaAndHoney = new() { LiquidID.Lava, LiquidID.Honey };
		private static List<int> ShimmerAndWater = new() { LiquidID.Shimmer, LiquidID.Water };
		private static List<int> ShimmerAndHoney = new() { LiquidID.Shimmer, LiquidID.Honey };
		private static List<int> ShimmerAndLava = new() { LiquidID.Shimmer, LiquidID.Lava };
		private static List<int> LavaAndWaterAndHoney = new() { LiquidID.Lava, LiquidID.Water, LiquidID.Honey };
		private static List<int> ShimmerAndWaterAndHoney = new() { LiquidID.Shimmer, LiquidID.Water, LiquidID.Honey };
		private static List<int> ShimmerAndLavaAndHoney = new() { LiquidID.Shimmer, LiquidID.Lava, LiquidID.Honey };
		private static List<int> ShimmerAndLavaAndWater = new() { LiquidID.Shimmer, LiquidID.Lava, LiquidID.Water };
		private static List<int> ShimmerAndLavaAndWaterAndHoney = new() { LiquidID.Shimmer, LiquidID.Lava, LiquidID.Water, LiquidID.Honey };
		private static List<TileChangeFromMultiLiquid> tilesThatChangeFromMultipleLiquids = new() {
			new(TileID.Granite, LavaAndWater, TileID.Marble),
			new(TileID.HayBlock, LavaAndHoney, TileID.Ash)
		};
		/// <param name="canMoveVanilla">Passed in to allow forcing a liquid to move even if vanilla wouldn't allow it.</param>
		private static bool? CanMove(int x, int y, int xMove, int yMove, bool canMoveVanilla) {
			if (!ES_WorldGen.SkyblockWorld)
				return null;

			Tile tile = Main.tile[x, y];
			int oppositeX = xMove - (x - xMove);
			if (oppositeX < 0 || oppositeX >= Main.maxTilesX)
				return null;

			Tile moveTile = Main.tile[xMove, yMove];
			Tile opposite = Main.tile[oppositeX, yMove];
			if (moveTile.HasTile) {
				foreach (TileChangeFromLiquid tileChange in tilesThatChangeFromLiquids) {
					if (tileChange.CheckTileType(moveTile.TileType) && (tileChange.CheckLiquidType(moveTile.LiquidType) || tileChange.CheckLiquidType(opposite.LiquidType))) {
						TryUpdateCombineInfo(xMove, yMove, convertDelay);

						return false;
					}
				}

				foreach (TileChangeFromMultiLiquid tileChange in tilesThatChangeFromMultipleLiquids) {
					if (tileChange.CheckTileType(moveTile.TileType) && tileChange.LiquidTypes.Contains(tile.LiquidType)) {
						TryUpdateCombineInfo(xMove, yMove, convertDelay);

						return false;
					}
				}

				return null;
			}

			if (opposite.LiquidAmount > 0 && tile.LiquidType != opposite.LiquidType) {
				foreach (StoneGenerator stoneGenerator in StoneGenerators) {
					if (stoneGenerator.CheckLiquids(tile, opposite)) {
						TryUpdateCombineInfo(xMove, yMove, stoneGenerator.Delay);
						break;
					}
				}

				return false;
			}


			if (yMove == Main.maxTilesY - 1)
				return null;

			int downY = yMove + 1;
			Tile down = Main.tile[xMove, downY];
			if (down.HasTile) {
				foreach (TileChangeFromLiquid tileChange in tilesThatChangeFromLiquids) {
					if (tileChange.CheckTileType(down.TileType) && tileChange.CheckLiquidType(moveTile.LiquidType)) {
						TryUpdateCombineInfo(xMove, downY, convertDelay);

						return null;
					}
				}

				foreach (TileChangeFromMultiLiquid tileChange in tilesThatChangeFromMultipleLiquids) {
					if (tileChange.CheckTileType(down.TileType) && tileChange.LiquidTypes.Contains(moveTile.LiquidType)) {
						TryUpdateCombineInfo(xMove, yMove, convertDelay);

						return null;
					}
				}
			}

			return null;
		}
		public static void TryUpdateCombineInfo(int x, int y, int delay = convertDelay) {
			Point combinePoint = new(x, y);
			int index = combinePoints.IndexOf(combinePoint);
			if (index == -1) {
				combinePoints.Add(combinePoint);
				combineInfos.Add(new CombineInfo(combineDelay, () => {
					CheckCombineLiquids(x, y);
				}));
			}
			else {
				CombineInfo combineInfo = combineInfos[index];
				if (combineInfo.InitialDelay > delay) {
					combineInfo.Delay -= combineInfo.InitialDelay - delay;
					combineInfo.InitialDelay = delay;
				}
			}
		}
		private static SortedDictionary<int, int> SandConversions = new() {
			{ TileID.Sand, TileID.Sandstone },
			{ TileID.Crimsand, TileID.CrimsonSandstone },
			{ TileID.Ebonsand, TileID.CorruptSandstone },
			{ TileID.Pearlsand, TileID.HallowSandstone }
		};
		public class StoneGenerator {
			public Func<int> Result;
			public List<int> Liquids;
			public int Delay;
			public StoneGenerator(Func<int> result, List<int> liquids, int delay = combineDelay) {
				Result = result;
				Liquids = liquids;
				Delay = delay;
			}
			public StoneGenerator(int result, List<int> liquids, int delay = combineDelay) : this(() => result, liquids, delay) { }

			public bool CheckLiquids(Tile left, Tile right) {
				bool[] found = new bool[Liquids.Count];
				for (int i = 0; i < Liquids.Count; i++) {
					if (left.LiquidType == Liquids[i]) {
						found[i] = true;
					}
					else if (right.LiquidType == Liquids[i]) {
						found[i] = true;
					}
				}

				bool foundAll = true;
				foreach (bool b in found) {
					if (!b) {
						foundAll = false;
						break;
					}
				}

				return foundAll;
			}
		}
		private static List<StoneGenerator> StoneGenerators = new() {
			new(() => Main.rand.Next(5) == 0 ? TileID.Silt : TileID.Stone, LavaAndWater),
			new(TileID.CrispyHoneyBlock, LavaAndHoney, combineDelay * 2),
			new(TileID.HoneyBlock, HoneyAndWater, combineDelay * 2),
			new(TileID.ShimmerBlock, ShimmerAndWater),
			//new(TileID., ShimmerAndLava),
			//new(TileID., ShimmerAndHoney),
		};
		private static void CheckCombineLiquids(int x, int y) {
			if (x <= 0 || x >= Main.maxTilesX - 1 || y <= 0 || y >= Main.maxTilesY - 1)
				return;

			Tile[] tiles = DirectionID.GetTiles(x, y);
			Tile tile = tiles[DirectionID.None];
			if (tile.HasTile) {
				foreach (TileChangeFromLiquid tileChange in tilesThatChangeFromLiquids) {
					if (tileChange.CheckTileType(tile.TileType)) {
						for (int i = DirectionID.None; i < DirectionID.Down; i++) {
							Tile liquidTile = tiles[i];
							if (liquidTile.LiquidAmount > 0 && tileChange.CheckLiquidType(liquidTile.LiquidType)) {
								PlaceBlockFromLiquidMerge(x, y, tileChange.ResultTileType(tile.TileType), liquidTile.LiquidType, tileChange.CombineLiquidForSound);
								return;
							}
						}
					}
				}

				foreach (TileChangeFromMultiLiquid tileChange in tilesThatChangeFromMultipleLiquids) {
					if (tileChange.CheckTileType(tile.TileType)) {
						if (tileChange.CheckLiquidTypes(tiles, DirectionID.None + 1, DirectionID.Down)) {
							PlaceBlockFromLiquidMerge(x, y, tileChange.ResultTileType(tile.TileType), tileChange.LiquidTypes[0], tileChange.LiquidTypes[1]);

							return;
						}
					}
				}

				return;
			}

			Tile left = tiles[DirectionID.Left];
			if (left.LiquidAmount > 0) {
				Tile right = tiles[DirectionID.Right];
				if (right.LiquidAmount > 0) {
					foreach (StoneGenerator stoneGenerator in StoneGenerators) {
						if (stoneGenerator.CheckLiquids(left, right))
							PlaceBlockFromLiquidMerge(x, y, stoneGenerator.Result(), left.LiquidType, right.LiquidType);
					}
				}
			}
		}
		private static bool Lava(this Tile tile) => tile.LiquidAmount > 0 && tile.LiquidType == LiquidID.Lava;
		private static bool Water(this Tile tile) => tile.LiquidAmount > 0 && tile.LiquidType == LiquidID.Water;
		private static void PlaceBlockFromLiquidMerge(int x, int y, int tileType, int liquidType, int otherLiquidType) {
			WorldGen.PlaceTile(x, y, tileType, true, true);
			TileChangeType liquidChangeType = WorldGen.GetLiquidChangeType(liquidType, otherLiquidType);
			WorldGen.PlayLiquidChangeSound(liquidChangeType, x, y);
			WorldGen.SquareTileFrame(x, y);
			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendTileSquare(-1, x - 1, y - 1, 3, liquidChangeType);
		}
	}
}
