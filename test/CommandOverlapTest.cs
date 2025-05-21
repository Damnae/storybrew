using StorybrewCommon.Storyboarding;

namespace Test
{
    [TestClass]
    public class CommandOverlapTest
    {
        [TestMethod]
        public void TestNoOverlapGap()
        {
            var sprite = new OsbSprite();

            sprite.MoveX(0, 1000, 0, 100);
            sprite.MoveX(2000, 3000, 20000, 30000);

            Assert.IsFalse(sprite.HasOverlappedCommands);

            Assert.AreEqual(0, sprite.PositionAt(-1000).X, "before start");

            Assert.AreEqual(0, sprite.PositionAt(0).X, "at start");
            Assert.AreEqual(50, sprite.PositionAt(500).X);
            Assert.AreEqual(100, sprite.PositionAt(1000).X, "at end");

            Assert.AreEqual(100, sprite.PositionAt(1500).X, "between commands");

            Assert.AreEqual(20000, sprite.PositionAt(2000).X, "at start");
            Assert.AreEqual(25000, sprite.PositionAt(2500).X);
            Assert.AreEqual(30000, sprite.PositionAt(3000).X, "at end");

            Assert.AreEqual(30000, sprite.PositionAt(4000).X, "past the end");
        }

        [TestMethod]
        public void TestNoOverlapTouching()
        {
            var sprite = new OsbSprite();

            sprite.MoveX(0, 1000, 0, 100);
            sprite.MoveX(1000, 2000, 10000, 20000);

            Assert.IsFalse(sprite.HasOverlappedCommands);

            Assert.AreEqual(0, sprite.PositionAt(-1000).X, "before start");

            Assert.AreEqual(0, sprite.PositionAt(0).X, "at start");
            Assert.AreEqual(50, sprite.PositionAt(500).X);
            Assert.AreEqual(100, sprite.PositionAt(1000).X, "first command still has priority at the time it ends");

            Assert.AreEqual(15000, sprite.PositionAt(1500).X, "second command takes over past the first one's end");
            Assert.AreEqual(20000, sprite.PositionAt(2000).X, "at end");

            Assert.AreEqual(20000, sprite.PositionAt(3000).X, "past the end");
        }

        [TestMethod]
        public void TestPartialOverlap()
        {
            var sprite = new OsbSprite();

            sprite.MoveX(0, 1000, 0, 100);
            sprite.MoveX(500, 1500, 5000, 15000);

            Assert.IsTrue(sprite.HasOverlappedCommands);

            Assert.AreEqual(0, sprite.PositionAt(-1000).X, "before start");

            Assert.AreEqual(0, sprite.PositionAt(0).X, "at start");
            Assert.AreEqual(25, sprite.PositionAt(250).X);
            Assert.AreEqual(50, sprite.PositionAt(500).X, "first command still has priority when second starts");
            Assert.AreEqual(75, sprite.PositionAt(750).X, "first command still has priority until it ends");
            Assert.AreEqual(100, sprite.PositionAt(1000).X, "first command still has priority at the time it ends");

            Assert.AreEqual(12500, sprite.PositionAt(1250).X, "second command takes over past the first one's end");
            Assert.AreEqual(15000, sprite.PositionAt(1500).X, "at end");

            Assert.AreEqual(15000, sprite.PositionAt(2000).X, "past the end");
        }

        [TestMethod]
        public void TestCompleteOverlap()
        {
            var sprite = new OsbSprite();

            sprite.MoveX(0, 1000, 0, 100);
            sprite.MoveX(250, 750, 200, 300);

            Assert.IsTrue(sprite.HasOverlappedCommands);

            Assert.AreEqual(0, sprite.PositionAt(-1000).X, "before start");

            Assert.AreEqual(0, sprite.PositionAt(0).X, "at start");
            Assert.AreEqual(25, sprite.PositionAt(250).X);
            Assert.AreEqual(50, sprite.PositionAt(500).X);
            Assert.AreEqual(75, sprite.PositionAt(750).X);
            Assert.AreEqual(100, sprite.PositionAt(1000).X, "at end");

            // Cursed osu!stable behavior, the second command takes over past the end of the first
            Assert.AreEqual(300, sprite.PositionAt(2000).X, "past the end");
        }

        [TestMethod]
        public void TestSameStartOverlap()
        {
            var sprite = new OsbSprite();

            // the command that ends first has priority
            sprite.MoveX(0, 1000, 0, 100);
            sprite.MoveX(0, 2000, 1000, 2000);

            Assert.IsTrue(sprite.HasOverlappedCommands);

            Assert.AreEqual(0, sprite.PositionAt(-1000).X, "before start");

            Assert.AreEqual(0, sprite.PositionAt(0).X, "at start");
            Assert.AreEqual(25, sprite.PositionAt(250).X);
            Assert.AreEqual(50, sprite.PositionAt(500).X);
            Assert.AreEqual(75, sprite.PositionAt(750).X);
            Assert.AreEqual(100, sprite.PositionAt(1000).X, "at end");

            Assert.AreEqual(1750, sprite.PositionAt(1500).X, "second command takes effect past the first one's end");
            Assert.AreEqual(2000, sprite.PositionAt(2000).X, "at end");

            Assert.AreEqual(2000, sprite.PositionAt(3000).X, "past the end");
        }

        [TestMethod]
        public void TestSameEndOverlap()
        {
            var sprite = new OsbSprite();

            sprite.MoveX(0, 1000, 0, 100);
            sprite.MoveX(500, 1000, 1000, 2000);

            Assert.IsTrue(sprite.HasOverlappedCommands);

            Assert.AreEqual(0, sprite.PositionAt(-1000).X, "before start");

            Assert.AreEqual(0, sprite.PositionAt(0).X, "at start");
            Assert.AreEqual(25, sprite.PositionAt(250).X);
            Assert.AreEqual(50, sprite.PositionAt(500).X);
            Assert.AreEqual(75, sprite.PositionAt(750).X);
            Assert.AreEqual(100, sprite.PositionAt(1000).X, "at end");

            // Cursed osu!stable behavior, the second command takes over past the end of the first
            Assert.AreEqual(2000, sprite.PositionAt(2000).X, "past the end");
        }

        [TestMethod]
        public void TestSameStartEndOverlap()
        {
            var sprite = new OsbSprite();

            sprite.MoveX(0, 1000, 100, 200);
            sprite.MoveX(0, 1000, -100, -200);

            Assert.IsTrue(sprite.HasOverlappedCommands);

            Assert.AreEqual(100, sprite.PositionAt(-1000).X, "before start");

            Assert.AreEqual(100, sprite.PositionAt(0).X, "at start");
            Assert.AreEqual(125, sprite.PositionAt(250).X);
            Assert.AreEqual(150, sprite.PositionAt(500).X);
            Assert.AreEqual(175, sprite.PositionAt(750).X);
            Assert.AreEqual(200, sprite.PositionAt(1000).X, "at end");

            // Cursed osu!stable behavior, the second command takes over past the end of the first
            Assert.AreEqual(-200, sprite.PositionAt(2000).X, "past the end");
        }
    }
}