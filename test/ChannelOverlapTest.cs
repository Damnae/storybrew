using StorybrewCommon.Storyboarding;

namespace Test
{
    [TestClass]
    public class ChannelOverlapTest
    {
        [TestMethod]
        public void TestPartialOverlaps()
        {
            var sprite = new OsbSprite();

            sprite.MoveX(0, 1000, 0, 100);

            sprite.StartLoopGroup(1000, 1);
            sprite.MoveX(0, 1000, 200, 300);
            sprite.EndGroup();

            sprite.MoveX(2000, 3000, 400, 500);

            Assert.AreEqual(0, sprite.PositionAt(-1000).X, "before start");

            Assert.AreEqual(0, sprite.PositionAt(0).X, "at start");
            Assert.AreEqual(100, sprite.PositionAt(1000).X, "first command still has priority when it ends");
            Assert.AreEqual(300, sprite.PositionAt(2000).X, "second command still has priority when it ends");
            Assert.AreEqual(500, sprite.PositionAt(3000).X, "at end");

            Assert.AreEqual(500, sprite.PositionAt(4000).X, "past the end");
        }

        [TestMethod]
        public void TestSameStartOverlapNoLoop()
        {
            var sprite = new OsbSprite();

            sprite.MoveX(0, 1000, 0, 100);
            sprite.MoveX(0, 500, 200, 300);

            Assert.AreEqual(200, sprite.PositionAt(-1000).X, "before start");

            Assert.AreEqual(200, sprite.PositionAt(0).X, "at start");
            Assert.AreEqual(300, sprite.PositionAt(500).X, "at short end");
            Assert.AreEqual(100, sprite.PositionAt(1000).X, "at long end");

            Assert.AreEqual(100, sprite.PositionAt(2000).X, "past the end");
        }

        [TestMethod]
        public void TestSameStartOverlapLongestInLoop()
        {
            var sprite = new OsbSprite();

            sprite.StartLoopGroup(0, 1);
            sprite.MoveX(0, 1000, 0, 100);
            sprite.EndGroup();

            sprite.MoveX(0, 500, 200, 300);

            Assert.AreEqual(200, sprite.PositionAt(-1000).X, "before start");

            Assert.AreEqual(200, sprite.PositionAt(0).X, "at start");
            Assert.AreEqual(300, sprite.PositionAt(500).X, "at short end");
            Assert.AreEqual(100, sprite.PositionAt(1000).X, "at long end");

            Assert.AreEqual(100, sprite.PositionAt(2000).X, "past the end");
        }

        [TestMethod]
        public void TestSameStartOverlapShortestInLoop()
        {
            var sprite = new OsbSprite();

            sprite.MoveX(0, 1000, 0, 100);

            sprite.StartLoopGroup(0, 1);
            sprite.MoveX(0, 500, 200, 300);
            sprite.EndGroup();

            Assert.AreEqual(200, sprite.PositionAt(-1000).X, "before start");

            Assert.AreEqual(200, sprite.PositionAt(0).X, "at start");
            Assert.AreEqual(300, sprite.PositionAt(500).X, "at short end");
            Assert.AreEqual(100, sprite.PositionAt(1000).X, "at long end");

            Assert.AreEqual(100, sprite.PositionAt(2000).X, "past the end");
        }
    }
}