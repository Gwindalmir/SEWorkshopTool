extern alias me;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Phoenix.WorkshopTool.Tests
{
    /// <summary>
    /// Represents a test fixture that requires the game assemblies.
    /// </summary>
    [TestFixture]
    public abstract class GameTestBase
    {
        #region Base Setup
        protected string _previousDirectory;

        public GameTestBase()
        {
        }

        internal abstract string ParameterPrefix { get; }

        [OneTimeSetUp]
        public virtual void OneTimeSetup()
        {
            if (AppDomainRunner.IsNotInTestAppDomain)
            {
                _previousDirectory = Environment.CurrentDirectory;
                Environment.CurrentDirectory = TestContext.Parameters[$"{ParameterPrefix}.Install"];
            }
            else
            {
                if (ParameterPrefix == "ME")
                    AppDomain.CurrentDomain.AssemblyResolve += me::Phoenix.WorkshopTool.GameBase.CurrentDomain_AssemblyResolve;
                else
                    AppDomain.CurrentDomain.AssemblyResolve += GameBase.CurrentDomain_AssemblyResolve;
            }
        }

        [OneTimeTearDown]
        public virtual void OneTimeTearDown()
        {
            if (AppDomainRunner.IsNotInTestAppDomain)
            {
                Environment.CurrentDirectory = _previousDirectory;
            }
            else
            {
                if (ParameterPrefix == "ME")
                    AppDomain.CurrentDomain.AssemblyResolve -= me::Phoenix.WorkshopTool.GameBase.CurrentDomain_AssemblyResolve;
                else
                    AppDomain.CurrentDomain.AssemblyResolve -= GameBase.CurrentDomain_AssemblyResolve;
            }
        }
        #endregion Base Setup
    }
}
