using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using TerrariaAutomations.Tiles.TileEntities;

namespace EngagedSkyblock.Tiles.TileEntities {
    public class VanillaAutohammerTE : AutohammerTE {
        public override int Timer => 60;
        protected override int ConsumeMultiplier => 1;
        protected override int TileToBeValidOn => TileID.Autohammer;
    }
}