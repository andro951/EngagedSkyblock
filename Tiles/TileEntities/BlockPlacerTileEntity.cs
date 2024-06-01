using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;

namespace EngagedSkyblock.Tiles.TileEntities {
	internal class BlockPlacerTileEntity : ModTileEntity {
		public override int Hook_AfterPlacement(int i, int j, int type, int style, int direction, int alternate) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				// Sync the entire multitile's area.  Modify "width" and "height" to the size of your multitile in tiles
				int width = 1;
				int height = 1;
				NetMessage.SendTileSquare(Main.myPlayer, i, j, width, height);

				// Sync the placement of the tile entity with other clients
				// The "type" parameter refers to the tile type which placed the tile entity, so "Type" (the type of the tile entity) needs to be used here instead
				NetMessage.SendData(MessageID.TileEntityPlacement, number: i, number2: j, number3: Type);
			}

			int placedEntity = Place(i, j);
			return placedEntity;
		}
		public override bool IsTileValidForEntity(int x, int y) {
			ModTile modTile = ModContent.GetModTile(Main.tile[x, y].TileType);
			return modTile is Tiles.BlockPlacer;
		}
		public override void OnNetPlace() {
			if (Main.netMode == NetmodeID.Server) {
				NetMessage.SendData(MessageID.TileEntitySharing, number: ID, number2: Position.X, number3: Position.Y);
			}
		}
		//private int timer = 0;
		public override void Update() {
			int x = Position.X;
			int y = Position.Y;
			Tile tile = Main.tile[x, y];
			if (tile.BlueWire || tile.YellowWire || tile.RedWire || tile.GreenWire)
				return;

			ModTile modTile = ModContent.GetModTile(Main.tile[x, y].TileType);
			if (modTile is Tiles.BlockPlacer blockPlacerModTile)
				blockPlacerModTile.HitWire(x, y);
		}
	}
}
