using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.WorldBuilding;

namespace EngagedSkyblock {
	public class ES_ModSystem : ModSystem {
		//public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight) {
		//	ES_WorldGen.ModifyWorldGenTasks(tasks, ref totalWeight);
		//}
		public override void OnWorldLoad() {
			ES_WorldGen.OnWorldLoad();
		}
		/*
		public static Task CreateNewWorld(GenerationProgress progress = null)
	{
		generatingWorld = true;
		Main.rand = new UnifiedRandom(Main.ActiveWorldFileData.Seed);
		gen = true;
		Main.menuMode = 888;
		try {
			Main.MenuUI.SetState(new UIWorldLoad());
		}
		catch {
		}

		return Task.Factory.StartNew(worldGenCallback, progress);
	} 
		*/
	}
}
