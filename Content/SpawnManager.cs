using androLib.Common.Utility;
using androLib.Common.Utility.LogSystem.WebpageComponenets;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using androLib.Tiles.TileData;
using TerrariaAutomations.Tiles.TileEntities;

namespace EngagedSkyblock.Content {
	public static class SpawnManager {
		public static void Load() {
			On_WorldGen.SpawnTownNPC += On_WorldGen_SpawnTownNPC;
			On_NPC.NewNPC += On_NPC_NewNPC;
			On_Main.UpdateTime_SpawnTownNPCs += On_Main_UpdateTime_SpawnTownNPCs;
		}

		private static void On_Main_UpdateTime_SpawnTownNPCs(On_Main.orig_UpdateTime_SpawnTownNPCs orig) {
			//$"UpdateTime_SpawnTownNPCs".LogSimpleNT();
			orig();
		}
		
		private static int On_NPC_NewNPC(On_NPC.orig_NewNPC orig, Terraria.DataStructures.IEntitySource source, int X, int Y, int Type, int Start, float ai0, float ai1, float ai2, float ai3, int Target) {
			if (ES_WorldGen.SkyblockWorld) {
				if (Type == NPCID.OldMan) {
					if (!ES_WorldGen.DungeonExists) {
						int availableNPCSlot = ReflectionHelper.CallNonPublicStaticMethod<int>(typeof(NPC), "GetAvailableNPCSlot", parameters: new object[] { Type, Start });
						return availableNPCSlot;
					}
				}
			}

			int npcNum = orig(source, X, Y, Type, Start, ai0, ai1, ai2, ai3, Target);
			//NPC npc = Main.npc[npcNum];
			//$"NewNPC -> npc: {npc.S()}".LogSimpleNT();
			return npcNum;
		}
		private static int testingTimer = testingTimerReset - 10;
		private static int testingTimerReset = 60;
		public static Action PostUpdateActions;
		private static int postUpdateActionsTimer = 0;
		public static int PostUpdateActionsTimerReset = 0;
		public static void Update() {





			if (Debugger.IsAttached) {
				Player player = Main.LocalPlayer;
				int dungX = Main.dungeonX;//3608
				int dungY = Main.dungeonY;//317
				double desiredWorldTilesUpdateRate = Main.desiredWorldTilesUpdateRate;//Max 24, controls Town NPC spawn rate
				//Main.desiredWorldTilesUpdateRate = 24;
				double desiredWorldEventsUpdateRate = Main.desiredWorldEventsUpdateRate;
				//Main.dungeonX = 3608 - 10;
				int dungeonTileCount = Main.SceneMetrics.DungeonTileCount;
				bool zoneDungeon = Main.LocalPlayer.ZoneDungeon;
				double worldSurface = Main.worldSurface * 16;
				float playerCenterY = Main.LocalPlayer.Center.Y;
				int spawnX = Main.spawnTileX;
				int spawnY = Main.spawnTileY;
				Point16 playerCenterCoord = Main.LocalPlayer.Center.ToTileCoordinates16();
				int playerX = playerCenterCoord.X;
				int playerY = playerCenterCoord.Y;
				int maxX = Main.maxTilesX;
				int maxY = Main.maxTilesY;
				var playerTileCoord = Main.LocalPlayer.Center.ToTileCoordinates();
				int playerTileX = playerTileCoord.X;
				int playerTileY = playerTileCoord.Y;
				TilePipeData[] tilePipeData = Main.tile.GetData<TilePipeData>();
				testingTimer++;
				if (testingTimer >= testingTimerReset) {
					testingTimer = 0;
					Point16 spawnPoint = new Point16(Main.spawnTileX, Main.spawnTileY);
					Dust testDust = Dust.NewDustPerfect(spawnPoint.ToWorldCoordinates(), ModContent.DustType<ExtractinatorDust>(), Vector2.Zero, newColor: Color.Red);
					bool failed = false;
					double worldUpdateRate = WorldGen.GetWorldUpdateRate();
					if (Main.netMode == 1 || worldUpdateRate <= 0)
						failed = true;

					if (Main.checkForSpawns < 7200 / worldUpdateRate)
						failed = true;
				}

				int prioritizedTownNPCType = WorldGen.prioritizedTownNPCType;
			}

			if (PostUpdateActions != null) {
				postUpdateActionsTimer++;
				if (postUpdateActionsTimer >= PostUpdateActionsTimerReset) {
					postUpdateActionsTimer = 0;
					PostUpdateActionsTimerReset = 0;
					PostUpdateActions?.Invoke();
					PostUpdateActions = null;
				}
			}
		}

		public static TownNPCSpawnResult On_WorldGen_SpawnTownNPC(On_WorldGen.orig_SpawnTownNPC orig, int x, int y) {
			TownNPCSpawnResult result = orig(x, y);
			if (Debugger.IsAttached && result != TownNPCSpawnResult.Blocked)
				$"SpawnTownNPC result: {result.ToString()}".LogSimpleNT();

			return result;
		}
	}
}
