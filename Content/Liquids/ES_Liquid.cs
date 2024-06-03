using androLib.Common.Utility;
using androLib.Common.Utility.Compairers;
using androLib.Common.Utility.LogSystem.WebpageComponenets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using static EngagedSkyblock.Content.Liquids.ES_Liquid;
using static Terraria.GameContent.Animations.Actions.NPCs;

namespace EngagedSkyblock.Content.Liquids
{
    public class ES_Liquid : GlobalLiquid {
		public override bool? CanMoveLeft(int x, int y, int xMove, int yMove, bool canMoveLeftVanilla) {
			return CanMove(x, y, xMove, yMove, canMoveLeftVanilla);
		}
		public override bool? CanMoveRight(int x, int y, int xMove, int yMove, bool canMoveRightVanilla) {
			return CanMove(x, y, xMove, yMove, canMoveRightVanilla);
		}
		public override bool? AllowMergeLiquids(int x, int y, Tile tile, bool tileSolid, int x2, int y2, Tile tile2, bool tileSolid2) {
			return base.AllowMergeLiquids(x, y, tile, tileSolid, x2, y2, tile2, tileSolid2);
		}
		public override bool PreventMerge(LiquidMerge liquidMerge) {
			return base.PreventMerge(liquidMerge);
		}
		public override void GetLiquidMergeTypes(int x, int y, int type, bool[] liquidNearby, ref int liquidMergeTileType, ref int liquidMergeType, LiquidMerge liquidMerge) {
			if (!ES_WorldGen.SkyblockWorld)
                return;

            if (liquidMergeTileType == TileID.Obsidian && liquidMerge.LiquidsNearby[LiquidID.Lava] && liquidMerge.LiquidsNearby[LiquidID.Water]) {
                int lavaCount = liquidMerge.LiquidsNearbyAmounts[LiquidID.Lava];
				liquidMergeTileType = lavaCount < 240 ? lavaCount < 64 ? TileID.Stone : TileID.Silt : TileID.Obsidian;
			}
		}

