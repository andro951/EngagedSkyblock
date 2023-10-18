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
	public class RainTotem : ModTile {
		public static void Load() {
			IL_Main.UpdateTime += IL_Main_UpdateTime;
		}
		private static void IL_Main_UpdateTime(ILContext il) {
			//// double num = 86400.0 / Main.desiredWorldEventsUpdateRate / 24.0;
			//IL_0143: ldc.r8 86400
			//IL_014c: ldsfld float64 Terraria.Main::desiredWorldEventsUpdateRate
			//IL_0151: div
			//IL_0152: ldc.r8 24
			//IL_015b: div

			var c = new ILCursor(il);

			if (!c.TryGotoNext(MoveType.After,
				i => i.MatchLdcR8(86400),
				i => i.MatchLdsfld(typeof(Main), nameof(Main.desiredWorldEventsUpdateRate)),
				i => i.MatchDiv(),
				i => i.MatchLdcR8(24),
				i => i.MatchDiv()
				)) {
				throw new Exception("Failed to find instructions IL_Main_UpdateTime");
			}

			c.EmitDelegate(ModifyRainChance);
		}
		private static double ModifyRainChance(double chance) {
			if (!ES_WorldGen.SkyblockWorld)
				return chance;

			if (TotemActive()) {
				chance /= 10d;
			}

			return chance;
		}
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
			TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide | AnchorType.Table, TileObjectData.newTile.Width, 0);
			TileObjectData.addTile(Type);
		}

		public override string Texture => (GetType().Namespace + ".Sprites." + Name).Replace('.', '/');
		private static readonly Point totemDefaultLocation = new Point(0, 0);
		private static Point totemLocaion = totemDefaultLocation;
		public override void RandomUpdate(int i, int j) {
			totemLocaion = new Point(i, j);
		}
		public override void NearbyEffects(int i, int j, bool closer) {
			totemLocaion = new Point(i, j);
		}
		public static bool TotemActive() {
			Tile tile = Main.tile[totemLocaion.X, totemLocaion.Y];
			return tile.HasTile && tile.TileType == ModContent.TileType<RainTotem>();
		}
	}
}
