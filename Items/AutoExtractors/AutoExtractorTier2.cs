﻿using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using EngagedSkyblock.Tiles.AutoExtractors;

namespace EngagedSkyblock.Items.AutoExtractors {
    public class AutoExtractorTier2 : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 16;

            Item.placeStyle = 0;
            Item.consumable = true;
            Item.autoReuse = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.maxStack = Item.CommonMaxStack;
            Item.createTile = ModContent.TileType<AutoExtractorTier2Tile>();
            Item.rare = ItemRarityID.Orange;
            Item.useTime = 10;
            Item.useTurn = true;
            Item.useAnimation = 15;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<AutoExtractor>())
                .AddIngredient(ItemID.HellstoneBar, 5)
                .AddIngredient(ItemID.Diamond, 3)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}