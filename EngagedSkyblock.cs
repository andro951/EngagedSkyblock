using androLib.Localization;
using EngagedSkyblock.Common.Globals;
using EngagedSkyblock.Content.Liquids;
using EngagedSkyblock.Items;
using EngagedSkyblock.Items;
using EngagedSkyblock.Tiles;
using EngagedSkyblock.Tiles.TileEntities;
using EngagedSkyblock.Weather;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
			AddNonLoadedContent();

			hooks.Add(new(ES_WorldGen.ModLoaderModSystemModifyWorldGenTasks, ES_WorldGen.ModSystem_ModifyWorldGenTasks_Detour));
			hooks.Add(new(ES_GlobalTile.TileLoaderDrop, ES_GlobalTile.TileLoader_Drop_Detour));
			hooks.Add(new(ES_WorldGen.ModLoaderModSystemPostWorldGen, ES_WorldGen.ModSystem_PostWorldGen_Detour));
			hooks.Add(new(LeafBlock.PlantLoaderShakeTree, LeafBlock.PlantLoaderShakeTreeDelegate));
			hooks.Add(new(GlobalAutoExtractor.OnTileRightClickInfo, GlobalAutoExtractor.TileLoaderRightClickDetour));

			foreach (Hook hook in hooks) {
				hook.Apply();
			}

			ES_WorldGen.Load();
			ES_Weather.Load();
			Tiles.RainTotem.Load();
			AutoFisher.Load();

			ES_LocalizationData.RegisterSDataPackage();
		}
		private void AddNonLoadedContent() {
			IEnumerable<Type> types = null;
			try {
				types = Assembly.GetExecutingAssembly().GetTypes();
			}
			catch (ReflectionTypeLoadException e) {
				types = e.Types.Where(t => t != null);
			}

			types = types.Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(ES_ModItem)));

			IEnumerable<ModItem> allItems = types.Select(t => Activator.CreateInstance(t)).Where(i => i != null).OfType<ModItem>();

			IEnumerable<ModItem> autoExtractinators = allItems.OfType<AutoExtractinator>().OrderBy(e => e.Tier);

			foreach (ModItem modItem in autoExtractinators) {
				Instance.AddContent(modItem);
			}
		}
		public enum ModPacketID {
			RequestWorldSeedFromClient,
			SendWorldSeedToClient,
			HitTile,
			ChestIndicatorInfo,
			AutoFisherUseItem,
			AutoFisherItemSync,
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
						AutoFisherTE.RequestAllTEsToClient(client);
						break;
					case ModPacketID.HitTile:
						int x = reader.ReadInt32();
						int y = reader.ReadInt32();
						int playerWhoAmI = reader.ReadInt32();
						ES_GlobalTile.HitTile(x, y, playerWhoAmI);
						break;
					case ModPacketID.AutoFisherItemSync:
						AutoFisherTE.ReceiveItem(reader, whoAmI);
						break;
					default:
						throw new Exception($"Received packet ID: {packetID}.  Not recognized.");
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
					case ModPacketID.AutoFisherUseItem:
						AutoFisherTE.ReadAutoFisherUseItem(reader);
						break;
					case ModPacketID.AutoFisherItemSync:
						AutoFisherTE.ReceiveItem(reader);
						break;
					default:
						throw new Exception($"Recieved packet ID: {packetID}.  Not recognized.");
				}
			}
		}
	}
}