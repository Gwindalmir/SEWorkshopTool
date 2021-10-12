extern alias me;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using M = me::Phoenix.WorkshopTool;

namespace Phoenix.WorkshopTool.Tests.Common
{
    [TestFixture]
    [RunInApplicationDomain]
    [Category("NoSteam")]
    public abstract class ReflectionTests : GameTestBase
    {
        [Test]
        public void PublishItemBlockingTest()
        {
            MethodInfo method;

            if(ParameterPrefix == "SE")
                method = typeof(WorkshopHelper).GetMethod("ReflectPublishItemBlocking", BindingFlags.Static | BindingFlags.NonPublic);
            else
                method = typeof(M.WorkshopHelper).GetMethod("ReflectPublishItemBlocking", BindingFlags.Static | BindingFlags.NonPublic);

            Assert.NotNull(method);

            var result = method.Invoke(null, new object[] { });
            Assert.NotNull(result);
        }

        [Test]
        public void LoadScriptsTest()
        {
            MethodInfo method;

            if (ParameterPrefix == "SE")
                method = typeof(WorkshopHelper).GetMethod("ReflectLoadScripts", BindingFlags.Static | BindingFlags.NonPublic);
            else
                method = typeof(M.WorkshopHelper).GetMethod("ReflectLoadScripts", BindingFlags.Static | BindingFlags.NonPublic);

            Assert.NotNull(method);

            var result = method.Invoke(null, new object[] { });
            Assert.NotNull(result);
        }

        [TestCase(new object[] { typeof(string), typeof(string) })]
        [TestCase(new object[] { typeof(string), typeof(string), typeof(Predicate<string>) })]
        public void CopyAllTest(object[] types)
        {
            MethodInfo method;

            if (ParameterPrefix == "SE")
                method = ReflectionHelper.ReflectFileCopy(types.Cast<Type>().ToArray());
            else
                method = M.ReflectionHelper.ReflectFileCopy(types.Cast<Type>().ToArray());

            Assert.IsNotNull(method);
        }

        [Test]
        public void UpdatePublishedItemTest()
        {
            MethodInfo method;

            if (ParameterPrefix == "SE")
                method = WorkshopHelper.ReflectSteamWorkshopItemPublisherMethod("UpdatePublishedItem", BindingFlags.Instance | BindingFlags.NonPublic);
            else
                method = M.WorkshopHelper.ReflectSteamWorkshopItemPublisherMethod("UpdatePublishedItem", BindingFlags.Instance | BindingFlags.Public);

            Assert.IsNotNull(method);
        }

        [TestCase("SubmitItemUpdateResult")]
        public void ItemPublisherMethodTest(string name)
        {
            MethodInfo method;

            if (ParameterPrefix == "SE")
                method = WorkshopHelper.ReflectSteamWorkshopItemPublisherMethod(name);
            else
                method = M.WorkshopHelper.ReflectSteamWorkshopItemPublisherMethod(name);

            Assert.IsNotNull(method);
        }

        [TestCase("m_steamService")]
        [TestCase("m_submitItemUpdateResult")]
        public void ItemPublisherFieldTest(string name)
        {
            FieldInfo method;

            if (ParameterPrefix == "SE")
                method = WorkshopHelper.ReflectSteamWorkshopItemPublisherField(name);
            else
                method = M.WorkshopHelper.ReflectSteamWorkshopItemPublisherField(name);

            Assert.IsNotNull(method);
        }

        [TestCase("SteamUGC")]
        public void ItemPublisherPropertyTest(string name)
        {
            PropertyInfo method;

            if (ParameterPrefix == "SE")
                method = WorkshopHelper.ReflectSteamWorkshopItemPublisherProperty(name);
            else
                method = M.WorkshopHelper.ReflectSteamWorkshopItemPublisherProperty(name);

            Assert.IsNotNull(method);
        }

        [Test]
        public void SteamAppRunningTest()
        {
            MethodInfo method;

            if (ParameterPrefix == "SE")
                method = ReflectionHelper.ReflectSteamRestartApp();
            else
                method = M.ReflectionHelper.ReflectSteamRestartApp();

            Assert.IsNotNull(method);
        }

        [Test]
        public void InitSteamWorkshopTest()
        {
            MethodInfo method;

            if (ParameterPrefix == "SE")
                method = WorkshopHelper.ReflectInitSteamWorkshop();
            else
                method = M.WorkshopHelper.ReflectInitSteamWorkshop();

            Assert.IsNotNull(method);
        }
    }
}
