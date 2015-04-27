using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using cn.noisyfox.fxml.xmlpull;

namespace cn.noisyfox.fsoap.serialization
{
    public class SoapSerializationEnvelope : SoapEnvelope
    {
        protected static readonly int QNAME_TYPE = 1;
        protected static readonly int QNAME_NAMESPACE = 0;
        protected static readonly int QNAME_MARSHAL = 3;
        protected static readonly String NULL_LABEL = "null";
        protected static readonly String NIL_LABEL = "nil";
        private static readonly Marshal DEFAULT_MARSHAL = new DM();
        private static readonly String ANY_TYPE_LABEL = "anyType";
        private static readonly String ARRAY_MAPPING_NAME = "Array";
        private static readonly String HREF_LABEL = "href";
        private static readonly String ID_LABEL = "id";
        private static readonly String ROOT_LABEL = "root";
        private static readonly String TYPE_LABEL = "type";
        private static readonly String ITEM_LABEL = "item";
        private static readonly String ARRAY_TYPE_LABEL = "arrayType";
        public Dictionary<object, object> properties = new Dictionary<object, object>();
        /**
     * Set this variable to true if you don't want that type definitions for complex types/objects
     * are automatically generated (with type "anyType") in the XML-Request, if you don't call the
     * Method addMapping. This is needed by some Servers which have problems with these type-definitions.
     */
        public bool implicitTypes;
        /**
     * If set to true then all properties with null value will be skipped from the soap message.
     * If false then null properties will be sent as <element nil="true" />
     */
        public bool skipNullProperties;
        /**
     * Set this variable to true for compatibility with what seems to be the default encoding for
     * .Net-Services. This feature is an extremely ugly hack. A much better option is to change the
     * configuration of the .Net-Server to standard Soap Serialization!
     */

        public bool dotNet;
        /**
     * Set this variable to true if you prefer to silently skip unknown properties.
     * {@link Exception} will be thrown otherwise.
     */
        public bool avoidExceptionForUnknownProperty;
        /**
     * Map from XML qualified names to Java classes
     */

        protected Dictionary<SoapPrimitive, object> qNameToClass = new Dictionary<SoapPrimitive, object>();
        /**
     * Map from Java class names to XML name and namespace pairs
     */

        protected Dictionary<string, object> classToQName = new Dictionary<string, object>();
        /**
     * Set to true to add and ID and ROOT label to the envelope. Change to false for compatibility with WSDL.
     */
        protected bool addAdornments = true;
        private Dictionary<object, object> idMap = new Dictionary<object, object>();
        private List<object> multiRef; // = new List<object>();

        public SoapSerializationEnvelope(int version) : base(version)
        {
            addMapping(enc, ARRAY_MAPPING_NAME, PropertyInfo.VECTOR_CLASS);
            DEFAULT_MARSHAL.register(this);
        }


        /**
     * @return the addAdornments
     */

        public bool isAddAdornments()
        {
            return addAdornments;
        }

        /**
     * @param addAdornments the addAdornments to set
     */

        public void setAddAdornments(bool addAdornments)
        {
            this.addAdornments = addAdornments;
        }

        /**
     * Set the bodyOut to be empty so that no un-needed xml is create. The null value for bodyOut will
     * cause #writeBody to skip writing anything redundant.
     *
     * @param emptyBody
     * @see "http://code.google.com/p/ksoap2-android/issues/detail?id=77"
     */

        public void setBodyOutEmpty(bool emptyBody)
        {
            if (emptyBody)
            {
                bodyOut = null;
            }
        }

        public override void parseBody(XmlPullParser parser)
        {
            bodyIn = null;
            parser.nextTag();
            if (parser.getEventType() == XmlPullParser.START_TAG && parser.getNamespace().Equals(env)
                && parser.getName().Equals("Fault"))
            {
                SoapFault fault;
                if (this.version < SoapEnvelope.VER12)
                {
                    fault = new SoapFault(this.version);
                }
                else
                {
                    fault = new SoapFault12(this.version);
                }
                fault.parse(parser);
                bodyIn = fault;
            }
            else
            {
                while (parser.getEventType() == XmlPullParser.START_TAG)
                {
                    String rootAttr = parser.getAttributeValue(enc, ROOT_LABEL);

                    Object o = read(parser, null, -1, parser.getNamespace(), parser.getName(),
                        PropertyInfo.OBJECT_TYPE);
                    if ("1".Equals(rootAttr) || bodyIn == null)
                    {
                        bodyIn = o;
                    }
                    parser.nextTag();
                }
            }
        }


