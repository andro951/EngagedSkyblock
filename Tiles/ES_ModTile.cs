using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace EngagedSkyblock.Tiles {
	public abstract class ES_ModTile : ModTile {
		public override string Texture => (GetType().Namespace + ".Sprites." + Name).Replace('.', '/');
	}
}
