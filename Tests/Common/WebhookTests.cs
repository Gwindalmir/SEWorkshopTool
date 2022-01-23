using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Phoenix.WorkshopTool.Tests.Common
{
    [TestFixture]
    [Category("NoSteam")]
    public class WebhookTests
    {
        [Test]
        public void TestInvalidLink()
        {
            DiscordWebhook webhook = new DiscordWebhook(WorkshopType.Blueprint, "Test01", "ChangeLog");
            var response = webhook.Call("https://discord.com/api/webhooks/NotARealLink");
            Assert.That(response == WebhookFailCause.InvalidURL);
        }

        [Test]
        public void TestRottedLink()
        {
            DiscordWebhook webhook = new DiscordWebhook(WorkshopType.Blueprint, "Test01", "ChangeLog");
            var response = webhook.Call("https://discord.com/api/webhooks/1234567890/abcdABBCD0123456789-");
            Assert.That(response == WebhookFailCause.NoResponse);
        }

    }
}
