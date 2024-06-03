using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using static Terraria.Localization.GameCulture;
using androLib.Common.Utility;
using androLib.Common.Globals;
using androLib.Common.Configs;
using Terraria.ID;
using Terraria;
using EngagedSkyblock;

namespace androLib.Localization
{
	public class ES_LocalizationData {
		public static void RegisterSDataPackage() {
			if (Main.netMode == NetmodeID.Server)
				return;

			AndroLogModSystem.RegisterModLocalizationSDataPackage(new(ModContent.GetInstance<EngagedSkyblock.ES_Mod>, () => AllData, () => ChangedData, () => RenamedKeys, () => RenamedFullKeys, () => SameAsEnglish));
		}

		private static SortedDictionary<string, SData> allData;
		public static SortedDictionary<string, SData> AllData {
			get {
				if (allData == null) {
					allData = new() {
						{ L_ID1.Items.ToString(), new(children: new() {
							//Intentionally empty.  Filled automatically
						}) },
						//{ L_ID1.Configs.ToString(), new(children: new() {
						//	{ nameof(AndroClientConfig), new(children: new() {
						//		{ nameof(AndroClientConfig.UITransparency), new(dict: new() {
						//				{ L_ID3.Label.ToString(), nameof(AndroClientConfig.UITransparency).AddSpaces() },
						//			{ L_ID3.Tooltip.ToString(), "The transparency of all UIs that depend on androLib.  0 is invisible, 255 fully saturated." }
						//		}) },
						//	},
						//	dict: new() {
						//		{ L_ID2.DisplayName.ToString(), "Client Config" },
						//		{ AndroClientConfig.DisplaySettingsKey, AndroClientConfig.DisplaySettingsKey.AddSpaces() },
						//	}) },
						//}) },
					};
				}

				return allData;
			}
		}

		private static List<string> changedData;
		public static List<string> ChangedData {
			get {
				if (changedData == null)
					changedData = new();

				return changedData;
			}

			set => changedData = value;
		}

		private static Dictionary<string, string> renamedFullKeys;
		public static Dictionary<string, string> RenamedFullKeys {
			get {
				if (renamedFullKeys == null)
					renamedFullKeys = new();

				return renamedFullKeys;
			}

			set => renamedFullKeys = value;
		}

		public static Dictionary<string, string> RenamedKeys = new() {
			//{ typeof(ItemCooldown).Name, "AllForOne" },
			//{ DialogueID.HateCrowded.ToString(), "HateCrouded" }
		};

		public static Dictionary<CultureName, List<string>> SameAsEnglish = new() {
			{ CultureName.German,
				new() {
					
				}
			},
			{
				CultureName.Spanish,
				new() {
					
				}
			},
			{
				CultureName.French,
				new() {
					
				}
			},
			{
				CultureName.Italian,
				new() {
					
				}
			},
			{
				CultureName.Polish,
				new() {
					
				}
			},
			{
				CultureName.Portuguese,
				new() {
					
				}
			},
			{
				CultureName.Russian,
				new() {
					
				}
			},
			{
				CultureName.Chinese,
				new() {
					
				}
			},
		};
	}
	public static class ES_LocalizationDataStaticMethods {
		/// <summary>
		/// Should only be used for items directly in androLib, not items derived from AndroModItem, or the localization will end up in androLib localization.
		/// </summary>
		public static void AddLocalizationTooltip(this ModItem modItem, string tooltip, string name = null) {
			if (AndroLogModSystem.printLocalization || AndroLogModSystem.printLocalizationKeysAndValues) {
				ES_LocalizationData.AllData[L_ID1.Items.ToString()].Children.Add(modItem.Name, new(dict: new()));
				ES_LocalizationData.AllData[L_ID1.Items.ToString()].Children[modItem.Name].Dict.Add(L_ID1.Tooltip.ToString(), tooltip);
				ES_LocalizationData.AllData[L_ID1.Items.ToString()].Children[modItem.Name].Dict.Add(L_ID2.DisplayName.ToString(), name ?? modItem.Name.AddSpaces());
			}
		}
	}
}
