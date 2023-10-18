using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EngagedSkyblock.Weather {
	//public class SnowFlakeSpawner : ModProjectile {
	//	private const int spawnAIIndex = 0;
	//	private void SnowFlakeSpawner_AI() {
	//		if (!Main.raining) {
	//			Projectile.Kill();
	//			return;
	//		}

	//		if (!ES_Weather.Snowing) {
	//			return;
	//		}

	//		Projectile.ai[spawnAIIndex] += (float)Main.desiredWorldEventsUpdateRate;
	//		if (Projectile.localAI[spawnAIIndex] == 0f && Main.netMode != NetmodeID.Server) {
	//			Projectile.localAI[spawnAIIndex] = 1f;
	//			if ((double)Main.LocalPlayer.position.Y < Main.worldSurface * 16.0)
	//				SnowFlakeManager.SnowFlakeFall(Projectile.position.X);
	//		}
	//	}
	//}
}
