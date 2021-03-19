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
        #pragma warning disable 169
        private MockPlayerTrackPlugin _plugin;
        #pragma warning restore 169
        
        [Test]
        public void PlayerTrackTest1()
        {
            Assert.IsTrue(true);
        }
    }
}