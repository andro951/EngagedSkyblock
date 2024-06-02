using androLib.Common.Utility;
using androLib.Common.Utility.LogSystem.WebpageComponenets;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Channels;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace EngagedSkyblock.Tiles {
    internal class ExtractionItem : GlobalItem {
        public struct TypeChancePair {
            public int ItemType;
            public float Chance;
            public TypeChancePair(int itemType, float chance) {
                ItemType = itemType;
                Chance = chance;
			}
			public override string ToString() {
				return $"{ItemType.GetItemIDOrName()} ({Chance.PercentString()})";
			}
		}
        public struct ExtractTypeSet {
            public static SortedDictionary<int, ExtractTypeSet> AllExtractTypes = new();
            public int DefaultResult { get; private set; }
            public readonly List<TypeChancePair> ExtractResults;
            public const int StackChancePerTier = 8;//%
            public const float RareChancePerTier = 0.25f;

			public ExtractTypeSet(IEnumerable<TypeChancePair> extractResults, int defaultResult = ItemID.None) {
                DefaultResult = defaultResult;
                ExtractResults = extractResults.GroupBy(er => er.Chance).Select(g => g.ToList().OrderBy(er => er.ItemType)).SelectMany(l => l).ToList();
            }
            public void GetResult(int extractinatorBlockType, ref int resultType, ref int resultStack) {
                int tier = 0;//TODO from extractinatorBlockType
                resultStack = 1;
                int stackRand = Main.rand.Next(100);
                int stackChance = tier * StackChancePerTier;
                //if (resultStack < 1)
                //    resultStack = 1;

                if (stackRand < stackChance)
                    resultStack *= 2;

				float rand = Main.rand.NextFloat();
                float runningTotal = 0f;
                float mult = 1f + RareChancePerTier * tier;
                foreach (TypeChancePair p in ExtractResults) {
                    runningTotal += p.Chance * mult;
                    if (rand < runningTotal) {
                        resultType = p.ItemType;
                        return;
                    }
                }

                resultType = DefaultResult;
            }
            public static void PostSetupContent() {
                AllExtractTypes = new() {
                    { ExtractID.Sandstone, new(new List<TypeChancePair>() {
                        new(ItemID.Amber, 0.005f),
                        new(ItemID.SandBlock, 0.5f),
                    }) },

                };
			}
			public override string ToString() {
                string s = "";
				float runningTotal = 0f;
                bool first = true;
				foreach (TypeChancePair p in ExtractResults) {
                    if (first) {
                        first = false;
                    }
                    else {
                        s += ", ";
                    }

					runningTotal += p.Chance;
                    float chance = Math.Min(p.Chance, 1f - runningTotal);
                    s += $"{p.ItemType.GetItemIDOrName()} ({p.Chance.PercentString()})";
				}

                return s;
			}
		}
        private static void ExtractinatorUseDetour(ref int resultType, ref int resultStack, int extractType, int extractinatorBlockType) {
            if (ExtractTypeSet.AllExtractTypes.TryGetValue(extractType, out ExtractTypeSet extractTypeSet))
                extractTypeSet.GetResult(extractinatorBlockType, ref resultType, ref resultStack);
        }
        private static class ExtractID {
            public const short Slit = 0;
            public const short Slush = ItemID.SlushBlock;
            public const short DesertFossil = ItemID.DesertFossil;
            public const short FishingJunk = ItemID.OldShoe;
            public const short Moss = ItemID.LavaMoss;
            public const short Sandstone = ItemID.Sandstone;
        }
        public static void PostSetupContent() {
            ItemID.SlushBlock.SetExtractionModeSelf();
            ItemID.Sandstone.SetExtractionModeSelf();
            ItemID.CorruptSandstone.SetExtractionMode(ExtractID.Sandstone);
            ItemID.CrimsonSandstone.SetExtractionMode(ExtractID.Sandstone);
            ItemID.HallowSandstone.SetExtractionMode(ExtractID.Sandstone);
            ItemID.SmoothSandstone.SetExtractionMode(ExtractID.Sandstone);
        }

        #region Detours and Reflection

        private static Hook extractinatorHook;
        public override void Load()
        {
            extractinatorHook = new Hook(ItemLoaderExtractinatorUse, ItemLoader_ExtractinatorUse_Detour);
            extractinatorHook.Apply();
            On_Player.DropItemFromExtractinator += On_Player_DropItemFromExtractinator;
            On_Player.ExtractinatorUse += On_Player_ExtractinatorUse;
			On_Player.TryGettingItemTraderFromBlock += On_Player_TryGettingItemTraderFromBlock;
			IL_Player.PlaceThing_ItemInExtractinator += IL_Player_PlaceThing_ItemInExtractinator;
		}

		private void IL_Player_PlaceThing_ItemInExtractinator(ILContext il) {
		    //IL_01d2: ldloca.s 0
	     //   IL_01d4: call instance uint16 & Terraria.Tile::get_type()
	     //   IL_01d9: ldind.u2
	     //   IL_01da: ldc.i4 219
	     //   IL_01df: beq.s IL_01f0

	     //   IL_01e1: ldloca.s 0
	     //   IL_01e3: call instance uint16 & Terraria.Tile::get_type()
	     //   IL_01e8: ldind.u2
	     //   IL_01e9: ldc.i4 642
	     //   IL_01ee: bne.un.s IL_0234

			ILCursor c = new(il);

            if (!c.TryGotoNext(MoveType.After,
				//i => i.MatchLdloca(0),
	            //i => i.MatchCall(typeof(Tile).GetMethod("get_type")),
	            i => i.MatchLdindU2(),
	            i => i.MatchLdcI4(TileID.Extractinator),
	            i => i.MatchBeq(out _)
				)) {
                throw new Exception("Failed to find instructions for IL_Player_PlaceThing_ItemInExtractinator 1/3");
            }

			if (!c.TryGotoNext(MoveType.After,
				//i => i.MatchLdloca(0),
				//i => i.MatchCall(typeof(Tile).GetMethod("get_type")),
				i => i.MatchLdindU2(),
				i => i.MatchLdcI4(TileID.ChlorophyteExtractinator)
				)) {
				throw new Exception("Failed to find instructions for IL_Player_PlaceThing_ItemInExtractinator 2/3");
			}

			c.Emit(OpCodes.Ceq);
			c.Emit(OpCodes.Ldloca, 0);
			c.EmitDelegate((bool canUse, ref Tile tile) => {
                if (canUse)
                    return true;

                return TileLoader.GetTile(tile.TileType) is AutoExtractorTile;
            });
			c.Emit(OpCodes.Ldc_I4, 1);//ends with two integers on the stack for the bne.un.s to process.  Also works with TileFunctionLibrary

            if (!c.TryGotoNext(MoveType.After,
                i => i.MatchLdloc2()
                )) {
				throw new Exception("Failed to find instructions for IL_Player_PlaceThing_ItemInExtractinator 3/3");
			}

            c.Emit(OpCodes.Ldloca, 0);
            c.EmitDelegate((float num, ref Tile tile) => {
                if (TileLoader.GetTile(tile.TileType) is AutoExtractorTile autoExtractorTile)
                    num /= autoExtractorTile.UseSpeedMultiplier;

                return num;
            });
		}

        private static int RealExtractinatorBlockType;
		private void On_Player_ExtractinatorUse(On_Player.orig_ExtractinatorUse orig, Player self, int extractType, int extractinatorBlockType) {
			RealExtractinatorBlockType = extractinatorBlockType;
			if (TileLoader.GetTile(extractinatorBlockType) is AutoExtractorTile autoExtractorTile && autoExtractorTile.Tier >= 3)
                extractinatorBlockType = TileID.ChlorophyteExtractinator;

            orig(self, extractType, extractinatorBlockType);
		}
		private ItemTrader On_Player_TryGettingItemTraderFromBlock(On_Player.orig_TryGettingItemTraderFromBlock orig, Tile targetBlock) {
			if (TileLoader.GetTile(targetBlock.TileType) is AutoExtractorTile autoExtractorTile && autoExtractorTile.Tier >= 3)
				return ItemTrader.ChlorophyteExtractinator;

            return orig(targetBlock);
		}

		private void On_Player_DropItemFromExtractinator(On_Player.orig_DropItemFromExtractinator orig, Player self, int itemType, int stack) {
            orig(self, itemType, stack);
        }

        public override void Unload() {
            extractinatorHook.Undo();
        }

        private static Player player = new();
        public static readonly MethodInfo extractinatorUse = typeof(Player).GetMethod("ExtractinatorUse", BindingFlags.NonPublic | BindingFlags.Instance);
        public delegate void ExtractinatorUseDelegate(Player player, int extractType, int extractinatorBlockType);
        public static ExtractinatorUseDelegate ExtractinatorUseMethod = (ExtractinatorUseDelegate)Delegate.CreateDelegate(typeof(ExtractinatorUseDelegate), extractinatorUse);

        /// <summary>
        /// Needs to be paired with a detour around ItemLoader.ExtractinatorUse() and set stack to zero.
        /// </summary>
        /// <param name="extractType"></param>
        /// <param name="extractinatorBlockType"></param>
        public static void AutoExtractinatorUse(int extractType, int extractinatorBlockType, out int type, out int stack) {
            Extracting = true;
            ExtractinatorUseMethod(null, extractType, RealExtractinatorBlockType);
            Extracting = false;
            type = extractItemType;
            stack = extractStack;
        }

        private static int extractItemType = 0;
        private static int extractStack = 0;
        public static bool Extracting = false;
        public delegate void orig_ItemLoader_ExtractinatorUse(ref int resultType, ref int resultStack, int extractType, int extractinatorBlockType);
        public delegate void hook_ItemLoader_ExtractinatorUse(orig_ItemLoader_ExtractinatorUse orig, ref int resultType, ref int resultStack, int extractType, int extractinatorBlockType);
        public static readonly MethodInfo ItemLoaderExtractinatorUse = typeof(ItemLoader).GetMethod("ExtractinatorUse", BindingFlags.Public | BindingFlags.Static);
        public static void ItemLoader_ExtractinatorUse_Detour(orig_ItemLoader_ExtractinatorUse orig, ref int resultType, ref int resultStack, int extractType, int extractinatorBlockType) {
            orig(ref resultType, ref resultStack, extractType, extractinatorBlockType);
            ExtractinatorUseDetour(ref resultType, ref resultStack, extractType, extractinatorBlockType);
            if (Extracting) {
                extractItemType = resultType;
                extractStack = resultStack;
                resultType = 0;
            }
        }

        #endregion
    }
    public static class ExtractionItemStaticMethods {
        //public static void SetExtractionMode(this int itemType, int extractionItemType) => 
        public static void SetExtractionMode(this short itemType, int extractionItemType) => ItemID.Sets.ExtractinatorMode[itemType] = extractionItemType;
		//public static void SetExtractionModeSelf(this int itemType) => SetExtractionMode(itemType, itemType);
        public static void SetExtractionModeSelf(this short itemType) => SetExtractionMode(itemType, itemType);
    }
}
