//using KokoLib.Emitters;
//using KokoLib;
//using KokoLib.Nets;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Terraria;
//using Terraria.ID;
//using androLib.Common.Utility;

//namespace EngagedSkyblock.ModLib.KokoLib {
//	public interface INetMethods {
//		public void RequestWorldSeed();
//		public void SendWorldSeed(string seed, int clientID);

//	}
//	internal class ES_NetManager : ModHandler<INetMethods>, INetMethods {
//		public override INetMethods Handler => this;

//		public void RequestWorldSeed() {
//			switch (Main.netMode) {
//				case NetmodeID.MultiplayerClient:
//					//Sends the request to the server.
//					$"Requesting World Seed from server.".LogSimple();
//					Net<INetMethods>.Proxy.RequestWorldSeed();
//					SendWorldSeed("", WhoAmI);
//					Net<INetMethods>.Proxy.SendWorldSeed("", WhoAmI);
//					break;
//				case NetmodeID.Server:
//					Net<INetMethods>.Proxy.SendWorldSeed(Main.ActiveWorldFileData.SeedText, WhoAmI);
//					SendWorldSeed(Main.ActiveWorldFileData.SeedText, WhoAmI);
//					break;
//				default:
//					throw new Exception($"Request World Seed called in single player.  Should never happen.");
//			}
//		}

//		public void SendWorldSeed(string seed, int clientID) {
//			switch (Main.netMode) {
//				case NetmodeID.MultiplayerClient:
//					ES_WorldGen.RecieveWorldSeed(seed);
//					break;
//				case NetmodeID.Server:
//					//Net.ToClient = clientID;
//					Net<INetMethods>.Proxy.SendWorldSeed(seed, clientID);
//					break;
//				default:
//					throw new Exception($"Send World Seed called in single player.  Should never happen.");
//			}
//		}
//	}
//}
