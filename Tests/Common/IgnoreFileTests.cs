using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Phoenix.WorkshopTool.Tests.Common
{
    [TestFixture]
    [Category("NoSteam")]
    public class IgnoreFileTests
    {
        [Test]
        public void TestReadIgnoreFile()
        {
            var testfilesLocation = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), 
                "..", "..", "..", "TestFiles", ".wtignore");

            IgnoreFile.TryLoadIgnoreFile(testfilesLocation, out var ignoredExtensions, out var ignoredPaths);

            // This test must match the filesystem, and the .wtignore in TestFiles.
            Assert.That(ignoredExtensions.Count, Is.EqualTo(3));
            Assert.That(ignoredExtensions, Has.Member(".wtignore"));
            Assert.That(ignoredExtensions, Has.Member(".diff"));
            Assert.That(ignoredExtensions, Has.Member(".wild"));

            Assert.That(ignoredPaths.Count, Is.EqualTo(7));
            Assert.That(ignoredPaths, Has.Member("Thumb.db"));
            Assert.That(ignoredPaths, Has.Member("DirectoryToIgnore\\FileIgnoredImplicitly.txt"));
            Assert.That(ignoredPaths, Has.Member("WildcardIgnoredDirectory\\"));
            Assert.That(ignoredPaths, Has.Member("WildcardIgnoredDirectory\\FileIgnoredImplicitly.txt"));
            Assert.That(ignoredPaths, Has.Member("DirectoryNotIgnored\\WildcardIgnoredDirectory\\"));
            Assert.That(ignoredPaths, Has.Member("DirectoryNotIgnored\\WildcardIgnoredDirectory\\FileIgnoredImplicitly.txt"));
            Assert.That(ignoredPaths, Has.Member(".IgnoredAsFile"));
        }
    }
}