		private static List<Point> combinePoints = new();
        private static List<CombineInfo> combineInfos = new();
        public class CombineInfo
        {
            public int InitialDelay;
            public int Delay;
            public Action Action;
            public CombineInfo(int delay, Action action)
            {
                InitialDelay = delay;
                Delay = delay;
                Action = action;
            }
        }
        public static void Update() {
            //If changes are made, update OnWorldUnload() too
            for (int i = combineInfos.Count - 1; i >= 0; i--) {
                CombineInfo combineInfo = combineInfos[i];
                ref int delay = ref combineInfo.Delay;
                delay--;
                if (delay <= 0) {
                    combineInfo.Action();
                    combinePoints.RemoveAt(i);
                    combineInfos.RemoveAt(i);
                }
            }
        }
		internal static void PreSaveWorld() {
            //Same as Update but ignore timer
			for (int i = combineInfos.Count - 1; i >= 0; i--) {
				CombineInfo combineInfo = combineInfos[i];
				combineInfo.Action();
				combinePoints.RemoveAt(i);
				combineInfos.RemoveAt(i);
			}
		}
		private const int combineDelay = 30;
        private const int convertDelay = 15;
        public class TileChangeFromLiquid
        {
            public Func<int, bool> CheckTileType;
            public Func<int, bool> CheckLiquidType;
            public Func<int, int> ResultTileType;
            /// <summary>
            /// The default other liquid type to combine with for sound is the liquid that was valid, so pick a different one if you want a sound, or the same if no sound.
            /// </summary>
            public int CombineLiquidForSound;
            public TileChangeFromLiquid(Func<int, bool> tileType, Func<int, bool> liquidType, Func<int, int> resultTileType, int combineLiquidForSound)
            {
                CheckTileType = tileType;
                CheckLiquidType = liquidType;
                ResultTileType = resultTileType;
                CombineLiquidForSound = combineLiquidForSound;
            }
            public TileChangeFromLiquid(Func<int, bool> tileType, int liquidType, Func<int, int> resultTileType, int combineLiquidForSound)
            {
                CheckTileType = tileType;
                CheckLiquidType = (type) => type == liquidType;
                ResultTileType = resultTileType;
                CombineLiquidForSound = combineLiquidForSound;
            }
            public TileChangeFromLiquid(int tileType, Func<int, bool> liquidType, Func<int, int> resultTileType, int combineLiquidForSound)
            {
                CheckTileType = (type) => type == tileType;
                CheckLiquidType = liquidType;
                ResultTileType = resultTileType;
                CombineLiquidForSound = combineLiquidForSound;
            }
            public TileChangeFromLiquid(int tileType, int liquidType, Func<int, int> resultTileType, int combineLiquidForSound)
            {
                CheckTileType = (type) => type == tileType;
                CheckLiquidType = (type) => type == liquidType;
                ResultTileType = resultTileType;
                CombineLiquidForSound = combineLiquidForSound;
            }
            public TileChangeFromLiquid(Func<int, bool> tileType, Func<int, bool> liquidType, int resultTileType, int combineLiquidForSound)
            {
                CheckTileType = tileType;
                CheckLiquidType = liquidType;
                ResultTileType = (type) => resultTileType;
                CombineLiquidForSound = combineLiquidForSound;
            }
            public TileChangeFromLiquid(Func<int, bool> tileType, int liquidType, int resultTileType, int combineLiquidForSound)
            {
                CheckTileType = tileType;
                CheckLiquidType = (type) => type == liquidType;
                ResultTileType = (type) => resultTileType;
                CombineLiquidForSound = combineLiquidForSound;
            }
            public TileChangeFromLiquid(int tileType, Func<int, bool> liquidType, int resultTileType, int combineLiquidForSound)
            {
                CheckTileType = (type) => type == tileType;
                CheckLiquidType = liquidType;
                ResultTileType = (type) => resultTileType;
                CombineLiquidForSound = combineLiquidForSound;
            }
            public TileChangeFromLiquid(int tileType, int liquidType, int resultTileType, int combineLiquidForSound)
            {
                CheckTileType = (type) => type == tileType;
                CheckLiquidType = (type) => type == liquidType;
                ResultTileType = (type) => resultTileType;
                CombineLiquidForSound = combineLiquidForSound;
            }
            public TileChangeFromLiquid(Func<int, bool> tileType, Func<int, bool> liquidType, int combineLiquidForSound)
            {
                CheckTileType = tileType;
                CheckLiquidType = liquidType;
                ResultTileType = (type) => type;
                CombineLiquidForSound = combineLiquidForSound;
            }
        }
        private static List<TileChangeFromLiquid> tilesThatChangeFromLiquids = new() {
            new((tileType) => TileID.Sets.Conversion.Sand[tileType], LiquidID.Lava, (tileType) => SandConversions.TryGetValue(tileType, out int dictType) ? dictType : TileID.Sandstone, LiquidID.Water),
            new(TileID.SnowBlock, LiquidID.Water, TileID.Slush, LiquidID.Honey),
        };
        public class TileChangeFromMultiLiquid {
            public Func<int, bool> CheckTileType;
            /// <summary>
            /// The first 2 are used to determine the sound, so make sure they are the desired ones.  Providing less than 2 will cause an error.
            /// </summary>
            public List<int> LiquidTypes;
            public Func<int, int> ResultTileType;
            public TileChangeFromMultiLiquid(Func<int, bool> tileType, IEnumerable<int> liquidTypes, Func<int, int> resultTileType) {
                CheckTileType = tileType;
                LiquidTypes = liquidTypes.ToList();
                ResultTileType = resultTileType;
            }
            public TileChangeFromMultiLiquid(int tileType, IEnumerable<int> liquidTypes, Func<int, int> resultTileType) {
                CheckTileType = (type) => type == tileType;
                LiquidTypes = liquidTypes.ToList();
                ResultTileType = resultTileType;
            }
            public TileChangeFromMultiLiquid(Func<int, bool> tileType, IEnumerable<int> liquidTypes, int resultTileType) {
                CheckTileType = tileType;
                LiquidTypes = liquidTypes.ToList();
                ResultTileType = (type) => resultTileType;
            }
            public TileChangeFromMultiLiquid(int tileType, IEnumerable<int> liquidTypes, int resultTileType) {
                CheckTileType = (type) => type == tileType;
                LiquidTypes = liquidTypes.ToList();
                ResultTileType = (type) => resultTileType;
            }
            public bool CheckLiquidTypes(Tile[] liquidTiles, int start, int endNotInclusive, out int[] liquidLocations) {
                liquidLocations = new int[endNotInclusive];
                for (int i = 0; i < liquidLocations.Length; i++) {
                    liquidLocations[i] = -1;
                }

				bool[] found = new bool[LiquidTypes.Count];
                for (int i = start; i < endNotInclusive; i++) {
                    Tile liquidTile = liquidTiles[i];
                    if (liquidTile.LiquidAmount <= 0)
                        continue;

                    for (int j = 0; j < LiquidTypes.Count; j++) {
                        int liquidType = LiquidTypes[j];
						if (liquidType == liquidTile.LiquidType) {
                            found[j] = true;
                            liquidLocations[i] = liquidType;
                            break;
                        }
                    }
                }

                for (int i = 0; i < found.Length; i++) {
                    if (!found[i])
                        return false;
                }

                return true;
            }
        }
        private static List<int> LavaAndWater = new() { LiquidID.Lava, LiquidID.Water };
        private static List<int> HoneyAndWater = new() { LiquidID.Water, LiquidID.Honey };
        private static List<int> LavaAndHoney = new() { LiquidID.Lava, LiquidID.Honey };
        private static List<int> ShimmerAndWater = new() { LiquidID.Shimmer, LiquidID.Water };
        private static List<int> ShimmerAndHoney = new() { LiquidID.Shimmer, LiquidID.Honey };
        private static List<int> ShimmerAndLava = new() { LiquidID.Shimmer, LiquidID.Lava };
        private static List<int> LavaAndWaterAndHoney = new() { LiquidID.Lava, LiquidID.Water, LiquidID.Honey };
        private static List<int> ShimmerAndWaterAndHoney = new() { LiquidID.Shimmer, LiquidID.Water, LiquidID.Honey };
        private static List<int> ShimmerAndLavaAndHoney = new() { LiquidID.Shimmer, LiquidID.Lava, LiquidID.Honey };
        private static List<int> ShimmerAndLavaAndWater = new() { LiquidID.Shimmer, LiquidID.Lava, LiquidID.Water };
        private static List<int> ShimmerAndLavaAndWaterAndHoney = new() { LiquidID.Shimmer, LiquidID.Lava, LiquidID.Water, LiquidID.Honey };
        private static List<TileChangeFromMultiLiquid> tilesThatChangeFromMultipleLiquids = new() {
            new(TileID.Granite, LavaAndWater, TileID.Marble),
            new(TileID.HayBlock, LavaAndHoney, TileID.Ash)
        };
        /// <param name="canMoveVanilla">Passed in to allow forcing a liquid to move even if vanilla wouldn't allow it.</param>
        private static bool? CanMove(int x, int y, int xMove, int yMove, bool canMoveVanilla)
        {
            if (!ES_WorldGen.SkyblockWorld)
                return null;

            Tile tile = Main.tile[x, y];
            int oppositeX = xMove - (x - xMove);
            if (oppositeX < 0 || oppositeX >= Main.maxTilesX)
                return null;

            Tile moveTile = Main.tile[xMove, yMove];
            Tile opposite = Main.tile[oppositeX, yMove];
            if (moveTile.HasTile)
            {
                foreach (TileChangeFromLiquid tileChange in tilesThatChangeFromLiquids)
                {
                    if (tileChange.CheckTileType(moveTile.TileType) && (tileChange.CheckLiquidType(moveTile.LiquidType) || tileChange.CheckLiquidType(opposite.LiquidType)))
                    {
                        TryUpdateCombineInfo(xMove, yMove, convertDelay);

                        return false;
                    }
                }

                foreach (TileChangeFromMultiLiquid tileChange in tilesThatChangeFromMultipleLiquids)
                {
                    if (tileChange.CheckTileType(moveTile.TileType) && tileChange.LiquidTypes.Contains(tile.LiquidType))
                    {
                        TryUpdateCombineInfo(xMove, yMove, convertDelay);

                        return false;
                    }
                }

                return null;
            }

            if (opposite.LiquidAmount > 0 && tile.LiquidType != opposite.LiquidType)
            {
                foreach (StoneGenerator stoneGenerator in StoneGenerators)
                {
                    if (stoneGenerator.CheckLiquids(tile, opposite))
                    {
                        TryUpdateCombineInfo(xMove, yMove, stoneGenerator.Delay);
                        break;
                    }
                }

                return false;
            }


            if (yMove == Main.maxTilesY - 1)
                return null;

            int downY = yMove + 1;
            Tile down = Main.tile[xMove, downY];
            if (down.HasTile)
            {
                foreach (TileChangeFromLiquid tileChange in tilesThatChangeFromLiquids)
                {
                    if (tileChange.CheckTileType(down.TileType) && tileChange.CheckLiquidType(moveTile.LiquidType))
                    {
                        TryUpdateCombineInfo(xMove, downY, convertDelay);

                        return null;
                    }
                }

                foreach (TileChangeFromMultiLiquid tileChange in tilesThatChangeFromMultipleLiquids)
                {
                    if (tileChange.CheckTileType(down.TileType) && tileChange.LiquidTypes.Contains(moveTile.LiquidType))
                    {
                        TryUpdateCombineInfo(xMove, yMove, convertDelay);

                        return null;
                    }
                }
            }

            return null;
        }
        public static void TryUpdateCombineInfo(int x, int y, int delay = convertDelay)
        {
            Point combinePoint = new(x, y);
            int index = combinePoints.IndexOf(combinePoint);
            if (index == -1) {
                combinePoints.Add(combinePoint);
                combineInfos.Add(new CombineInfo(combineDelay, () => {
                    CheckCombineLiquids(x, y);
                }));
            }
            else {
                CombineInfo combineInfo = combineInfos[index];
                if (combineInfo.InitialDelay > delay) {
                    combineInfo.Delay -= combineInfo.InitialDelay - delay;
                    combineInfo.InitialDelay = delay;
                }
            }
        }
        private static SortedDictionary<int, int> SandConversions = new() {
            { TileID.Sand, TileID.Sandstone },
            { TileID.Crimsand, TileID.CrimsonSandstone },
            { TileID.Ebonsand, TileID.CorruptSandstone },
            { TileID.Pearlsand, TileID.HallowSandstone }
        };
        public class StoneGenerator
        {
            public Func<int> Result;
            public List<int> Liquids;
            public int Delay;
            public StoneGenerator(Func<int> result, List<int> liquids, int delay = combineDelay)
            {
                Result = result;
                Liquids = liquids;
                Delay = delay;
            }
            public StoneGenerator(int result, List<int> liquids, int delay = combineDelay) : this(() => result, liquids, delay) { }

