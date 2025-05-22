using StorybrewCommon.Storyboarding;

namespace Test
{
    [TestClass]
    public class ReversedCommandTest
    {
        [TestMethod]
        public void TestSimpleReversedCommand()
        {
            var sprite = new OsbSprite();

            sprite.MoveX(2000, 1000, 200, 100);

            Assert.AreEqual(200, sprite.PositionAt(0).X, "before start");

            Assert.AreEqual(200, sprite.PositionAt(1000).X, "at reversed end");
            Assert.AreEqual(200, sprite.PositionAt(1500).X, "during reversed");
            Assert.AreEqual(200, sprite.PositionAt(2000).X, "at reversed start");
            Assert.AreEqual(100, sprite.PositionAt(2001).X, "right past reversed start");

            Assert.AreEqual(100, sprite.PositionAt(3000).X, "past the end");
        }
    }
}