        /**
     * Read a SoapObject. This extracts any attributes and then reads the object as a FvmSerializable.
     */

        protected void readSerializable(XmlPullParser parser, SoapObject obj)
        {
            for (int counter = 0; counter < parser.getAttributeCount(); counter++)
            {
                String attributeName = parser.getAttributeName(counter);
                String value = parser.getAttributeValue(counter);
                ((SoapObject) obj).addAttribute(attributeName, value);
            }
            readSerializable(parser, (FvmSerializable) obj);
        }

        /**
     * Read a FvmSerializable.
     */

        protected void readSerializable(XmlPullParser parser, FvmSerializable obj)
        {
            int tag = 0;
            try
            {
                tag = parser.nextTag();
            }
            catch (XmlPullParserException e)
            {
                if (obj is HasInnerText)
                {
                    ((HasInnerText) obj).setInnerText((parser.getText() != null) ? parser.getText() : "");
                }
                tag = parser.nextTag();
            }
            while (tag != XmlPullParser.END_TAG)
            {
                String name = parser.getName();
                if (!implicitTypes || !(obj is SoapObject))
                {
                    PropertyInfo info = new PropertyInfo();
                    int propertyCount = obj.getPropertyCount();
                    bool propertyFound = false;

                    for (int i = 0; i < propertyCount && !propertyFound; i++)
                    {
                        info.clear();
                        obj.getPropertyInfo(i, properties, info);

                        if ((name.Equals(info.name) && info.namespace_ == null) ||
                            (name.Equals(info.name) && parser.getNamespace().Equals(info.namespace_)))
                        {
                            propertyFound = true;
                            obj.setProperty(i, read(parser, obj, i, null, null, info));
                        }
                    }

                    if (!propertyFound)
                    {
                        if (avoidExceptionForUnknownProperty)
                        {
                            // Dummy loop to read until corresponding END tag
                            while (parser.next() != XmlPullParser.END_TAG || !name.Equals(parser.getName()))
                            {
                            }
                            ;
                        }
                        else
                        {
                            throw new Exception("Unknown Property: " + name);
                        }
                    }
                    else
                    {
                        if (obj is HasAttributes)
                        {
                            HasAttributes soapObject = (HasAttributes) obj;
                            int cnt = parser.getAttributeCount();
                            for (int counter = 0; counter < cnt; counter++)
                            {
                                AttributeInfo attributeInfo = new AttributeInfo();
                                attributeInfo.setName(parser.getAttributeName(counter));
                                attributeInfo.setValue(parser.getAttributeValue(counter));
                                attributeInfo.setNamespace(parser.getAttributeNamespace(counter));
                                attributeInfo.setType(parser.getAttributeType(counter));
                                soapObject.setAttribute(attributeInfo);

                            }
                        }
                    }
                }
                else
                {
                    // I can only make this work for SoapObjects - hence the check above
                    // I don't understand namespaces well enough to know whether it is correct in the next line...
                    ((SoapObject) obj).addProperty(parser.getName(), read(parser, obj, obj.getPropertyCount(),
                        ((SoapObject) obj).getNamespace(), name, PropertyInfo.OBJECT_TYPE));
                }
                try
                {
                    tag = parser.nextTag();
                }
                catch (XmlPullParserException e)
                {
                    if (obj is HasInnerText)
                    {
                        ((HasInnerText) obj).setInnerText((parser.getText() != null) ? parser.getText() : "");
                    }
                    tag = parser.nextTag();
                }

            }
            parser.require(XmlPullParser.END_TAG, null, null);
        }

        /**
     * If the type of the object cannot be determined, and thus no Marshal class can handle the object, this
     * method is called. It will build either a SoapPrimitive or a SoapObject
     *
     * @param parser
     * @param typeNamespace
     * @param typeName
     * @return unknownObject wrapped as a SoapPrimitive or SoapObject
     * @throws IOException
     * @throws XmlPullParserException
     */

