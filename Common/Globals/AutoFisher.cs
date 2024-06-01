using androLib.Common.Utility;
using androLib.UI;
using KokoLib.Nets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Achievements;
using Terraria.GameContent.Tile_Entities;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;
using Terraria.UI;
using static EngagedSkyblock.EngagedSkyblock;

namespace EngagedSkyblock.Common.Globals {
	internal static class AutoFisher {
		public static void Load() {
			On_Projectile.NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float += On_Projectile_NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float;
			On_Player.ItemCheck_OwnerOnlyCode += On_Player_ItemCheck_OwnerOnlyCode;
			On_Projectile.AI_061_FishingBobber += On_Projectile_AI_061_FishingBobber;
			On_Main.DrawProj_DrawExtras += On_Main_DrawProj_DrawExtras;
			On_Player.ItemCheck_ApplyHoldStyle_Inner += On_Player_ItemCheck_ApplyHoldStyle_Inner;
			On_Player.ItemCheck_ApplyUseStyle_Inner += On_Player_ItemCheck_ApplyUseStyle_Inner;
			On_Player.ItemCheck_PlayInstruments += On_Player_ItemCheck_PlayInstruments;
			On_Main.DrawProjDirect += On_Main_DrawProjDirect;
			On_Projectile.AI_061_FishingBobber_GiveItemToPlayer += On_Projectile_AI_061_FishingBobber_GiveItemToPlayer;
			On_Player.ItemCheck_CheckFishingBobbers += On_Player_ItemCheck_CheckFishingBobbers;
			IL_Projectile.Kill += IL_Projectile_Kill;

			On_TEDisplayDoll.Draw += On_TEDisplayDoll_Draw;
			On_TEDisplayDoll.DrawInner += On_TEDisplayDoll_DrawInner;
			On_TEDisplayDoll.OverrideItemSlotHover += On_TEDisplayDoll_OverrideItemSlotHover;
			On_TEDisplayDoll.OverrideItemSlotLeftClick += On_TEDisplayDoll_OverrideItemSlotLeftClick;
			On_TEDisplayDoll.TryFitting += On_TEDisplayDoll_TryFitting;
			On_TEDisplayDoll.ContainsItems += On_TEDisplayDoll_ContainsItems;
			On_TEDisplayDoll.FixLoadedData += On_TEDisplayDoll_FixLoadedData;
			On_NetMessage.SendData += On_NetMessage_SendData;
			On_TEDisplayDoll.Place += On_TEDisplayDoll_Place;
			On_TEDisplayDoll.Kill += On_TEDisplayDoll_Kill;
			On_MapHeadRenderer.DrawPlayerHead += On_MapHeadRenderer_DrawPlayerHead;
			On_ItemSlot.PickItemMovementAction += On_ItemSlot_PickItemMovementAction;
			On_PlayerInteractionAnchor.Clear += On_PlayerInteractionAnchor_Clear;
		}

		#region Fixes

		private static void On_MapHeadRenderer_DrawPlayerHead(On_MapHeadRenderer.orig_DrawPlayerHead orig, MapHeadRenderer self, Camera camera, Player drawPlayer, Vector2 position, float alpha, float scale, Color borderColor) {
			if (drawPlayer.whoAmI == 255)
				return;

			orig(self, camera, drawPlayer, position, alpha, scale, borderColor);
		}
		private static void On_Player_ItemCheck_PlayInstruments(On_Player.orig_ItemCheck_PlayInstruments orig, Player self, Item sItem) {
			if (self.whoAmI == 255 && Main.netMode == NetmodeID.Server)
				return;

			orig(self, sItem);
		}

		#endregion

		#region TEDisplayDoll

		private static void On_TEDisplayDoll_Kill(On_TEDisplayDoll.orig_Kill orig, int x, int y) {
			orig(x, y);
			AutoFisherTE.NewAutoFisherTE.Kill(x + 1, y);
		}
		private static int On_TEDisplayDoll_Place(On_TEDisplayDoll.orig_Place orig, int x, int y) {
			int id = orig(x, y);
			AutoFisherTE.OnPlaceTEDisplayDoll(id, x, y);
			return id;
		}
		private static void On_PlayerInteractionAnchor_Clear(On_PlayerInteractionAnchor.orig_Clear orig, ref PlayerInteractionAnchor self) {
			int old = self.interactEntityID;
			if (old == 0 && self.X == 0 && self.Y == 0)
				old = -1;

			TileEntity.IsOccupied(old, out int playerWhoAmI1);

			orig(ref self);
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;

			if (old == -1)
				return;

			if (playerWhoAmI1 == -1 || playerWhoAmI1 != Main.myPlayer)
				return;

			TileEntity.IsOccupied(old, out int playerWhoAmI2);
			if (playerWhoAmI2 > -1)
				return;

			//$"On_PlayerInteractionAnchor_Clear, old: {old}".LogSimpleNT();
			AutoFisherTE.TrySendAllItems(old);
		}
		private static void On_NetMessage_SendData(On_NetMessage.orig_SendData orig, int msgType, int remoteClient, int ignoreClient, NetworkText text, int number, float number2, float number3, float number4, int number5, int number6, int number7) {
			switch (msgType) {
				case MessageID.TileEntitySharing:
					int id = number;
					if (TileEntity.ByID.TryGetValue(id, out TileEntity te) && te is TEDisplayDoll displayDoll && displayDoll.TryGetAutoFisherTE(out AutoFisherTE autoFisherTE)) {
						NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, autoFisherTE.ID, autoFisherTE.Position.X, autoFisherTE.Position.Y);
					}
					break;
				case MessageID.RequestTileEntityInteraction:
					if (Main.netMode == NetmodeID.Server && number >= 0 && TileEntity.IsOccupied(number, out int ocupant) && ocupant == (int)number2 && number2 != -1f) {
						//$"On_NetMessage_SendData, id: {number}, ocupant: {ocupant}, playerWhoAmI: {number2}".LogSimpleNT();
						AutoFisherTE.TrySendAllItems(number, (int)number2);
					}
					break;
			}

			orig(msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7);
		}
		private static int autoFisherContext => 1928;
		private static void On_TEDisplayDoll_FixLoadedData(On_TEDisplayDoll.orig_FixLoadedData orig, TEDisplayDoll self) {
			orig(self);

			if (self.TryGetAutoFisherTE(out AutoFisherTE autoFisherTE)) {
				for (int i = 0; i < 8; i++) {
					autoFisherTE.autoFishingItems[i].FixAgainstExploit();
				}
			}
		}
		private static bool On_TEDisplayDoll_ContainsItems(On_TEDisplayDoll.orig_ContainsItems orig, TEDisplayDoll self) {
			if (orig(self))
				return true;

			if (!self.TryGetAutoFisherTE(out AutoFisherTE autoFisherTE))
				return false;

			for (int i = 0; i < 8; i++) {
				if (!autoFisherTE.autoFishingItems[i].NullOrAir())
					return true;
			}

			return false;
		}
		private static bool On_TEDisplayDoll_TryFitting(On_TEDisplayDoll.orig_TryFitting orig, TEDisplayDoll self, Item[] inv, int context, int slot, bool justCheck) {
			if (FitsDisplayDoll(inv[slot])) {
				if (self.TryGetAutoFisherTE(out AutoFisherTE autoFisherTE)) {
					Item item = inv[slot];
					int stack = item.stack;
					int num = -1;
					if (item.favorited)
						return false;

					if (item.fishingPole > 0) {
						num = 0;
					}
					else if (item.bait > 0) {
						num = 1;
					}

					if (num != -1) {
						if (justCheck)
							return true;

						if (num == 0) {
							Utils.Swap(ref autoFisherTE.autoFishingItems[num], ref inv[slot]);
							if (Main.netMode == 1)
								autoFisherTE.SendItem(num);

							SoundEngine.PlaySound(SoundID.Grab);
							return true;
						}
						else {
							bool transferedAny = false;
							for (; num < 8; num++) {
								Item existing = autoFisherTE.autoFishingItems[num];
								if (existing.NullOrAir()) {
									Utils.Swap(ref autoFisherTE.autoFishingItems[num], ref inv[slot]);
									transferedAny = true;
									//if (Main.netMode == 1)
									//	autoFisherTE.SendItem(num);

									break;
								}
								else if (existing.type == item.type && ItemLoader.TryStackItems(existing, item, out int transferred)) {
									stack -= transferred;
									transferedAny = true;
									//if (Main.netMode == 1)
									//	autoFisherTE.SendItem(num);

									if (stack <= 0)
										break;
								}
							}

							if (transferedAny) {
								SoundEngine.PlaySound(SoundID.Grab);
							}

							return transferedAny;
						}
					}
				}

				return false;
			}

			return orig(self,inv, context, slot, justCheck);
		}
		private static bool FitsDisplayDoll(Item item) => item.fishingPole > 0 || item.bait > 0;
		private static bool On_TEDisplayDoll_OverrideItemSlotLeftClick(On_TEDisplayDoll.orig_OverrideItemSlotLeftClick orig, TEDisplayDoll self, Item[] inv, int context, int slot) {
			if (self.TryGetAutoFisherTE(out AutoFisherTE autoFisherTE)) {
				if (!ItemSlot.ShiftInUse)
					return false;

				if (Main.cursorOverride == 9 && context == 0) {
					Item item = inv[slot];
					if (!item.IsAir && !item.favorited && FitsDisplayDoll(item))
						return self.TryFitting(inv, context, slot);
				}

				if (Main.cursorOverride == 8 && context == autoFisherContext) {
					inv[slot] = Main.player[Main.myPlayer].GetItem(Main.myPlayer, inv[slot], GetItemSettings.InventoryEntityToPlayerInventorySettings);
					if (Main.netMode == NetmodeID.MultiplayerClient && slot == 0) {
						autoFisherTE.SendItem(slot);
					}

					return true;
				}
			}

			return orig(self, inv, context, slot);
		}
		private static bool On_TEDisplayDoll_OverrideItemSlotHover(On_TEDisplayDoll.orig_OverrideItemSlotHover orig, TEDisplayDoll self, Item[] inv, int context, int slot) {
			if (self.TryGetAutoFisherTE(out _)) {
				Item item = inv[slot];
				if (!item.NullOrAir() && !item.favorited && context == 0 && (item.fishingPole > 0 || item.bait > 0)) {
					Main.cursorOverride = 9;
					return true;
				}

				if (!item.NullOrAir() && context == autoFisherContext && Main.player[Main.myPlayer].ItemSpace(inv[slot]).CanTakeItemToPersonalInventory) {
					Main.cursorOverride = 8;
					return true;
				}
			}

			return orig(self, inv, context, slot);
		}
		private static bool HandlingItem = false;
		private static void On_TEDisplayDoll_DrawInner(On_TEDisplayDoll.orig_DrawInner orig, TEDisplayDoll self, Player player, SpriteBatch spriteBatch) {
			orig(self, player, spriteBatch);
			if (self.TryGetAutoFisherTE(out AutoFisherTE autoFisherTE)) {
				Item[] items = autoFisherTE.autoFishingItems;
				int inventoryContextTarget = autoFisherContext;
				float offsetX = 0f;
				float offsetY = 2.5f;
				int num = inventoryContextTarget;
				for (int i = 0; i < 8; i++) {
					int num2 = (int)(73f + ((float)i + offsetX) * 56f * Main.inventoryScale);
					int num3 = (int)((float)Main.instance.invBottom + offsetY * 56f * Main.inventoryScale);
					if (Utils.FloatIntersect(Main.mouseX, Main.mouseY, 0f, 0f, num2, num3, (float)TextureAssets.InventoryBack.Width() * Main.inventoryScale, (float)TextureAssets.InventoryBack.Height() * Main.inventoryScale) && !Terraria.GameInput.PlayerInput.IgnoreMouseInterface) {
						player.mouseInterface = true;
						if (Main.mouseItem.NullOrAir() || i == 0 && Main.mouseItem.fishingPole > 0 || i > 0 && Main.mouseItem.bait > 0) {
							Item item = items[i].Clone();
							HandlingItem = true;
							ItemSlot.Handle(items, num, i);
							HandlingItem = false;
							if (i == 0 && (Main.mouseLeft && Main.mouseLeftRelease || Main.mouseRight && Main.mouseRightRelease || item.stack != items[i].stack || item.type != items[i].type))
								autoFisherTE.SendItem(i);
						}
					}

					ItemSlot.Draw(spriteBatch, items, 28, i, new Vector2(num2, num3));
				}
			}
		}
		private static int On_ItemSlot_PickItemMovementAction(On_ItemSlot.orig_PickItemMovementAction orig, Item[] inv, int context, int slot, Item checkItem) {
			if (HandlingItem)
				return 0;

			return orig(inv, context, slot, checkItem);
		}
		private static void On_TEDisplayDoll_Draw(On_TEDisplayDoll.orig_Draw orig, TEDisplayDoll self, int tileLeftX, int tileTopY) {
			if (self.TryGetAutoFisherTE(out AutoFisherTE autoFisherTE)) {
				if (autoFisherTE.Player.direction == -1) {
					if (autoFisherTE.Player.itemRotation < 0f)
						autoFisherTE.Player.itemRotation = 0f;
				}
				else {
					if (autoFisherTE.Player.itemRotation > 0f)
						autoFisherTE.Player.itemRotation = 0f;
				}

				Player dollPlayer = autoFisherTE.Player;
				for (int i = 0; i < 8; i++) {
					dollPlayer.inventory[i] = autoFisherTE.autoFishingItems[i];
				}


				autoFisherTE.DrawProjectiles();
			}

			orig(self, tileLeftX, tileTopY);

			if (autoFisherTE != null)
				autoFisherTE.Player.UpdateEquips(autoFisherTE.Player.whoAmI);
		}

