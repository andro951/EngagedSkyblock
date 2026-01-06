using androLib.Common.Utility;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace EngagedSkyblock.Tiles {
	public class SunTotem : ModTile {
		public override void SetStaticDefaults() {
			base.SetStaticDefaults();
			Main.tileNoAttach[Type] = true;
			Main.tileLavaDeath[Type] = false;
			Main.tileFrameImportant[Type] = true;
			TileID.Sets.IgnoredByNpcStepUp[Type] = true;

			AdjTiles = new int[] { Type };
			Color mapColor = Color.Brown;
			mapColor.A = byte.MaxValue;
			AddMapEntry(mapColor, CreateMapEntryName());

			TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
			TileObjectData.newTile.CoordinateHeights = new[] { 16, 16 };
			TileObjectData.newTile.AnchorInvalidTiles = new int[] {
				TileID.MagicalIceBlock,
				TileID.Boulder,
				TileID.BouncyBoulder,
				TileID.LifeCrystalBoulder,
				TileID.RollingCactus
			};
			TileObjectData.newTile.StyleHorizontal = true;
			TileObjectData.newTile.LavaDeath = false;
			TileObjectData.newTile.DrawYOffset = 2;
			TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide | AnchorType.Table, TileObjectData.newTile.Width, 0);
			TileObjectData.addTile(Type);
		}

		public override string Texture => (GetType().Namespace + ".Sprites." + Name).Replace('.', '/');
		private static readonly Point totemDefaultLocation = new(0, 0);
		private static Point totemLocation = totemDefaultLocation;
		public override void RandomUpdate(int i, int j) {
			totemLocation = new Point(i, j);
		}
		public override void NearbyEffects(int i, int j, bool closer) {
			totemLocation = new Point(i, j);
		}
		public static bool TotemActive() {
			Tile tile = Main.tile[totemLocation.X, totemLocation.Y];
			return tile.HasTile && tile.TileType == ModContent.TileType<SunTotem>();
		}
	}
}
