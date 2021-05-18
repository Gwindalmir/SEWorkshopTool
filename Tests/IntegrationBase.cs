extern alias me;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MEWorkshopTool = me::Phoenix.MEWorkshopTool;

namespace Phoenix.WorkshopTool.Tests
{
    [TestFixture]
    [RequiresThread]
    [NonParallelizable]
    public abstract class IntegrationBase
    {
        #region Base Setup
        protected string[] _extraArguments = new string[0];

        public IntegrationBase()
        {
        }

        internal abstract string ParameterPrefix { get; }
        internal abstract string GameName { get; }

        protected int LaunchMain(string[] args)
        {
            if (ParameterPrefix == "SE")
            {
                return SEWorkshopTool.Program.Main(args);
            }
            else
            {
                // ME calls GetEntryAssembly, which is null when called from NUnit
                SetEntryAssembly(typeof(MEWorkshopTool.Program).Assembly);
                return MEWorkshopTool.Program.Main(args);
            }
        }

        /// <summary>
        /// Allows setting the Entry Assembly when needed. 
        /// Use AssemblyUtilities.SetEntryAssembly() as first line in XNA ad hoc tests
        /// </summary>
        /// <param name="assembly">Assembly to set as entry assembly</param>
        public static void SetEntryAssembly(Assembly assembly)
        {
            AppDomainManager manager = new AppDomainManager();
            FieldInfo entryAssemblyfield = manager.GetType().GetField("m_entryAssembly", BindingFlags.Instance | BindingFlags.NonPublic);
            entryAssemblyfield.SetValue(manager, assembly);

            AppDomain domain = AppDomain.CurrentDomain;
            FieldInfo domainManagerField = domain.GetType().GetField("_domainManager", BindingFlags.Instance | BindingFlags.NonPublic);
            domainManagerField.SetValue(domain, manager);
        }

        [OneTimeSetUp]
        public virtual void OneTimeSetup()
        {
            Environment.CurrentDirectory = TestContext.Parameters[$"{ParameterPrefix}.Install"];

            if (TestContext.Parameters.Exists($"{ParameterPrefix}.AppData"))
            {
                _extraArguments = new[] { "--appdata", TestContext.Parameters[$"{ParameterPrefix}.AppData"] };
            }

        }
        #endregion Base Setup

        #region Common Tests
        [Test]
        [Explicit]
        public void DownloadMod()
        {
            var args = new List<string>(new[] { "--download", "--mods", TestContext.Parameters[$"{ParameterPrefix}.ModIDToDownload"], "--extract" });
            args.AddRange(_extraArguments);

            var exitCode = LaunchMain(args.ToArray());
            Assert.That(exitCode, Is.EqualTo(0));
        }

        [Test]
        [Explicit]
        public void UpdateTags()
        {
            var args = new List<string>(new[] { "--update-only", "--mods", TestContext.Parameters[$"{ParameterPrefix}.ModNameToUpload"], "--tags", "Mod,Other" });
            args.AddRange(_extraArguments);

            var exitCode = LaunchMain(args.ToArray());
            Assert.That(exitCode, Is.EqualTo(0));
        }

        [Test]
        [Explicit]
        public void UploadMod()
        {
            var args = new List<string>(new[] { "--upload", "--mods", TestContext.Parameters[$"{ParameterPrefix}.ModNameToUpload"], "--tags", "Mod" });
            args.AddRange(_extraArguments);

            var exitCode = LaunchMain(args.ToArray());
            Assert.That(exitCode, Is.EqualTo(0));
        }

        [Test]
        [Explicit]
        public void CompileMod()
        {
            var args = new List<string>(new[] { "--upload", "--compile", "--mods", TestContext.Parameters[$"{ParameterPrefix}.ModNameToUpload"], "--dry-run" });
            args.AddRange(_extraArguments);

            var exitCode = LaunchMain(args.ToArray());
            Assert.That(exitCode, Is.EqualTo(0));
        }

        [Test]
        [Explicit]
        public void UploadModWithDescription()
        {
            var filename = Path.Combine(TestContext.CurrentContext.WorkDirectory, "..", "..", "..", "TestFiles", TestContext.Parameters[$"{ParameterPrefix}.ModDescriptionFile"]);
            var args = new List<string>(new[] { "--upload", "--mods", TestContext.Parameters[$"{ParameterPrefix}.ModNameToUpload"], "--tags", "Mod", "--description", filename });
            args.AddRange(_extraArguments);

            var exitCode = LaunchMain(args.ToArray());
            Assert.That(exitCode, Is.EqualTo(0));
        }

        // This requires an actual change to push, otherwise there's no changelog posted.
        [Test]
        [Explicit]
        public void UploadModWithChangelog()
        {
            var args = new List<string>(new[] { "--upload", "--mods", TestContext.Parameters[$"{ParameterPrefix}.ModNameToUpload"], "--tags", "Mod", "--message", $"SEWT Unit Test: {DateTime.Now.ToShortTimeString()}" });
            args.AddRange(_extraArguments);

            var exitCode = LaunchMain(args.ToArray());
            Assert.That(exitCode, Is.EqualTo(0));
        }

        // This requires an actual change to push, otherwise there's no changelog posted.
        [Test]
        [Explicit]
        public void UploadModWithChangelogFile ()
        {
            var filename = Path.Combine(TestContext.CurrentContext.WorkDirectory, "..", "..", "..", "TestFiles", TestContext.Parameters[$"{ParameterPrefix}.ModChangelogFile"]);
            var args = new List<string>(new[] { "--upload", "--mods", TestContext.Parameters[$"{ParameterPrefix}.ModNameToUpload"], "--tags", "Mod", "--message", filename });
            args.AddRange(_extraArguments);

            var exitCode = LaunchMain(args.ToArray());
            Assert.That(exitCode, Is.EqualTo(0));
        }

        [Test]
        [Explicit]
        public void UploadNewModDryRun()
        {
            var newModName = "NewMod";
            var appdata = TestContext.Parameters[$"{ParameterPrefix}.AppData"] ?? 
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), GameName);
            var moddir = Path.Combine(appdata, "Mods", newModName);

            Directory.CreateDirectory(moddir);
            try
            {
                var args = new List<string>(new[] { "--upload", "--mods", newModName, "--tags", "Mod", "--dry-run" });
                args.AddRange(_extraArguments);

                var exitCode = LaunchMain(args.ToArray());
                Assert.That(exitCode, Is.EqualTo(0));
            }
            finally
            {
                Directory.Delete(moddir);
            }
        }
        #endregion Common Tests
    }
}
