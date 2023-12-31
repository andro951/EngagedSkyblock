﻿using androLib.Common.Utility;
using androLib.Common.Utility.PathFinders;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace EngagedSkyblock.Common.Globals {
	public class ES_GlobalWall : GlobalWall {

		#region Spider Walls

		private static int checkChestNum = 0;
		private static float spiderSpawnChanceMultiplier = 1f;
		private static float baseTicksPerSpiderPlacement = 3600f;//1 per minute
		private static int chestsPerTick = 15;
		private static int chestsCounted = 0;
		private static SortedDictionary<int ,int> bugFilledChests = new();
		private static int spiderTileChecksPerTick = 20;
		public static float PlayerDistanceToDisableX = 1000f;
		public static float PlayerDistanceToDisableY = 550f;
		public static List<Vector2> PlayerPositions = new();

		#region Bug Item Types

		private static SortedSet<int> BugItemTypes {
			get {
				if (bugItemTypes == null)
					SetupBugItemTypes();

				return bugItemTypes;
			}
		}
		private static SortedSet<int> bugItemTypes;
		private static void SetupBugItemTypes() {
			bugItemTypes = new() {
				ItemID.Worm,
				ItemID.EnchantedNightcrawler,
				ItemID.GoldWorm,
				ItemID.GoldButterfly,
				ItemID.GoldDragonfly,
				ItemID.WaterStrider,
				ItemID.GoldWaterStrider,
				ItemID.Grasshopper,
				ItemID.GoldGrasshopper,
				ItemID.LadyBug,
				ItemID.GoldLadyBug,
				ItemID.Firefly,
				ItemID.LightningBug,
				ItemID.Lavafly,
				ItemID.Stinkbug,
				ItemID.Maggot,
				ItemID.HellButterfly,
				ItemID.TruffleWorm,
				ItemID.MagmaSnail,
			};

			int[] groups = new int[] {
				RecipeGroupID.Bugs,
				RecipeGroupID.Butterflies,
				RecipeGroupID.Fireflies,
				RecipeGroupID.Snails,
				RecipeGroupID.Dragonflies,
			};

			foreach (int group in groups) {
				HashSet<int> items = RecipeGroup.recipeGroups[group].ValidItems;
				foreach (int item in items) {
					bugItemTypes.Add(item);
				}
			}
		}

		#endregion

		public static void Update() {
			int chestsChecked = 0;
			for (; checkChestNum < Main.chest.Length && chestsChecked <= chestsPerTick; checkChestNum++) {
				Chest chest = Main.chest[checkChestNum];
				if (chest == null || bugFilledChests.ContainsKey(checkChestNum))
					continue;

				Item[] inv = chest.item;
				for (int i = 0; i < inv.Length; i++) {
					Item item = inv[i];
					if (BugItemTypes.Contains(item.type)) {
						if (TryGetNextBugChanceDenom(checkChestNum, false, out int denom, out _))
							bugFilledChests.Add(checkChestNum, denom);

						break;
					}
				}

				chestsChecked++;
				chestsCounted++;
			}

			if (checkChestNum >= Main.chest.Length) {
				chestsPerTick = chestsCounted / 600;
				if (chestsPerTick < 1)
					chestsPerTick = 1;

				chestsCounted = 0;
				checkChestNum = 0;
			}
			
			if (bugFilledChests.Count < 1)
				return;

			List<(int, int)> bugFilledChestsEdits = new();
			int tilesToCheck = spiderTileChecksPerTick.CeilingDivide(bugFilledChests.Count);
			PlayerPositions.Clear();
			if (Main.netMode == NetmodeID.SinglePlayer) {
				PlayerPositions.Add(Main.LocalPlayer.Center);
			}
			else {
				for (int i = 0; i < Main.player.Length; i++) {
					Player player = Main.player[i];
					if (player.NullOrNotActive())
						continue;

					PlayerPositions.Add(player.Center);
				}
			}

			foreach (KeyValuePair<int, int> p in bugFilledChests) {
				Chest chest = Main.chest[p.Key];
				Vector2 chestPosition = new Vector2(chest.x, chest.y).ToWorldCoordinates(16f, 16f);
				foreach (Vector2 playerCenter in PlayerPositions) {
					float xDist = Math.Abs(playerCenter.X - chestPosition.X);
					float yDist = Math.Abs(playerCenter.Y - chestPosition.Y);
					if (xDist < PlayerDistanceToDisableX && yDist < PlayerDistanceToDisableY)
						return;
				}
				
				SpiderGridManager.ProcessNewGrid(p.Key, tilesToCheck);
				if (Main.rand.NextBool(p.Value)) {
					if (TryGetNextBugChanceDenom(p.Key, true, out int denom, out Action EatBug)) {
						if (CreateSpiderWall(p.Key, EatBug))
							bugFilledChestsEdits.Add((p.Key, denom));
					}
					else {
						bugFilledChestsEdits.Add((p.Key, -1));
					}
				}
			}

			foreach ((int key, int value) in bugFilledChestsEdits) {
				if (value == -1) {
					bugFilledChests.Remove(key);
				}
				else {
					bugFilledChests[key] = value;
				}
			}

			SpiderGridManager.CleanUp(bugFilledChests);
		}
		private const int HighestBaitLimit = 100;
		private static bool GetBugInfo(int chestNum, bool eatBug, out int highestBait, out int totalBait, out int totalStack, out Action EatBug) {
			Chest chest = Main.chest[chestNum];
			highestBait = 0;
			totalBait = 0;
			totalStack = 0;
			EatBug = null;
			if (chest == null)
				return false;

			Item[] inv = chest.item;
			if (inv == null)
				return false;

			List<int> bugsIndexes = new();
			for (int i = 0; i < inv.Length; i++) {
				Item item = inv[i];
				if (item.NullOrAir())
					continue;

				if (BugItemTypes.Contains(item.type))
					bugsIndexes.Add(i);
			}

			if (bugsIndexes.Count < 1)
				return false;

			if (eatBug) {
				int index = Main.rand.Next(bugsIndexes);
				Item bugToEat = inv[index];
				totalStack--;
				int bait = bugToEat.bait > HighestBaitLimit ? HighestBaitLimit : bugToEat.bait;
				totalBait -= bugToEat.bait;
				EatBug = () => {
					bugToEat.stack--;
					if (bugToEat.stack <= 0)
						inv[index] = new Item();
				};
			}

			for (int i = 0; i < bugsIndexes.Count; i++) {
				Item item = inv[bugsIndexes[i]];
				int stack = item.stack;
				totalStack += stack;
				int bait = item.bait;
				if (bait > highestBait) {
					if (bait > HighestBaitLimit)
						bait = HighestBaitLimit;

					highestBait = bait;
				}

				totalBait += bait * stack;
			}

			return true;
		}
		private static bool TryGetNextBugChanceDenom(int chestNum, bool eatBug, out int denom, out Action EatBug) {
			denom = 0;
			if (!GetBugInfo(chestNum, eatBug, out int highestBait, out int totalBait, out int totalStack, out EatBug))
				return false;

			float highest = 1f + highestBait / 40f;
			float avg = 1f + (totalBait / (float)totalStack) / 40f;
			float dist = 1f + 3f * totalBait.LogisticDistributionIncreasingRoot(2500);
			float chanceF = highest * avg * dist;
			denom = (baseTicksPerSpiderPlacement / (chanceF * spiderSpawnChanceMultiplier)).Round();
			if (denom < 1)
				denom = 1;

			return true;
		}
		private static SpiderGridManager SpiderGridManager = new();
		private static bool CreateSpiderWall(int chestNum, Action EatBug) {
			if (SpiderGridManager.PlaceNextTarget(chestNum)) {
				EatBug();
				return true;
			}

			return false;
		}

		#endregion

	}
}