            public bool CheckLiquids(Tile left, Tile right)
            {
                bool[] found = new bool[Liquids.Count];
                for (int i = 0; i < Liquids.Count; i++)
                {
                    if (left.LiquidType == Liquids[i])
                    {
                        found[i] = true;
                    }
                    else if (right.LiquidType == Liquids[i])
                    {
                        found[i] = true;
                    }
                }

                bool foundAll = true;
                foreach (bool b in found)
                {
                    if (!b)
                    {
                        foundAll = false;
                        break;
                    }
                }

                return foundAll;
            }
        }
        private static List<StoneGenerator> StoneGenerators = new() {
            new(() => Main.rand.Next(5) == 0 ? TileID.Silt : TileID.Stone, LavaAndWater),
            new(TileID.CrispyHoneyBlock, LavaAndHoney, combineDelay * 2),
            new(TileID.HoneyBlock, HoneyAndWater, combineDelay * 2),
            new(TileID.ShimmerBlock, ShimmerAndWater),
			//new(TileID., ShimmerAndLava),
			//new(TileID., ShimmerAndHoney),
		};
        private static void CheckCombineLiquids(int x, int y)
        {
            if (x <= 0 || x >= Main.maxTilesX - 1 || y <= 0 || y >= Main.maxTilesY - 1)
                return;

            Tile[] tiles = DirectionID.GetTiles(x, y);
            Tile tile = tiles[DirectionID.None];
            if (tile.HasTile) {
                foreach (TileChangeFromLiquid tileChange in tilesThatChangeFromLiquids) {
                    if (tileChange.CheckTileType(tile.TileType)) {
                        for (int i = DirectionID.None; i < DirectionID.Down; i++) {
                            Tile liquidTile = tiles[i];
                            if (liquidTile.LiquidAmount > 0 && tileChange.CheckLiquidType(liquidTile.LiquidType)) {
                                (int x, int y) direction = DirectionID.GetDirection(x, y, i);
								CreateTileChangeLiquidMerge(x, y, direction.x, direction.y, tileChange.ResultTileType(tile.TileType));//PlaceBlockFromLiquidMerge(x, y, tileChange.ResultTileType(tile.TileType), liquidTile.LiquidType, tileChange.CombineLiquidForSound);
								return;
                            }
                        }
                    }
                }

                foreach (TileChangeFromMultiLiquid tileChange in tilesThatChangeFromMultipleLiquids) {
                    if (tileChange.CheckTileType(tile.TileType)) {
                        if (tileChange.CheckLiquidTypes(tiles, DirectionID.None + 1, DirectionID.Down, out int[] liquidLocations)) {

							CreateTileChangeLiquidMerge(x, y, liquidLocations, tileChange.ResultTileType(tile.TileType));//PlaceBlockFromLiquidMerge(x, y, tileChange.ResultTileType(tile.TileType), tileChange.LiquidTypes[0], tileChange.LiquidTypes[1]);
							return;
                        }
                    }
                }

                return;
            }

            Tile left = tiles[DirectionID.Left];
            if (left.LiquidAmount > 0) {
                Tile right = tiles[DirectionID.Right];
                if (right.LiquidAmount > 0) {
                    foreach (StoneGenerator stoneGenerator in StoneGenerators) {
                        if (stoneGenerator.CheckLiquids(left, right))
                            CreateStoneGeneratorLiquidMerge(x, y, stoneGenerator.Result());//PlaceBlockFromLiquidMerge(x, y, stoneGenerator.Result(), left.LiquidType, right.LiquidType);
                    }
                }
            }
        }

