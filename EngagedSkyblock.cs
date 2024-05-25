using androLib.Localization;
using EngagedSkyblock.Common.Globals;
using EngagedSkyblock.Items;
using EngagedSkyblock.Tiles.TileEntities;
using EngagedSkyblock.Weather;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace EngagedSkyblock
{
	public class EngagedSkyblock : Mod {
		public static EngagedSkyblock Instance;
		public const string ModName = "EngagedSkyblock";
		private static List<Hook> hooks = new();
		public override void Load() {
			Instance = this;
			hooks.Add(new(ES_WorldGen.ModLoaderModSystemModifyWorldGenTasks, ES_WorldGen.ModSystem_ModifyWorldGenTasks_Detour));
			hooks.Add(new(ES_GlobalTile.TileLoaderDrop, ES_GlobalTile.TileLoader_Drop_Detour));
			hooks.Add(new(ES_WorldGen.ModLoaderModSystemPostWorldGen, ES_WorldGen.ModSystem_PostWorldGen_Detour));

			foreach (Hook hook in hooks) {
				hook.Apply();
			}

			ES_WorldGen.Load();
			ES_Liquid.Load();
			ES_Weather.Load();
			Tiles.RainTotem.Load();

			ES_LocalizationData.RegisterSDataPackage();
		}
		public enum ModPacketID {
			RequestWorldSeedFromClient,
			SendWorldSeedToClient,
			HitTile,
			ChestIndicatorInfo,
		}
		public override void HandlePacket(BinaryReader reader, int whoAmI) {
			if (Main.netMode == NetmodeID.Server) {
				byte packetID = reader.ReadByte();
				switch ((ModPacketID)packetID) {
					case ModPacketID.RequestWorldSeedFromClient:
						int client = reader.ReadInt32();
						ModPacket modPacket = Instance.GetPacket();
						modPacket.Write((byte)ModPacketID.SendWorldSeedToClient);
						modPacket.Write(Main.ActiveWorldFileData.SeedText);
						modPacket.Send(client);
						break;
					case ModPacketID.HitTile:
						int x = reader.ReadInt32();
						int y = reader.ReadInt32();
						int playerWhoAmI = reader.ReadInt32();
						ES_GlobalTile.HitTile(x, y, playerWhoAmI);
						break;
					default:
						throw new Exception($"Recieved packet ID: {packetID}.  Not recognized.");
				}
			}
			else if (Main.netMode == NetmodeID.MultiplayerClient) {
				byte packetID = reader.ReadByte();
				switch ((ModPacketID)packetID) {
					case ModPacketID.SendWorldSeedToClient:
						string seed = reader.ReadString();
						ES_WorldGen.RecieveWorldSeed(seed);
						break;
					case ModPacketID.ChestIndicatorInfo:
						ChestIndicatorInfo.Read(reader);
						break;
					default:
						throw new Exception($"Recieved packet ID: {packetID}.  Not recognized.");
				}
			}
		}
	}
}