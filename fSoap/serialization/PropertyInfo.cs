using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace cn.noisyfox.fsoap.serialization
{
    /**
     * This class is used to store information about each property an implementation of KvmSerializable exposes.
     */

    public class PropertyInfo
    {
        public static readonly Type OBJECT_CLASS = new object().GetType();
        public static readonly Type STRING_CLASS = "".GetType();
        public static readonly Type INTEGER_CLASS = new int().GetType();
        public static readonly Type LONG_CLASS = new long().GetType();
        public static readonly Type BOOLEAN_CLASS = new bool().GetType();
        public static readonly Type VECTOR_CLASS = new List<object>().GetType();
        public static readonly PropertyInfo OBJECT_TYPE = new PropertyInfo();
        public static readonly int TRANSIENT = 1;
        public static readonly int MULTI_REF = 2;
        public static readonly int REF_ONLY = 4;


        /**
     * Name of the property
     */
        public String name;

        /**
     * Namespace of this property
     */
        public String namespace_;

        /**
     * Type of property, Transient, multi_ref, Ref_only *JHS* Note, not really used that effectively
     */
        public int flags;

        /**
     * The current value of this property.
     */
        protected internal Object value;

        /**
     * Type of the property/elements. Should usually be an instance of Class.
     */
        public Object type = OBJECT_CLASS;

        /**
     * if a property is multi-referenced, set this flag to true.
     */
        public bool multiRef;

        /**
     * Element type for array properties, null if not array prop.
     */
        public PropertyInfo elementType;

        public void clear()
        {
            type = OBJECT_CLASS;
            flags = 0;
            name = null;
            namespace_ = null;
        }

        /**
     * @return Returns the elementType.
     */

        public PropertyInfo getElementType()
        {
            return elementType;
        }

        /**
     * @param elementType
     *            The elementType to set.
     */

        public void setElementType(PropertyInfo elementType)
        {
            this.elementType = elementType;
        }

        /**
     * @return Returns the flags.
     */

        public int getFlags()
        {
            return flags;
        }

        /**
     * @param flags
     *            The flags to set.
     */

        public void setFlags(int flags)
        {
            this.flags = flags;
        }

        /**
     * @return Returns the multiRef.
     */

        public bool isMultiRef()
        {
            return multiRef;
        }

        /**
     * @param multiRef
     *            The multiRef to set.
     */

        public void setMultiRef(bool multiRef)
        {
            this.multiRef = multiRef;
        }

        /**
     * @return Returns the name.
     */

        public String getName()
        {
            return name;
        }

        /**
     * @param name
     *            The name to set.
     */

        public void setName(String name)
        {
            this.name = name;
        }

        /**
     * @return Returns the namespace.
     */

        public String getNamespace()
        {
            return namespace_;
        }

        /**
     * @param namespace
     *            The namespace to set.
     */

        public void setNamespace(String namespace_)
        {
            this.namespace_ = namespace_;
        }

        /**
     * @return Returns the type.
     */

        public Object getType()
        {
            return type;
        }

        /**
     * @param type
     *            The type to set.
     */

        public void setType(Object type)
        {
            this.type = type;
        }

        /**
     * @return Returns the value.
     */

        public Object getValue()
        {
            return value;
        }

        /**
     * @param value
     *            The value to set.
     */

        public void setValue(Object value)
        {
            this.value = value;
        }

        /**
 * Show the name and value.
 *
 * @see java.lang.Object#toString()
 */

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(name);
            sb.Append(" : ");
            if (value != null)
            {
                sb.Append(value);
            }
            else
            {
                sb.Append("(not set)");
            }
            return sb.ToString();
        }

        /**
 * Make a deep clone of the properties through Object serialization
 *
 * @see java.lang.Object#clone()
 */

        public Object clone()
        {
            DataContractSerializer Serializer = new DataContractSerializer(typeof (PropertyInfo));
            StringBuilder SBuilder = new StringBuilder();
            XmlWriter Writer = XmlWriter.Create(SBuilder);
            Serializer.WriteObject(Writer, this);
            Writer.Flush();
            Writer.Dispose();
            string Xml = SBuilder.ToString();
            Object obj = Serializer.ReadObject(XmlReader.Create(new StringReader(Xml)));

            return obj;
        }
    }
}
