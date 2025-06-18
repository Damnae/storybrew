using StorybrewCommon.Storyboarding;

namespace Test
{
    [TestClass]
    public class SpriteDisplayTimeTest
    {
        [TestMethod]
        public void TestSimpleFade()
        {
            var sprite = new OsbSprite();

            sprite.MoveX(0, 1000, 0, 100);

            sprite.Fade(250, 500, 0, 1);
            sprite.Fade(500, 750, 1, 0);

            Assert.IsFalse(sprite.HasOverlappedCommands);

            Assert.AreEqual(0, sprite.CommandsStartTime);
            Assert.AreEqual(1000, sprite.CommandsEndTime);

            Assert.AreEqual(250, sprite.DisplayStartTime);
            Assert.AreEqual(750, sprite.DisplayEndTime);
        }

        [TestMethod]
        public void TestSimpleScale()
        {
            var sprite = new OsbSprite();

            sprite.MoveX(0, 1000, 0, 100);

            sprite.Scale(250, 500, 0, 1);
            sprite.Scale(500, 750, 1, 0);

            Assert.IsFalse(sprite.HasOverlappedCommands);

            Assert.AreEqual(0, sprite.CommandsStartTime);
            Assert.AreEqual(1000, sprite.CommandsEndTime);

            Assert.AreEqual(250, sprite.DisplayStartTime);
            Assert.AreEqual(750, sprite.DisplayEndTime);
        }

        [TestMethod]
        public void TestSimpleScaleX()
        {
            var sprite = new OsbSprite();

            sprite.MoveX(0, 1000, 0, 100);

            sprite.ScaleVec(250, 500, 0, 1, 1, 1);
            sprite.ScaleVec(500, 750, 1, 1, 0, 1);

            Assert.IsFalse(sprite.HasOverlappedCommands);

            Assert.AreEqual(0, sprite.CommandsStartTime);
            Assert.AreEqual(1000, sprite.CommandsEndTime);

            Assert.AreEqual(250, sprite.DisplayStartTime);
            Assert.AreEqual(750, sprite.DisplayEndTime);
        }

        [TestMethod]
        public void TestSimpleScaleY()
        {
            var sprite = new OsbSprite();

            sprite.MoveX(0, 1000, 0, 100);

            sprite.ScaleVec(250, 500, 1, 0, 1, 1);
            sprite.ScaleVec(500, 750, 1, 1, 1, 0);

            Assert.IsFalse(sprite.HasOverlappedCommands);

            Assert.AreEqual(0, sprite.CommandsStartTime);
            Assert.AreEqual(1000, sprite.CommandsEndTime);

            Assert.AreEqual(250, sprite.DisplayStartTime);
            Assert.AreEqual(750, sprite.DisplayEndTime);
        }

        [TestMethod]
        public void TestFadeAndScale()
        {
            var sprite = new OsbSprite();

            sprite.MoveX(0, 1000, 0, 100);

            sprite.Fade(250, 500, 0, 1);
            sprite.Scale(500, 750, 1, 0);

            Assert.IsFalse(sprite.HasOverlappedCommands);

            Assert.AreEqual(0, sprite.CommandsStartTime);
            Assert.AreEqual(1000, sprite.CommandsEndTime);

            Assert.AreEqual(250, sprite.DisplayStartTime);
            Assert.AreEqual(750, sprite.DisplayEndTime);
        }

        [TestMethod]
        public void TestStartEndOverlap()
        {
            var sprite = new OsbSprite();

            sprite.MoveX(0, 1000, 0, 100);

            sprite.Fade(250, 500, 0, 1);
            sprite.Fade(500, 750, 1, .5);
            sprite.Fade(600, 700, 1, 0);

            Assert.IsTrue(sprite.HasOverlappedCommands);

            Assert.AreEqual(0, sprite.CommandsStartTime);
            Assert.AreEqual(1000, sprite.CommandsEndTime);

            Assert.AreEqual(250, sprite.DisplayStartTime);
            Assert.AreEqual(750, sprite.DisplayEndTime);
        }

        [TestMethod]
        public void TestZeroToZeroValue()
        {
            var sprite = new OsbSprite();

            sprite.MoveX(0, 1000, 0, 100);

            sprite.Fade(200, 400, 0, 0);
            sprite.Fade(400, 500, 0, 1);
            sprite.Fade(500, 600, 1, 0);
            sprite.Fade(600, 800, 0, 0);

            Assert.IsFalse(sprite.HasOverlappedCommands);

            Assert.AreEqual(0, sprite.CommandsStartTime);
            Assert.AreEqual(1000, sprite.CommandsEndTime);

            Assert.AreEqual(400, sprite.DisplayStartTime);
            Assert.AreEqual(600, sprite.DisplayEndTime);
        }

        [TestMethod]
        public void TestLoop()
        {
            var sprite = new OsbSprite();

            sprite.StartLoopGroup(0, 2);
            sprite.MoveX(0, 2500, 100, 0);
            sprite.Scale(0, 500, 0, 1);
            sprite.Scale(1000, 2000, 1, 0);
            sprite.EndGroup();

            Assert.IsFalse(sprite.HasOverlappedCommands);

            Assert.AreEqual(0, sprite.CommandsStartTime);
            Assert.AreEqual(0 + 2500 * 2, sprite.CommandsEndTime);

            Assert.AreEqual(0, sprite.DisplayStartTime);
            Assert.AreEqual(0 + 2500 + 2000, sprite.DisplayEndTime);
        }
    }
}