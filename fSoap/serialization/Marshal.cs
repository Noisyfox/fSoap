using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cn.noisyfox.fobjects.base64;
using cn.noisyfox.fobjects.isodate;
using cn.noisyfox.fxml.xmlpull;

namespace cn.noisyfox.fsoap.serialization
{

/**
 * Interface for custom (de)serialization.
 */

    public interface Marshal
    {
        /**
     * This methods reads an instance from the given parser. For implementation,
     * please note that the start and and tag must be consumed. This is not
     * symmetric to writeInstance, but otherwise it would not be possible to
     * access the attributes of the start tag here.
     * 
     * @param parser
     *            the xml parser
     * @param namespace
     *            the namespace.
     * @return the object read from the xml stream.
     */
        Object readInstance(XmlPullParser parser, String namespace_, String name, PropertyInfo expected);

        /**
     * Write the instance to the given XmlSerializer. In contrast to
     * readInstance, it is not neccessary to care about the surrounding start
     * and end tags. Additional attributes must be writen before anything else
     * is written.
     * 
     * @param writer
     *            the xml serializer.
     * @param instance
     *            the instance to write to the writer.
     */
        void writeInstance(XmlSerializer writer, Object instance);

        /**
     * Register this Marshal with Envelope
     * 
     * @param envelope
     *            the soap serialization envelope.
     */
        void register(SoapSerializationEnvelope envelope);
    }

    /** 
 * Base64 (de)serializer 
 */

    public class MarshalBase64 : Marshal
    {
        public static Type BYTE_ARRAY_CLASS = new byte[0].GetType();

        public Object readInstance(XmlPullParser parser, String namespace_, String name, PropertyInfo expected)
        {
            return Base64.decode(parser.nextText());
        }

        public void writeInstance(XmlSerializer writer, Object obj)
        {
            writer.text(Base64.encode((byte[]) obj));
        }

        public void register(SoapSerializationEnvelope cm)
        {
            cm.addMapping(cm.xsd, "base64Binary", MarshalBase64.BYTE_ARRAY_CLASS, this);
            cm.addMapping(SoapEnvelope.ENC, "base64", MarshalBase64.BYTE_ARRAY_CLASS, this);
        }
    }


/** 
 * Marshal class for Dates. 
 */

    public class MarshalDate : Marshal
    {
        public static Type DATE_CLASS = new DateTime().GetType();

        public Object readInstance(XmlPullParser parser, String namespace_, String name, PropertyInfo expected)
        {
            return IsoDate.stringToDate(parser.nextText(), IsoDate.DATE_TIME);
        }

        public void writeInstance(XmlSerializer writer, Object obj)
        {
            writer.text(IsoDate.dateToString((DateTime) obj, IsoDate.DATE_TIME));
        }

        public void register(SoapSerializationEnvelope cm)
        {
            cm.addMapping(cm.xsd, "dateTime", MarshalDate.DATE_CLASS, this);
        }

    }

    public class MarshalFloat : Marshal
    {

        public Object readInstance(XmlPullParser parser, String namespace_, String name, PropertyInfo propertyInfo)
        {
            String stringValue = parser.nextText();
            Object result;
            if (name.Equals("float"))
            {
                result = float.Parse(stringValue);
            }
            else if (name.Equals("double"))
            {
                result = double.Parse(stringValue);
            }
            else if (name.Equals("decimal"))
            {
                result = Decimal.Parse(stringValue);
            }
            else
            {
                throw new Exception("float, double, or decimal expected");
            }
            return result;
        }

        public void writeInstance(XmlSerializer writer, Object instance)
        {
            writer.text(instance.ToString());
        }

        public void register(SoapSerializationEnvelope cm)
        {
            cm.addMapping(cm.xsd, "float", typeof (float), this);
            cm.addMapping(cm.xsd, "double", typeof (double), this);
            cm.addMapping(cm.xsd, "decimal", typeof (Decimal), this);
        }
    }


/**
 * Serializes instances of hashtable to and from xml. This implementation is
 * based on the xml schema from apache-soap, namely the type 'map' in the
 * namespace 'http://xml.apache.org/xml-soap'. Other soap implementations
 * including apache (obviously) and glue are also interoperable with the
 * schema.
 */

    public class MarshalHashtable : Marshal
    {

        /** use then during registration */
        public static readonly String NAMESPACE = "http://xml.apache.org/xml-soap";
        /** use then during registration */
        public static readonly String NAME = "Map";
        /** CLDC does not support .class, so this helper is needed. */
        public static readonly Type HASHTABLE_CLASS = new Dictionary<object, object>().GetType();
        private SoapSerializationEnvelope envelope;

