extern alias me;

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using M = me::Phoenix.WorkshopTool;


namespace Phoenix.WorkshopTool.Tests.ME
{
    [TestFixture]
    [RunInApplicationDomain]
    [Category("NoSteam")]
    public class ReflectionTests : Common.ReflectionTests
    {
        internal override string ParameterPrefix => "ME";

        [Test]
        public void PublishSuccessTest()
        {
            dynamic method = typeof(M.WorkshopHelper).GetMethod("ReflectPublishSuccess", BindingFlags.Static | BindingFlags.NonPublic);

            Assert.NotNull(method);

            var result = method.Invoke(null, new object[] { });
            Assert.NotNull(result);
        }

        [Test]
        public void CompileMessagesTest()
        {
            var method = typeof(M.WorkshopHelper).GetMethod("ReflectCompileMessages", BindingFlags.Static | BindingFlags.NonPublic);

            Assert.NotNull(method);

            var result = method.Invoke(null, new object[] { });
            Assert.NotNull(result);
        }

        [TestCase("LoadSystems")]
        [TestCase("InitSystems")]
        [TestCase("LoadMetadata")]
        public void VRageCoreMethodTest(string name)
        {
            var method = M.ReflectionHelper.ReflectVRageCoreMethod(name);

            Assert.NotNull(method);
        }

        [TestCase("m_state")]
        public void VRageCoreFieldTest(string name)
        {
            var field = M.ReflectionHelper.ReflectVRageCoreField(name);

            Assert.NotNull(field);
        }

        [Test]
        public void MySteamUgcInstanceTest()
        {
            var method = M.WorkshopHelper.ReflectMySteamUgcInstance();
            Assert.NotNull(method);
        }
    }
}
