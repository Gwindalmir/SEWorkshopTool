using NUnit.Framework;
using System.Collections.Generic;

namespace Phoenix.WorkshopTool.Tests.ME
{
    // Medieval Engineers MEWT Integration tests
    public class Integration : IntegrationBase
    {
        internal override string ParameterPrefix => "ME";

        internal override string GameName => "MedievalEngineers";
    }
}