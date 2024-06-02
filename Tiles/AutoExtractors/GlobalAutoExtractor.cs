using EngagedSkyblock.Tiles.TileEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using System.Reflection;
using Terraria.GameContent.Drawing;
using MonoMod.Cil;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Terraria.ObjectData;

namespace EngagedSkyblock.Tiles {
	public class GlobalAutoExtractor : GlobalTile {
		private static bool IsExtractinator(int tileType) => tileType == TileID.Extractinator || tileType == TileID.ChlorophyteExtractinator || tileType == AutoExtractorTE.T1 || tileType == AutoExtractorTE.T3 || tileType == AutoExtractorTE.T5;
		private bool TryGetEntityChest(int x, int y, out int chest) => AutoExtractorTE.TryGetChest(AutoExtractorTE.TilePositionToMultiTileTopLeft(x, y), out chest);
		private bool TryGetEntity(int x, int y, out AutoExtractorTE entity) {
			Point16 topLeft = AutoExtractorTE.TilePositionToMultiTileTopLeft(x, y);
			return TryGetEntity(topLeft, out entity);
		}

		private bool TryGetEntity(Point16 position, out AutoExtractorTE entity) {
			if (TileEntity.ByPosition.TryGetValue(position, out TileEntity tileEntity)) {
				if (tileEntity is AutoExtractorTE autoExtractorEntity) {
					entity = autoExtractorEntity;
					return true;
				}
			}

			entity = null;
			return false;
		}
		private bool TryGetNewEntity(int tileType, out AutoExtractorTE autoExtractor_BaseEntity) {
			if (tileType == AutoExtractorTE.T1) {
				autoExtractor_BaseEntity = ModContent.GetInstance<WoodAutoExtractinatorTE>();
			}
			else if (tileType == TileID.Extractinator) {
				autoExtractor_BaseEntity = ModContent.GetInstance<VanillaAutoExtractinatorTE>();
			}
			else if (tileType == AutoExtractorTE.T3) {
				autoExtractor_BaseEntity = ModContent.GetInstance<HellstoneAutoExtractinatorTE>();
			}
			else if (tileType == TileID.ChlorophyteExtractinator) {
				autoExtractor_BaseEntity = ModContent.GetInstance<ChlorophyteAutoExtractinatorTE>();
			}
			else if (tileType == AutoExtractorTE.T5) {
				autoExtractor_BaseEntity = ModContent.GetInstance<LuminiteAutoExtractinatorTE>();
			}
			else {
				autoExtractor_BaseEntity = null;
				return false;
			}

			return true;
		}
		public override void Load() {
			On_Player.TileInteractionsUse += On_Player_TileInteractionsUse;
			IL_TileDrawing.CacheSpecialDraws_Part2 += IL_TileDrawing_CacheSpecialDraws_Part2;
		}

		private void IL_TileDrawing_CacheSpecialDraws_Part2(ILContext il) {
			ILCursor c = new(il);

			//// if (TileID.Sets.BasicChest[drawData.typeCache])
			//IL_0000: ldsfld bool[] Terraria.ID.TileID / Sets::BasicChest
			//IL_0005: ldarg.3
			//IL_0006: ldfld uint16 Terraria.DataStructures.TileDrawInfo::typeCache
			//IL_000b: ldelem.u1
			//IL_000c: brfalse IL_013d

			FieldReference tileTypeRef = null;
			if (!c.TryGotoNext(MoveType.After,
				i => i.MatchLdsfld(out _),
				i => i.MatchLdarg3(),
				i => i.MatchLdfld(out tileTypeRef),
				i => i.MatchLdelemU1()
				)) {
				throw new Exception("Failed to find instructions for IL_TileDrawing_CacheSpecialDraws_Part2");
			}

			c.Emit(OpCodes.Ldarg_3);
			c.Emit(OpCodes.Ldfld, tileTypeRef);
			c.EmitDelegate((bool basicChest, int tileType) => {
				if (basicChest) {
					if (IsExtractinator(tileType)) {
						return false;
					}
				}

				return basicChest;	
			});
		}

		private void On_Player_TileInteractionsUse(On_Player.orig_TileInteractionsUse orig, Player self, int myX, int myY) {
			Tile tile = Main.tile[myX, myY];
			if (self.releaseUseTile && self.tileInteractAttempted && IsExtractinator(tile.TileType)) {
				TileLoader.RightClick(myX, myY);
				return;
			}

			orig(self, myX, myY);
		}