        protected Object readUnknown(XmlPullParser parser, String typeNamespace, String typeName)
        {
            String name = parser.getName();
            String namespace_ = parser.getNamespace();

            // cache the attribute info list from the current element before we move on
            List<object> attributeInfoVector = new List<object>();
            for (int attributeCount = 0; attributeCount < parser.getAttributeCount(); attributeCount++)
            {
                AttributeInfo attributeInfo = new AttributeInfo();
                attributeInfo.setName(parser.getAttributeName(attributeCount));
                attributeInfo.setValue(parser.getAttributeValue(attributeCount));
                attributeInfo.setNamespace(parser.getAttributeNamespace(attributeCount));
                attributeInfo.setType(parser.getAttributeType(attributeCount));
                attributeInfoVector.Add(attributeInfo);
            }

            parser.next(); // move to text, inner start tag or end tag
            Object result = null;
            String text = null;
            if (parser.getEventType() == XmlPullParser.TEXT)
            {
                text = parser.getText();
                SoapPrimitive sp = new SoapPrimitive(typeNamespace, typeName, text);
                result = sp;
                // apply all the cached attribute info list before we add the property and descend further for parsing
                for (int i = 0; i < attributeInfoVector.Count; i++)
                {
                    sp.addAttribute((AttributeInfo) attributeInfoVector[i]);
                }
                parser.next();
            }
            else if (parser.getEventType() == XmlPullParser.END_TAG)
            {
                SoapObject so = new SoapObject(typeNamespace, typeName);
                // apply all the cached attribute info list before we add the property and descend further for parsing
                for (int i = 0; i < attributeInfoVector.Count; i++)
                {
                    so.addAttribute((AttributeInfo) attributeInfoVector[i]);
                }
                result = so;
            }

            if (parser.getEventType() == XmlPullParser.START_TAG)
            {
                if (text != null && text.Trim().Length != 0)
                {
                    throw new Exception("Malformed input: Mixed content");
                }
                SoapObject so = new SoapObject(typeNamespace, typeName);
                // apply all the cached attribute info list before we add the property and descend further for parsing
                for (int i = 0; i < attributeInfoVector.Count; i++)
                {
                    so.addAttribute((AttributeInfo) attributeInfoVector[i]);
                }

                while (parser.getEventType() != XmlPullParser.END_TAG)
                {
                    so.addProperty(parser.getNamespace(), parser.getName(), read(parser, so, so.getPropertyCount(),
                        null, null, PropertyInfo.OBJECT_TYPE));
                    parser.nextTag();
                }
                result = so;
            }
            parser.require(XmlPullParser.END_TAG, namespace_, name);
            return result;
        }

        private int getIndex(String value, int start, int dflt)
        {
            if (value == null)
            {
                return dflt;
            }
            try
            {
                return value.Length - start < 3
                    ? dflt
                    : int.Parse(value.Substring(start + 1,
                        value.Length - 1 - start));
            }
            catch (Exception ex)
            {
                return dflt;
            }
        }

        private static void setSize(List<object> v, int newSize)
        {
            int dSize = v.Count - newSize;
            if (dSize > 0)
            {
                for (; dSize > 0; dSize--)
                {
                    v.Add(null);
                }
            }
            else if (dSize < 0)
            {
                v.RemoveRange(newSize, -dSize);
            }
        }

