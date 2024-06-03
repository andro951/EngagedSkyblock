using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using TerrariaAutomations.Tiles;

namespace EngagedSkyblock.Content {
	public static class ExtractionManager {
		public static List<(int extractType, ExtractTypeSet extractTypeSet, List<int> blocksInExtractType)> ES_ExtractTypeSets => new() {
			(ES_ExtractID.Sandstone, new(
				new List<TypeChancePair>() {
					new(ItemID.Amber, 0.005f),
					new(ItemID.SandBlock, 0.5f),
				}),
				new List<int>() {
					ItemID.Sandstone,
					ItemID.CorruptSandstone,
					ItemID.CrimsonSandstone,
					ItemID.HallowSandstone,
					ItemID.SmoothSandstone
				}),
			(ES_ExtractID.Slush, new(
				new List<TypeChancePair>() {
					new(ItemID.Amber, 0.005f),
					new(ItemID.SandBlock, 0.5f),
				}),
				new List<int>() {
					ItemID.SlushBlock,
				})
		};
		public static void PostSetupContent() {
			foreach (var set in ES_ExtractTypeSets) {
				ExtractTypeSet.RegisterExtractTypeSet(set.extractType, set.extractTypeSet, set.blocksInExtractType);
			}
		}

		private static class ES_ExtractID {
			public const short Slit = 0;
			public const short Slush = ItemID.SlushBlock;
			public const short DesertFossil = ItemID.DesertFossil;
			public const short FishingJunk = ItemID.OldShoe;
			public const short Moss = ItemID.LavaMoss;
			public const short Sandstone = ItemID.Sandstone;
		}
	}
}
