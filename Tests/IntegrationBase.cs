using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Phoenix.WorkshopTool.Tests
{
    [TestFixture]
    [RequiresThread]
    [NonParallelizable]
    public abstract class IntegrationBase
    {
        #region Base Setup
        protected readonly string _parameterPrefix = "SE";
        protected string[] _extraArguments = new string[0];

        public IntegrationBase()
        {
            if (this.GetType().Namespace.EndsWith("ME"))
                _parameterPrefix = "ME";
        }

        protected int LaunchMain(string[] args)
        {
            if (_parameterPrefix == "SE")
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
            Environment.CurrentDirectory = TestContext.Parameters[$"{_parameterPrefix}.Install"];

            if (TestContext.Parameters.Exists($"{_parameterPrefix}.AppData"))
            {
                _extraArguments = new[] { "--appdata", TestContext.Parameters[$"{_parameterPrefix}.AppData"] };
            }

        }
        #endregion Base Setup

        #region Common Tests
        [Test]
        [Explicit]
        public void DownloadMod()
        {
            var args = new List<string>(new[] { "--download", "--mods", TestContext.Parameters[$"{_parameterPrefix}.ModIDToDownload"], "--extract" });
            args.AddRange(_extraArguments);

            var exitCode = LaunchMain(args.ToArray());
            Assert.That(exitCode, Is.EqualTo(0));
        }

        [Test]
        [Explicit]
        public void UploadMod()
        {
            var args = new List<string>(new[] { "--upload", "--mods", TestContext.Parameters[$"{_parameterPrefix}.ModNameToUpload"], "--tags", "Mod" });
            args.AddRange(_extraArguments);

            var exitCode = LaunchMain(args.ToArray());
            Assert.That(exitCode, Is.EqualTo(0));
        }

        [Test]
        [Explicit]
        public void UpdateTags()
        {
            var args = new List<string>(new[] { "--update-only", "--mods", TestContext.Parameters[$"{_parameterPrefix}.ModNameToUpload"], "--tags", "Mod,Other" });
            args.AddRange(_extraArguments);

            var exitCode = LaunchMain(args.ToArray());
            Assert.That(exitCode, Is.EqualTo(0));
        }
        #endregion Common Tests
    }
}
