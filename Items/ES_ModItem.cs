﻿using androLib.Items;
using androLib.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace EngagedSkyblock.Items {
	public abstract class ES_ModItem : AndroModItem {
		public override string Texture => (GetType().Namespace + ".Sprites." + Name).Replace('.', '/');
		protected override Action<ModItem, string, string> AddLocalizationTooltipFunc => ES_LocalizationDataStaticMethods.AddLocalizationTooltip;
	}
}
