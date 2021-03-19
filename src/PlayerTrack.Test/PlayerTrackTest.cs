// ReSharper disable NotAccessedField.Local

using NUnit.Framework;
using PlayerTrack.Mock;

namespace PlayerTrack.Test
{
    [TestFixture]
    public class PlayerTrackTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [TearDown]
        public void TearDown()
        {
        }

        public LodestoneService LodestoneService;
        public PlayerService PlayerService;
        private MockPlayerTrackPlugin _plugin;

        public PlayerTrackTest(MockPlayerTrackPlugin plugin)
        {
            _plugin = plugin;
        }

        [Test]
        public void PlayerTrackTest1()
        {
            Assert.IsTrue(true);
        }
    }
}