using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    internal class FixItCollector : IFixItCollector
    {
        public List<FixIt> FixIts { get; private set; }
        public bool HasFixIts => FixIts.Any();

        internal FixItCollector()
        {
            FixIts = new List<FixIt>();
        }

        public void Collect(FixIt fixit)
        {
            FixIts.Add(fixit);
        }
    }
}
