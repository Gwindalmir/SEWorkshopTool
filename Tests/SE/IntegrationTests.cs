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
        }

        [Test]
        [Explicit]
        public void CompileScript()
        {
            var args = new List<string>(new[] { "--upload", "--compile", "--scripts", TestContext.Parameters[$"{ParameterPrefix}.ScriptNameToUpload"], "--dry-run" });
            args.AddRange(_extraArguments);

            var exitCode = LaunchMain(args.ToArray());
            Assert.That(exitCode, Is.EqualTo(0));
        }
    }
}