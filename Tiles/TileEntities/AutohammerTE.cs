using EngagedSkyblock.Common.Globals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent;
using TerrariaAutomations.Tiles.TileEntities;
using TerrariaAutomations.Tiles;

namespace EngagedSkyblock.Tiles.TileEntities {
    public abstract class AutohammerTE : ExtractorBaseTE {
        public static readonly ItemTrader ItemTrader = new();
        public override void Load() {
            base.Load();

            ExtractionItem.AddItemTrader((Tile tile) => {
                if (GlobalAutohammer.IsAutohammerTile(tile.TileType)) {
                    return ItemTrader;
                }

                return null;
            });
        }
    }
}