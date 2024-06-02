using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using Terraria.ObjectData;
using Terraria.Enums;
using androLib;
using EngagedSkyblock.Tiles;
using System;
using System.Collections.Generic;
using Terraria.DataStructures;
using androLib.Items;
using androLib.Common.Utility;

namespace EngagedSkyblock.Items {
	[Autoload(false)]
	public abstract class AutoExtractinator : ES_ModItem {
        public abstract int CreateTile { get; }
        public abstract int Rarity { get; }
        public abstract List<(int, int)> Ingredients { get; }
        public abstract int RecipeRequiredTile { get; }
        public abstract int Tier { get; }
		public override string Artist => null;
		public override string Designer => "andro951";
		public override List<WikiTypeID> WikiItemTypes => new() { WikiTypeID.Mechanism, WikiTypeID.Storage };
		public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 1;
        }
        public override void SetDefaults() {
            Item.width = 16;
            Item.height = 16;

            Item.placeStyle = 0;
            Item.consumable = true;
            Item.autoReuse = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.maxStack = Item.CommonMaxStack;
            Item.createTile = CreateTile;
            Item.rare = Rarity;
            Item.useTime = 10;
            Item.useTurn = true;
            Item.useAnimation = 15;
        }

        public override void AddRecipes() {
            Recipe recipe = CreateRecipe();
            foreach ((int itemType, int stack) p in Ingredients) {
                recipe.AddIngredient(p.itemType, p.stack);
            }

            recipe.AddTile(RecipeRequiredTile)
                .Register();
        }
    }
}
