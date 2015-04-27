using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cn.noisyfox.fsoap
{
    public class HeaderProperty
    {
        private string key;
        private string value;

        public HeaderProperty(string key, string value)
        {
            this.key = key;
            this.value = value;
        }

        public string getKey()
        {
            return key;
        }
        public void setKey(string key)
        {
            this.key = key;
        }
        public string getValue()
        {
            return value;
        }
        public void setValue(string value)
        {
            this.value = value;
        }
    }
}