        protected void readVector(XmlPullParser parser, List<object> v, PropertyInfo elementType)
        {
            String namespace_ = null;
            String name = null;
            int size = v.Count;
            bool dynamic = true;
            String type = parser.getAttributeValue(enc, ARRAY_TYPE_LABEL);
            if (type != null)
            {
                int cut0 = type.IndexOf(':');
                int cut1 = type.IndexOf("[", cut0);
                name = type.Substring(cut0 + 1, cut1 - cut0 - 1);
                String prefix = cut0 == -1 ? "" : type.Substring(0, cut0);
                namespace_ = parser.getNamespace(prefix);
                size = getIndex(type, cut1, -1);
                if (size != -1)
                {
                    setSize(v, size);
                    dynamic = false;
                }
            }
            if (elementType == null)
            {
                elementType = PropertyInfo.OBJECT_TYPE;
            }
            parser.nextTag();
            int position = getIndex(parser.getAttributeValue(enc, "offset"), 0, 0);
            while (parser.getEventType() != XmlPullParser.END_TAG)
            {
                // handle position
                position = getIndex(parser.getAttributeValue(enc, "position"), 0, position);
                if (dynamic && position >= size)
                {
                    size = position + 1;
                    setSize(v, size);
                }
                // implicit handling of position exceeding specified size
                v[position] = read(parser, v, position, namespace_, name, elementType);
                position++;
                parser.nextTag();
            }
            parser.require(XmlPullParser.END_TAG, null, null);
        }

        /**
     * This method returns id from the href attribute value.
     * By default we assume that href value looks like this: #id so we basically have to remove the first character.
     * But in theory there could be a different value format, like cid:value, etc...
     */

        protected String getIdFromHref(String hrefValue)
        {
            return hrefValue.Substring(1);
        }

        /**
     * Builds an object from the XML stream. This method is public for usage in conjuction with Marshal
     * subclasses. Precondition: On the start tag of the object or property, so href can be read.
     */

        public Object read(XmlPullParser parser, Object owner, int index, String namespace_, String name,
            PropertyInfo expected)
        {
            String elementName = parser.getName();
            String href = parser.getAttributeValue(null, HREF_LABEL);
            Object obj;
            if (href != null)
            {
                if (owner == null)
                {
                    throw new Exception("href at root level?!?");
                }
                href = getIdFromHref(href);
                obj = idMap[href];
                if (obj == null || obj is FwdRef)
                {
                    FwdRef f = new FwdRef();
                    f.next = (FwdRef) obj;
                    f.obj = owner;
                    f.index = index;
                    idMap[href] = f;
                    obj = null;
                }
                parser.nextTag(); // start tag
                parser.require(XmlPullParser.END_TAG, null, elementName);
            }
            else
            {
                String nullAttr = parser.getAttributeValue(xsi, NIL_LABEL);
                String id = parser.getAttributeValue(null, ID_LABEL);
                if (nullAttr == null)
                {
                    nullAttr = parser.getAttributeValue(xsi, NULL_LABEL);
                }
                if (nullAttr != null && SoapEnvelope.stringToBoolean(nullAttr))
                {
                    obj = null;
                    parser.nextTag();
                    parser.require(XmlPullParser.END_TAG, null, elementName);
                }
                else
                {
                    String type = parser.getAttributeValue(xsi, TYPE_LABEL);
                    if (type != null)
                    {
                        int cut = type.IndexOf(':');
                        name = type.Substring(cut + 1);
                        String prefix = cut == -1 ? "" : type.Substring(0, cut);
                        namespace_ = parser.getNamespace(prefix);
                    }
                    else if (name == null && namespace_ == null)
                    {
                        if (parser.getAttributeValue(enc, ARRAY_TYPE_LABEL) != null)
                        {
                            namespace_ = enc;
                            name = ARRAY_MAPPING_NAME;
                        }
                        else
                        {
                            Object[] names = getInfo(expected.type, null);
                            namespace_ = (String) names[0];
                            name = (String) names[1];
                        }
                    }
                    // be sure to set this flag if we don't know the types.
                    if (type == null)
                    {
                        implicitTypes = true;
                    }
                    obj = readInstance(parser, namespace_, name, expected);
                    if (obj == null)
                    {
                        obj = readUnknown(parser, namespace_, name);
                    }
                }
                // finally, care about the id....
                if (id != null)
                {
                    resolveReference(id, obj);

                }
            }

            parser.require(XmlPullParser.END_TAG, null, elementName);
            return obj;
        }

        protected void resolveReference(String id, Object obj)
        {
            Object hlp = idMap[id];
            if (hlp is FwdRef)
            {
                FwdRef f = (FwdRef) hlp;
                do
                {
                    if (f.obj is FvmSerializable)
                    {
                        ((FvmSerializable) f.obj).setProperty(f.index, obj);
                    }
                    else
                    {
                        ((List<object>) f.obj)[f.index] = obj;
                    }
                    f = f.next;
                } while (f != null);
            }
            else if (hlp != null)
            {
                throw new Exception("double ID");
            }
            idMap[id] = obj;
        }

