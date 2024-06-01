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
	public class RainTotem : ModTile {
		public static void Load() {
			IL_Main.UpdateTime += IL_Main_UpdateTime;
		}
		private static void IL_Main_UpdateTime(ILContext il) {
			//// if (Main.rand.NextDouble() <= 1.0 / (num2 * 5.75))
			//IL_01fa: call class Terraria.Utilities.UnifiedRandom Terraria.Main::get_rand()
			//IL_01ff: callvirt instance float64 Terraria.Utilities.UnifiedRandom::NextDouble()
			//IL_0204: ldc.r8 1
			//IL_020d: ldloc.3
			//IL_020e: ldc.r8 5.75
			//IL_0217: mul
			//IL_0218: div

			var c = new ILCursor(il);

			if (!c.TryGotoNext(MoveType.After,
				i => i.MatchCall(typeof(Main), "get_rand"),
				i => i.MatchCallvirt(typeof(Terraria.Utilities.UnifiedRandom), "NextDouble"),
				i => i.MatchLdcR8(1.0),
				i => i.MatchLdloc(3),
				i => i.MatchLdcR8(5.75),
				i => i.MatchMul(),
				i => i.MatchDiv()
				)) {
				throw new Exception("Failed to find instructions IL_Main_UpdateTime");
			}

			c.EmitDelegate(ModifyRainChance);
		}
		private static double ModifyRainChance(double chanceDenom) {
			if (!ES_WorldGen.SkyblockWorld)
				return chanceDenom;

			if (TotemActive()) {
				chanceDenom *= 10d;
			}

			return chanceDenom;
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
			TileObjectData.newTile.DrawYOffset = 2;
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