		public static void PostSetupContent() {
			List<int> vanillaExtractors = new() {
				TileID.Extractinator,
				TileID.ChlorophyteExtractinator
			};

			foreach (int type in vanillaExtractors) {
				Main.tileContainer[type] = true;
				TileID.Sets.BasicChest[type] = true;
				TileID.Sets.IsAContainer[type] = true;
			}
		}
		public delegate bool orig_TileLoaderRightClick(int i, int j);
		public delegate bool hook_TileLoaderRightClick(orig_TileLoaderRightClick orig, int i, int j);
		public static MethodInfo OnTileRightClickInfo = typeof(TileLoader).GetMethod("RightClick", BindingFlags.Public | BindingFlags.Static);
		public static bool TileLoaderRightClickDetour(orig_TileLoaderRightClick orig, int i, int j) {
			bool result = orig(i, j);
			result |= OnRightClick(i, j);
			return result;
		}
		public static bool OnRightClick(int i, int j) {
			Tile tile = Main.tile[i, j];
			int tileType = tile.TileType;
			//if (tileType == TileID.Extractinator || tileType == TileID.ChlorophyteExtractinator)
			//	return false;

			if (!IsExtractinator(tileType))
				return false;

			Point16 topLeft = AutoExtractorTE.TilePositionToMultiTileTopLeft(i, j);
			int x = topLeft.X;
			int y = topLeft.Y;

			Player player = Main.LocalPlayer;
			Main.mouseRightRelease = false;

			player.CloseSign();
			player.SetTalkNPC(-1);
			Main.npcChatCornerItem = 0;
			Main.npcChatText = "";
			if (Main.editChest) {
				SoundEngine.PlaySound(SoundID.MenuTick);
				Main.editChest = false;
				Main.npcChatText = string.Empty;
			}

			if (player.editedChestName) {
				NetMessage.SendData(MessageID.SyncPlayerChest, -1, -1, NetworkText.FromLiteral(Main.chest[player.chest].name), player.chest, 1f);
				player.editedChestName = false;
			}

			bool isLocked = Chest.IsLocked(x, y);
			if (Main.netMode == NetmodeID.MultiplayerClient && !isLocked) {
				if (x == player.chestX && y == player.chestY && player.chest != -1) {
					player.chest = -1;
					Recipe.FindRecipes();
					SoundEngine.PlaySound(SoundID.MenuClose);
				}
				else {
					NetMessage.SendData(MessageID.RequestChestOpen, -1, -1, null, x, y);
					Main.stackSplit = 600;
				}
			}
			else {
				if (isLocked) {
					//Chest for the AutoExtractors should never be locked.  Force it to unlock.
					if (Chest.Unlock(x, y)) {
						if (Main.netMode == NetmodeID.MultiplayerClient) {
							NetMessage.SendData(MessageID.LockAndUnlock, -1, -1, null, player.whoAmI, 1f, x, y);
						}
					}
				}
				else {
					Chest chest = Main.chest[0];
					if (AutoExtractorTE.TryGetChest(topLeft, out int chestId)) {
						Main.stackSplit = 600;
						if (chestId == player.chest) {
							player.chest = -1;
							SoundEngine.PlaySound(SoundID.MenuClose);
						}
						else {
							SoundEngine.PlaySound(player.chest < 0 ? SoundID.MenuOpen : SoundID.MenuTick);
							player.OpenChest(x, y, chestId);
						}

						Recipe.FindRecipes();
					}
				}
			}

			return true;
		}
		public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem) {
			if (!TryGetNewEntity(type, out AutoExtractorTE autoExtractor_BaseEntity))
				return;

			Tile tile = Main.tile[i, j];
			if (tile.TileFrameX % 54 != 0 || tile.TileFrameY % 54 != 18)
				return;

			autoExtractor_BaseEntity.Kill(i, j);

			if (!TryGetEntityChest(i, j, out int chestID))
				return;

			Chest chest = Main.chest[chestID];

			if (!Chest.DestroyChest(chest.x, chest.y)) {
				EntitySource_TileBreak source = new EntitySource_TileBreak(i, j, "Breaking AutoExtractor");
				for (int k = 0; k < chest.item.Length; k++) {
					Item.NewItem(source, i * 16, j * 16, 1, 1, chest.item[k].type, chest.item[k].stack, noBroadcast: false, -1);
					chest.item[k].TurnToAir();
				}

				Chest.DestroyChestDirect(chest.x, chest.y, chestID);
			}
		}
		public override bool CanKillTile(int i, int j, int type, ref bool blockDamaged) {
			if (!IsExtractinator(Main.tile[i, j].TileType))
				return true;

			if (TryGetEntityChest(i, j, out int chestId)) {
				Chest chest = Main.chest[chestId];
				bool canDestroyChest = Chest.CanDestroyChest(chest.x, chest.y);
				return canDestroyChest;
			}

			return true;
		}
		public override void PlaceInWorld(int i, int j, int type, Item item) {
			if (!TryGetNewEntity(type, out AutoExtractorTE autoExtractor_BaseEntity))
				return;

			autoExtractor_BaseEntity.PlaceAutoExtractor(i, j, type);
		}
	}
}