        public Object readInstance(XmlPullParser parser, String namespace_, String name, PropertyInfo expected)
        {
            Dictionary<object, object> instance = new Dictionary<object, object>();
            String elementName = parser.getName();
            while (parser.nextTag() != XmlPullParser.END_TAG)
            {
                SoapObject item = new ItemSoapObject(instance);
                parser.require(XmlPullParser.START_TAG, null, "item");
                parser.nextTag();
                Object key = envelope.read(parser, item, 0, null, null, PropertyInfo.OBJECT_TYPE);
                parser.nextTag();
                if (key != null)
                {
                    item.setProperty(0, key);
                }
                Object value = envelope.read(parser, item, 1, null, null, PropertyInfo.OBJECT_TYPE);
                parser.nextTag();
                if (value != null)
                {
                    item.setProperty(1, value);
                }
                parser.require(XmlPullParser.END_TAG, null, "item");
            }
            parser.require(XmlPullParser.END_TAG, null, elementName);
            return instance;
        }

        public void writeInstance(XmlSerializer writer, Object instance)
        {
            Dictionary<object, object> h = instance as Dictionary<object, object>;
            SoapObject item = new SoapObject(null, null);
            item.addProperty("key", null);
            item.addProperty("value", null);
            foreach (KeyValuePair<object, object> keyValuePair in h)
            {
                writer.startTag("", "item");
                item.setProperty(0, keyValuePair.Key);
                item.setProperty(1, keyValuePair.Value);
                envelope.writeObjectBodyWithAttributes(writer, item);
                writer.endTag("", "item");
            }
        }

        private class ItemSoapObject : SoapObject
        {
            private Dictionary<object, object> h;
            private int resolvedIndex = -1;

            public ItemSoapObject(Dictionary<object, object> h) : base(null, null)
            {
                this.h = h;
                addProperty("key", null);
                addProperty("value", null);
            }

            // 0 & 1 only valid
            public override void setProperty(int index, Object value)
            {
                if (resolvedIndex == -1)
                {
                    base.setProperty(index, value);
                    resolvedIndex = index;
                }
                else
                {
                    // already have a key or value
                    Object resolved = resolvedIndex == 0 ? getProperty(0) : getProperty(1);
                    if (index == 0)
                    {
                        h[value] = resolved;
                    }
                    else
                    {
                        h[resolved] = value;
                    }
                }
            }
        }

        public void register(SoapSerializationEnvelope cm)
        {
            envelope = cm;
            cm.addMapping(MarshalHashtable.NAMESPACE, MarshalHashtable.NAME, HASHTABLE_CLASS, this);
        }
    }


/**
 * This class is not public, so save a few bytes by using a short class name (DM
 * stands for DefaultMarshal)...
 */

    internal class DM : Marshal
    {

        public Object readInstance(XmlPullParser parser, String namespace_, String name, PropertyInfo excepted)
        {
            String text = parser.nextText();
            switch (name[0])
            {
                case 's':
                    return text;
                case 'i':
                    return int.Parse(text);
                case 'l':
                    return long.Parse(text);
                case 'b':
                    return SoapEnvelope.stringToBoolean(text);
                default:
                    throw new Exception();
            }
        }

        /**
     * Write the instance out. In case it is an AttributeContainer write those our first though. 
     * If it HasAttributes then write the attributes and values.
     *
     * @param writer   the xml serializer.
     * @param instance
     * @throws IOException
     */

        public void writeInstance(XmlSerializer writer, Object instance)
        {
            if (instance is AttributeContainer)
            {
                AttributeContainer attributeContainer = (AttributeContainer) instance;
                int cnt = attributeContainer.getAttributeCount();
                for (int counter = 0; counter < cnt; counter++)
                {
                    AttributeInfo attributeInfo = new AttributeInfo();
                    attributeContainer.getAttributeInfo(counter, attributeInfo);
                    try
                    {
                        attributeContainer.getAttribute(counter, attributeInfo);
                    }
                    catch (Exception e)
                    {
                        //e.printStackTrace();
                    }
                    if (attributeInfo.getValue() != null)
                    {
                        writer.attribute(attributeInfo.getNamespace(), attributeInfo.getName(),
                            (attributeInfo.getValue() != null) ? attributeInfo.getValue().ToString() : "");
                    }
                }
            }
            else if (instance is HasAttributes)
            {
                HasAttributes soapObject = (HasAttributes) instance;
                int cnt = soapObject.getAttributeCount();
                for (int counter = 0; counter < cnt; counter++)
                {
                    AttributeInfo attributeInfo = new AttributeInfo();
                    soapObject.getAttributeInfo(counter, attributeInfo);
                    try
                    {
                        soapObject.getAttribute(counter, attributeInfo);
                    }
                    catch (Exception e)
                    {
                        //e.printStackTrace();
                    }
                    if (attributeInfo.getValue() != null)
                    {
                        writer.attribute(attributeInfo.getNamespace(), attributeInfo.getName(),
                            attributeInfo.getValue() != null ? attributeInfo.getValue().ToString() : "");
                    }
                }
            }
            writer.text(instance.ToString());
        }

        public void register(SoapSerializationEnvelope cm)
        {
            cm.addMapping(cm.xsd, "int", PropertyInfo.INTEGER_CLASS, this);
            cm.addMapping(cm.xsd, "long", PropertyInfo.LONG_CLASS, this);
            cm.addMapping(cm.xsd, "string", PropertyInfo.STRING_CLASS, this);
            cm.addMapping(cm.xsd, "boolean", PropertyInfo.BOOLEAN_CLASS, this);
        }
    }

}
