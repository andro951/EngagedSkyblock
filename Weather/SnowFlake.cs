using androLib.Common.Utility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.Map;
using Terraria.ModLoader;

namespace EngagedSkyblock.Weather {
	public class SnowFlake : ModProjectile {
		public override void SetDefaults() {
			Projectile.aiStyle = 0;
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.ignoreWater = false;
			Projectile.light = 1f;
			Projectile.tileCollide = true;
			Projectile.timeLeft = 600;
			Projectile.coldDamage = true;
		}
		public static void TrySpawnSnowFlake() {
			if (Main.netMode == NetmodeID.Server)
				return;

			if (!Main.raining)
				return;

			if (!ES_Weather.CanRain)
				return;

			if (!Tiles.RainTotem.TotemActive())
				return;

			//int snowBlocks = Main.SceneMetrics.SnowTileCount;
			if (!AndroUtilityMethods.Snowing)
				return;

			//if (y > SnowStartHeight)
			//	return;

			float snowChance = ES_Weather.SnowMultiplier * 0.05f;
			if (snowChance <= 0f)
				return;

			if (Main.rand.NextFloat() > snowChance)
				return;

			SpawnSnowFlake();
		}
		private static void SpawnSnowFlake() {
			Vector2 scaledSize = Main.Camera.ScaledSize;
			Vector2 scaledPosition = Main.Camera.ScaledPosition;
			float num = (float)Main.SceneMetrics.SnowTileCount / (float)SceneMetrics.SnowTileMax;
			num *= num;
			num *= num;
			float num2 = Main.Camera.ScaledSize.X / (float)Main.maxScreenW;
			int num3 = (int)(500f * num2);
			num3 = (int)((float)num3 * (1f + 2f * Main.cloudAlpha));
			float num4 = 1f + 50f * Main.cloudAlpha;
			bool flag = NPC.IsADeerclopsNearScreen();
			if (flag) {
				num /= 20f;
				num3 /= 3;
			}

			for (int i = 0; i < num4; i++) {
				if (!((float)Main.snowDust < (float)num3 * (Main.gfxQuality / 2f + 0.5f) + (float)num3 * 0.1f))
					break;

				if (!(Main.rand.NextFloat() < num))
					continue;

				int num5 = Main.rand.Next((int)scaledSize.X + 1500) - 750;
				int num6 = (int)scaledPosition.Y - Main.rand.Next(50);
				if (Main.player[Main.myPlayer].velocity.Y > 0f)
					num6 -= (int)Main.player[Main.myPlayer].velocity.Y;

				if (Main.rand.Next(5) == 0)
					num5 = Main.rand.Next(500) - 500;
				else if (Main.rand.Next(5) == 0)
					num5 = Main.rand.Next(500) + (int)scaledSize.X;

				if (num5 < 0 || (float)num5 > scaledSize.X)
					num6 += Main.rand.Next((int)((double)scaledSize.Y * 0.8)) + (int)((double)scaledSize.Y * 0.1);

				num5 += (int)scaledPosition.X;
				int num7 = num5 / 16;
				int num8 = num6 / 16;
				if (WorldGen.InWorld(num7, num8) && Main.tile[num7, num8] != null && !Main.tile[num7, num8].HasUnactuatedTile && Main.tile[num7, num8].WallType == 0) {
					int num9 = Projectile.NewProjectile(null, num5, num6, 10, 10, ModContent.ProjectileType<SnowFlake>(), 8, 2);//  Dust.NewDust(new Vector2(num5, num6), 10, 10, DustID.Snow);
					Main.projectile[num9].scale += Main.cloudAlpha * 0.2f;
					Main.projectile[num9].velocity.Y = 3f + (float)Main.rand.Next(30) * 0.1f;
					Main.projectile[num9].velocity.Y *= Main.projectile[num9].scale;
					if (!Main.raining) {
						Main.projectile[num9].velocity.X = Main.windSpeedCurrent + (float)Main.rand.Next(-10, 10) * 0.1f;
						Main.projectile[num9].velocity.X += Main.windSpeedCurrent * 15f;
					}
					else {
						Main.projectile[num9].velocity.X = (float)Math.Sqrt(Math.Abs(Main.windSpeedCurrent)) * (float)Math.Sign(Main.windSpeedCurrent) * (Main.cloudAlpha + 0.5f) * 10f + Main.rand.NextFloat() * 0.2f - 0.1f;
						Main.projectile[num9].velocity.Y *= 0.5f;
					}

					Main.projectile[num9].velocity.Y *= 1f + 0.3f * Main.cloudAlpha;
					Main.projectile[num9].scale += Main.cloudAlpha * 0.2f;
					if (flag)
						Main.projectile[num9].scale -= 0.5f;

					Main.projectile[num9].velocity *= 1f + Main.cloudAlpha * 0.5f;
					Main.projectile[num9].netUpdate = true;
				}
			}

			//Player player = Main.LocalPlayer;
			//float temp = Main.windSpeedCurrent;
			//bool wind = Main.windPhysics;
			//float temp2 = Main.windPhysicsStrength;
			//float target = Main.windSpeedTarget;
			//Projectile.NewProjectile(null, player.Center.X, 16f, 0, 5, ModContent.ProjectileType<SnowFlake>(), 1000, 10f, player.whoAmI);
		}
		public override void OnKill(int timeLeft) {
			if (Projectile.scale >= 0.75f)
				Item.NewItem(Projectile.GetSource_DropAsItem(), (int)Projectile.position.X, (int)Projectile.position.Y, Projectile.width, Projectile.height, ItemID.Snowball);
		}

		public override void OnSpawn(IEntitySource source) {
			Projectile.scale = Main.rand.NextFloat(0.5f, 2f);
		}

		public override void AI() {
			if (!Main.raining) {
				Projectile.Kill();
				return;
			}

			if (!AndroUtilityMethods.Snowing) {
				Projectile.Kill();
				return;
			}

			if (Projectile.ai[1] == 0f && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height)) {
				Projectile.ai[1] = 1f;
				Projectile.netUpdate = true;
			}

			//if (Projectile.soundDelay == 0) {
			//	Projectile.soundDelay = 20 + Main.rand.Next(40);
			//	SoundEngine.PlaySound(SoundID.Item9, Projectile.position);
			//}

			if (Projectile.localAI[0] == 0f)
				Projectile.localAI[0] = 1f;

			Projectile.alpha += (int)(25f * Projectile.localAI[0]);
			if (Projectile.alpha > 200) {
				Projectile.alpha = 200;
				Projectile.localAI[0] = -1f;
			}

			if (Projectile.alpha < 0) {
				Projectile.alpha = 0;
				Projectile.localAI[0] = 1f;
			}

			Projectile.rotation += (Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y) * 0.01f * (float)Projectile.direction);

			Vector2 vect = new(Main.screenWidth, Main.screenHeight);
			if (Projectile.Hitbox.Intersects(Utils.CenteredRectangle(Main.screenPosition + vect / 2f, vect + new Vector2(400f))) && Main.rand.Next(20) == 0) {
				int goreType = GoreID.DD2DarkMageT1_8;
				Gore.NewGore(null, Projectile.position, Projectile.velocity * 0.2f, goreType, 0.5f);
			}

			Projectile.light = 0.9f;
			//if (Main.rand.Next(20) == 0)
			//	Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Snow, Projectile.velocity.X, Projectile.velocity.Y, 150, default, 1.2f);


		}
	}
}
