using androLib.UI.GuideBook;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaAutomations;
using TerrariaAutomations.Items;

namespace EngagedSkyblock.UI.GuideBook {
    internal class ES_GuideBook : androLib.UI.GuideBook.GuideBook {
        public override string ModName => ES_Mod.ModName;
        protected override List<GuideBookSection> sections {
            get {
                if (_sections == null || Debugger.IsAttached)
                    SetupSections();

                return _sections;
            }
        }
        private List<GuideBookSection> _sections = null;

        private const string GeneralSectionName = "General";
        private const string IntroductionTopicName = "Introduction";
        private const string StartingOffTopicName = "Starting Off";

        private const string TestSectionName = "TestSection";
        private const string TestTopicName = "TestTopic";
        private const string TestTopic2Name = "TestTopic2";
        private void SetupSections() {
            _sections = [
                new(GeneralSectionName, GetVanillaItemTexture(ItemID.Acorn), [
                    new(IntroductionTopicName, GetVanillaItemTexture(ItemID.WireKite), IntroductionSetupTopic),
                    new(StartingOffTopicName, GetVanillaItemTexture(ItemID.Wood), StartingOffSetupTopic),
                ]),
                new(TestSectionName, GetVanillaItemTexture(ItemID.DirtBlock), [
                    new(TestTopicName, GetVanillaItemTexture(ItemID.QueenSlimeMask), TestSetupTopic),
                    new(TestTopic2Name, GetVanillaItemTexture(ItemID.KingSlimeBossBag), Test2SetupTopic),
                ]),
            ];
        }
        private void IntroductionSetupTopic(UIPanel topicPanel) {
            AddListAndScrollbar(topicPanel, out AM_UIList list);

            list.Add(MakeText("The typical Terraria skyblock experience requires waiting around for things to happen.  The goal of Engaged Skyblock is to provide new mechanics that allow players to always have a way to progress that doesn't require waiting."));
            list.Add(MakeText("It focuses on building and automation.  The starting world is a tiny island with a tree and a chest with a bucket of water and lava.  The rest of the world is empty."));
            list.Add(MakeText("To make a skyblock world, you have to use one of the skyblock seeds:"));
            list.Add(MakeText("-skyblock"));
            list.Add(MakeText("-fortheworthyskyblock - starts you with the same skyblock, but has the vanilla fortheworthy settings while playing.  (Beware of Tree bombs)"));
        }
        private void StartingOffSetupTopic(UIPanel topicPanel) {
            AddListAndScrollbar(topicPanel, out AM_UIList list);

            list.Add(MakeTitle("Speedy Growth"));
            list.Add(GetScreenshotImage("GrowthEffect"));
            list.Add(MakeText("Running, Jumping and Moving fast causes very fast growth around the player for trees, grass and similar.  A small green sparkle occurs on the block each time this effect has a chance to trigger."));

            list.Add(MakeTitle("Stone Generator"));
            list.Add(GetScreenshotImage("StoneGenerator"));
            list.Add(MakeText("Some liquid physics have been adjusted for this mod.  If lava and water are separated by 1 block horizontally, they will form stone or silt without consuming any of the water or lava, allowing for infinite stone/silt generation.  75% chance of stone.  25% chance of silt."));

            list.Add(MakeTitle("Early Dirt"));
            list.Add(GetScreenshotImage("DirtCrafting"));
            list.Add(MakeText("Breaking/Shaking trees spawns Leaf Blocks which can be used with silt to craft dirt blocks."));

            list.Add(MakeTitle("Hammers!"));
            list.Add(MakeText("Hammers have been given extra functionality.  Click and hold right click with a hammer to break blocks as if the hammer is a pickaxe.  This will convert some blocks into more crushed versions."));
            list.Add(MakeText("Stone -> Sand"));
            list.Add(MakeText("Wood -> Wood Chips"));

            list.Add(MakeTitle("Fishing"));
            list.Add(MakeText("Wood Chips can be opened by right clicking them like loot bags, giving you Acorns and bugs."));
        }
        private void TestSetupTopic(UIPanel topicPanel) {
            AddListAndScrollbar(topicPanel, out AM_UIList list);


        }
        private void Test2SetupTopic(UIPanel topicPanel) {
            AddListAndScrollbar(topicPanel, out AM_UIList list);


        }

        public static UIImage GetScreenshotImage(string screenshotName) {
            Asset<Texture2D> asset = ModContent.Request<Texture2D>($"EngagedSkyblock/Content/GuideBook/Screenshots/{screenshotName}", AssetRequestMode.ImmediateLoad);
            UIImage image = new UIImage(asset);
            return image;
        }
    }
}
