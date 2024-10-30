using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public static class CompilerWarningFactory
    {
        public static CompilerWarning ShadowedSymbol(string kind, string symbol, Metadata meta)
        {
            return new CompilerWarning(
                CompilerWarningCode.SymbolShadowing,
                $"{kind} {symbol} shadows another symbol with the same name",
                meta
            );
        }
    }
}