        /**
     * Returns a new object read from the given parser. If no mapping is found, null is returned. This method
     * is used by the SoapParser in order to convert the XML code to Java objects.
     */

        public Object readInstance(XmlPullParser parser, String namespace_, String name, PropertyInfo expected)
        {
            SoapPrimitive key = new SoapPrimitive(namespace_, name, null);

            if (!qNameToClass.ContainsKey(key))
            {
                return null;
            }
            Object obj = qNameToClass[key];
            if (obj is Marshal)
            {
                return ((Marshal) obj).readInstance(parser, namespace_, name, expected);
            }
            else if (obj is SoapObject)
            {
                obj = ((SoapObject) obj).newInstance();
            }
            else if (obj == typeof (SoapObject))
            {
                obj = new SoapObject(namespace_, name);
            }
            else
            {
                try
                {
                    obj = Activator.CreateInstance((Type) obj);
                }
                catch (Exception e)
                {
                    throw new Exception(e.ToString());
                }
            }
            if (obj is HasAttributes)
            {
                HasAttributes soapObject = (HasAttributes) obj;
                int cnt = parser.getAttributeCount();
                for (int counter = 0; counter < cnt; counter++)
                {

                    AttributeInfo attributeInfo = new AttributeInfo();
                    attributeInfo.setName(parser.getAttributeName(counter));
                    attributeInfo.setValue(parser.getAttributeValue(counter));
                    attributeInfo.setNamespace(parser.getAttributeNamespace(counter));
                    attributeInfo.setType(parser.getAttributeType(counter));

                    soapObject.setAttribute(attributeInfo);

                }
            }

            // ok, obj is now the instance, fill it....
            if (obj is SoapObject)
            {
                readSerializable(parser, (SoapObject) obj);

            }
            else if (obj is FvmSerializable)
            {

                if (obj is HasInnerText)
                {
                    ((HasInnerText) obj).setInnerText((parser.getText() != null) ? parser.getText() : "");
                }
                readSerializable(parser, (FvmSerializable) obj);

            }
            else if (obj is List<object>)
            {
                readVector(parser, (List<object>) obj, expected.elementType);

            }
            else
            {
                throw new Exception("no deserializer for " + obj.GetType());
            }

            return obj;
        }

        /**
     * Returns a string array containing the namespace, name, id and Marshal object for the given java object.
     * This method is used by the SoapWriter in order to map Java objects to the corresponding SOAP section
     * five XML code.
     */

        public Object[] getInfo(Object type, Object instance)
        {
            if (type == null)
            {
                if (instance is SoapObject || instance is SoapPrimitive)
                {
                    type = instance;
                }
                else
                {
                    type = instance.GetType();
                }
            }
            if (type is SoapObject)
            {
                SoapObject so = (SoapObject) type;
                return new Object[] {so.getNamespace(), so.getName(), null, null};
            }
            if (type is SoapPrimitive)
            {
                SoapPrimitive sp = (SoapPrimitive) type;
                return new Object[] {sp.getNamespace(), sp.getName(), null, DEFAULT_MARSHAL};
            }
            if ((type is Type) && type != PropertyInfo.OBJECT_CLASS)
            {
                Object[] tmp = (Object[]) classToQName[((Type) type).Name];
                if (tmp != null)
                {
                    return tmp;
                }
            }
            return new Object[] {xsd, ANY_TYPE_LABEL, null, null};
        }

        /**
     * Defines a direct mapping from a namespace and name to a java class (and vice versa), using the given
     * marshal mechanism
     */

        public void addMapping(String namespace_, String name, Type clazz, Marshal marshal)
        {
            qNameToClass
                [new SoapPrimitive(namespace_, name, null)] = marshal == null ? (Object) clazz : marshal;
            classToQName[clazz.Name] = new Object[] {namespace_, name, null, marshal};
        }

        /**
     * Defines a direct mapping from a namespace and name to a java class (and vice versa)
     */

