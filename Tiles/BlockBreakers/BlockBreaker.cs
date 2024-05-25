using androLib.Common.Utility;
using EngagedSkyblock.Tiles.TileEntities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using static Terraria.HitTile;

namespace EngagedSkyblock.Tiles {
	public abstract class BlockBreaker : ModTile {
		protected class PickaxePowerID {
			public const int Wood = 1;

			public const int Copper = 35;
			public const int Iron = 40;
			public const int Lead = 43;
			public const int Silver = 45;
			public const int Tungsten = 50;
			public const int Gold = 55;
			public const int Platinum = 59;
			public const int Nightmare = 65;
			public const int Deathbringer = 70;
			public const int Molten = 100;
			public const int Cobalt = 110;
			public const int Palladium = 130;
			public const int Mythril = 150;
			public const int Orichalcum = 165;
			public const int Adamantite = 180;
			public const int Titanium = 190;
			public const int Chlorophyte = 200;
			public const int Picksaw = 210;
			public const int Luminite = 225;
		}
		public abstract int pickaxePower { get; }
		public abstract int miningCooldown { get; }
		protected virtual Color MapColor => Color.Gray;
		protected ModTileEntity Entity => ModContent.GetInstance<BlockBreakerTileEntity>();
		public override void SetStaticDefaults() {
			base.SetStaticDefaults();
			TileID.Sets.DrawsWalls[Type] = true;
			TileID.Sets.DontDrawTileSliced[Type] = true;
			TileID.Sets.IgnoresNearbyHalfbricksWhenDrawn[Type] = true;

			//Main.tileNoAttach[Type] = true;
			Main.tileLavaDeath[Type] = false;
			Main.tileFrameImportant[Type] = true;
			Main.tileSolid[Type] = true;
			Main.tileBlockLight[Type] = true;
			//Main.tileSolidTop[Type] = true;

			Color mapColor = MapColor;
			mapColor.A = byte.MaxValue;
			AddMapEntry(mapColor, CreateMapEntryName());

			TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
			TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(Entity.Hook_AfterPlacement, -1, 0, false);
			//TileObjectData.newTile.anch
			//TileObjectData.newTile.CoordinateHeights = new[] { 16 };
			//TileObjectData.newTile.AnchorInvalidTiles = new int[] {
			//	TileID.MagicalIceBlock,
			//	TileID.Boulder,
			//	TileID.BouncyBoulder,
			//	TileID.LifeCrystalBoulder,
			//	TileID.RollingCactus
			//};
			TileObjectData.newTile.StyleHorizontal = true;
			TileObjectData.newTile.LavaDeath = false;
			//TileObjectData.newTile.CoordinateWidth = 16;
			//TileObjectData.newTile.CoordinatePadding = 2;
			TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.EmptyTile | AnchorType.SolidSide | AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide | AnchorType.Table | AnchorType.None | AnchorType.Tree | AnchorType.SolidBottom | AnchorType.PlatformNonHammered | AnchorType.AlternateTile | AnchorType.PlanterBox | AnchorType.Platform, TileObjectData.newTile.Width, 0);

			//TileObjectData.newTile.AnchorBottom = 
			//TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide | AnchorType.Table, TileObjectData.newTile.Width, 0);
			TileObjectData.addTile(Type);
		}
		public override void Load() {
			On_TileObject.DrawPreview += On_TileObject_DrawPreview;
			On_WorldGen.KillTile_GetItemDrops += On_WorldGen_KillTile_GetItemDrops;
		}
		private SortedDictionary<int, Point> chestDatas = new();
		private void On_WorldGen_KillTile_GetItemDrops(On_WorldGen.orig_KillTile_GetItemDrops orig, int x, int y, Tile tileCache, out int dropItem, out int dropItemStack, out int secondaryItem, out int secondaryItemStack, bool includeLargeObjectDrops) {
			orig(x, y, tileCache, out dropItem, out dropItemStack, out secondaryItem, out secondaryItemStack, includeLargeObjectDrops);
			if (dropItem <= ItemID.None && secondaryItem <= ItemID.None)
				return;

			//Intentionally allowed even if not SkyblockWorld
			if (breakingTileX == x && breakingTileY == y) {
				int breakerFacingDirection = GetDirectionID(blockBreakerX, blockBreakerY);
				SortedSet<int> foundChests = new();
				for (int directionID = 0; directionID < 4; directionID++) {
					if (directionID == breakerFacingDirection)
						continue;

					PathDirectionID.GetDirection(directionID, blockBreakerX, blockBreakerY, out int chestX, out int chestY);
					Tile tile = Main.tile[chestX, chestY];
					if (!TileID.Sets.BasicChest[tile.TileType])
						continue;

					if (chestY > 0) {
						Tile chestUp = Main.tile[chestX, chestY - 1];
						while (tile.TileFrameY > 0 && tile.TileFrameY > chestUp.TileFrameY && chestUp.TileType == tile.TileType && chestUp.TileFrameX == chestUp.TileFrameX) {
							chestY--;
							tile = chestUp;
							if (chestY > 0) {
								chestUp = Main.tile[chestX, chestY - 1];
							}
							else {
								break;
							}
						}
					}

					if (chestX > 0) {
						Tile chestLeft = Main.tile[chestX - 1, chestY];
						while (tile.TileFrameX > 0 && tile.TileFrameX > chestLeft.TileFrameX && chestLeft.TileType == tile.TileType && chestLeft.TileFrameY == chestLeft.TileFrameY) {
							chestX--;
							tile = chestLeft;
							if (chestX > 0) {
								chestLeft = Main.tile[chestX - 1, chestY];
							}
							else {
								break;
							}
						}
					}
					
					Point chestPoint = new(chestX, chestY);
					bool found = false;
					foreach (KeyValuePair<int, Point> p in chestDatas) {
						if (chestPoint == p.Value) {
							found = true;
							foundChests.Add(p.Key);
							break;
						}
					}

					if (found)
						continue;

					int foundChest = Chest.FindChest(chestX, chestY);
					if (foundChest >= 0) {
						foundChests.Add(foundChest);
						chestDatas.Add(foundChest, chestPoint);
					}
				}

				int[] chestNums = chestDatas.Keys.ToArray();
				foreach (int chestNum in chestNums) {
					if (!foundChests.Contains(chestNum)) {
						chestDatas.Remove(chestNum);
					}
				}

				if (dropItem > ItemID.None) {
					foreach (int chestNum in chestDatas.Keys) {
						if (chestNum.TryDepositToChest(dropItem, ref dropItemStack)) {
							dropItem = ItemID.None;
							break;
						}
					}
				}

				if (secondaryItem > ItemID.None) {
					foreach (int chestNum in chestDatas.Keys) {
						if (chestNum.TryDepositToChest(secondaryItem, ref secondaryItemStack)) {
							secondaryItem = ItemID.None;
							break;
						}
					}
				}
			}
		}
		private void On_TileObject_DrawPreview(On_TileObject.orig_DrawPreview orig, SpriteBatch sb, TileObjectPreviewData op, Vector2 position) {
			ModTile modTile = ModContent.GetModTile(op.Type);
			if (modTile is Tiles.BlockBreaker) {
				GetDirectionID(op.Coordinates.X, op.Coordinates.Y, out short directionID);
				op.Style = directionID;
			}

			orig(sb, op, position);
		}

