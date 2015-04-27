using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cn.noisyfox.fsoap.serialization
{
    public interface HasAttributes
    {
        int getAttributeCount();

        void getAttributeInfo(int index, AttributeInfo info);

        void getAttribute(int index, AttributeInfo info);

        void setAttribute(AttributeInfo info);
    }
}
