using StorybrewCommon.Storyboarding;

namespace Test
{
    [TestClass]
    public class CommandTest
    {
        [TestMethod]
        public void TestBooleanPointCommand()
        {
            var sprite = new OsbSprite();

            sprite.Additive(1000, 1000);

            Assert.IsTrue(sprite.AdditiveAt(0), "before point command");
            Assert.IsTrue(sprite.AdditiveAt(1000), "at point command");
            Assert.IsTrue(sprite.AdditiveAt(2000), "after point command");
        }

        [TestMethod]
        public void TestBooleanCommand()
        {
            var sprite = new OsbSprite();

            sprite.Additive(1000, 2000);

            Assert.IsFalse(sprite.AdditiveAt(0), "before command");
            Assert.IsTrue(sprite.AdditiveAt(1000), "command start");
            Assert.IsTrue(sprite.AdditiveAt(1500), "during");
            Assert.IsTrue(sprite.AdditiveAt(2000), "command end");
            Assert.IsFalse(sprite.AdditiveAt(3000), "after command");
        }

        [TestMethod]
        public void TestBooleanCommandAfterPoint()
        {
            var sprite = new OsbSprite();

            sprite.Additive(1000, 1000);
            sprite.Additive(2000, 3000);

            Assert.IsTrue(sprite.AdditiveAt(0), "before commands");
            Assert.IsTrue(sprite.AdditiveAt(1000), "at point command");
            Assert.IsTrue(sprite.AdditiveAt(1500), "between commands");
            Assert.IsTrue(sprite.AdditiveAt(2000), "command start");
            Assert.IsTrue(sprite.AdditiveAt(2500), "during");
            Assert.IsTrue(sprite.AdditiveAt(3000), "command end");
            Assert.IsFalse(sprite.AdditiveAt(4000), "after commands");
        }

        [TestMethod]
        public void TestBooleanCommandBeforePoint()
        {
            var sprite = new OsbSprite();

            sprite.Additive(1000, 2000);
            sprite.Additive(3000, 3000);

            Assert.IsFalse(sprite.AdditiveAt(0), "before commands");
            Assert.IsTrue(sprite.AdditiveAt(1000), "command start");
            Assert.IsTrue(sprite.AdditiveAt(1500), "during");
            Assert.IsTrue(sprite.AdditiveAt(2000), "command end");
            Assert.IsFalse(sprite.AdditiveAt(2500), "between commands");
            Assert.IsTrue(sprite.AdditiveAt(3000), "at point command");
            Assert.IsTrue(sprite.AdditiveAt(4000), "after commands");
        }

        [TestMethod]
        public void TestBooleanCommandsInLoop()
        {
            var loopCount = 3;

            var sprite = new OsbSprite();

            sprite.StartLoopGroup(0, loopCount);
            sprite.FlipH(0, 200);
            sprite.FlipV(100, 300);
            sprite.EndGroup();

            Assert.IsFalse(sprite.FlipHAt(-1000), $"before loops, H");
            Assert.IsFalse(sprite.FlipVAt(-1000), $"before loops, V");
            Assert.IsFalse(sprite.FlipHAt(300 * loopCount + 1000), $"after loops, H");
            Assert.IsFalse(sprite.FlipVAt(300 * loopCount + 1000), $"after loops, V");

            // In/out of commands
            for (var i = 0; i < loopCount; i++)
            {
                Assert.IsTrue(sprite.FlipHAt(i * 300 + 100), $"loop {i}, during H");
                Assert.IsFalse(sprite.FlipHAt(i * 300 + 250), $"loop {i}, after H");

                Assert.IsTrue(sprite.FlipVAt(i * 300 + 200), $"loop {i}, during V");
                Assert.IsFalse(sprite.FlipVAt(i * 300 + 50), $"loop {i}, before V");
            }

            // At command boundary
            for (var i = 0; i < loopCount; i++)
            {
                Assert.IsTrue(sprite.FlipHAt(i * 300), $"loop {i}, at H start");
                Assert.IsTrue(sprite.FlipHAt(i * 300 + 200), $"loop {i}, at H end");

                Assert.IsTrue(sprite.FlipVAt(i * 300 + 100), $"loop {i}, at V start");
                Assert.IsTrue(sprite.FlipVAt(i * 300 + 300), $"loop {i}, at V end");
            }
        }

        [TestMethod]
        public void TestBooleanCommandsChannelOverlap()
        {
        }
    }
}