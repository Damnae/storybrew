using StorybrewCommon.Storyboarding;

namespace Test
{
    [TestClass]
    public class LoopOverlapTest
    {
        [TestMethod]
        public void TestPlainCommands()
        {
            var sprite = new OsbSprite();

            sprite.MoveX(0, 1000, 0, 100);
            sprite.MoveX(1000, 2000, 100, 200);
            sprite.MoveX(2000, 3000, 0, 100);
            sprite.MoveX(3000, 4000, 100, 200);

            assertSpritePositions(sprite);
        }

        [TestMethod]
        public void TestSimpleLoop()
        {
            var sprite = new OsbSprite();

            sprite.StartLoopGroup(0, 2);
            sprite.MoveX(0, 1000, 0, 100);
            sprite.MoveX(1000, 2000, 100, 200);
            sprite.EndGroup();

            assertSpritePositions(sprite);
        }

        [TestMethod]
        public void TestOverlappingLoops()
        {
            var sprite = new OsbSprite();

            sprite.StartLoopGroup(0, 2);
            sprite.MoveX(0, 1000, 0, 100);
            sprite.Fade(1000, 2000, 1, 1);
            sprite.EndGroup();

            sprite.StartLoopGroup(0, 2);
            sprite.Fade(0, 1000, 1, 1);
            sprite.MoveX(1000, 2000, 100, 200);
            sprite.EndGroup();

            assertSpritePositions(sprite);
        }

        private static void assertSpritePositions(OsbSprite sprite)
        {
            Assert.IsFalse(sprite.HasOverlappedCommands);

            Assert.AreEqual(0, sprite.PositionAt(-1000).X, "before start");

            Assert.AreEqual(0, sprite.PositionAt(0).X, "at start");
            Assert.AreEqual(50, sprite.PositionAt(500).X);
            Assert.AreEqual(100, sprite.PositionAt(1000).X);
            Assert.AreEqual(150, sprite.PositionAt(1500).X);
            Assert.AreEqual(200, sprite.PositionAt(2000).X, "at loop repeat");
            Assert.AreEqual(50, sprite.PositionAt(2500).X);
            Assert.AreEqual(100, sprite.PositionAt(3000).X);
            Assert.AreEqual(150, sprite.PositionAt(3500).X);
            Assert.AreEqual(200, sprite.PositionAt(4000).X, "at end");

            Assert.AreEqual(200, sprite.PositionAt(10000).X, "after end");
        }
    }
}