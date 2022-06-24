using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gwindalmir.Updater;

namespace Phoenix.WorkshopTool.Tests.Common
{
    [TestFixture]
    [Category("NoSteam")]
    public class UpdaterTests
    {
        [Test]
        public void GetReleaseTest()
        {
            var updateChecker = new UpdateChecker(prereleases: true);
            var release = updateChecker.GetLatestRelease();

            Assert.That(release, Is.Not.Null);
            Assert.That(release.Id, Is.Not.Zero);
            Assert.That(release.Assets, Is.Not.Null.And.Count.GreaterThan(0));
        }

        [Test]
        public void IsNewerThanTest()
        {
            var updateChecker = new UpdateChecker(prereleases: true);

            Assert.That(updateChecker.IsNewerThan(new Version(0, 1, 0)), Is.True);
        }

        [TestCase("SE")]
        [TestCase("ME")]
        public void GetMatchingAssetTest(string prefix)
        {
            var updateChecker = new UpdateChecker(prereleases: true);
            var asset = updateChecker.GetLatestRelease().GetMatchingAsset(prefix);

            if (asset == null)
                Assert.Inconclusive("No asset for the latest release, cannot continue test.");

            Assert.That(asset.Id, Is.Not.Zero);
            Assert.That(asset.Url, Is.Not.Null.And.Not.Empty.And.Contains($"{prefix}WorkshopTool"));
        }
    }
}
