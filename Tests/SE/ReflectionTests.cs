using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Phoenix.WorkshopTool.Tests.SE
{
    [TestFixture]
    [RunInApplicationDomain]
    [Category("NoSteam")]
    public class ReflectionTests : Common.ReflectionTests
    {
        internal override string ParameterPrefix => "SE";

        [Test]
        public void ToServiceTest()
        {
            var method = WorkshopHelper.ReflectToService();
            Assert.IsNotNull(method);
        }

        [Test]
        public void ToSteamTest()
        {
            var method = WorkshopHelper.ReflectToSteam();
            Assert.IsNotNull(method);
        }

        [Test]
        public void ModIoCreateRequestTest()
        {
            var method = WorkshopHelper.ReflectCreateRequest();
            Assert.IsNotNull(method);
        }
    }
}
