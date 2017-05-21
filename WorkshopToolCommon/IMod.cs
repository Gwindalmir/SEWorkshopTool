using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phoenix.WorkshopTool
{
    interface IMod
    {
        string Title { get; }
        ulong ModId { get; }
        string ModPath { get; }
    }
}
