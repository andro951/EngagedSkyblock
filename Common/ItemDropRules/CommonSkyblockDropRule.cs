using androLib.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.GameContent.ItemDropRules;

namespace EngagedSkyblock.Common.ItemDropRules {
	public class CommonSkyblockDropRule : BasicDropRule {
		public CommonSkyblockDropRule(int id, float chance, float configChance) : base(id, chance, configChance) {}
		public override bool CanDrop(DropAttemptInfo info) => ES_WorldGen.SkyblockWorld;
		public override ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info) {
			if (!ES_WorldGen.SkyblockWorld) {
				ItemDropAttemptResult result = new();
				result.State = ItemDropAttemptResultState.DoesntFillConditions;
				return result;
			}
			
			return base.TryDroppingItem(info);
		}
		protected override IEnumerable<IItemDropRuleCondition> Conditions => new List<IItemDropRuleCondition>() {
			new SkyblockDropCondition()
		};
	}

	public class SkyblockDropCondition : IItemDropRuleCondition {
		public bool CanDrop(DropAttemptInfo info) => ES_WorldGen.SkyblockWorld;

		public bool CanShowItemDropInUI() => ES_WorldGen.SkyblockWorld;

		public string GetConditionDescription() {
			return $"Skyblock Only";
		}
	}
}
