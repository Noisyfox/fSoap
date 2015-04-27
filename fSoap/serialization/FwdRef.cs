using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cn.noisyfox.fsoap.serialization
{
    class FwdRef
    {
        internal FwdRef next;
        internal Object obj;
        internal int index;
    }
}