		public const string TileChangeMergeContext = "ES_TileChange";
		private static void CreateTileChangeLiquidMerge(int x, int y, int liquidTileX, int liquidTileY, int tileType) {
			List<LiquidMergeIngredient> ingredients = new() {
				new LiquidMergeIngredientTileCustom(liquidTileX, liquidTileY, liquidConsumeAmount: 0)
			};

			LiquidMerge merge = new(x, y, ingredients, TileChangeMergeContext);
			merge.GetLiquidMergeTypes();
			merge.LiquidMergeTileType = tileType;
			merge.Merge(out Dictionary<int, int> consumedLiquids);
			LL_LiquidLoader.OnMerge(merge, consumedLiquids);
		}
        private static void CreateTileChangeLiquidMerge(int x, int y, int[] liquidLocations, int tileType) {
            List<LiquidMergeIngredient> ingredients = new();
            for (int i = 0; i < liquidLocations.Length; i++) {
                if (liquidLocations[i] > -1) {
                    (int x, int y) direction = DirectionID.GetDirection(x, y, i);
                    ingredients.Add(new LiquidMergeIngredientTileCustom(direction.x, direction.y, liquidConsumeAmount: 0));
				}
            }

            LiquidMerge merge = new(x, y, ingredients, TileChangeMergeContext);
			merge.GetLiquidMergeTypes();
			merge.LiquidMergeTileType = tileType;
			merge.Merge(out Dictionary<int, int> consumedLiquids);
			LL_LiquidLoader.OnMerge(merge, consumedLiquids);
		}
		public const string StoneGeneratorMergeContext = "ES_StoneGenerator";
		private static void CreateStoneGeneratorLiquidMerge(int x, int y, int tileType) {
            List<LiquidMergeIngredient> ingredients = new() {
				new LiquidMergeIngredientTileCustom(x - 1, y, liquidConsumeAmount: 0),
				new LiquidMergeIngredientTileCustom(x + 1, y, liquidConsumeAmount: 0)
			};

            LiquidMerge merge = new(x, y, ingredients, StoneGeneratorMergeContext);
            merge.GetLiquidMergeTypes();
            merge.LiquidMergeTileType = tileType;
			merge.Merge(out Dictionary<int, int> consumedLiquids);
            LL_LiquidLoader.OnMerge(merge, consumedLiquids);
		}
	}
}