        public void addMapping(String namespace_, String name, Type clazz)
        {
            addMapping(namespace_, name, clazz, null);
        }

        /**
     * Adds a SoapObject to the class map. During parsing, objects of the given type (namespace/name) will be
     * mapped to corresponding copies of the given SoapObject, maintaining the structure of the template.
     */

        public void addTemplate(SoapObject so)
        {
            qNameToClass[new SoapPrimitive(so.namespace_, so.name, null)] = so;
        }

        /**
     * Response from the soap call. Pulls the object from the wrapper object and returns it.
     *
     * @return response from the soap call.
     * @throws SoapFault
     * @since 2.0.3
     */

        public Object getResponse()
        {
            if (bodyIn == null)
            {
                return null;
            }
            if (bodyIn is SoapFault)
            {
                throw (SoapFault) bodyIn;
            }
            FvmSerializable ks = (FvmSerializable) bodyIn;

            if (ks.getPropertyCount() == 0)
            {
                return null;
            }
            else if (ks.getPropertyCount() == 1)
            {
                return ks.getProperty(0);
            }
            else
            {
                List<object> ret = new List<object>();
                for (int i = 0; i < ks.getPropertyCount(); i++)
                {
                    ret.Add(ks.getProperty(i));
                }
                return ret;
            }
        }

        /**
     * Serializes the request object to the given XmlSerliazer object
     *
     * @param writer XmlSerializer object to write the body into.
     */

        public override void writeBody(XmlSerializer writer)
        {
            // allow an empty body without any tags in it
            // see http://code.google.com/p/ksoap2-android/issues/detail?id=77
            if (bodyOut != null)
            {
                multiRef = new List<object>();
                multiRef.Add(bodyOut);
                Object[] qName = getInfo(null, bodyOut);

                writer.startTag((dotNet) ? "" : (String) qName[QNAME_NAMESPACE], (String) qName[QNAME_TYPE]);

                if (dotNet)
                {
                    writer.attribute(null, "xmlns", (String) qName[QNAME_NAMESPACE]);
                }

                if (addAdornments)
                {
                    writer.attribute(null, ID_LABEL, qName[2] == null ? ("o" + 0) : (String) qName[2]);
                    writer.attribute(enc, ROOT_LABEL, "1");
                }
                writeElement(writer, bodyOut, null, qName[QNAME_MARSHAL]);
                writer.endTag((dotNet) ? "" : (String) qName[QNAME_NAMESPACE], (String) qName[QNAME_TYPE]);
            }
        }

        private void writeAttributes(XmlSerializer writer, HasAttributes obj)
        {
            HasAttributes soapObject = (HasAttributes) obj;
            int cnt = soapObject.getAttributeCount();
            for (int counter = 0; counter < cnt; counter++)
            {
                AttributeInfo attributeInfo = new AttributeInfo();
                soapObject.getAttributeInfo(counter, attributeInfo);
                soapObject.getAttribute(counter, attributeInfo);
                if (attributeInfo.getValue() != null)
                {
                    writer.attribute(attributeInfo.getNamespace(), attributeInfo.getName(),
                        attributeInfo.getValue().ToString());
                }
            }
        }

        public void writeArrayListBodyWithAttributes(XmlSerializer writer, FvmSerializable obj)
        {
            if (obj is HasAttributes)
            {
                writeAttributes(writer, (HasAttributes) obj);
            }
            writeArrayListBody(writer, (List<object>) obj);
        }

        public void writeObjectBodyWithAttributes(XmlSerializer writer, FvmSerializable obj)
        {
            if (obj is HasAttributes)
            {
                writeAttributes(writer, (HasAttributes) obj);
            }
            writeObjectBody(writer, obj);
        }

        /**
     * Writes the body of an FvmSerializable object. This method is public for access from Marshal subclasses.
     */

