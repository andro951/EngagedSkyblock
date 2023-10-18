using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Utilities;

namespace EngagedSkyblock.Weather {
	//public class SnowFlakeManager {
	//	private static int maxSnowFlakes = 100;
	//	private static LinkedList<SnowFlakeManager> snowFlakes = new();

	//	private Vector2 position;
	//	private float rotation;
	//	private float rotationSpeed;

	//	public static void SnowFlakeFall(float x) {

	//	}
	//	public static void SpawnSnowFlakes(int numSnowflakes) {
	//		if (numSnowflakes < 1)
	//			return;

	//		FastRandom fastRandom = FastRandom.CreateWithRandomSeed();
	//		for (int i = 0; i < numSnowflakes && snowFlakes.Count < maxSnowFlakes; i++) {
	//			snowFlakes.AddLast(new SnowFlakeManager());
	//			SnowFlakeManager snowFlake = snowFlakes.Last();
	//			snowFlake.position.X = fastRandom.Next(1921);
	//			snowFlake.position.Y = fastRandom.Next(1201);
	//			snowFlake.rotation = (float)fastRandom.Next(628) * 0.01f;
	//			snowFlake.rotationSpeed = (float)fastRandom.Next(5, 50) * 0.0001f;
	//			if (fastRandom.Next(2) == 0)
	//				snowFlake.rotationSpeed *= -1f;

	//			if (fastRandom.Next(40) == 0) {
	//				snowFlake.rotationSpeed /= 2f;
	//			}
	//		}
	//	}
	//}
}
