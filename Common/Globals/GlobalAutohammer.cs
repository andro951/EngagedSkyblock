using androLib.Common.Utility;
using EngagedSkyblock.Tiles.TileEntities;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaAutomations.Common.Globals;
using TerrariaAutomations.Tiles;
using TerrariaAutomations.Tiles.TileEntities;

namespace EngagedSkyblock.Common.Globals {
    public class GlobalAutohammer : GlobalExtractorBase {
        public static GlobalAutohammer Instance;
        public static bool IsAutohammerTile(int tileType) => autohammerTileTypes.Contains(tileType);
        private static HashSet<int> autohammerTileTypes = [];
        public static int GetTier(int autohammerBlockType) {
            //if (TileLoader.GetTile(autohammerBlockType) is AutohammerTile autohammerTile)
            //    return autohammerTile.Tier;

            if (autohammerBlockType == TileID.Autohammer)
                return 0;

            return 0;
        }
        public override void Load() {
            Instance = this;
        }
        public static void PostSetupContent() {
            List<int> vanillaAutohammers = new() {
                TileID.Autohammer
            };

            foreach (int type in vanillaAutohammers) {
                Main.tileContainer[type] = true;
                TileID.Sets.BasicChest[type] = true;
                TileID.Sets.IsAContainer[type] = true;
            }

            autohammerTileTypes.Add(TileID.Autohammer);

            AddExtractorBaseTEGetter(TileID.Autohammer, ModContent.GetInstance<VanillaAutohammerTE>);

            Vector2 offset = new(-6, 12);
            AddChestIndicatorOffset(TileID.Autohammer, offset);
        }
    }
}