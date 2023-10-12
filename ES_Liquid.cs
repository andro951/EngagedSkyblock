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
using Terraria;
using Terraria.ID;

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
			Tile tile = Main.tile[x, y];
			Tile up = Main.tile[x, y - 1];
			Tile left = Main.tile[x - 1, y];
			Tile right = Main.tile[x + 1, y];
			int[] liquids = new int[4];
			liquids[thisLiquidType] += tile.LiquidAmount;
			if (left.LiquidAmount > 0 && left.LiquidType != thisLiquidType) {
				liquids[left.LiquidType] += left.LiquidAmount;
				left.LiquidAmount = 0;
			}

			if (right.LiquidAmount > 0 && right.LiquidType != thisLiquidType) {
				liquids[right.LiquidType] += right.LiquidAmount;
				right.LiquidAmount = 0;
			}

			if (up.LiquidAmount > 0 && up.LiquidType != thisLiquidType) {
				liquids[up.LiquidType] += up.LiquidAmount;
				up.LiquidAmount = 0;
			}

			return MergeLiquidsShouldDoVanilla(x, y, thisLiquidType, liquids);
		}
		private static bool MergeLiquidsShouldDoVanillaDownOnly(int x, int y, int thisLiquidType) {
			Tile tile = Main.tile[x, y];
			Tile down = Main.tile[x, y + 1];
			int[] liquids = new int[4];
			liquids[thisLiquidType] += tile.LiquidAmount;
			if (down.LiquidAmount > 0 && down.LiquidType != thisLiquidType) {
				liquids[down.LiquidType] += down.LiquidAmount;
				down.LiquidAmount = 0;
			}

			return MergeLiquidsShouldDoVanilla(x, y + 1, thisLiquidType, liquids);
		}
		private static bool MergeLiquidsShouldDoVanilla(int x, int y, int thisLiquidType, int[] liquids) {
			Liquid.GetLiquidMergeTypes(thisLiquidType, out int liquidMergeTileType, out int liquidMergeType, liquids[LiquidID.Water] > 0, liquids[LiquidID.Lava] > 0, liquids[LiquidID.Honey] > 0, liquids[LiquidID.Shimmer] > 0);
			if (liquidMergeType == thisLiquidType)
				return false;

			int lavaCount = liquids[LiquidID.Lava];
			switch (liquidMergeTileType) {
				case TileID.Obsidian:
					int tileType = lavaCount < 240 ? lavaCount < 64 ? TileID.Stone : TileID.Silt : TileID.Obsidian;
					PlaceBlockFromLiquidMerge(x, y, tileType, thisLiquidType, liquidMergeType);
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

					if (leftCanMove && CanMove(x, y, leftX, y)) {
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

					if (rightCanMove && CanMove(x, y, rightX, y)) {
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
		private static List<int> combineCooldowns = new();
		private static List<Action> combineActions = new();
		public static void Update() {
			if (!ES_WorldGen.SkyblockWorld)
				return;

			for (int i = combineCooldowns.Count - 1; i >= 0; i--) {
				combineCooldowns[i]--;
				if (combineCooldowns[i] <= 0) {
					combineActions[i]();
					combineCooldowns.RemoveAt(i);
					combinePoints.RemoveAt(i);
					combineActions.RemoveAt(i);
				}
			}
		}
		private static readonly int combineCooldown = 60;
		private static bool CanMove(int x, int y, int xMove, int yMove) {
			if (!ES_WorldGen.SkyblockWorld)
				return true;

			Tile tile = Main.tile[x, y];
			int oppositeX = xMove - (x - xMove);
			Tile opposite = Main.tile[oppositeX, yMove];
			if (opposite.LiquidAmount > 0 && tile.LiquidType != opposite.LiquidType) {
				if (tile.LiquidType == LiquidID.Water && opposite.LiquidType == LiquidID.Lava || tile.LiquidType == LiquidID.Lava && opposite.LiquidType == LiquidID.Water) {
					Point combinePoint = new(xMove, yMove);
					if (!combinePoints.Contains(combinePoint)) {
						combinePoints.Add(combinePoint);
						combineCooldowns.Add(combineCooldown);
						combineActions.Add(() => {
							CheckCombineLiquids(xMove, yMove);
						});
					}

					return false;
				}
			}

			return true;
		}
		private static void CheckCombineLiquids(int x, int y) {
			Tile tile = Main.tile[x, y];
			if (tile.HasTile)
				return;

			Tile left = Main.tile[x - 1, y];
			if (left.LiquidAmount > 0) {
				bool water = left.LiquidType == LiquidID.Water;
				if (water || left.LiquidType == LiquidID.Lava) {
					Tile right = Main.tile[x + 1, y];
					if (right.LiquidAmount > 0) {
						int requiredType = water ? LiquidID.Lava : LiquidID.Water;
						if (right.LiquidType == requiredType) {
							int tileType = Main.rand.Next(5) == 0 ? TileID.Silt : TileID.Stone;
							PlaceBlockFromLiquidMerge(x, y, tileType, left.LiquidType, right.LiquidType);
						}
					}
				}
			}
		}
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