        public void writeObjectBody(XmlSerializer writer, FvmSerializable obj)
        {
            int cnt = obj.getPropertyCount();
            PropertyInfo propertyInfo = new PropertyInfo();
            String namespace_;
            String name;
            String type;
            for (int i = 0; i < cnt; i++)
            {
                // get the property
                Object prop = obj.getProperty(i);
                // and importantly also get the property info which holds the name potentially!
                obj.getPropertyInfo(i, properties, propertyInfo);

                if (!(prop is SoapObject))
                {
                    // prop is a PropertyInfo
                    if ((propertyInfo.flags & PropertyInfo.TRANSIENT) == 0)
                    {
                        Object objValue = obj.getProperty(i);
                        if ((prop != null || !skipNullProperties) && (objValue != SoapPrimitive.NullSkip))
                        {
                            writer.startTag(propertyInfo.namespace_, propertyInfo.name);
                            writeProperty(writer, objValue, propertyInfo);
                            writer.endTag(propertyInfo.namespace_, propertyInfo.name);
                        }
                    }
                }
                else
                {

                    // prop is a SoapObject
                    SoapObject nestedSoap = (SoapObject) prop;
                    // lets get the info from the soap object itself
                    Object[] qName = getInfo(null, nestedSoap);
                    namespace_ = (String) qName[QNAME_NAMESPACE];
                    type = (String) qName[QNAME_TYPE];

                    // prefer the name from the property info
                    if (propertyInfo.name != null && propertyInfo.name.Length > 0)
                    {
                        name = propertyInfo.name;
                    }
                    else
                    {
                        name = (String) qName[QNAME_TYPE];
                    }

                    // prefer the namespace_ from the property info
                    if (propertyInfo.namespace_ != null && propertyInfo.namespace_.Length > 0)
                    {
                        namespace_ = propertyInfo.namespace_;
                    }
                    else
                    {
                        namespace_ = (String) qName[QNAME_NAMESPACE];
                    }

                    writer.startTag(namespace_, name);
                    if (!implicitTypes)
                    {
                        String prefix = writer.getPrefix(namespace_, true);
                        writer.attribute(xsi, TYPE_LABEL, prefix + ":" + type);
                    }
                    writeObjectBodyWithAttributes(writer, nestedSoap);
                    writer.endTag(namespace_, name);
                }
            }
            if (obj is HasInnerText)
            {

                if (((HasInnerText) obj).getInnerText() != null)
                {
                    writer.cdsect(((HasInnerText) obj).getInnerText());
                }
            }

        }

        protected void writeProperty(XmlSerializer writer, Object obj, PropertyInfo type)
        {
            if (obj == null || obj == SoapPrimitive.NullNilElement)
            {
                writer.attribute(xsi, version >= VER12 ? NIL_LABEL : NULL_LABEL, "true");
                return;
            }
            Object[] qName = getInfo(null, obj);
            if (type.multiRef || qName[2] != null)
            {
                int i = multiRef.IndexOf(obj);
                if (i == -1)
                {
                    i = multiRef.Count;
                    multiRef.Add(obj);
                }
                writer.attribute(null, HREF_LABEL, qName[2] == null ? ("#o" + i) : "#" + qName[2]);
            }
            else
            {
                if (!implicitTypes || obj.GetType() != type.type)
                {
                    String prefix = writer.getPrefix((String) qName[QNAME_NAMESPACE], true);
                    writer.attribute(xsi, TYPE_LABEL, prefix + ":" + qName[QNAME_TYPE]);
                }
                writeElement(writer, obj, type, qName[QNAME_MARSHAL]);
            }
        }

        protected void writeElement(XmlSerializer writer, Object element, PropertyInfo type, Object marshal)
        {
            if (marshal != null)
            {
                ((Marshal) marshal).writeInstance(writer, element);
            }
            else if (element is FvmSerializable || element == SoapPrimitive.NullNilElement
                     || element == SoapPrimitive.NullSkip)
            {
                if (element is List<object>)
                {
                    writeArrayListBodyWithAttributes(writer, (FvmSerializable) element);
                }
                else
                {
                    writeObjectBodyWithAttributes(writer, (FvmSerializable) element);
                }
            }
            else if (element is HasAttributes)
            {
                writeAttributes(writer, (HasAttributes) element);
            }
            else if (element is List<object>)
            {
                writeVectorBody(writer, (List<object>) element, type.elementType);
            }
            else
            {
                throw new Exception("Cannot serialize: " + element);
            }
        }