		#endregion

		#region ItemCheck

		private static void On_Player_ItemCheck_OwnerOnlyCode(On_Player.orig_ItemCheck_OwnerOnlyCode orig, Player self, ref Player.ItemCheckContext context, Item sItem, int weaponDamage, Rectangle heldItemFrame) {
			if (Main.netMode == NetmodeID.SinglePlayer && self.TryGetAutoFisherTE(out _)) {
				Action postAction = () => self.whoAmI = 255;
				self.whoAmI = Main.myPlayer;
				orig(self, ref context, sItem, weaponDamage, heldItemFrame);
				postAction.Invoke();
			}
			else {
				orig(self, ref context, sItem, weaponDamage, heldItemFrame);
			}
		}
		private const float TEDisplayDollHeldItemYCorrection = 4f;
		private static void On_Player_ItemCheck_ApplyHoldStyle_Inner(On_Player.orig_ItemCheck_ApplyHoldStyle_Inner orig, Player self, float mountOffset, Item sItem, Rectangle heldItemFrame) {
			if (self.isDisplayDollOrInanimate && sItem.fishingPole > 0) {
				ItemCheck_HackHoldStyles(self, sItem);

				if (sItem.holdStyle == 1 && !self.pulley) {
					self.itemLocation.X = self.position.X + (float)self.width * 0.5f + (float)heldItemFrame.Width * 0.18f * (float)self.direction;
					self.itemLocation.Y = self.position.Y + 24f + TEDisplayDollHeldItemYCorrection;
				}
			}
			else {
				orig(self, mountOffset, sItem, heldItemFrame);
			}
		}
		private static void On_Player_ItemCheck_ApplyUseStyle_Inner(On_Player.orig_ItemCheck_ApplyUseStyle_Inner orig, Player self, float mountOffset, Item sItem, Rectangle heldItemFrame) {
			if (self.isDisplayDollOrInanimate && sItem.fishingPole > 0) {
				orig(self, mountOffset, sItem, heldItemFrame);
				self.itemLocation.Y += TEDisplayDollHeldItemYCorrection;
			}
			else {
				orig(self, mountOffset, sItem, heldItemFrame);
			}
		}
		public static void ItemCheck_HackHoldStyles(Player player, Item sItem) {
			if (!player.TryGetAutoFisherTE(out AutoFisherTE autoFisherTE))
				return;

			if (sItem.fishingPole > 0) {
				sItem.holdStyle = 0;
				if (player.ItemTimeIsZero && player.itemAnimation == 0) {
					for (int i = 0; i < 1000; i++) {
						Projectile projectile = Main.projectile[i];
						if (projectile.active && projectile.bobber) {
							int autoFisherID = projectile.GetAutoFisherID();
							if (autoFisherID == autoFisherTE.ID) {
								sItem.holdStyle = 1;
								break;
							}
						}
					}
				}
			}
		}
		private static T DoActionHideBobbers<T>(Player self, Func<T> action) {
			int myAutoFisherID = -1;
			if (self.TryGetAutoFisherTE(out AutoFisherTE autoFisherTE))
				myAutoFisherID = autoFisherTE.ID;

			Action returnOwners = null;
			for (int i = 0; i < 1000; i++) {
				Projectile projectile = Main.projectile[i];
				if (projectile.active && Main.projectile[i].owner == self.whoAmI && projectile.bobber) {
					if (projectile.TryGetAutoFisher(out AutoFisherTE projAutoFisherTE)) {
						if (myAutoFisherID == -1 || myAutoFisherID != projAutoFisherTE.ID) {
							int owner = projectile.owner;
							projectile.owner += Main.player.Length - 2;
							projectile.owner %= Main.player.Length;
							returnOwners += () => projectile.owner = owner;
						}
					}
				}
			}

			T result = action();
			returnOwners?.Invoke();
			return result;
		}
		private static void DoActionHideBobbers(Player self, Action action) {
			int myAutoFisherID = -1;
			if (self.TryGetAutoFisherTE(out AutoFisherTE autoFisherTE))
				myAutoFisherID = autoFisherTE.ID;

			Action returnOwners = null;
			for (int i = 0; i < 1000; i++) {
				Projectile projectile = Main.projectile[i];
				if (projectile.active && Main.projectile[i].owner == self.whoAmI && projectile.bobber) {
					if (projectile.TryGetAutoFisher(out AutoFisherTE projAutoFisherTE)) {
						if (myAutoFisherID == -1 || myAutoFisherID != projAutoFisherTE.ID) {
							int owner = projectile.owner;
							projectile.owner += Main.player.Length - 2;
							projectile.owner %= Main.player.Length;
							returnOwners += () => projectile.owner = owner;
						}
					}
				}
			}

			action();
			returnOwners?.Invoke();
		}
		private static bool On_Player_ItemCheck_CheckFishingBobbers(On_Player.orig_ItemCheck_CheckFishingBobbers orig, Player self, bool canUse) => DoActionHideBobbers(self, () => orig(self, canUse));
		//private void On_Player_ItemCheck_HackHoldStyles(On_Player.orig_ItemCheck_HackHoldStyles orig, Player self, Item sItem) => DoActionHideBobbers(self, () => orig(self, sItem));

		#endregion

		#region Projectile

