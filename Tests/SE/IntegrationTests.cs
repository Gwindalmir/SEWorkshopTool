using NUnit.Framework;
using System.Collections.Generic;

namespace Phoenix.WorkshopTool.Tests.SE
{
    // Space Engineers SEWT Integration tests
    public class Integration : IntegrationBase
    {
        [Test]
        [Explicit]
        public void UploadScript()
        {
            var args = new List<string>(new[] { "--upload", "--compile", "--scripts", TestContext.Parameters[$"{_parameterPrefix}.ScriptNameToUpload"], "--tags", "IngameScript" });
            args.AddRange(_extraArguments);

            var exitCode = LaunchMain(args.ToArray());
            Assert.That(exitCode, Is.EqualTo(0));
        }

        [Test]
        [Explicit]
        public void CompileScript()
        {
            var args = new List<string>(new[] { "--upload", "--compile", "--scripts", TestContext.Parameters[$"{_parameterPrefix}.ScriptNameToUpload"], "--dry-run" });
            args.AddRange(_extraArguments);

            var exitCode = LaunchMain(args.ToArray());
            Assert.That(exitCode, Is.EqualTo(0));
        }
    }
}