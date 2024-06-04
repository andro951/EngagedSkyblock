using androLib.Common.Utility;
using EngagedSkyblock.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EngagedSkyblock.Common.Globals {
	public class GlobalHammer : GlobalItem {
		public override bool AppliesToEntity(Item entity, bool lateInstantiation) {
			bool hammer = entity.hammer > 0;
			if (hammer) {
				if (!ItemID.Sets.ItemsThatAllowRepeatedRightClick[entity.type]) {
					ItemID.Sets.ItemsThatAllowRepeatedRightClick[entity.type] = true;
					EnabledHammers.Add(entity.type);
				}
			}

			return hammer;
		}
		private static SortedSet<int> EnabledHammers = new();
		public static void UpdateHammersAllowRepeatedRightclick() {
			bool skyblock = ES_WorldGen.SkyblockWorld;
			foreach (int hammer in EnabledHammers) {
				ItemID.Sets.ItemsThatAllowRepeatedRightClick[hammer] = skyblock;
			}
		}
		public override void Load() {
			On_Player.ItemCheck_Inner += On_Player_ItemCheck_Inner;
		}
		private void On_Player_ItemCheck_Inner(On_Player.orig_ItemCheck_Inner orig, Player self) {
			orig(self);

			if (!ES_WorldGen.SkyblockWorld)
				return;

			if (PostUseActions != null) {
				PostUseActions();
				PostUseActions = null;
			}
		}

		public static Action PostUseActions;
		public override bool AltFunctionUse(Item item, Player player) {
			if (!ES_WorldGen.SkyblockWorld)
				return false;

			Item heldItem = player.HeldItem;
			if (ItemLoader.CanUseItem(heldItem, player) && !player.mouseInterface && !heldItem.NullOrAir() && heldItem.TryGetGlobalItem(out GlobalHammer _)) {
				player.controlUseItem = true;
				Tile target = Main.tile[Player.tileTargetX, Player.tileTargetY];
				if (target.HasTile && TileID.Sets.IsATreeTrunk[target.TileType]) {
					int hammer = heldItem.hammer;
					int axe = heldItem.axe;
					heldItem.axe = Math.Max(axe, hammer);
					heldItem.hammer = 0;
					PostUseActions += () => {
						heldItem.hammer = hammer;
						heldItem.axe = axe;
					};
				}
				else {
					int hammer = heldItem.hammer;
					int pick = heldItem.pick;
					heldItem.pick = Math.Max(pick, hammer);
					heldItem.hammer = 0;
					PostUseActions += () => {
						heldItem.hammer = hammer;
						heldItem.pick = pick;
					};
				}
			}

			return false;
		}
		public static SortedDictionary<int, int> TileHammerConversions = new();
		public static void PostSetupRecipes() {
			TileHammerConversions.Add(TileID.Stone, ItemID.SandBlock);
			TileHammerConversions.Add(TileID.WoodBlock, ModContent.ItemType<WoodChips>());
		}
		public static bool IsHammerableTileType(int x, int y) {
			Tile tile = Main.tile[x, y];
			bool dict = TileHammerConversions.ContainsKey(tile.TileType);
			if (dict)
				return true;

			return CheckTileHammer(x, y, tile.TileType, out _, false);
		}
		public static bool BreakTileWithHammer(int x, int y, int tileType, Item item) {
			int dropItemType;
			if (TileHammerConversions.TryGetValue(tileType, out int dictionaryConversion)) {
				dropItemType = dictionaryConversion;
			}
			else {
				if (!CheckTileHammer(x, y, tileType, out dropItemType))
					return false;

				if (dropItemType < 0)
					return true;//Item destroyed for other type of conversion like 2 snow to 1 ice.
			}

			if (dropItemType >= 0) {
				int stack = item.stack;
				item.SetDefaults(dropItemType);
				item.stack = stack;

				return true;
			}

			return false;
		}

		/// <summary>
		/// Don't use Main.tile[x, y], it has already been destroyed by the time this is called.
		/// </summary>
		public static bool CheckTileHammer(int x, int y, int tileType, out int dropItemType, bool breakTile = true) {
			if (y != Main.maxTilesY - 1 && tileType == TileID.SnowBlock) {
				Tile below = Main.tile[x, y + 1];
				if (below.HasTile && below.TileType == TileID.SnowBlock) {
					if (breakTile) {
						ES_ModSystem.PreUpdateWorldActions += () => ES_GlobalTile.PlaceTile(x, y + 1, TileID.IceBlock, false);
					}

					dropItemType = -1;
					return true;
				}
			}

			if (TileID.Sets.IsATreeTrunk[tileType]) {
				if (tileType >= TileID.TreeTopaz && tileType <= TileID.GemSaplings) {
					dropItemType = ItemID.SandBlock;
				}
				else {
					dropItemType = ModContent.ItemType<WoodChips>();
				}
				
				return true;
			}

			dropItemType = -1;
			return false;
		}

	}
}