        protected void writeArrayListBody(XmlSerializer writer, List<object> list)
        {
            FvmSerializable obj = (FvmSerializable) list;
            int cnt = list.Count;
            PropertyInfo propertyInfo = new PropertyInfo();
            String namespace_;
            String name;
            String type;
            for (int i = 0; i < cnt; i++)
            {
                // get the property
                Object prop = obj.getProperty(i);
                // and importantly also get the property info which holds the name potentially!
                obj.getPropertyInfo(i, properties, propertyInfo);

                if (!(prop is SoapObject))
                {
                    // prop is a PropertyInfo
                    if ((propertyInfo.flags & PropertyInfo.TRANSIENT) == 0)
                    {
                        Object objValue = obj.getProperty(i);
                        if ((prop != null || !skipNullProperties) && (objValue != SoapPrimitive.NullSkip))
                        {
                            writer.startTag(propertyInfo.namespace_, propertyInfo.name);
                            writeProperty(writer, objValue, propertyInfo);
                            writer.endTag(propertyInfo.namespace_, propertyInfo.name);
                        }
                    }
                }
                else
                {

                    // prop is a SoapObject
                    SoapObject nestedSoap = (SoapObject) prop;
                    // lets get the info from the soap object itself
                    Object[] qName = getInfo(null, nestedSoap);
                    namespace_ = (String) qName[QNAME_NAMESPACE];
                    type = (String) qName[QNAME_TYPE];

                    // prefer the name from the property info
                    if (propertyInfo.name != null && propertyInfo.name.Length > 0)
                    {
                        name = propertyInfo.name;
                    }
                    else
                    {
                        name = (String) qName[QNAME_TYPE];
                    }

                    // prefer the namespace_ from the property info
                    if (propertyInfo.namespace_ != null && propertyInfo.namespace_.Length > 0)
                    {
                        namespace_ = propertyInfo.namespace_;
                    }
                    else
                    {
                        namespace_ = (String) qName[QNAME_NAMESPACE];
                    }

                    writer.startTag(namespace_, name);
                    if (!implicitTypes)
                    {
                        String prefix = writer.getPrefix(namespace_, true);
                        writer.attribute(xsi, TYPE_LABEL, prefix + ":" + type);
                    }
                    writeObjectBodyWithAttributes(writer, nestedSoap);
                    writer.endTag(namespace_, name);
                }
            }
            if (obj is HasInnerText)
            {
                if (((HasInnerText) obj).getInnerText() != null)
                {
                    writer.cdsect(((HasInnerText) obj).getInnerText());

                }
            }

        }

        protected void writeVectorBody(XmlSerializer writer, List<object> vector, PropertyInfo elementType)
        {
            String itemsTagName = ITEM_LABEL;
            String itemsNamespace = null;

            if (elementType == null)
            {
                elementType = PropertyInfo.OBJECT_TYPE;
            }
            else if (elementType is PropertyInfo)
            {
                if (elementType.name != null)
                {
                    itemsTagName = elementType.name;
                    itemsNamespace = elementType.namespace_;
                }
            }

            int cnt = vector.Count;
            Object[] arrType = getInfo(elementType.type, null);

            // This removes the arrayType attribute from the xml for arrays(required for most .Net services to work)
            if (!implicitTypes)
            {
                writer.attribute(enc, ARRAY_TYPE_LABEL, writer.getPrefix((String) arrType[0], false) + ":"
                                                        + arrType[1] + "[" + cnt + "]");
            }
            else
            {
                // Get the namespace from mappings if available when arrayType is removed for .Net
                if (itemsNamespace == null)
                {
                    itemsNamespace = (String) arrType[0];
                }
            }

            bool skipped = false;
            for (int i = 0; i < cnt; i++)
            {
                if (vector[i] == null)
                {
                    skipped = true;
                }
                else
                {
                    writer.startTag(itemsNamespace, itemsTagName);
                    if (skipped)
                    {
                        writer.attribute(enc, "position", "[" + i + "]");
                        skipped = false;
                    }
                    writeProperty(writer, vector[i], elementType);
                    writer.endTag(itemsNamespace, itemsTagName);
                }
            }
        }
    }
}
