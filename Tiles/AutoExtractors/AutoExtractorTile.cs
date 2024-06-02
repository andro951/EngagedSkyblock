using System;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Terraria;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TileFunctionLibrary.API;
using EngagedSkyblock.Tiles.TileEntities;

namespace EngagedSkyblock.Tiles {
    public abstract class AutoExtractorTile : ES_ModTile {
		public abstract AutoExtractorTE NewEntity { get; }
		public abstract Func<int> MyItemType { get; }
        public abstract int Tier { get; }
        public abstract float UseSpeedMultiplier { get; }
		public override void SetStaticDefaults() {
            // Properties
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileContainer[Type] = true;
            Main.tileLavaDeath[Type] = true;
            TileID.Sets.HasOutlines[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;
            TileID.Sets.BasicChest[Type] = true;
            TileID.Sets.InteractibleByNPCs[Type] = true;
            TileID.Sets.IsAContainer[Type] = true;
            TileID.Sets.HasOutlines[Type] = false;

            DustType = DustID.Stone;
            AdjTiles = new int[] { TileID.Extractinator };
            // Names
            AddMapEntry(new Color(200, 200, 200), CreateMapEntryName());

			// Placement
			AnimationFrameHeight = 54;
			TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
            TileObjectData.newTile.DrawYOffset = 2;
			//TileObjectData.newTile.Origin = new Point16(0, 1);
			TileObjectData.newTile.LavaDeath = false;
            TileObjectData.addTile(Type);
        }
        public override LocalizedText DefaultContainerName(int frameX, int frameY) {
            return CreateMapEntryName();
        }

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) {
            return true;
        }

        public override void ModifySmartInteractCoords(ref int width, ref int height, ref int frameWidth, ref int frameHeight, ref int extraY) {
            width = 3;
            height = 1;
            extraY = 0;
        }

        public override void AnimateTile(ref int frame, ref int frameCounter) {
            frame = Main.tileFrame[TileID.Extractinator];
            frameCounter = Main.tileFrameCounter[TileID.Extractinator];
        }
        public override void MouseOverFar(int i, int j) {
			Player player = Main.LocalPlayer;
			player.cursorItemIconText = "";
		}

        public override void MouseOver(int i, int j) {
            Player player = Main.LocalPlayer;
			player.cursorItemIconID = MyItemType();
			player.cursorItemIconText = "";
        }
    }
}
