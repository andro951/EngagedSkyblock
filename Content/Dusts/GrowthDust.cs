﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace EngagedSkyblock.Content.Dusts
{
    public class GrowthDust : ModDust {
		public override void OnSpawn(Dust dust) {
			dust.velocity *= 0.1f;
			dust.noGravity = true;
			dust.noLight = true;
			dust.scale *= 1.5f;
			dust.color = new Color(0, 255, 0, 100);
		}
		public override bool Update(Dust dust) {
			dust.position += dust.velocity;
			dust.rotation += dust.velocity.X * 0.15f;
			dust.scale *= 0.95f;

			float light = 0.1f * dust.scale;

			Lighting.AddLight(dust.position, light, light, light);

			if (dust.scale < 0.5f) {
				dust.active = false;
			}

			return false;
		}
	}
}