		private static int On_Projectile_NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float(On_Projectile.orig_NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float orig, IEntitySource spawnSource, float X, float Y, float SpeedX, float SpeedY, int Type, int Damage, float KnockBack, int Owner, float ai0, float ai1, float ai2) {
			if (AutoFisherTE.ItemCheckFisherID != -1 && TileEntity.ByID.TryGetValue(AutoFisherTE.ItemCheckFisherID, out TileEntity te) && te is AutoFisherTE autoFisherTE) {
				autoFisherTE.UpdatePlayerDirection();
				KnockBack = autoFisherTE.AutoFisherIDToEncodedID();
				Owner = 255;
				spawnSource = autoFisherTE.Player.GetSource_ItemUse_WithPotentialAmmo(autoFisherTE.Player.HeldItem, 0);
				Vector2 pointPoisition = autoFisherTE.Player.RotatedRelativePoint(autoFisherTE.Player.MountedCenter);
				float speed = 10;
				float num2 = autoFisherTE.Player.direction * 8000;
				float num3 = 0;
				float num4 = (float)Math.Sqrt(num2 * num2 + num3 * num3);
				if ((float.IsNaN(num2) && float.IsNaN(num3)) || (num2 == 0f && num3 == 0f)) {
					num2 = autoFisherTE.Player.direction;
					num3 = 0f;
					num4 = speed;
				}
				else {
					num4 = speed / num4;
				}

				num2 *= num4;
				num3 *= num4;

				X = pointPoisition.X;
				Y = pointPoisition.Y;
				SpeedX = num2;
				SpeedY = num3;
			}

			return orig(spawnSource, X, Y, SpeedX, SpeedY, Type, Damage, KnockBack, Owner, ai0, ai1, ai2);
		}
		private static void On_Main_DrawProjDirect(On_Main.orig_DrawProjDirect orig, Main self, Projectile proj) {
			if (!AutoFisherTE.DrawingBobbers) {
				if (proj.TryGetAutoFisher(out _))
					return;
			}

			orig(self, proj);
		}
		private static void On_Main_DrawProj_DrawExtras(On_Main.orig_DrawProj_DrawExtras orig, Main self, Projectile proj, Vector2 mountedCenter, ref float polePosX, ref float polePosY) {
			if (proj.TryGetAutoFisher(out AutoFisherTE autoFisherTE)) {
				autoFisherTE.DrawProj_FishingLine(proj, ref polePosX, ref polePosY);
				return;
			}

			orig(self, proj, mountedCenter, ref polePosX, ref polePosY);
		}
		private static void On_Projectile_AI_061_FishingBobber(On_Projectile.orig_AI_061_FishingBobber orig, Projectile self) {
			if (AI_061_FishingBobber(self))
				return;

			orig(self);
		}
		private static bool AI_061_FishingBobber(Projectile projectile) {
			if (!projectile.TryGetAutoFisher(out AutoFisherTE autoFisherTE))
				return false;

			bool doClientChecks = Main.netMode == NetmodeID.SinglePlayer ? true : Main.myPlayer == projectile.owner;
			Player player = autoFisherTE.Player;
			int width = player.width;
			int height = player.height;
			TileObjectData data = TileObjectData.GetTileData(TileID.Mannequin, 0);
			Vector2 position = autoFisherTE.Position.ToWorldCoordinates(0f, 0f);
			bool flag = projectile.type >= 986 && projectile.type <= 993;
			projectile.timeLeft = 60;
			bool flag2 = false;
			if (player.inventory[player.selectedItem].fishingPole == 0 || player.CCed || player.noItems)
				flag2 = true;
			else if (player.inventory[player.selectedItem].shoot != projectile.type && !flag)
				flag2 = true;
			else if (player.pulley)
				flag2 = true;
			else if (player.dead)
				flag2 = true;

			if (flag2) {
				projectile.Kill();
				return true;
			}

			if (projectile.ai[1] > 0f && projectile.localAI[1] != 0f) {
				projectile.localAI[1] = 0f;
				if (!projectile.lavaWet && !projectile.honeyWet)
					projectile.AI_061_FishingBobber_DoASplash();
			}

			if (projectile.ai[0] >= 1f) {
				if (projectile.ai[0] == 2f) {
					projectile.ai[0] += 1f;
					SoundEngine.PlaySound(SoundID.Item17, projectile.position);
					if (!projectile.lavaWet && !projectile.honeyWet)
						projectile.AI_061_FishingBobber_DoASplash();
				}

				if (projectile.localAI[0] < 100f)
					projectile.localAI[0] += 1f;

				if (projectile.frameCounter == 0) {
					projectile.frameCounter = 1;
					projectile.ReduceRemainingChumsInPool();
				}

				projectile.tileCollide = false;
				int num = 10;
				Vector2 vector = new Vector2(projectile.position.X + (float)projectile.width * 0.5f, projectile.position.Y + (float)projectile.height * 0.5f);
				float num2 = position.X + (float)(width / 2) - vector.X;
				float num3 = position.Y + (float)(height / 2) - vector.Y;
				float num4 = (float)Math.Sqrt(num2 * num2 + num3 * num3);
				if (num4 > 3000f)
					projectile.Kill();

				num4 = 15.9f / num4;
				num2 *= num4;
				num3 *= num4;
				projectile.velocity.X = (projectile.velocity.X * (float)(num - 1) + num2) / (float)num;
				projectile.velocity.Y = (projectile.velocity.Y * (float)(num - 1) + num3) / (float)num;
				projectile.rotation = (float)Math.Atan2(projectile.velocity.Y, projectile.velocity.X) + 1.57f;
				if (doClientChecks && projectile.Hitbox.Intersects(player.Hitbox))
					projectile.Kill();

				return true;
			}

			bool flag3 = false;
			Vector2 vector2 = new Vector2(projectile.position.X + (float)projectile.width * 0.5f, projectile.position.Y + (float)projectile.height * 0.5f);
			float num5 = position.X + (float)(width / 2) - vector2.X;
			float num6 = position.Y + (float)(height / 2) - vector2.Y;
			projectile.rotation = (float)Math.Atan2(num6, num5) + 1.57f;
			if ((float)Math.Sqrt(num5 * num5 + num6 * num6) > 900f)
				projectile.ai[0] = 1f;

			if (projectile.wet) {
				if (projectile.shimmerWet) {
					if (doClientChecks)
						player.AddBuff(353, 60);

					if (projectile.localAI[2] == 0f) {
						projectile.localAI[2] = 1f;
						SoundEngine.PlaySound(SoundID.Splash, player.position);
					}
				}

				projectile.rotation = 0f;
				projectile.velocity.X *= 0.9f;
				int num7 = (int)(projectile.Center.X + (float)((projectile.width / 2 + 8) * projectile.direction)) / 16;
				int num8 = (int)(projectile.Center.Y / 16f);
				_ = projectile.position.Y / 16f;
				int num9 = (int)((projectile.position.Y + (float)projectile.height) / 16f);
				//if (Main.tile[num7, num8] == null)
				//	Main.tile[num7, num8] = new Tile();

				//if (Main.tile[num7, num9] == null)
				//	Main.tile[num7, num9] = new Tile();

				if (projectile.velocity.Y > 0f)
					projectile.velocity.Y *= 0.5f;

				num7 = (int)(projectile.Center.X / 16f);
				num8 = (int)(projectile.Center.Y / 16f);
				float num10 = projectile.AI_061_FishingBobber_GetWaterLine(num7, num8);
				if (projectile.Center.Y > num10) {
					projectile.velocity.Y -= 0.1f;
					if (projectile.velocity.Y < -8f)
						projectile.velocity.Y = -8f;

					if (projectile.Center.Y + projectile.velocity.Y < num10)
						projectile.velocity.Y = num10 - projectile.Center.Y;
				}
				else {
					projectile.velocity.Y = num10 - projectile.Center.Y;
				}

				if ((double)projectile.velocity.Y >= -0.01 && (double)projectile.velocity.Y <= 0.01)
					flag3 = true;
			}
			else {
				if (projectile.velocity.Y == 0f)
					projectile.velocity.X *= 0.95f;

				projectile.velocity.X *= 0.98f;
				projectile.velocity.Y += 0.2f;
				if (projectile.velocity.Y > 15.9f)
					projectile.velocity.Y = 15.9f;
			}

			//Duke Fishron warning aka Bait 2673.
			if (doClientChecks && player.GetFishingConditions().BaitItemType == 2673)
				player.displayedFishingInfo = Language.GetTextValue("GameUI.FishingWarning");

			if (projectile.ai[1] != 0f)
				flag3 = true;//Not in water

			if (!flag3)
				return true;

			if (projectile.ai[1] == 0f && doClientChecks) {
				//Add to catch timer
				int finalFishingLevel = player.GetFishingConditions().FinalFishingLevel;
				if (Main.rand.Next(300) < finalFishingLevel)
					projectile.localAI[1] += Main.rand.Next(1, 3);

				projectile.localAI[1] += finalFishingLevel / 30;
				projectile.localAI[1] += Main.rand.Next(1, 3);
				if (Main.rand.Next(60) == 0)
					projectile.localAI[1] += 60f;

				if (projectile.localAI[1] > 660f) {
					//Catch and reset timer
					projectile.localAI[1] = 0f;
					FishingCheck(projectile, player);
					//$"FishingCheck({player.S()}), finalFishingLevel: {finalFishingLevel}".LogSimpleNT();
					if (projectile.localAI[1] > 0f)
						autoFisherTE.shouldPullBobber = true;
				}
			}
			else if (projectile.ai[1] < 0f) {
				if (projectile.velocity.Y == 0f || (projectile.honeyWet && Math.Abs(projectile.velocity.Y) <= 0.01f)) {
					projectile.velocity.Y = (float)Main.rand.Next(100, 500) * 0.015f;
					projectile.velocity.X = (float)Main.rand.Next(-100, 101) * 0.015f;
					projectile.wet = false;
					projectile.lavaWet = false;
					projectile.honeyWet = false;
				}

				projectile.ai[1] += Main.rand.Next(1, 5);
				if (projectile.ai[1] >= 0f) {
					projectile.ai[1] = 0f;
					projectile.localAI[1] = 0f;
					projectile.netUpdate = true;
				}
			}

			return true;
		}
		private static void IL_Projectile_Kill(ILContext il) {
			ILCursor c = new(il);

			//// if (Main.myPlayer == this.owner && this.bobber)
			//IL_4be8: ldsfld int32 Terraria.Main::myPlayer
			//IL_4bed: ldarg.0
			//IL_4bee: ldfld int32 Terraria.Projectile::owner
			//IL_4bf3: bne.un IL_4c88

			//IL_4bf8: ldarg.0
			//IL_4bf9: ldfld bool Terraria.Projectile::bobber
			//IL_4bfe: brfalse IL_4c88

			if (!c.TryGotoNext(MoveType.Before,
				i => i.MatchLdsfld(typeof(Main), nameof(Main.myPlayer)),
				i => i.MatchLdarg0(),
				i => i.MatchLdfld(typeof(Projectile), nameof(Projectile.owner)),
				i => i.MatchBneUn(out _),
				i => i.MatchLdarg0(),
				i => i.MatchLdfld(typeof(Projectile), nameof(Projectile.bobber)),
				i => i.MatchBrfalse(out _)
				)) {
				throw new Exception("Failed to find instructions for IL_Projectile_Kill");
			}

			//Note to self: Instead of trying to change every instruction with a label to an instruction you want to remove,
			//	just pop it instead and remove all after that you don't need.
			c.Index++;
			c.EmitPop();
			c.RemoveRange(2);

			c.EmitLdarg0();
			c.EmitDelegate((Projectile projectile) => Main.netMode == NetmodeID.SinglePlayer ? true : Main.myPlayer == projectile.owner);

			c.Emit(OpCodes.Brfalse, c.Next.Operand);
			c.Remove();
		}
		private static void FishingCheck(Projectile projectile, Player player) {
			if (player.wet && !(projectile.Center.Y >= player.RotatedRelativePoint(player.MountedCenter).Y))
				return;

			FishingAttempt fisher = default(FishingAttempt);
			fisher.X = (int)(projectile.Center.X / 16f);
			fisher.Y = (int)(projectile.Center.Y / 16f);
			fisher.bobberType = projectile.type;
			AutoFisherStaticMethods.GetFishingPondState(fisher.X, fisher.Y, out fisher.inLava, out fisher.inHoney, out fisher.waterTilesCount, out fisher.chumsInWater);
			if (Main.notTheBeesWorld && Main.rand.Next(2) == 0)
				fisher.inHoney = false;

			if (fisher.waterTilesCount < 75) {
				player.displayedFishingInfo = Language.GetTextValue("GameUI.NotEnoughWater");
				return;
			}

			fisher.playerFishingConditions = player.GetFishingConditions();
			int baitItemType = fisher.playerFishingConditions.BaitItemType;
			if (baitItemType == 2673) {
				player.displayedFishingInfo = Language.GetTextValue("GameUI.FishingWarning");
				if ((fisher.X < 380 || fisher.X > Main.maxTilesX - 380) && fisher.waterTilesCount > 1000 && !NPC.AnyNPCs(370)) {
					projectile.ai[1] = Main.rand.Next(-180, -60) - 100;
					projectile.localAI[1] = 1f;
					projectile.netUpdate = true;
				}

				return;
			}

			fisher.fishingLevel = fisher.playerFishingConditions.FinalFishingLevel;
			if (fisher.fishingLevel == 0)
				return;

			fisher.CanFishInLava = ItemID.Sets.CanFishInLava[fisher.playerFishingConditions.PoleItemType] || ItemID.Sets.IsLavaBait[fisher.playerFishingConditions.BaitItemType] || player.accLavaFishing;
			if (fisher.chumsInWater > 0)
				fisher.fishingLevel += 11;

			if (fisher.chumsInWater > 1)
				fisher.fishingLevel += 6;

			if (fisher.chumsInWater > 2)
				fisher.fishingLevel += 3;

			player.displayedFishingInfo = Language.GetTextValue("GameUI.FishingPower", fisher.fishingLevel);
			fisher.waterNeededToFish = 300;
			float num = (float)Main.maxTilesX / 4200f;
			num *= num;
			fisher.atmo = (float)((double)(projectile.position.Y / 16f - (60f + 10f * num)) / (Main.worldSurface / 6.0));
			if ((double)fisher.atmo < 0.25)
				fisher.atmo = 0.25f;

			if (fisher.atmo > 1f)
				fisher.atmo = 1f;

			fisher.waterNeededToFish = (int)((float)fisher.waterNeededToFish * fisher.atmo);
			fisher.waterQuality = (float)fisher.waterTilesCount / (float)fisher.waterNeededToFish;
			if (fisher.waterQuality < 1f)
				fisher.fishingLevel = (int)((float)fisher.fishingLevel * fisher.waterQuality);

			fisher.waterQuality = 1f - fisher.waterQuality;
			if (fisher.waterTilesCount < fisher.waterNeededToFish)
				player.displayedFishingInfo = Language.GetTextValue("GameUI.FullFishingPower", fisher.fishingLevel, 0.0 - Math.Round(fisher.waterQuality * 100f));

			if (player.luck < 0f) {
				if (Main.rand.NextFloat() < 0f - player.luck)
					fisher.fishingLevel = (int)((double)fisher.fishingLevel * (0.9 - (double)Main.rand.NextFloat() * 0.3));
			}
			else if (Main.rand.NextFloat() < player.luck) {
				fisher.fishingLevel = (int)((double)fisher.fishingLevel * (1.1 + (double)Main.rand.NextFloat() * 0.3));
			}

			int num2 = (fisher.fishingLevel + 75) / 2;
			if (Main.rand.Next(100) > num2)
				return;

			fisher.heightLevel = 0;
			if (Main.remixWorld) {
				if ((double)fisher.Y < Main.worldSurface * 0.5)
					fisher.heightLevel = 0;
				else if ((double)fisher.Y < Main.worldSurface)
					fisher.heightLevel = 1;
				else if ((double)fisher.Y < Main.rockLayer)
					fisher.heightLevel = 3;
				else if (fisher.Y < Main.maxTilesY - 300)
					fisher.heightLevel = 2;
				else
					fisher.heightLevel = 4;

				if (fisher.heightLevel == 2 && Main.rand.Next(2) == 0)
					fisher.heightLevel = 1;
			}
			else if ((double)fisher.Y < Main.worldSurface * 0.5) {
				fisher.heightLevel = 0;
			}
			else if ((double)fisher.Y < Main.worldSurface) {
				fisher.heightLevel = 1;
			}
			else if ((double)fisher.Y < Main.rockLayer) {
				fisher.heightLevel = 2;
			}
			else if (fisher.Y < Main.maxTilesY - 300) {
				fisher.heightLevel = 3;
			}
			else {
				fisher.heightLevel = 4;
			}

			FishingCheck_RollDropLevels(projectile, player, fisher.fishingLevel, out fisher.common, out fisher.uncommon, out fisher.rare, out fisher.veryrare, out fisher.legendary, out fisher.crate);

			PlayerLoader.ModifyFishingAttempt(player, ref fisher);

			FishingCheck_ProbeForQuestFish(projectile, player, ref fisher);
			//projectile.FishingCheck_RollEnemySpawns(ref fisher);
			FishingCheck_RollItemDrop(projectile, player, ref fisher);
			bool flag = false;

			var sonar = new AdvancedPopupRequest();
			Vector2 sonarPosition = new Vector2(projectile.position.X, projectile.position.Y); // Bobber position as default
			PlayerLoader.CatchFish(player, fisher, ref fisher.rolledItemDrop, ref fisher.rolledEnemySpawn, ref sonar, ref sonarPosition);

			if (sonar.Text != null && player.sonarPotion) {
				PopupText.AssignAsSonarText(PopupText.NewText(sonar, sonarPosition));
			}

			if (fisher.rolledItemDrop > 0) {
				if (sonar.Text == null && player.sonarPotion) {
					Item item = new Item();
					item.SetDefaults(fisher.rolledItemDrop);
					item.position = projectile.position;
					PopupText.AssignAsSonarText(PopupText.NewText(PopupTextContext.SonarAlert, item, 1, noStack: true));
				}

				float num3 = fisher.fishingLevel;
				projectile.ai[1] = (float)Main.rand.Next(-240, -90) - num3;
				projectile.localAI[1] = fisher.rolledItemDrop;
				projectile.netUpdate = true;
				flag = true;
			}

			if (fisher.rolledEnemySpawn > 0) {
				if (sonar.Text == null && player.sonarPotion)
					PopupText.AssignAsSonarText(PopupText.NewText(PopupTextContext.SonarAlert, fisher.rolledEnemySpawn, projectile.Center, stay5TimesLonger: false));

				float num4 = fisher.fishingLevel;
				projectile.ai[1] = (float)Main.rand.Next(-240, -90) - num4;
				projectile.localAI[1] = -fisher.rolledEnemySpawn;
				projectile.netUpdate = true;
				flag = true;
			}

			if (!flag && fisher.inLava) {
				int num5 = 0;
				if (ItemID.Sets.IsLavaBait[fisher.playerFishingConditions.BaitItemType])
					num5++;

				if (ItemID.Sets.CanFishInLava[fisher.playerFishingConditions.PoleItemType])
					num5++;

				if (player.accLavaFishing)
					num5++;

				if (num5 >= 2)
					projectile.localAI[1] += 240f;
			}

			if (fisher.CanFishInLava && fisher.inLava)
				AchievementsHelper.HandleSpecialEvent(player, 19);
		}
		private static void FishingCheck_RollDropLevels(Projectile projectile, Player player, int fishingLevel, out bool common, out bool uncommon, out bool rare, out bool veryrare, out bool legendary, out bool crate) {
			int num = 150 / fishingLevel;
			int num2 = 150 * 2 / fishingLevel;
			int num3 = 150 * 7 / fishingLevel;
			int num4 = 150 * 15 / fishingLevel;
			int num5 = 150 * 30 / fishingLevel;
			int num6 = 10;
			if (player.cratePotion)
				num6 += 15;

			if (num < 2)
				num = 2;

			if (num2 < 3)
				num2 = 3;

			if (num3 < 4)
				num3 = 4;

			if (num4 < 5)
				num4 = 5;

			if (num5 < 6)
				num5 = 6;

			common = false;
			uncommon = false;
			rare = false;
			veryrare = false;
			legendary = false;
			crate = false;
			if (Main.rand.Next(num) == 0)
				common = true;

			if (Main.rand.Next(num2) == 0)
				uncommon = true;

			if (Main.rand.Next(num3) == 0)
				rare = true;

			if (Main.rand.Next(num4) == 0)
				veryrare = true;

			if (Main.rand.Next(num5) == 0)
				legendary = true;

			if (Main.rand.Next(100) < num6)
				crate = true;
		}
		private static void FishingCheck_ProbeForQuestFish(Projectile projectile, Player player, ref FishingAttempt fisher) {
			fisher.questFish = Main.anglerQuestItemNetIDs[Main.anglerQuest];
			if (player.HasItem(fisher.questFish))
				fisher.questFish = -1;

			if (!NPC.AnyNPCs(369))
				fisher.questFish = -1;

			if (Main.anglerQuestFinished)
				fisher.questFish = -1;
		}
		private static void FishingCheck_RollItemDrop(Projectile projectile, Player player, ref FishingAttempt fisher) {
			bool flag = player.ZoneCorrupt;
			bool flag2 = player.ZoneCrimson;
			bool flag3 = player.ZoneJungle;
			bool flag4 = player.ZoneSnow;
			bool flag5 = player.ZoneDungeon;
			if (!NPC.downedBoss3)
				flag5 = false;

			if (Main.notTheBeesWorld && !Main.remixWorld && Main.rand.Next(2) == 0)
				flag3 = false;

			if (Main.remixWorld && fisher.heightLevel == 0) {
				flag = false;
				flag2 = false;
			}
			else if (flag && flag2) {
				if (Main.rand.Next(2) == 0)
					flag2 = false;
				else
					flag = false;
			}

			if (fisher.rolledEnemySpawn > 0)
				return;

			if (fisher.inLava) {
				if (fisher.CanFishInLava) {
					if (fisher.crate && Main.rand.Next(6) == 0) {
						fisher.rolledItemDrop = (Main.hardMode ? 4878 : 4877);
					}
					else if (fisher.legendary && Main.hardMode && Main.rand.Next(3) == 0) {
						fisher.rolledItemDrop = Main.rand.NextFromList(new short[4] {
						4819,
						4820,
						4872,
						2331
					});
					}
					else if (fisher.legendary && !Main.hardMode && Main.rand.Next(3) == 0) {
						fisher.rolledItemDrop = Main.rand.NextFromList(new short[3] {
						4819,
						4820,
						4872
					});
					}
					else if (fisher.veryrare) {
						fisher.rolledItemDrop = 2312;
					}
					else if (fisher.rare) {
						fisher.rolledItemDrop = 2315;
					}
				}

				return;
			}

			if (fisher.inHoney) {
				if (fisher.rare || (fisher.uncommon && Main.rand.Next(2) == 0))
					fisher.rolledItemDrop = 2314;
				else if (fisher.uncommon && fisher.questFish == 2451)
					fisher.rolledItemDrop = 2451;

				return;
			}

			if (Main.rand.Next(50) > fisher.fishingLevel && Main.rand.Next(50) > fisher.fishingLevel && fisher.waterTilesCount < fisher.waterNeededToFish) {
				fisher.rolledItemDrop = Main.rand.Next(2337, 2340);
				if (Main.rand.Next(8) == 0)
					fisher.rolledItemDrop = 5275;

				return;
			}

			if (fisher.crate) {
				bool hardMode = Main.hardMode;
				if (fisher.rare && flag5)
					fisher.rolledItemDrop = (hardMode ? 3984 : 3205);
				else if (fisher.rare && (player.ZoneBeach || (Main.remixWorld && fisher.heightLevel == 1 && (double)fisher.Y >= Main.rockLayer && Main.rand.Next(2) == 0)))
					fisher.rolledItemDrop = (hardMode ? 5003 : 5002);
				else if (fisher.rare && flag)
					fisher.rolledItemDrop = (hardMode ? 3982 : 3203);
				else if (fisher.rare && flag2)
					fisher.rolledItemDrop = (hardMode ? 3983 : 3204);
				else if (fisher.rare && player.ZoneHallow)
					fisher.rolledItemDrop = (hardMode ? 3986 : 3207);
				else if (fisher.rare && flag3)
					fisher.rolledItemDrop = (hardMode ? 3987 : 3208);
				else if (fisher.rare && player.ZoneSnow)
					fisher.rolledItemDrop = (hardMode ? 4406 : 4405);
				else if (fisher.rare && player.ZoneDesert)
					fisher.rolledItemDrop = (hardMode ? 4408 : 4407);
				else if (fisher.rare && fisher.heightLevel == 0)
					fisher.rolledItemDrop = (hardMode ? 3985 : 3206);
				else if (fisher.veryrare || fisher.legendary)
					fisher.rolledItemDrop = (hardMode ? 3981 : 2336);
				else if (fisher.uncommon)
					fisher.rolledItemDrop = (hardMode ? 3980 : 2335);
				else
					fisher.rolledItemDrop = (hardMode ? 3979 : 2334);

				return;
			}

			if (!NPC.combatBookWasUsed && Main.bloodMoon && fisher.legendary && Main.rand.Next(3) == 0) {
				fisher.rolledItemDrop = 4382;
				return;
			}

			if (Main.bloodMoon && fisher.legendary && Main.rand.Next(2) == 0) {
				fisher.rolledItemDrop = 5240;
				return;
			}

			if (fisher.legendary && Main.rand.Next(5) == 0) {
				fisher.rolledItemDrop = 2423;
				return;
			}

			if (fisher.legendary && Main.rand.Next(5) == 0) {
				fisher.rolledItemDrop = 3225;
				return;
			}

			if (fisher.legendary && Main.rand.Next(10) == 0) {
				fisher.rolledItemDrop = 2420;
				return;
			}

			if (!fisher.legendary && !fisher.veryrare && fisher.uncommon && Main.rand.Next(5) == 0) {
				fisher.rolledItemDrop = 3196;
				return;
			}

			bool flag6 = player.ZoneDesert;
			if (flag5) {
				flag6 = false;
				if (fisher.rolledItemDrop == 0 && fisher.veryrare && Main.rand.Next(7) == 0)
					fisher.rolledItemDrop = 3000;
			}
			else {
				if (flag) {
					if (fisher.legendary && Main.hardMode && player.ZoneSnow && fisher.heightLevel == 3 && Main.rand.Next(3) != 0)
						fisher.rolledItemDrop = 2429;
					else if (fisher.legendary && Main.hardMode && Main.rand.Next(2) == 0)
						fisher.rolledItemDrop = 3210;
					else if (fisher.rare)
						fisher.rolledItemDrop = 2330;
					else if (fisher.uncommon && fisher.questFish == 2454)
						fisher.rolledItemDrop = 2454;
					else if (fisher.uncommon && fisher.questFish == 2485)
						fisher.rolledItemDrop = 2485;
					else if (fisher.uncommon && fisher.questFish == 2457)
						fisher.rolledItemDrop = 2457;
					else if (fisher.uncommon)
						fisher.rolledItemDrop = 2318;
				}
				else if (flag2) {
					if (fisher.legendary && Main.hardMode && player.ZoneSnow && fisher.heightLevel == 3 && Main.rand.Next(3) != 0)
						fisher.rolledItemDrop = 2429;
					else if (fisher.legendary && Main.hardMode && Main.rand.Next(2) == 0)
						fisher.rolledItemDrop = 3211;
					else if (fisher.uncommon && fisher.questFish == 2477)
						fisher.rolledItemDrop = 2477;
					else if (fisher.uncommon && fisher.questFish == 2463)
						fisher.rolledItemDrop = 2463;
					else if (fisher.uncommon)
						fisher.rolledItemDrop = 2319;
					else if (fisher.common)
						fisher.rolledItemDrop = 2305;
				}
				else if (player.ZoneHallow) {
					if (flag6 && Main.rand.Next(2) == 0) {
						if (fisher.uncommon && fisher.questFish == 4393)
							fisher.rolledItemDrop = 4393;
						else if (fisher.uncommon && fisher.questFish == 4394)
							fisher.rolledItemDrop = 4394;
						else if (fisher.uncommon)
							fisher.rolledItemDrop = 4410;
						else if (Main.rand.Next(3) == 0)
							fisher.rolledItemDrop = 4402;
						else
							fisher.rolledItemDrop = 4401;
					}
					else if (fisher.legendary && Main.hardMode && player.ZoneSnow && fisher.heightLevel == 3 && Main.rand.Next(3) != 0) {
						fisher.rolledItemDrop = 2429;
					}
					else if (fisher.legendary && Main.hardMode && Main.rand.Next(2) == 0) {
						fisher.rolledItemDrop = 3209;
					}
					else if (fisher.legendary && Main.hardMode && Main.rand.Next(3) != 0) {
						fisher.rolledItemDrop = 5274;
					}
					else if (fisher.heightLevel > 1 && fisher.veryrare) {
						fisher.rolledItemDrop = 2317;
					}
					else if (fisher.heightLevel > 1 && fisher.uncommon && fisher.questFish == 2465) {
						fisher.rolledItemDrop = 2465;
					}
					else if (fisher.heightLevel < 2 && fisher.uncommon && fisher.questFish == 2468) {
						fisher.rolledItemDrop = 2468;
					}
					else if (fisher.rare) {
						fisher.rolledItemDrop = 2310;
					}
					else if (fisher.uncommon && fisher.questFish == 2471) {
						fisher.rolledItemDrop = 2471;
					}
					else if (fisher.uncommon) {
						fisher.rolledItemDrop = 2307;
					}
				}

				if (fisher.rolledItemDrop == 0 && player.ZoneGlowshroom && fisher.uncommon && fisher.questFish == 2475)
					fisher.rolledItemDrop = 2475;

				if (flag4 && flag3 && Main.rand.Next(2) == 0)
					flag4 = false;

				if (fisher.rolledItemDrop == 0 && flag4) {
					if (fisher.heightLevel < 2 && fisher.uncommon && fisher.questFish == 2467)
						fisher.rolledItemDrop = 2467;
					else if (fisher.heightLevel == 1 && fisher.uncommon && fisher.questFish == 2470)
						fisher.rolledItemDrop = 2470;
					else if (fisher.heightLevel >= 2 && fisher.uncommon && fisher.questFish == 2484)
						fisher.rolledItemDrop = 2484;
					else if (fisher.heightLevel > 1 && fisher.uncommon && fisher.questFish == 2466)
						fisher.rolledItemDrop = 2466;
					else if ((fisher.common && Main.rand.Next(12) == 0) || (fisher.uncommon && Main.rand.Next(6) == 0))
						fisher.rolledItemDrop = 3197;
					else if (fisher.uncommon)
						fisher.rolledItemDrop = 2306;
					else if (fisher.common)
						fisher.rolledItemDrop = 2299;
					else if (fisher.heightLevel > 1 && Main.rand.Next(3) == 0)
						fisher.rolledItemDrop = 2309;
				}

				if (fisher.rolledItemDrop == 0 && flag3) {
					if (fisher.heightLevel == 1 && fisher.uncommon && fisher.questFish == 2452)
						fisher.rolledItemDrop = 2452;
					else if (fisher.heightLevel == 1 && fisher.uncommon && fisher.questFish == 2483)
						fisher.rolledItemDrop = 2483;
					else if (fisher.heightLevel == 1 && fisher.uncommon && fisher.questFish == 2488)
						fisher.rolledItemDrop = 2488;
					else if (fisher.heightLevel >= 1 && fisher.uncommon && fisher.questFish == 2486)
						fisher.rolledItemDrop = 2486;
					else if (fisher.heightLevel > 1 && fisher.uncommon)
						fisher.rolledItemDrop = 2311;
					else if (fisher.uncommon)
						fisher.rolledItemDrop = 2313;
					else if (fisher.common)
						fisher.rolledItemDrop = 2302;
				}
			}

			if (fisher.rolledItemDrop == 0) {
				if ((Main.remixWorld && fisher.heightLevel == 1 && (double)fisher.Y >= Main.rockLayer && Main.rand.Next(3) == 0) || (fisher.heightLevel <= 1 && (fisher.X < 380 || fisher.X > Main.maxTilesX - 380) && fisher.waterTilesCount > 1000)) {
					if (fisher.veryrare && Main.rand.Next(2) == 0)
						fisher.rolledItemDrop = 2341;
					else if (fisher.veryrare)
						fisher.rolledItemDrop = 2342;
					else if (fisher.rare && Main.rand.Next(5) == 0)
						fisher.rolledItemDrop = 2438;
					else if (fisher.rare && Main.rand.Next(3) == 0)
						fisher.rolledItemDrop = 2332;
					else if (fisher.uncommon && fisher.questFish == 2480)
						fisher.rolledItemDrop = 2480;
					else if (fisher.uncommon && fisher.questFish == 2481)
						fisher.rolledItemDrop = 2481;
					else if (fisher.uncommon)
						fisher.rolledItemDrop = 2316;
					else if (fisher.common && Main.rand.Next(2) == 0)
						fisher.rolledItemDrop = 2301;
					else if (fisher.common)
						fisher.rolledItemDrop = 2300;
					else
						fisher.rolledItemDrop = 2297;
				}
				else if (flag6) {
					if (fisher.uncommon && fisher.questFish == 4393)
						fisher.rolledItemDrop = 4393;
					else if (fisher.uncommon && fisher.questFish == 4394)
						fisher.rolledItemDrop = 4394;
					else if (fisher.uncommon)
						fisher.rolledItemDrop = 4410;
					else if (Main.rand.Next(3) == 0)
						fisher.rolledItemDrop = 4402;
					else
						fisher.rolledItemDrop = 4401;
				}
			}

			if (fisher.rolledItemDrop != 0)
				return;

			if (fisher.heightLevel < 2 && fisher.uncommon && fisher.questFish == 2461) {
				fisher.rolledItemDrop = 2461;
			}
			else if (fisher.heightLevel == 0 && fisher.uncommon && fisher.questFish == 2453) {
				fisher.rolledItemDrop = 2453;
			}
			else if (fisher.heightLevel == 0 && fisher.uncommon && fisher.questFish == 2473) {
				fisher.rolledItemDrop = 2473;
			}
			else if (fisher.heightLevel == 0 && fisher.uncommon && fisher.questFish == 2476) {
				fisher.rolledItemDrop = 2476;
			}
			else if (fisher.heightLevel < 2 && fisher.uncommon && fisher.questFish == 2458) {
				fisher.rolledItemDrop = 2458;
			}
			else if (fisher.heightLevel < 2 && fisher.uncommon && fisher.questFish == 2459) {
				fisher.rolledItemDrop = 2459;
			}
			else if (fisher.heightLevel == 0 && fisher.uncommon) {
				fisher.rolledItemDrop = 2304;
			}
			else if (fisher.heightLevel > 0 && fisher.heightLevel < 3 && fisher.uncommon && fisher.questFish == 2455) {
				fisher.rolledItemDrop = 2455;
			}
			else if (fisher.heightLevel == 1 && fisher.uncommon && fisher.questFish == 2479) {
				fisher.rolledItemDrop = 2479;
			}
			else if (fisher.heightLevel == 1 && fisher.uncommon && fisher.questFish == 2456) {
				fisher.rolledItemDrop = 2456;
			}
			else if (fisher.heightLevel == 1 && fisher.uncommon && fisher.questFish == 2474) {
				fisher.rolledItemDrop = 2474;
			}
			else if (fisher.heightLevel > 1 && fisher.rare && Main.rand.Next(5) == 0) {
				if (Main.hardMode && Main.rand.Next(2) == 0)
					fisher.rolledItemDrop = 2437;
				else
					fisher.rolledItemDrop = 2436;
			}
			else if (fisher.heightLevel > 1 && fisher.legendary && Main.rand.Next(3) != 0) {
				fisher.rolledItemDrop = 2308;
			}
			else if (fisher.heightLevel > 1 && fisher.veryrare && Main.rand.Next(2) == 0) {
				fisher.rolledItemDrop = 2320;
			}
			else if (fisher.heightLevel > 1 && fisher.rare) {
				fisher.rolledItemDrop = 2321;
			}
			else if (fisher.heightLevel > 1 && fisher.uncommon && fisher.questFish == 2478) {
				fisher.rolledItemDrop = 2478;
			}
			else if (fisher.heightLevel > 1 && fisher.uncommon && fisher.questFish == 2450) {
				fisher.rolledItemDrop = 2450;
			}
			else if (fisher.heightLevel > 1 && fisher.uncommon && fisher.questFish == 2464) {
				fisher.rolledItemDrop = 2464;
			}
			else if (fisher.heightLevel > 1 && fisher.uncommon && fisher.questFish == 2469) {
				fisher.rolledItemDrop = 2469;
			}
			else if (fisher.heightLevel > 2 && fisher.uncommon && fisher.questFish == 2462) {
				fisher.rolledItemDrop = 2462;
			}
			else if (fisher.heightLevel > 2 && fisher.uncommon && fisher.questFish == 2482) {
				fisher.rolledItemDrop = 2482;
			}
			else if (fisher.heightLevel > 2 && fisher.uncommon && fisher.questFish == 2472) {
				fisher.rolledItemDrop = 2472;
			}
			else if (fisher.heightLevel > 2 && fisher.uncommon && fisher.questFish == 2460) {
				fisher.rolledItemDrop = 2460;
			}
			else if (fisher.heightLevel > 1 && fisher.uncommon && Main.rand.Next(4) != 0) {
				fisher.rolledItemDrop = 2303;
			}
			else if (fisher.heightLevel > 1 && (fisher.uncommon || fisher.common || Main.rand.Next(4) == 0)) {
				if (Main.rand.Next(4) == 0)
					fisher.rolledItemDrop = 2303;
				else
					fisher.rolledItemDrop = 2309;
			}
			else if (fisher.uncommon && fisher.questFish == 2487) {
				fisher.rolledItemDrop = 2487;
			}
			else if (fisher.waterTilesCount > 1000 && fisher.common) {
				fisher.rolledItemDrop = 2298;
			}
			else {
				fisher.rolledItemDrop = 2290;
			}
		}
		private static void On_Projectile_AI_061_FishingBobber_GiveItemToPlayer(On_Projectile.orig_AI_061_FishingBobber_GiveItemToPlayer orig, Projectile self, Player thePlayer, int itemType) {
			if (self.TryGetAutoFisher(out AutoFisherTE autoFisherTE)) {
				AI_061_FishingBobber_GiveItemToPlayer(self, autoFisherTE.Player, itemType);
				return;
			}

			orig(self, thePlayer, itemType);
		}
		private static void AI_061_FishingBobber_GiveItemToPlayer(Projectile projectile, Player thePlayer, int itemType) {
			Item item = new Item();
			item.SetDefaults(itemType);
			if (itemType == 3196) {
				int finalFishingLevel = thePlayer.GetFishingConditions().FinalFishingLevel;
				int minValue = (finalFishingLevel / 20 + 3) / 2;
				int num = (finalFishingLevel / 10 + 6) / 2;
				if (Main.rand.Next(50) < finalFishingLevel)
					num++;

				if (Main.rand.Next(100) < finalFishingLevel)
					num++;

				if (Main.rand.Next(150) < finalFishingLevel)
					num++;

				if (Main.rand.Next(200) < finalFishingLevel)
					num++;

				int stack = Main.rand.Next(minValue, num + 1);
				item.stack = stack;
			}

			if (itemType == 3197) {
				int finalFishingLevel2 = thePlayer.GetFishingConditions().FinalFishingLevel;
				int minValue2 = (finalFishingLevel2 / 4 + 15) / 2;
				int num2 = (finalFishingLevel2 / 2 + 40) / 2;
				if (Main.rand.Next(50) < finalFishingLevel2)
					num2 += 6;

				if (Main.rand.Next(100) < finalFishingLevel2)
					num2 += 6;

				if (Main.rand.Next(150) < finalFishingLevel2)
					num2 += 6;

				if (Main.rand.Next(200) < finalFishingLevel2)
					num2 += 6;

				int stack2 = Main.rand.Next(minValue2, num2 + 1);
				item.stack = stack2;
			}

			PlayerLoader.ModifyCaughtFish(thePlayer, item);
			ItemLoader.CaughtFishStack(item);

			item.newAndShiny = true;

			int x = (int)thePlayer.position.X / 16;
			int y = (int)thePlayer.position.Y / 16 + 1;
			bool deposited = false;
			int chest1X = x - 2 * thePlayer.direction;
			int chest1Num = Chest.FindChest(chest1X, y);
			if (chest1Num != -1 && Chest.UsingChest(chest1Num) == -1) {
				deposited = Main.chest[chest1Num].item.Deposit(item, out _);
			}

			if (!deposited) {
				int chest2X = x + 2 * thePlayer.direction;
				int chest2Num = Chest.FindChest(chest2X, y);
				if (chest2Num != -1 && Chest.UsingChest(chest2Num) == -1) {
					deposited = Main.chest[chest2Num].item.Deposit(item, out _);
				}
			}

			//Item item2 = thePlayer.GetItem(projectile.owner, item, default(GetItemSettings));
			if (item.stack > 0) {
				int number = Item.NewItem(new EntitySource_OverfullInventory(thePlayer), (int)projectile.position.X, (int)projectile.position.Y, projectile.width, projectile.height, itemType, item.stack, noBroadcast: false, 0, noGrabDelay: true);
				if (Main.netMode == 1)
					NetMessage.SendData(21, -1, -1, null, number, 1f);
			}
			else {
				item.position.X = projectile.Center.X - (float)(item.width / 2);
				item.position.Y = projectile.Center.Y - (float)(item.height / 2);
				item.active = true;
				PopupText.NewText(PopupTextContext.RegularItemPickup, item, 0);
			}
		}

