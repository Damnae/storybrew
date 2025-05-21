using StorybrewCommon.Storyboarding;

namespace Test
{
    [TestClass]
    public class LoopOffsetTest
    {
        [TestMethod]
        public void TestNonZeroStart()
        {
            var sprite = new OsbSprite();

            sprite.StartLoopGroup(0, 2);
            sprite.MoveX(1000, 2000, 0, 100);
            sprite.EndGroup();

            Assert.AreEqual(0, sprite.PositionAt(-1000).X, "before start");

            Assert.AreEqual(0, sprite.PositionAt(0).X, "at loop start");
            Assert.AreEqual(0, sprite.PositionAt(500).X, "before move start");
            Assert.AreEqual(0, sprite.PositionAt(1000).X, "at move start");
            Assert.AreEqual(50, sprite.PositionAt(1500).X);
            Assert.AreEqual(100, sprite.PositionAt(2000).X, "at move end");

            Assert.AreEqual(50, sprite.PositionAt(2500).X);
            Assert.AreEqual(100, sprite.PositionAt(3000).X, "at end");

            Assert.AreEqual(100, sprite.PositionAt(4000).X, "past the end");
        }

        [TestMethod]
        public void TestNegativeStart()
        {
            var sprite = new OsbSprite();

            sprite.StartLoopGroup(0, 2);
            sprite.MoveX(-500, 500, 0, 100);
            sprite.EndGroup();

            Assert.AreEqual(0, sprite.PositionAt(-1000).X, "before start");

            Assert.AreEqual(0, sprite.PositionAt(-500).X, "at move start");
            Assert.AreEqual(50, sprite.PositionAt(0).X, "at 'loop start'");
            Assert.AreEqual(100, sprite.PositionAt(500).X, "at move end");

            Assert.AreEqual(50, sprite.PositionAt(1000).X);
            Assert.AreEqual(100, sprite.PositionAt(1500).X);

            Assert.AreEqual(100, sprite.PositionAt(2000).X, "past the end");
        }
    }
}