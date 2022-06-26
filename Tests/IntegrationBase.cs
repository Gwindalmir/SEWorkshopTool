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
    public abstract class IntegrationBase : GameTestBase
    {
        #region Base Setup
        protected TextWriter ConsoleOut = new StringWriter();
        protected TextWriter ConsoleError = new StringWriter();
        protected string[] _extraArguments = new string[0];

        public IntegrationBase()
        {
        }

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

        public override void OneTimeSetup()
        {
            base.OneTimeSetup();

            if (TestContext.Parameters.Exists($"{ParameterPrefix}.AppData"))
            {
                _extraArguments = new[] { "--appdata", TestContext.Parameters[$"{ParameterPrefix}.AppData"] };
            }

        }

        [SetUp]
        public void Setup()
        {
            // Setup console stream redirection, so we can check the output during the test
            Console.SetOut(ConsoleOut);
            Console.SetError(ConsoleError);
        }

        [TearDown]
        public void TearDown()
        {
            // Restore previous streams for the next test
            ConsoleOut.Close();
            ConsoleError.Close();

            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()));
            Console.SetError(new StreamWriter(Console.OpenStandardError()));

            // Write out the contents to the real stdout/stderr, so they are visible in the test explorer.
            TestContext.Out.Write(ConsoleOut.ToString());
            TestContext.Error.Write(ConsoleError.ToString());
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

            var output = ConsoleOut.ToString();
            Assert.That(output, Contains.Substring("Download success!"));
            Assert.That(output, Contains.Substring($"\\{TestContext.Parameters[$"{ParameterPrefix}.ModIDToDownload"]}"));
        }

        [Test]
        [Explicit]
        public void UpdateTags()
        {
            var args = new List<string>(new[] { "--update-only", "--mods", TestContext.Parameters[$"{ParameterPrefix}.ModNameToUpload"], "--tags", "Mod,Other" });
            args.AddRange(_extraArguments);

            var exitCode = LaunchMain(args.ToArray());
            Assert.That(exitCode, Is.EqualTo(0));

            var output = ConsoleOut.ToString();
            Assert.That(output, Contains.Substring("Tags: Mod, other"));
            Assert.That(output, Contains.Substring("Published file update successful"));
        }

        [Test]
        [Explicit]
        public void UploadMod()
        {
            var args = new List<string>(new[] { "--upload", "--mods", TestContext.Parameters[$"{ParameterPrefix}.ModNameToUpload"], "--tags", "Mod" });
            args.AddRange(_extraArguments);

            var exitCode = LaunchMain(args.ToArray());
            Assert.That(exitCode, Is.EqualTo(0));

            var output = ConsoleOut.ToString();
            Assert.That(output, Contains.Substring("Updating Mod: "));
            Assert.That(output, Contains.Substring("Upload/Publish success: "));
        }

        [Test]
        [Explicit]
        public void CompileMod()
        {
            var args = new List<string>(new[] { "--upload", "--compile", "--mods", TestContext.Parameters[$"{ParameterPrefix}.ModNameToUpload"], "--dry-run" });
            args.AddRange(_extraArguments);

            var exitCode = LaunchMain(args.ToArray());
            Assert.That(exitCode, Is.EqualTo(0));

            var output = ConsoleOut.ToString();
            Assert.That(output, Contains.Substring("Compilation successful!"));
            Assert.That(output, Contains.Substring("Publish skipped"));
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

            var desc = File.ReadAllText(filename);
            var output = ConsoleOut.ToString();
            Assert.That(output, Contains.Substring("Updating Mod: "));
            Assert.That(output, Contains.Substring($"Description: {desc.Substring(0, 20)}"));
            Assert.That(output, Contains.Substring("Upload/Publish success: "));
        }

        // This requires an actual change to push, otherwise there's no changelog posted.
        [Test]
        [Explicit]
        public void UploadModWithChangelog()
        {
            var changelog = $"SEWT Unit Test: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}";
            var args = new List<string>(new[] { "--upload", "--mods", TestContext.Parameters[$"{ParameterPrefix}.ModNameToUpload"], "--tags", "Mod", "--message", changelog });
            args.AddRange(_extraArguments);

            var exitCode = LaunchMain(args.ToArray());
            Assert.That(exitCode, Is.EqualTo(0));

            var output = ConsoleOut.ToString();
            Assert.That(output, Contains.Substring("Updating Mod: "));
            Assert.That(output, Contains.Substring($"Changelog: {changelog}"));
            Assert.That(output, Contains.Substring("Upload/Publish success: "));
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

            var log = File.ReadAllText(filename);
            var output = ConsoleOut.ToString();
            Assert.That(output, Contains.Substring("Updating Mod: "));
            Assert.That(output, Contains.Substring($"Changelog: {log.Substring(0, 20)}"));
            Assert.That(output, Contains.Substring("Upload/Publish success: "));
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

                // Since this is a new upload, this needs to be a throughout check
                var output = ConsoleOut.ToString();
                Assert.That(output, Contains.Substring($"Uploading new Mod: {newModName}"));
                Assert.That(output, Contains.Substring("Visibility: Private"));
                Assert.That(output, Contains.Substring("Tags: Mod"));
                Assert.That(output, Contains.Substring("Dependencies: None"));
                if(ParameterPrefix == "SE")
                    Assert.That(output, Contains.Substring("DLC requirements: None"));
                Assert.That(output, Contains.Substring("Thumbnail: No change"));
                Assert.That(output, Contains.Substring("DRY-RUN; Publish skipped"));
            }
            finally
            {
                Directory.Delete(moddir, true);
            }
        }

        [Test]
        [Explicit]
        public void UploadModNoData()
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

                // Since this is a new upload, this needs to be a throughout check
                var output = ConsoleOut.ToString();
                Assert.That(output, Contains.Substring("ERROR: Data folder doesn't exist, this is required for mods!"));
                Assert.That(output, Contains.Substring("ERROR: Use --force to create a placeholder file automatically."));
            }
            finally
            {
                Directory.Delete(moddir, true);
            }
        }

        [Test]
        [Explicit]
        public void UploadModEmptyData()
        {
            var newModName = "NewMod";
            var appdata = TestContext.Parameters[$"{ParameterPrefix}.AppData"] ??
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), GameName);
            var moddir = Path.Combine(appdata, "Mods", newModName);

            Directory.CreateDirectory(moddir);
            Directory.CreateDirectory(Path.Combine(moddir, "Data"));

            try
            {
                var args = new List<string>(new[] { "--upload", "--mods", newModName, "--tags", "Mod", "--dry-run" });
                args.AddRange(_extraArguments);

                var exitCode = LaunchMain(args.ToArray());
                Assert.That(exitCode, Is.EqualTo(0));

                // Since this is a new upload, this needs to be a throughout check
                var output = ConsoleOut.ToString();
                Assert.That(output, Contains.Substring("ERROR: Data folder exists, but is empty, this will be removed on publish!"));
                Assert.That(output, Contains.Substring("ERROR: Place an empty file in that folder to ensure it will be uploaded."));
                Assert.That(output, Contains.Substring("ERROR: Use --force to create a placeholder file automatically."));
            }
            finally
            {
                Directory.Delete(moddir, true);
            }
        }

        [Test]
        [Explicit]
        public void UploadModNoDataForce()
        {
            var newModName = "NewMod";
            var appdata = TestContext.Parameters[$"{ParameterPrefix}.AppData"] ??
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), GameName);
            var moddir = Path.Combine(appdata, "Mods", newModName);

            Directory.CreateDirectory(moddir);
            try
            {
                var args = new List<string>(new[] { "--upload", "--mods", newModName, "--tags", "Mod", "--dry-run", "--force" });
                args.AddRange(_extraArguments);

                var exitCode = LaunchMain(args.ToArray());
                Assert.That(exitCode, Is.EqualTo(0));

                // Since this is a new upload, this needs to be a throughout check
                var output = ConsoleOut.ToString();
                Assert.That(output, Contains.Substring("WARNING: Data folder doesn't exist, this is required for mods!"));
                Assert.That(output, Contains.Substring("WARNING: Creating folder and temporary file to ensure upload."));

                var path = Path.Combine(moddir, "Data", ".sewt-preserved");
                Assert.That(path, Does.Exist);
                Assert.That(File.GetAttributes(path).HasFlag(FileAttributes.Hidden));
            }
            finally
            {
                Directory.Delete(moddir, true);
            }
        }

        [Test]
        [Explicit]
        public void UploadModEmptyDataForce()
        {
            var newModName = "NewMod";
            var appdata = TestContext.Parameters[$"{ParameterPrefix}.AppData"] ??
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), GameName);
            var moddir = Path.Combine(appdata, "Mods", newModName);

            Directory.CreateDirectory(moddir);
            Directory.CreateDirectory(Path.Combine(moddir, "Data"));

            try
            {
                var args = new List<string>(new[] { "--upload", "--mods", newModName, "--tags", "Mod", "--dry-run", "--force" });
                args.AddRange(_extraArguments);

                var exitCode = LaunchMain(args.ToArray());
                Assert.That(exitCode, Is.EqualTo(0));

                // Since this is a new upload, this needs to be a thorough check
                var output = ConsoleOut.ToString();
                Assert.That(output, Contains.Substring("WARNING: Data folder exists, but is empty, this will be removed on publish!"));
                Assert.That(output, Contains.Substring("WARNING: Creating temporary file to ensure upload."));

                var path = Path.Combine(moddir, "Data", ".sewt-preserved");
                Assert.That(path, Does.Exist);
                Assert.That(File.GetAttributes(path).HasFlag(FileAttributes.Hidden));
            }
            finally
            {
                Directory.Delete(moddir, true);
            }
        }
        #endregion Common Tests
    }
}
