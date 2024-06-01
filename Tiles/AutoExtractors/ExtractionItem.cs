using androLib.Common.Utility;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Channels;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EngagedSkyblock.Tiles {
    internal class ExtractionItem : GlobalItem
    {
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
                int stackRand = Main.rand.Next(100);
                int stackChance = tier * StackChancePerTier;
                if (resultStack < 1)
                    resultStack = 1;

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
		}

        private void On_Player_ExtractinatorUse(On_Player.orig_ExtractinatorUse orig, Player self, int extractType, int extractinatorBlockType)
        {
            orig(self, extractType, extractinatorBlockType);
        }

        private void On_Player_DropItemFromExtractinator(On_Player.orig_DropItemFromExtractinator orig, Player self, int itemType, int stack)
        {
            orig(self, itemType, stack);
        }

        public override void Unload()
        {
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
        public static void AutoExtractinatorUse(int extractType, int extractinatorBlockType, out int type, out int stack)
        {
            Extracting = true;
            ExtractinatorUseMethod(null, extractType, extractinatorBlockType);
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
        public static void ItemLoader_ExtractinatorUse_Detour(orig_ItemLoader_ExtractinatorUse orig, ref int resultType, ref int resultStack, int extractType, int extractinatorBlockType)
        {
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