		#endregion
	}
	internal class AutoFisherTE : ModTileEntity {

		#region Properties

		public static AutoFisherTE NewAutoFisherTE => ModContent.GetInstance<AutoFisherTE>();

		private const string TEDollPlayerFieldName = "_dollPlayer";
		private const string AutoFisherItems = "AutoFisherItems";

		private TEDisplayDoll DisplayDoll {
			get {
				if (displayDoll == null) {
					TryGetDisplayDoll(Position.X, Position.Y);
				}

				return displayDoll;
			}
		}
		private TEDisplayDoll displayDoll = null;

		private ReflectionHelper Helper {
			get {
				if (helper == null) {
					helper = new(DisplayDoll);
				}

				return helper;
			}
		}
		private ReflectionHelper helper = null;

		public Player Player {
			get {
				if (player == null) {
					player = Helper.GetField<Player>(TEDollPlayerFieldName);
					player.position = new Vector2(Position.X, Position.Y + 3) * 16f + new Vector2(-Player.width / 2, -Player.height - StandHeight);
					player.isDisplayDollOrInanimate = true;
					player.whoAmI = 255;
				}

				return player;
			}
		}
		private Player player;

		public Item[] autoFishingItems;
		public const int StandHeight = 6;

		private int timer = 0;
		private static int timerReset = 60;
		private bool canUse = true;

		private static int bobberDelay = 30;
		public bool shouldPullBobber = false;

		private int checkAllTimer = 0;
		private static int checkAllDelay = 125;

		public static int ItemCheckFisherID { get; private set; } = -1;
		public static bool DrawingBobbers { get; private set; } = false;

		#endregion

		public AutoFisherTE() {
			autoFishingItems = new Item[8];
			for (int i = 0; i < autoFishingItems.Length; i++) {
				autoFishingItems[i] = new Item();
			}
		}

		#region Save/Load

		public override void SaveData(TagCompound tag) {
			tag[AutoFisherItems] = autoFishingItems;
		}
		public override void LoadData(TagCompound tag) {
			if (tag.TryGet(AutoFisherItems, out Item[] items))
				autoFishingItems = items;

			if (autoFishingItems.Length < 8) {
				Item[] items1 = new Item[8];
				for (int i = 0; i < 8; i++) {
					items1[i] = i < autoFishingItems.Length ? autoFishingItems[i].Clone() : new();
				}

				autoFishingItems = items1;
			}
		}

		#endregion

		#region Validation

		private bool TryGetDisplayDoll(int myX, int myY) {
			if (!ByPosition.TryGetValue(new Point16(myX - 1, myY), out TileEntity te) || te is not TEDisplayDoll foundDoll) {
				displayDoll = null;
				return false;
			}

			displayDoll = foundDoll;

			return true;
		}
		public static bool TileValidForEntity(int x, int y) {
			//Place on the top right tile of a mannequin/womannequin.
			Tile tile = Main.tile[x, y];
			if (tile.TileType != TileID.DisplayDoll)
				return false;

			if (tile.TileFrameY != 0)
				return false;

			if (tile.TileFrameX % 36 != 18)
				return false;

			return true;
		}
		public override bool IsTileValidForEntity(int x, int y) => TileValidForEntity(x, y);
		internal static void OnPlaceTEDisplayDoll(int id, int x, int y) {
			Tile tile = Main.tile[x, y];
			if (!tile.HasTile)
				return;

			if (tile.TileType != TileID.DisplayDoll)
				return;

			int fisherX = x + 1;
			if (!TileValidForEntity(fisherX, y))
				return;

			NewAutoFisherTE.PlaceAutoFisherTE(fisherX, y);
		}
		private void PlaceAutoFisherTE(int i, int j) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				NetMessage.SendData(MessageID.TileEntityPlacement, number: i, number2: j, number3: Type);
				return;
			}

			Place(i, j);
		}

		#endregion

		#region Update

		public override void Update() {
			bool isOccupied = Main.netMode != NetmodeID.SinglePlayer && IsOccupied(DisplayDoll.ID, out _);
			if (!isOccupied) {
				if (canUse) {
					timer++;
					if (timer >= timerReset) {
						timer = 0;
						timerReset = Main.rand.Next(30, 60);
						canUse = false;
						checkAllTimer = 0;

						Player.controlUseItem = true;
						Player.releaseUseItem = true;

						if (Main.netMode == NetmodeID.Server)
							SendPacketAutoFisherUseItem();
					}
				}
				else if (shouldPullBobber) {
					timer++;
					if (timer >= bobberDelay) {
						timer = 0;
						shouldPullBobber = false;
						checkAllTimer = 0;
						bobberDelay = Main.rand.Next(10, 60);
						ItemCheck_CheckFishingBobbers();
					}
				}
				else {
					checkAllTimer++;
					if (checkAllTimer >= checkAllDelay) {
						checkAllTimer = 0;
						CheckAllCanUse();
					}
				}
			}

			ItemCheck();
		}
		public void ItemCheck() {
			for (int i = 0; i < 8; i++) {
				Player.inventory[i] = autoFishingItems[i];
			}

			ItemCheckFisherID = ID;
			Player.ItemCheck();
			UpdatePlayerDirection();
			if (Player.itemAnimation == 0)
				Player.lastVisualizedSelectedItem = Player.HeldItem.Clone();

			ItemCheckFisherID = -1;
		}
		private void CheckAllCanUse() {
			if (Player.HeldItem.fishingPole > 0) {
				for (int i = 0; i < 1000; i++) {
					Projectile projectile = Main.projectile[i];
					if (projectile.active && projectile.bobber) {
						int autoFisherID = projectile.GetAutoFisherID();
						if (autoFisherID == ID) {
							return;
						}
					}
				}

				//$"CheckAllCanUse Reset".LogSimpleNT();
				canUse = true;
				shouldPullBobber = false;
			}
		}
		public void UpdatePlayerDirection() {
			Tile tile = Main.tile[Position.X - 1, Position.Y];
			Player.direction = tile.TileFrameX % 72 == 0 ? -1 : 1;
		}
		public static void UpdateAll() {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				foreach (TileEntity te in ByID.Select(p => p.Value)) {
					if (te is AutoFisherTE autoFisherTE) {
						autoFisherTE.ItemCheck();
					}
				}
			}
		}
		public void ItemCheck_CheckFishingBobbers() {
			for (int i = 0; i < 1000; i++) {
				Projectile projectile = Main.projectile[i];
				if (!projectile.active || !projectile.bobber)
					continue;

				if (!projectile.TryGetAutoFisher(out AutoFisherTE autoFisherTE) || autoFisherTE.ID != ID)
					continue;

				if (projectile.ai[0] != 0f)
					continue;

				projectile.ai[0] = 1f;
				float num = -10f;
				if (projectile.wet && projectile.velocity.Y > num)
					projectile.velocity.Y = num;

				projectile.netUpdate2 = true;
				canUse = true;
				if (projectile.ai[1] < 0f && projectile.localAI[1] != 0f) {
					Player.ItemCheck_CheckFishingBobber_PickAndConsumeBait(projectile, out var pullTheBobber, out var baitTypeUsed);
					if (pullTheBobber)
						Player.ItemCheck_CheckFishingBobber_PullBobber(projectile, baitTypeUsed);
				}
			}
		}

		#endregion

		#region Draw

		public void DrawProj_FishingLine(Projectile proj, ref float polePosX, ref float polePosY) {
			//Vanilla method uses Main.player[proj.owner]
			Vector2 mountedCenter = Player.MountedCenter;
			polePosX = mountedCenter.X;
			polePosY = mountedCenter.Y;
			polePosY += Player.gfxOffY;
			if (Player.mount.Active && Player.mount.Type == 52) {
				polePosX -= Player.direction * 14;
				polePosY -= -10f;
			}

			int direction = Player.direction;
			int type = Player.HeldItem.type;

			Color stringColor = new Color(200, 200, 200, 100);
			if (type == 2294)
				stringColor = new Color(100, 180, 230, 100);

			if (type == 2295)
				stringColor = new Color(250, 90, 70, 100);

			if (type == 2293)
				stringColor = new Color(203, 190, 210, 100);

			if (type == 2421)
				stringColor = new Color(183, 77, 112, 100);

			if (type == 2422)
				stringColor = new Color(255, 226, 116, 100);

			if (type == 4325)
				stringColor = new Color(200, 100, 100, 100);

			if (type == 4442)
				stringColor = new Color(100, 100, 200, 100);

			ProjectileLoader.ModifyFishingLine(proj, ref polePosX, ref polePosY, ref stringColor);
			stringColor = AutoFisherStaticMethods.TryApplyingPlayerStringColor(Player.stringColor, stringColor);

			float gravDir = Player.gravDir;
			switch (type) {
				case 2289:
					polePosX += 43 * direction;
					if (direction < 0)
						polePosX -= 13f;
					polePosY -= 36f * gravDir;
					break;
				case 2291:
					polePosX += 43 * direction;
					if (direction < 0)
						polePosX -= 13f;
					polePosY -= 34f * gravDir;
					break;
				case 2292:
					polePosX += 46 * direction;
					if (direction < 0)
						polePosX -= 13f;
					polePosY -= 34f * gravDir;
					break;
				case 2293:
					polePosX += 43 * direction;
					if (direction < 0)
						polePosX -= 13f;
					polePosY -= 34f * gravDir;
					break;
				case 2294:
					polePosX += 43 * direction;
					if (direction < 0)
						polePosX -= 13f;
					polePosY -= 30f * gravDir;
					break;
				case 2295:
					polePosX += 43 * direction;
					if (direction < 0)
						polePosX -= 13f;
					polePosY -= 30f * gravDir;
					break;
				case 2296:
					polePosX += 43 * direction;
					if (direction < 0)
						polePosX -= 13f;
					polePosY -= 30f * gravDir;
					break;
				case 2421:
					polePosX += 47 * direction;
					if (direction < 0)
						polePosX -= 13f;
					polePosY -= 36f * gravDir;
					break;
				case 2422:
					polePosX += 47 * direction;
					if (direction < 0)
						polePosX -= 13f;
					polePosY -= 32f * gravDir;
					break;
				case 4325:
					polePosX += 44 * direction;
					if (direction < 0)
						polePosX -= 13f;
					polePosY -= 32f * gravDir;
					break;
				case 4442:
					polePosX += 44 * direction;
					if (direction < 0)
						polePosX -= 13f;
					polePosY -= 32f * gravDir;
					break;
			}

			if (gravDir == -1f)
				polePosY -= 12f;

			Vector2 vector = new Vector2(polePosX, polePosY);
			vector = Player.RotatedRelativePoint(vector + new Vector2(8f)) - new Vector2(8f);
			float num = proj.position.X + (float)proj.width * 0.5f - vector.X;
			float num2 = proj.position.Y + (float)proj.height * 0.5f - vector.Y;
			Math.Sqrt(num * num + num2 * num2);
			float num3 = (float)Math.Atan2(num2, num) - 1.57f;
			bool flag = true;
			if (num == 0f && num2 == 0f) {
				flag = false;
			}
			else {
				float num4 = (float)Math.Sqrt(num * num + num2 * num2);
				num4 = 12f / num4;
				num *= num4;
				num2 *= num4;
				vector.X -= num;
				vector.Y -= num2;
				num = proj.position.X + (float)proj.width * 0.5f - vector.X;
				num2 = proj.position.Y + (float)proj.height * 0.5f - vector.Y;
			}

			while (flag) {
				float num5 = 12f;
				float num6 = (float)Math.Sqrt(num * num + num2 * num2);
				float num7 = num6;
				if (float.IsNaN(num6) || float.IsNaN(num7)) {
					flag = false;
					continue;
				}

				if (num6 < 20f) {
					num5 = num6 - 8f;
					flag = false;
				}

				num6 = 12f / num6;
				num *= num6;
				num2 *= num6;
				vector.X += num;
				vector.Y += num2;
				num = proj.position.X + (float)proj.width * 0.5f - vector.X;
				num2 = proj.position.Y + (float)proj.height * 0.1f - vector.Y;
				if (num7 > 12f) {
					float num8 = 0.3f;
					float num9 = Math.Abs(proj.velocity.X) + Math.Abs(proj.velocity.Y);
					if (num9 > 16f)
						num9 = 16f;

					num9 = 1f - num9 / 16f;
					num8 *= num9;
					num9 = num7 / 80f;
					if (num9 > 1f)
						num9 = 1f;

					num8 *= num9;
					if (num8 < 0f)
						num8 = 0f;

					num9 = 1f - proj.localAI[0] / 100f;
					num8 *= num9;
					if (num2 > 0f) {
						num2 *= 1f + num8;
						num *= 1f - num8;
					}
					else {
						num9 = Math.Abs(proj.velocity.X) / 3f;
						if (num9 > 1f)
							num9 = 1f;

						num9 -= 0.5f;
						num8 *= num9;
						if (num8 > 0f)
							num8 *= 2f;

						num2 *= 1f + num8;
						num *= 1f - num8;
					}
				}

				num3 = (float)Math.Atan2(num2, num) - 1.57f;
				Color color = Lighting.GetColor((int)vector.X / 16, (int)(vector.Y / 16f), stringColor);
				Main.EntitySpriteDraw(TextureAssets.FishingLine.Value, new Vector2(vector.X - Main.screenPosition.X + (float)TextureAssets.FishingLine.Width() * 0.5f, vector.Y - Main.screenPosition.Y + (float)TextureAssets.FishingLine.Height() * 0.5f), new Microsoft.Xna.Framework.Rectangle(0, 0, TextureAssets.FishingLine.Width(), (int)num5), color, num3, new Vector2((float)TextureAssets.FishingLine.Width() * 0.5f, 0f), 1f, SpriteEffects.None);
			}
		}
		internal void DrawProjectiles() {
			DrawingBobbers = true;
			foreach (Projectile projectile in Main.projectile) {
				if (!projectile.active || projectile.owner != 255)
					continue;

				if (projectile.TryGetAutoFisher(out AutoFisherTE autoFisherTE) && autoFisherTE.ID == ID) {
					Main.instance.DrawProjDirect(projectile);
				}
			}

			DrawingBobbers = false;
		}

		#endregion

		#region Net

		internal static void ReadAutoFisherUseItem(BinaryReader reader) {
			int id = reader.ReadInt32();
			if (ByID.TryGetValue(id, out TileEntity te) && te is AutoFisherTE autoFisherTE) {
				autoFisherTE.Player.controlUseItem = true;
				autoFisherTE.Player.releaseUseItem = true;
			}
		}
		private void SendPacketAutoFisherUseItem() {
			ModPacket modPacket = EngagedSkyblock.Instance.GetPacket();
			modPacket.Write((byte)ModPacketID.AutoFisherUseItem);
			modPacket.Write(ID);
			modPacket.Send();
		}
		internal static void RequestAllTEsToClient(int clientWhoAmI) {
			//$"RequestAllTEsToClient".LogSimpleNT();
			foreach (TileEntity te in TileEntity.ByID.Select(p => p.Value)) {
				TrySendAllItems(te, clientWhoAmI);
			}
		}
		internal static void TrySendAllItems(int teID, int targetClientWhoAmI = -1, int ignoreClient = -1) {
			if (ByID.TryGetValue(teID, out TileEntity te)) {
				TrySendAllItems(te, targetClientWhoAmI, ignoreClient);
			}
		}
		private static void TrySendAllItems(TileEntity te, int targetClientWhoAmI = -1, int ignoreClient = -1) {
			if (te is TEDisplayDoll displayDoll && displayDoll.TryGetAutoFisherTE(out AutoFisherTE autoFisherTE)) {
				//$"TrySendAllItems(teID: {te.ID}, targetClientWhoAmI: {targetClientWhoAmI}), targetClientWhoAmI: {targetClientWhoAmI}, ignoreClient: {ignoreClient}".LogSimpleNT();
				for (int i = 0; i < 8; i++) {
					autoFisherTE.SendItem(i, targetClientWhoAmI, ignoreClient);
				}
			}
		}
		internal void SendItem(int index, int sendTarget = -1, int ignoreClient = -1) {
			ModPacket modPacket = EngagedSkyblock.Instance.GetPacket();
			modPacket.Write((byte)ModPacketID.AutoFisherItemSync);
			modPacket.Write(ID);
			modPacket.Write((byte)index);
			Item item = autoFishingItems[index];
			modPacket.Write(item.type);
			modPacket.Write(item.prefix);
			modPacket.Write(item.stack);
			modPacket.Write(item.favorited);
			modPacket.Send(sendTarget, ignoreClient);
			//$"Send Item: {item.S()} ({item.stack}), index: {index}, sendTarget: {sendTarget}, ignoreClient: {ignoreClient}".LogSimpleNT();
		}
		internal static void ReceiveItem(BinaryReader reader, int ignoreClient = -1) {
			int id = reader.ReadInt32();
			int index = reader.ReadByte();
			int type = reader.ReadInt32();
			int prefix = reader.ReadInt32();
			int stack = reader.ReadInt32();
			bool favorited = reader.ReadBoolean();
			if (ByID.TryGetValue(id, out TileEntity te) && te is AutoFisherTE autoFisherTE) {
				Item item = autoFisherTE.autoFishingItems[index];
				item.SetDefaults(type);
				item.Prefix(prefix);
				item.stack = stack;
				item.favorited = favorited;
				//$"Recieve Item: {item.S()} ({item.stack}), index: {index}, ignoreClient: {ignoreClient}".LogSimpleNT();
				if (ignoreClient != -1)
					autoFisherTE.SendItem(index, ignoreClient: ignoreClient);
			}
		}

		#endregion
	}
	internal static class AutoFisherStaticMethods {
		private static MethodInfo AI_061_FishingBobber_DoASplashInfo = typeof(Projectile).GetMethod("AI_061_FishingBobber_DoASplash", BindingFlags.NonPublic | BindingFlags.Instance);
		public static void AI_061_FishingBobber_DoASplash(this Projectile projectile) => AI_061_FishingBobber_DoASplashInfo.Invoke(projectile, null);
		private static MethodInfo ReduceRemainingChumsInPoolInfo = typeof(Projectile).GetMethod("ReduceRemainingChumsInPool", BindingFlags.NonPublic | BindingFlags.Instance);
		public static void ReduceRemainingChumsInPool(this Projectile projectile) => ReduceRemainingChumsInPoolInfo.Invoke(projectile, null);
		private static MethodInfo AI_061_FishingBobber_GetWaterLineInfo = typeof(Projectile).GetMethod("AI_061_FishingBobber_GetWaterLine", BindingFlags.NonPublic | BindingFlags.Instance);
		public static float AI_061_FishingBobber_GetWaterLine(this Projectile projectile, int X, int Y) => (float)AI_061_FishingBobber_GetWaterLineInfo.Invoke(projectile, [X, Y]);
		//private static MethodInfo AI_061_FishingBobber_GiveItemToPlayerInfo = typeof(Projectile).GetMethod("AI_061_FishingBobber_GiveItemToPlayer", BindingFlags.NonPublic | BindingFlags.Instance);
		//public static void AI_061_FishingBobber_GiveItemToPlayer(this Projectile projectile, Player player, int itemType) => AI_061_FishingBobber_GiveItemToPlayerInfo.Invoke(projectile, [player, itemType]);
		private static MethodInfo GetFishingPondStateInfo = typeof(Projectile).GetMethod("GetFishingPondState", BindingFlags.NonPublic | BindingFlags.Static);
		public static void GetFishingPondState(int x, int y, out bool lava, out bool honey, out int numWaters, out int chumCount) {
			lava = false;
			honey = false;
			numWaters = 0;
			chumCount = 0;
			object[] paramaters = new object[] { x, y, lava, honey, numWaters, chumCount };
			GetFishingPondStateInfo.Invoke(null, paramaters);
			lava = (bool)paramaters[2];
			honey = (bool)paramaters[3];
			numWaters = (int)paramaters[4];
			chumCount = (int)paramaters[5];
		}
		//private static MethodInfo FishingCheck_RollEnemySpawnsInfo = typeof(Projectile).GetMethod("FishingCheck_RollEnemySpawns", BindingFlags.NonPublic | BindingFlags.Instance);
		//public static void FishingCheck_RollEnemySpawns(this Projectile projectile, ref FishingAttempt fisher) => FishingCheck_RollEnemySpawnsInfo.Invoke(projectile, [fisher]);
		private static MethodInfo ItemCheck_CheckFishingBobber_PickAndConsumeBaitInfo = typeof(Player).GetMethod("ItemCheck_CheckFishingBobber_PickAndConsumeBait", BindingFlags.NonPublic | BindingFlags.Instance);
		public static void ItemCheck_CheckFishingBobber_PickAndConsumeBait(this Player player, Projectile bobber, out bool pullTheBobber, out int baitTypeUsed) {
			pullTheBobber = false;
			baitTypeUsed = 0;
			object[] parameters = [bobber, pullTheBobber, baitTypeUsed];
			ItemCheck_CheckFishingBobber_PickAndConsumeBaitInfo.Invoke(player, parameters);
			pullTheBobber = (bool)parameters[1];
			baitTypeUsed = (int)parameters[2];
		}
		private static MethodInfo ItemCheck_CheckFishingBobber_PullBobberInfo = typeof(Player).GetMethod("ItemCheck_CheckFishingBobber_PullBobber", BindingFlags.NonPublic | BindingFlags.Instance);
		public static void ItemCheck_CheckFishingBobber_PullBobber(this Player player, Projectile bobber, int baitTypeUsed) => ItemCheck_CheckFishingBobber_PullBobberInfo.Invoke(player, [bobber, baitTypeUsed]);
		//private static MethodInfo ItemCheckWrappedInfo = typeof(Player).GetMethod("ItemCheckWrapped", BindingFlags.NonPublic | BindingFlags.Instance);
		//public static void ItemCheckWrapped(this Player player, int i) => ItemCheckWrappedInfo.Invoke(player, [i]);
		private static MethodInfo TryFittingInfo = typeof(TEDisplayDoll).GetMethod("TryFitting", BindingFlags.NonPublic | BindingFlags.Instance);
		public static bool TryFitting(this TEDisplayDoll teDisplayDoll, Item[] inv, int context = 0, int slot = 0, bool justCheck = false) => (bool)TryFittingInfo.Invoke(teDisplayDoll, [inv, context, slot, justCheck]);
		private static MethodInfo TryApplyingPlayerStringColorInfo = typeof(Main).GetMethod("TryApplyingPlayerStringColor", BindingFlags.NonPublic | BindingFlags.Static);
		public static Color TryApplyingPlayerStringColor(int colorInd, Color color) => (Color)TryApplyingPlayerStringColorInfo.Invoke(null, [colorInd, color]);

		public static bool TryGetAutoFisher(this Projectile projectile, out AutoFisherTE autoFisherTE) {
			autoFisherTE = null;
			if (!projectile.bobber)
				return false;

			int autoFisherID = projectile.GetAutoFisherID();
			if (autoFisherID <= autoFisherIdDefaultValue)
				return false;

			if (!ModTileEntity.ByID.TryGetValue(autoFisherID, out TileEntity te) || te is not AutoFisherTE foundAutoFisherTE)
				return false;

			autoFisherTE = foundAutoFisherTE;
			return true;
		}
		private static int autoFisherIdDefaultValue = 0;
		public static int GetAutoFisherID(this Projectile projectile) => (int)projectile.knockBack - 1;
		public static int AutoFisherIDToEncodedID(this AutoFisherTE autoFisherTE) => autoFisherTE.ID + 1;
		public static bool TryGetAutoFisherTE(this TEDisplayDoll teDisplayDoll, out AutoFisherTE autoFisherTE) {
			autoFisherTE = null;
			if (!TileEntity.ByPosition.TryGetValue(new Point16(teDisplayDoll.Position.X + 1, teDisplayDoll.Position.Y), out TileEntity te) || te is not AutoFisherTE foundAutoFisherTE)
				return false;

			autoFisherTE = foundAutoFisherTE;
			return true;
		}
		public static bool TryGetAutoFisherTE(this Player dollPlayer, out AutoFisherTE autoFisherTE) {
			autoFisherTE = null;
			if (!TileEntity.ByPosition.TryGetValue(new Point16((int)dollPlayer.position.X / 16 + 1, (int)(dollPlayer.position.Y + AutoFisherTE.StandHeight) / 16), out TileEntity te) || te is not AutoFisherTE foundAutoFisherTE)
				return false;

			autoFisherTE = foundAutoFisherTE;
			return true;
		}
	}
}
