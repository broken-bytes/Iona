using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public static class WarningCollectorFactory
    {
        public static IWarningCollector Create()
        {
            return new WarningCollector();
        }
    }
}
