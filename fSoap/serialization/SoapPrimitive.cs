using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cn.noisyfox.fsoap.serialization
{

    /**
 * A class that is used to encapsulate primitive types (represented by a string
 * in XML serialization).
 *
 * Basically, the SoapPrimitive class encapsulates "unknown" primitive types
 * (similar to SoapObject encapsulating unknown complex types). For example, new
 * SoapPrimitive (classMap.xsd, "float", "12.3") allows you to send a float from
 * a MIDP device to a server although MIDP does not support floats. In the other
 * direction, kSOAP will deserialize any primitive type (=no subelements) that
 * are not recognized by the ClassMap to SoapPrimitive, preserving the
 * namespace, name and string value (this is how the stockquote example works).
 */
    public class SoapPrimitive : AttributeContainer
    {
        protected readonly String namespace_;
        protected readonly String name;
        protected readonly Object value;

        public static readonly Object NullSkip = new Object();
        public static readonly Object NullNilElement = new Object();

        public SoapPrimitive(String namespace_, String name, Object value)
        {
            this.namespace_ = namespace_;
            this.name = name;
            this.value = value;
        }

        public override bool Equals(object o)
        {
            if (!(o is SoapPrimitive))
            {
                return false;
            }
            SoapPrimitive p = (SoapPrimitive) o;
            bool varsEqual = name.Equals(p.name)
                             && (namespace_ == null ? p.namespace_ == null : namespace_.Equals(p.namespace_))
                             && (value == null ? (p.value == null) : value.Equals(p.value));
            return varsEqual && attributesAreEqual(p);
        }

        public override int GetHashCode()
        {
            return name.GetHashCode() ^ (namespace_ == null ? 0 : namespace_.GetHashCode());
        }

        public override string ToString()
        {
            return value != null ? value.ToString() : null;
        }

        public String getNamespace()
        {
            return namespace_;
        }

        public String getName()
        {
            return name;
        }

        public Object getValue()
        {
            return value;
        }
    }
}