		public override string Texture => (GetType().Namespace + ".Sprites." + Name).Replace('.', '/');
		public override void PlaceInWorld(int i, int j, Item item) {
			Tile tile = Main.tile[i, j];
			GetDirectionID(i, j, out short directionID);
			//Point playerCenterTile = Main.LocalPlayer.Center.ToTileCoordinates();
			//int xDiff = i - playerCenterTile.X;
			//int yDiff = j - playerCenterTile.Y;
			//int directionID;
			//if (Math.Abs(xDiff) >= Math.Abs(yDiff)) {
			//	directionID = xDiff > 0 ? PathDirectionID.Right : PathDirectionID.Left;
			//}
			//else {
			//	directionID = yDiff > 0 ? PathDirectionID.Down : PathDirectionID.Up;
			//}

			SetTileDirection(tile, directionID);
		}
		private static void GetDirectionID(int tileX, int tileY, out short directionID) {
			Point playerCenterTile = Main.LocalPlayer.Center.ToTileCoordinates();
			int xDiff = tileX - playerCenterTile.X;
			int yDiff = tileY - playerCenterTile.Y;
			if (Math.Abs(xDiff) >= Math.Abs(yDiff)) {
				directionID = xDiff > 0 ? (short)PathDirectionID.Right : (short)PathDirectionID.Left;
			}
			else {
				directionID = yDiff > 0 ? (short)PathDirectionID.Down : (short)PathDirectionID.Up;
			}
		}
		private static int GetDirectionID(int x, int y) => Main.tile[x, y].TileFrameX / 18;
		public override bool Slope(int i, int j) {
			Tile tile = Main.tile[i, j];
			short directionID = (short)((tile.TileFrameX / 18 + 1) % 4);
			SetTileDirection(tile, directionID);

			SoundEngine.PlaySound(SoundID.Dig, new Point(i, j).ToWorldCoordinates());

			return false;
		}
		private void SetTileDirection(Tile tile, short directionID) {
			tile.TileFrameX = (short)(directionID * 18);
			if (Main.netMode == NetmodeID.MultiplayerClient)
				NetMessage.SendTileSquare(-1, Player.tileTargetX, Player.tileTargetY, 1, TileChangeType.None);
		}

		protected HitTileObject hitData = new();
		private static int breakingTileX;
		private static int breakingTileY;
		private static int blockBreakerX;
		private static int blockBreakerY;

		public override void HitWire(int i, int j) {
			//Intentionally allowed even if not SkyblockWorld
			Tile tile = Main.tile[i, j];
			int directionID = tile.TileFrameX / 18;
			PathDirectionID.GetDirection(directionID, i, j, out int x, out int y);
			Tile target = Main.tile[x, y];
			if (!WorldGen.CanKillTile(x, y))
				return;

			if (target.HasTile) {
				if (!Wiring.CheckMech(i, j, miningCooldown))
					return;

				if (Main.tileContainer[target.TileType] || TileID.Sets.BasicChest[target.TileType])
					return;

				breakingTileX = x;
				breakingTileY = y;
				blockBreakerX = i;
				blockBreakerY = j;
				WorldGen.KillTile(x, y);
				breakingTileX = -1;
				breakingTileY = -1;
			}

			//Show drill


		}
		public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem) {
			Entity.Kill (i, j);
		}
	}
}
