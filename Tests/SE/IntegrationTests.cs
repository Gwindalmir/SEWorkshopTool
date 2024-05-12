using NUnit.Framework;
using System.Collections.Generic;

namespace Phoenix.WorkshopTool.Tests.SE
{
    // Space Engineers SEWT Integration tests
    public class Integration : IntegrationBase
    {
        internal override string ParameterPrefix => "SE";

        internal override string GameName => "SpaceEngineers";

        [Test]
        [Explicit]
        public void UploadScript()
        {
            var args = new List<string>(new[] { "--upload", "--compile", "--scripts", TestContext.Parameters[$"{ParameterPrefix}.ScriptNameToUpload"], "--tags", "IngameScript" });
            args.AddRange(_extraArguments);

            var exitCode = LaunchMain(args.ToArray());
            Assert.That(exitCode, Is.EqualTo(0));

            var output = ConsoleOut.ToString();
            Assert.That(output, Contains.Substring("Compilation successful!"));
            Assert.That(output, Contains.Substring("Updating IngameScript: "));
            Assert.That(output, Contains.Substring("Upload/Publish success: "));
        }

        [Test]
        [Explicit]
        public void CompileScript()
        {
            var args = new List<string>(new[] { "--upload", "--compile", "--scripts", TestContext.Parameters[$"{ParameterPrefix}.ScriptNameToUpload"], "--dry-run" });
            args.AddRange(_extraArguments);

            var exitCode = LaunchMain(args.ToArray());
            Assert.That(exitCode, Is.EqualTo(0));

            var output = ConsoleOut.ToString();
            Assert.That(output, Contains.Substring("Compilation successful!"));
            Assert.That(output, Contains.Substring("DRY-RUN; Publish skipped"));
        }

        [Test]
        [Explicit]
        [TestCase(1676100U)]
        public void UploadModWithDLC(uint appid)
        {
            var args = new List<string>(new[] { "push", "--mods", TestContext.Parameters[$"{ParameterPrefix}.ModNameToUpload"], "--tags", "Mod", "--dlc", appid.ToString() });
            args.AddRange(_extraArguments);

            var exitCode = LaunchMain(args.ToArray());
            Assert.That(exitCode, Is.EqualTo(0));

            var output = ConsoleOut.ToString();
            Assert.That(output, Contains.Substring("Updating Mod: "));
            Assert.That(output, Contains.Substring($"DLC requirements: Unknown({appid.ToString()})"));
            Assert.That(output, Contains.Substring("Upload/Publish success: "));
        }
    }
}