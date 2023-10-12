using EngagedSkyblock.Common.Globals;
using MonoMod.RuntimeDetour;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace EngagedSkyblock
{
	public class EngagedSkyblock : Mod {
		private static List<Hook> hooks = new();
		public override void Load() {
			hooks.Add(new(ES_WorldGen.ModLoaderModSystemModifyWorldGenTasks, ES_WorldGen.ModSystem_ModifyWorldGenTasks_Detour));
			hooks.Add(new(ES_GlobalTile.TileLoaderDrop, ES_GlobalTile.TileLoader_Drop_Detour));

			foreach (Hook hook in hooks) {
				hook.Apply();
			}

			ES_WorldGen.Load();
			ES_Liquid.Load();
		}
	}
}