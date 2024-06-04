using androLib.Localization;
using EngagedSkyblock.Common.Globals;
using EngagedSkyblock.Items;
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
    public class ES_Mod : Mod {
		public static ES_Mod Instance;
		public const string ModName = "EngagedSkyblock";
		private static List<Hook> hooks = new();
		public override void Load() {
			Instance = this;

			hooks.Add(new(ES_WorldGen.ModLoaderModSystemModifyWorldGenTasks, ES_WorldGen.ModSystem_ModifyWorldGenTasks_Detour));
			hooks.Add(new(ES_WorldGen.ModLoaderModSystemPostWorldGen, ES_WorldGen.ModSystem_PostWorldGen_Detour));
			hooks.Add(new(LeafBlock.PlantLoaderShakeTree, LeafBlock.PlantLoaderShakeTreeDelegate));

			foreach (Hook hook in hooks) {
				hook.Apply();
			}

			ES_WorldGen.Load();
			ES_Weather.Load();
			Tiles.RainTotem.Load();

			ES_LocalizationData.RegisterSDataPackage();
		}
		public enum ES_ModPacketID {
			RequestWorldSeedFromServer,
			SendWorldSeedToClient,
			HitTile,
		}
		public override void HandlePacket(BinaryReader reader, int whoAmI) {
			if (Main.netMode == NetmodeID.Server) {
				byte packetID = reader.ReadByte();
				switch ((ES_ModPacketID)packetID) {
					case ES_ModPacketID.RequestWorldSeedFromServer:
						ModPacket modPacket = Instance.GetPacket();
						modPacket.Write((byte)ES_ModPacketID.SendWorldSeedToClient);
						modPacket.Write(Main.ActiveWorldFileData.SeedText);
						modPacket.Send(whoAmI);
						break;
					case ES_ModPacketID.HitTile:
						int x = reader.ReadInt16();
						int y = reader.ReadInt16();
						ushort tileType = reader.ReadUInt16();
						ES_GlobalTile.HitTile(x, y, tileType, whoAmI);
						break;
					default:
						throw new Exception($"Received packet ID: {packetID}.  Not recognized.");
				}
			}
			else if (Main.netMode == NetmodeID.MultiplayerClient) {
				byte packetID = reader.ReadByte();
				switch ((ES_ModPacketID)packetID) {
					case ES_ModPacketID.SendWorldSeedToClient:
						string seed = reader.ReadString();
						ES_WorldGen.RecieveWorldSeed(seed);
						break;
					default:
						throw new Exception($"Recieved packet ID: {packetID}.  Not recognized.");
				}
			}
		}
	}
}