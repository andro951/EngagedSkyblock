using androLib.Common.Utility;
using EngagedSkyblock.Common.Globals;
using EngagedSkyblock.Items;
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

namespace EngagedSkyblock.Tiles {
	public abstract class BlockPlacer : ModTile {
		public abstract int cooldown { get; }
		protected virtual Color MapColor => Color.Gray;
		protected ModTileEntity Entity => ModContent.GetInstance<BlockPlacerTileEntity>();
		public override void SetStaticDefaults() {
			base.SetStaticDefaults();
			TileID.Sets.DrawsWalls[Type] = true;
			TileID.Sets.DontDrawTileSliced[Type] = true;
			TileID.Sets.IgnoresNearbyHalfbricksWhenDrawn[Type] = true;

			Main.tileLavaDeath[Type] = false;
			Main.tileFrameImportant[Type] = true;
			Main.tileSolid[Type] = true;
			Main.tileBlockLight[Type] = true;

			Color mapColor = MapColor;
			mapColor.A = byte.MaxValue;
			AddMapEntry(mapColor, CreateMapEntryName());

			TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
			TileObjectData.newTile.UsesCustomCanPlace = false;
			TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
			TileObjectData.newTile.StyleHorizontal = true;
			TileObjectData.newTile.LavaDeath = false;
			TileObjectData.addTile(Type);
		}
		private SortedDictionary<int, Point> chestDatas = new();

		public override string Texture => (GetType().Namespace + ".Sprites." + Name).Replace('.', '/');
		public override void PlaceInWorld(int i, int j, Item item) {
			Tile tile = Main.tile[i, j];
			Main.LocalPlayer.GetDirectionID(i, j, out short directionID);

			SetTileDirection(tile, directionID);
			Entity.Hook_AfterPlacement(i, j, tile.TileType, 0, 0, 0);
		}
		private static int GetDirectionID(int x, int y) => Main.tile[x, y].TileFrameX / 18;
		public override bool Slope(int i, int j) {
			Tile tile = Main.tile[i, j];
			short directionID = (short)((tile.TileFrameX / 18 + 3) % 4);
			SetTileDirection(tile, directionID);

			SoundEngine.PlaySound(SoundID.Dig, new Point(i, j).ToWorldCoordinates());

			return false;
		}
		private void SetTileDirection(Tile tile, short directionID) {
			tile.TileFrameX = (short)(directionID * 18);
			if (Main.netMode == NetmodeID.MultiplayerClient)
				NetMessage.SendTileSquare(-1, Player.tileTargetX, Player.tileTargetY, 1, TileChangeType.None);
		}

		public override void HitWire(int i, int j) {
			//Intentionally allowed even if not SkyblockWorld

			int placerFacingDirection = GetDirectionID(i, j);
			PathDirectionID.GetDirection(placerFacingDirection, i, j, out int x, out int y);
			Tile target = Main.tile[x, y];

			if (target.HasTile)
				return;

			SortedSet<int> foundChests = new();
			for (int directionID = 0; directionID < 4; directionID++) {
				if (directionID == placerFacingDirection)
					continue;

				PathDirectionID.GetDirection(directionID, i, j, out int chestX, out int chestY);
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

			if (!Wiring.CheckMech(i, j, cooldown))
				return;

			//Select Block to place
			int tileToPlace = -1;
			foreach (int chestNum in chestDatas.Keys) {
				if (SelectPlacableBlockAndConsumeItem(chestNum, out tileToPlace))
					break;
			}

			if (tileToPlace != -1)
				ES_GlobalTile.PlaceTile(x, y, tileToPlace, false);
		}
		private static bool SelectPlacableBlockAndConsumeItem(int chestNum, out int tileType) {
			tileType = -1;
			foreach (Item item in Main.chest[chestNum].item) {
				if (item.NullOrAir() || item.stack < 1)
					continue;

				int createTile = item.createTile;
				if (createTile == -1)
					continue;

				TileObjectData data = TileObjectData.GetTileData(createTile, 0);
				if (data != null && (data.Width > 1 || data.Height > 1))
					continue;

				item.stack--;
				if (item.stack <= 0)
					item.TurnToAir();

				tileType = createTile;
				return true;
			}

			return false;
		}
		public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem) {
			Entity.Kill(i, j);
		}
	}
}
