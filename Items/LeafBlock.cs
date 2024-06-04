using androLib.Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace EngagedSkyblock.Items {
	public class LeafBlock : ES_ModItem {
		public override void SetDefaults() {
			Item.DefaultToPlaceableTile(TileID.LeafBlock);
			Item.width = 16;
			Item.height = 16;
		}

		public delegate bool orig_PlantLoader_ShakeTree(int treeX, int treeTopY, int treeBottomTileType, ref bool createLeaves);
		public delegate bool hook_PlantLoader_ShakeTree(orig_PlantLoader_ShakeTree orig, int treeX, int treeTopY, int treeBottomTileType, ref bool createLeaves);
		public static readonly MethodInfo PlantLoaderShakeTree = typeof(PlantLoader).GetMethod("ShakeTree", BindingFlags.Static | BindingFlags.Public);
		public static bool PlantLoaderShakeTreeDelegate(orig_PlantLoader_ShakeTree orig, int treeX, int treeTopY, int treeBottomTileType, ref bool createLeaves) {
			bool result = orig(treeX, treeTopY, treeBottomTileType, ref createLeaves);
			ShakeTree(treeX, treeTopY, treeBottomTileType, ref createLeaves);
			return result;
		}
		public static void ShakeTree(int treeX, int treeTopY, int treeBottomTileType, ref bool createLeaves) {
			bool spawnLeafBlock = Main.rand.NextBool();
			if (spawnLeafBlock) {
				Item.NewItem(new EntitySource_ShakeTree(treeX, treeTopY), treeX * 16, treeTopY * 16, 16, 16, ModContent.ItemType<LeafBlock>());

				//Makes leaf visual effect only
				createLeaves = true;
			}
		}
		public override void Load() {
			On_WorldGen.KillTile_GetTreeDrops += On_WorldGen_KillTile_GetTreeDrops;
		}

		private void On_WorldGen_KillTile_GetTreeDrops(On_WorldGen.orig_KillTile_GetTreeDrops orig, int i, int j, Tile tileCache, ref bool bonusWood, ref int dropItem, ref int secondaryItem) {
			orig(i, j, tileCache, ref bonusWood, ref dropItem, ref secondaryItem);
			
			//From WorldGen.ShakeTree()
			
			WorldGen.GetTreeBottom(i, j, out int x, out int y);
			y--;
			while (y > 10 && Main.tile[x, y].HasTile && TileID.Sets.IsShakeable[Main.tile[x, y].TileType]) {
				y--;
			}

			y++;
			if (!WorldGen.IsTileALeafyTreeTop(x, y) || Collision.SolidTiles(x - 2, x + 2, y - 2, y + 2))
				return;

			int stack = Main.rand.Next(1, 3);
			Item.NewItem(new EntitySource_ShakeTree(x, y), x * 16, y * 16, 16, 16, ModContent.ItemType<LeafBlock>(), stack);
		}

		public override List<WikiTypeID> WikiItemTypes => new() { WikiTypeID.CraftingMaterial };

		public override string Artist => null;

		public override string Designer => "andro951";
	}
}
