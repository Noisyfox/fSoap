using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cn.noisyfox.fsoap.serialization
{
    public class SoapObject : AttributeContainer, FvmSerializable, HasInnerText
    {

        private static readonly String EMPTY_STRING = "";
        /**
     * The namespace_ of this soap object.
     */
        protected internal String namespace_;
        /**
     * The name of this soap object.
     */
        protected internal String name;
        /**
     * The Vector of properties (can contain PropertyInfo and SoapObject)
     */
        protected List<object> properties = new List<object>();

        protected String innerText;

        // TODO: accessing properties and attributes would work much better if we
        // kept a list of known properties instead of iterating through the list
        // each time

        /**
     * Creates a new <code>SoapObject</code> instance.
     */

        public SoapObject() : this("", "")
        {
        }

        /**
     * Creates a new <code>SoapObject</code> instance.
     *
     * @param namespace_
     *            the namespace_ for the soap object
     * @param name
     *            the name of the soap object
     */

        public SoapObject(String namespace_, String name)
        {
            this.namespace_ = namespace_;
            this.name = name;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SoapObject))
            {
                return false;
            }

            SoapObject otherSoapObject = (SoapObject) obj;

            if (!name.Equals(otherSoapObject.name)
                || !namespace_.Equals(otherSoapObject.namespace_))
            {
                return false;
            }

            int numProperties = properties.Count;
            if (numProperties != otherSoapObject.properties.Count)
            {
                return false;
            }

            // SoapObjects are only considered the same if properties Equals and in the same order
            for (int propIndex = 0; propIndex < numProperties; propIndex++)
            {
                Object thisProp = this.properties[propIndex];
                if (!otherSoapObject.isPropertyEqual(thisProp, propIndex))
                {
                    return false;
                }
            }

            return attributesAreEqual(otherSoapObject);
        }


        /**
* Helper function for SoapObject.Equals
* Checks if a given property and index are the same as in this
*
*  @param otherProp, index
*  @return
*/

        public bool isPropertyEqual(Object otherProp, int index)
        {
            if (index >= getPropertyCount())
            {
                return false;
            }
            Object thisProp = this.properties[index];
            if (otherProp is PropertyInfo &&
                thisProp is PropertyInfo)
            {
                // Get both PropertInfos and compare values
                PropertyInfo otherPropInfo = (PropertyInfo) otherProp;
                PropertyInfo thisPropInfo = (PropertyInfo) thisProp;
                return otherPropInfo.getName().Equals(thisPropInfo.getName()) &&
                       otherPropInfo.getValue().Equals(thisPropInfo.getValue());
            }
            else if (otherProp is SoapObject && thisProp is SoapObject)
            {
                SoapObject otherPropSoap = (SoapObject) otherProp;
                SoapObject thisPropSoap = (SoapObject) thisProp;
                return otherPropSoap.Equals(thisPropSoap);
            }
            return false;
        }

        public String getName()
        {
            return name;
        }

        public String getNamespace()
        {
            return namespace_;
        }

        public object getProperty(int index)
        {
            Object prop = properties[index];
            if (prop is PropertyInfo)
            {
                return ((PropertyInfo) prop).getValue();
            }
            else
            {
                return ((SoapObject) prop);
            }
        }


        /**
         * Get the ToString value of the property.
         *
         * @param index
         * @return
         */

        public String getPropertyAsString(int index)
        {
            PropertyInfo propertyInfo = (PropertyInfo) properties[index];
            return propertyInfo.getValue().ToString();
        }

        /**
         * Get the property with the given name
         *
         * @throws java.lang.Exception
         *             if the property does not exist
         */

        public Object getProperty(String name)
        {
            int? index = propertyIndex(name);
            if (index != null)
            {
                return getProperty(index.Value);
            }
            else
            {
                throw new Exception("illegal property: " + name);
            }
        }

        /**
     * Get a property using namespace_ and name without chance of throwing an exception
     *
     * @return the property if it exists; if not, {@link NullSoapObject} is
     *         returned
     */

        public Object getPropertyByNamespaceSafely(String namespace_, String name)
        {
            int? i = propertyIndex(namespace_, name);
            if (i != null)
            {
                return getProperty(i.Value);
            }
            else
            {
                return new NullSoapObject();
            }
        }

        /**
     * Get the ToString value of a property without chance of throwing an
     * exception
     *
     * @return the string value of the property if it exists; if not, #EMPTY_STRING is
     *         returned
     */

        public String getPropertyByNamespaceSafelyAsString(String namespace_, String name)
        {
            int? i = propertyIndex(namespace_, name);
            if (i != null)
            {
                Object foo = getProperty(i.Value);
                if (foo == null)
                {
                    return EMPTY_STRING;
                }
                else
                {
                    return foo.ToString();
                }
            }
            else
            {
                return EMPTY_STRING;
            }
        }

        /**
     * Get a property without chance of throwing an exception. An object can be
     * provided to this method; if the property is not found, this object will
     * be returned.
     *
     * @param defaultThing
     *            the object to return if the property is not found
     * @return the property if it exists; defaultThing if the property does not
     *         exist
     */

        public Object getPropertySafely(String namespace_, String name, Object defaultThing)
        {
            int? i = propertyIndex(namespace_, name);
            if (i != null)
            {
                return getProperty(i.Value);
            }
            else
            {
                return defaultThing;
            }
        }

        /**
     * Get the ToString value of a property without chance of throwing an
     * exception. An object can be provided to this method; if the property is
     * not found, this object's string representation will be returned.
     *
     * @param defaultThing
     *            ToString of the object to return if the property is not found
     * @return the property ToString if it exists; defaultThing ToString if the
     *         property does not exist, if the defaultThing is null #EMPTY_STRING
     *         is returned
     */

        public String getPropertySafelyAsString(String namespace_, String name,
            Object defaultThing)
        {
            int? i = propertyIndex(namespace_, name);
            if (i != null)
            {
                Object property = getProperty(i.Value);
                if (property != null)
                {
                    return property.ToString();
                }
                else
                {
                    return EMPTY_STRING;
                }
            }
            else
            {
                if (defaultThing != null)
                {
                    return defaultThing.ToString();
                }
                else
                {
                    return EMPTY_STRING;
                }
            }
        }

        /**
     * Get the primitive property with the given name.
     *
     * @param name
     * @return PropertyInfo containing an empty string if property either complex or empty
     */

        public Object getPrimitiveProperty(String namespace_, String name)
        {
            int? index = propertyIndex(namespace_, name);
            if (index != null)
            {
                PropertyInfo propertyInfo = (PropertyInfo) properties[index.Value];
                if (!propertyInfo.getType().Equals(typeof (SoapObject)) && propertyInfo.getValue() != null)
                {
                    return propertyInfo.getValue();
                }
                else
                {
                    propertyInfo = new PropertyInfo();
                    propertyInfo.setType(typeof (String));
                    propertyInfo.setValue(EMPTY_STRING);
                    propertyInfo.setName(name);
                    propertyInfo.setNamespace(namespace_);
                    return (Object) propertyInfo.getValue();
                }
            }
            else
            {
                throw new Exception("illegal property: " + name);
            }
        }

        /**
     * Get the ToString value of the primitive property with the given name.
     * Returns empty string if property either complex or empty
     *
     * @param name
     * @return the string value of the property
     */

        public String getPrimitivePropertyAsString(String namespace_, String name)
        {
            int? index = propertyIndex(namespace_, name);
            if (index != null)
            {
                PropertyInfo propertyInfo = (PropertyInfo) properties[index.Value];
                if (!propertyInfo.getType().Equals(typeof (SoapObject)) && propertyInfo.getValue() != null)
                {
                    return propertyInfo.getValue().ToString();
                }
                else
                {
                    return EMPTY_STRING;
                }
            }
            else
            {
                throw new Exception("illegal property: " + name);
            }
        }

        /**
     * Get the ToString value of a primitive property without chance of throwing an
     * exception
     *
     * @param name
     * @return the string value of the property if it exists and is primitive; if not, #EMPTY_STRING is
     *         returned
     */

        public Object getPrimitivePropertySafely(String namespace_, String name)
        {
            int? index = propertyIndex(namespace_, name);
            if (index != null)
            {
                PropertyInfo propertyInfo = (PropertyInfo) properties[index.Value];
                if (!propertyInfo.getType().Equals(typeof (SoapObject)) && propertyInfo.getValue() != null)
                {
                    return propertyInfo.getValue().ToString();
                }
                else
                {
                    propertyInfo = new PropertyInfo();
                    propertyInfo.setType(typeof (String));
                    propertyInfo.setValue(EMPTY_STRING);
                    propertyInfo.setName(name);
                    propertyInfo.setNamespace(namespace_);
                    return (Object) propertyInfo.getValue();
                }
            }
            else
            {
                return new NullSoapObject();
            }
        }

        /**
     * Get the ToString value of a primitive property without chance of throwing an
     * exception
     *
     * @param name
     * @return the string value of the property if it exists and is primitive; if not, #EMPTY_STRING is
     *         returned
     */

        public String getPrimitivePropertySafelyAsString(String namespace_, String name)
        {
            int? index = propertyIndex(namespace_, name);
            if (index != null)
            {
                PropertyInfo propertyInfo = (PropertyInfo) properties[index.Value];
                if (!propertyInfo.getType().Equals(typeof (SoapObject)) && propertyInfo.getValue() != null)
                {
                    return propertyInfo.getValue().ToString();
                }
                else
                {
                    return EMPTY_STRING;
                }
            }
            else
            {
                return EMPTY_STRING;
            }
        }

        /**
     * Knows whether the given property exists
     */

        public bool hasProperty(String namespace_, String name)
        {
            if (propertyIndex(namespace_, name) != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /**
     * Get the ToString value of the property.
     *
     * @param namespace_
     * @param name
     * @return
     */

        public String getPropertyAsString(String namespace_, String name)
        {
            int? index = propertyIndex(namespace_, name);
            if (index != null)
            {
                return getProperty(index.Value).ToString();
            }
            else
            {
                throw new Exception("illegal property: " + name);
            }
        }

        /**
     * Get the ToString value of the property.
     *
     * @param name
     * @return
     */

        public String getPropertyAsString(String name)
        {
            int? index = propertyIndex(name);
            if (index != null)
            {
                return getProperty(index.Value).ToString();
            }
            else
            {
                throw new Exception("illegal property: " + name);
            }
        }

        /**
     * Knows whether the given property exists
     */

        public bool hasProperty(String name)
        {
            if (propertyIndex(name) != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /**
     * Get a property without chance of throwing an exception
     *
     * @return the property if it exists; if not, {@link NullSoapObject} is
     *         returned
     */

        public Object getPropertySafely(String name)
        {
            int? i = propertyIndex(name);
            if (i != null)
            {
                return getProperty(i.Value);
            }
            else
            {
                return new NullSoapObject();
            }
        }

        /**
     * Get the ToString value of a property without chance of throwing an
     * exception
     *
     * @return the string value of the property if it exists; if not, #EMPTY_STRING is
     *         returned
     */

        public String getPropertySafelyAsString(String name)
        {
            int? i = propertyIndex(name);
            if (i != null)
            {
                Object foo = getProperty(i.Value);
                if (foo == null)
                {
                    return EMPTY_STRING;
                }
                else
                {
                    return foo.ToString();
                }
            }
            else
            {
                return EMPTY_STRING;
            }
        }

        /**
     * Get a property without chance of throwing an exception. An object can be
     * provided to this method; if the property is not found, this object will
     * be returned.
     *
     * @param defaultThing
     *            the object to return if the property is not found
     * @return the property if it exists; defaultThing if the property does not
     *         exist
     */

        public Object getPropertySafely(String name, Object defaultThing)
        {
            int? i = propertyIndex(name);
            if (i != null)
            {
                return getProperty(i.Value);
            }
            else
            {
                return defaultThing;
            }
        }

        /**
     * Get the ToString value of a property without chance of throwing an
     * exception. An object can be provided to this method; if the property is
     * not found, this object's string representation will be returned.
     *
     * @param defaultThing
     *            ToString of the object to return if the property is not found
     * @return the property ToString if it exists; defaultThing ToString if the
     *         property does not exist, if the defaultThing is null #EMPTY_STRING
     *         is returned
     */

        public String getPropertySafelyAsString(String name,
            Object defaultThing)
        {
            int? i = propertyIndex(name);
            if (i != null)
            {
                Object property = getProperty(i.Value);
                if (property != null)
                {
                    return property.ToString();
                }
                else
                {
                    return EMPTY_STRING;
                }
            }
            else
            {
                if (defaultThing != null)
                {
                    return defaultThing.ToString();
                }
                else
                {
                    return EMPTY_STRING;
                }
            }
        }

        /**
     * Get the primitive property with the given name.
     *
     * @param name
     * @return PropertyInfo containing an empty string if property either complex or empty
     */

        public Object getPrimitiveProperty(String name)
        {
            int? index = propertyIndex(name);
            if (index != null)
            {
                PropertyInfo propertyInfo = (PropertyInfo) properties[index.Value];
                if (!propertyInfo.getType().Equals(typeof (SoapObject)) && propertyInfo.getValue() != null)
                {
                    return propertyInfo.getValue();
                }
                else
                {
                    propertyInfo = new PropertyInfo();
                    propertyInfo.setType(typeof (String));
                    propertyInfo.setValue(EMPTY_STRING);
                    propertyInfo.setName(name);
                    return (Object) propertyInfo.getValue();
                }
            }
            else
            {
                throw new Exception("illegal property: " + name);
            }
        }

        /**
     * Get the ToString value of the primitive property with the given name.
     * Returns empty string if property either complex or empty
     *
     * @param name
     * @return the string value of the property
     */

        public String getPrimitivePropertyAsString(String name)
        {
            int? index = propertyIndex(name);
            if (index != null)
            {
                PropertyInfo propertyInfo = (PropertyInfo) properties[index.Value];
                if (!propertyInfo.getType().Equals(typeof (SoapObject)) && propertyInfo.getValue() != null)
                {
                    return propertyInfo.getValue().ToString();
                }
                else
                {
                    return EMPTY_STRING;
                }
            }
            else
            {
                throw new Exception("illegal property: " + name);
            }
        }

        /**
     * Get the ToString value of a primitive property without chance of throwing an
     * exception
     *
     * @param name
     * @return the string value of the property if it exists and is primitive; if not, #EMPTY_STRING is
     *         returned
     */

        public Object getPrimitivePropertySafely(String name)
        {
            int? index = propertyIndex(name);
            if (index != null)
            {
                PropertyInfo propertyInfo = (PropertyInfo) properties[index.Value];
                if (!propertyInfo.getType().Equals(typeof (SoapObject)) && propertyInfo.getValue() != null)
                {
                    return propertyInfo.getValue().ToString();
                }
                else
                {
                    propertyInfo = new PropertyInfo();
                    propertyInfo.setType(typeof (String));
                    propertyInfo.setValue(EMPTY_STRING);
                    propertyInfo.setName(name);
                    return (Object) propertyInfo.getValue();
                }
            }
            else
            {
                return new NullSoapObject();
            }
        }

        /**
     * Get the ToString value of a primitive property without chance of throwing an
     * exception
     *
     * @param name
     * @return the string value of the property if it exists and is primitive; if not, #EMPTY_STRING is
     *         returned
     */

        public String getPrimitivePropertySafelyAsString(String name)
        {
            int? index = propertyIndex(name);
            if (index != null)
            {
                PropertyInfo propertyInfo = (PropertyInfo) properties[index.Value];
                if (!propertyInfo.getType().Equals(typeof (SoapObject)) && propertyInfo.getValue() != null)
                {
                    return propertyInfo.getValue().ToString();
                }
                else
                {
                    return EMPTY_STRING;
                }
            }
            else
            {
                return EMPTY_STRING;
            }
        }



        private int? propertyIndex(String name)
        {
            if (name != null)
            {
                for (int i = 0; i < properties.Count; i++)
                {
                    if (name.Equals(((PropertyInfo) properties[i]).getName()))
                    {
                        return i;
                    }
                }
            }
            return null;
        }


        private int? propertyIndex(String namespace_, String name)
        {
            if (name != null && namespace_ != null)
            {
                for (int i = 0; i < properties.Count; i++)
                {
                    PropertyInfo info = (PropertyInfo) properties[i];
                    if (name.Equals(info.getName()) && namespace_.Equals(info.getNamespace()))
                    {
                        return i;
                    }
                }
            }
            return null;
        }

        public int getPropertyCount()
        {
            return properties.Count;
        }

        public virtual void setProperty(int index, object value)
        {
            Object prop = properties[index];
            if (prop is PropertyInfo)
            {
                ((PropertyInfo) prop).setValue(value);
            }
            // TODO: not sure how you want to handle an exception here if the index points to a SoapObject
        }

        /**
         * Adds a property (parameter) to the object. This is essentially a sub
         * element.
         *
         * @param name
         *            The name of the property
         * @param value
         *            the value of the property
         */

        public SoapObject addProperty(String name, Object value)
        {
            PropertyInfo propertyInfo = new PropertyInfo();
            propertyInfo.name = name;
            propertyInfo.type = value == null
                ? PropertyInfo.OBJECT_CLASS
                : value
                    .GetType();
            propertyInfo.value = value;
            return addProperty(propertyInfo);
        }

        /**
     * Adds a property (parameter) to the object. This is essentially a sub
     * element.
     *
     * @param namespace
     *            The namespace of the property
     * @param name
     *            The name of the property
     * @param value
     *            the value of the property
     */

        public SoapObject addProperty(String namespace_, String name, Object value)
        {
            PropertyInfo propertyInfo = new PropertyInfo();
            propertyInfo.name = name;
            propertyInfo.namespace_ = namespace_;
            propertyInfo.type = value == null
                ? PropertyInfo.OBJECT_CLASS
                : value
                    .GetType();
            propertyInfo.value = value;
            return addProperty(propertyInfo);
        }

        /**
     * Add a property only if the value is not null.
     *
     * @param namespace
     *            The namespace of the property
     * @param name
     *            The name of the property
     * @param value
     *            the value of the property
     * @return
     */

        public SoapObject addPropertyIfValue(String namespace_, String name, Object value)
        {
            if (value != null)
            {
                return addProperty(namespace_, name, value);
            }
            else
            {
                return this;
            }
        }

        /**
     * Add a property only if the value is not null.
     *
     * @param name
     * @param value
     * @return
     */

        public SoapObject addPropertyIfValue(String name, Object value)
        {
            if (value != null)
            {
                return addProperty(name, value);
            }
            else
            {
                return this;
            }
        }

        /**
     * Add a property only if the value is not null.
     *
     * @param propertyInfo
     * @param value
     * @return
     */

        public SoapObject addPropertyIfValue(PropertyInfo propertyInfo, Object value)
        {
            if (value != null)
            {
                propertyInfo.setValue(value);
                return addProperty(propertyInfo);
            }
            else
            {
                return this;
            }
        }

        /**
     * Adds a property (parameter) to the object. This is essentially a sub
     * element.
     *
     * @param propertyInfo
     *            designated retainer of desired property
     */

        public SoapObject addProperty(PropertyInfo propertyInfo)
        {
            properties.Add(propertyInfo);
            return this;
        }

        /**
     * Ad the propertyInfo only if the value of it is not null.
     *
     * @param propertyInfo
     * @return
     */

        public SoapObject addPropertyIfValue(PropertyInfo propertyInfo)
        {
            if (propertyInfo.value != null)
            {
                properties.Add(propertyInfo);
                return this;
            }
            else
            {
                return this;
            }
        }

        /**
     * Adds a SoapObject the properties array. This is a sub element to
     * allow nested SoapObjects
     *
     * @param soapObject
     *            to be added as a property of the current object
     */

        public SoapObject addSoapObject(SoapObject soapObject)
        {
            properties.Add(soapObject);
            return this;
        }


        public void getPropertyInfo(int index, Dictionary<object, object> properties, PropertyInfo propertyInfo)
        {
            getPropertyInfo(index, propertyInfo);
        }


        /**
     * Places PropertyInfo of desired property into a designated PropertyInfo
     * object
     *
     * @param index
     *            index of desired property
     * @param propertyInfo
     *            designated retainer of desired property
     */

        public void getPropertyInfo(int index, PropertyInfo propertyInfo)
        {
            Object element = properties[index];
            if (element is PropertyInfo)
            {
                PropertyInfo p = (PropertyInfo) element;
                propertyInfo.name = p.name;
                propertyInfo.namespace_ = p.namespace_;
                propertyInfo.flags = p.flags;
                propertyInfo.type = p.type;
                propertyInfo.elementType = p.elementType;
                propertyInfo.value = p.value;
                propertyInfo.multiRef = p.multiRef;
            }
            else
            {
                // SoapObject
                propertyInfo.name = null;
                propertyInfo.namespace_ = null;
                propertyInfo.flags = 0;
                propertyInfo.type = null;
                propertyInfo.elementType = null;
                propertyInfo.value = element;
                propertyInfo.multiRef = false;
            }
        }

        /**
     * Creates a new SoapObject based on this, allows usage of SoapObjects as
     * templates. One application is to set the expected return type of a soap
     * call if the server does not send explicit type information.
     *
     * @return a copy of this.
     */

        public SoapObject newInstance()
        {
            SoapObject o = new SoapObject(namespace_, name);
            for (int propIndex = 0; propIndex < properties.Count; propIndex++)
            {
                Object prop = properties[propIndex];
                if (prop is PropertyInfo)
                {
                    PropertyInfo propertyInfo = (PropertyInfo) properties[propIndex];
                    PropertyInfo propertyInfoClonned = (PropertyInfo) propertyInfo.clone();
                    o.addProperty(propertyInfoClonned);
                }
                else if (prop is SoapObject)
                {
                    o.addSoapObject(((SoapObject) prop).newInstance());
                }
            }
            for (int attribIndex = 0; attribIndex < getAttributeCount(); attribIndex++)
            {
                AttributeInfo newAI = new AttributeInfo();
                getAttributeInfo(attribIndex, newAI);
                AttributeInfo attributeInfo = newAI; // (AttributeInfo)
                // attributes.elementAt(attribIndex);
                o.addAttribute(attributeInfo);
            }
            return o;
        }

        public override string ToString()
        {
            StringBuilder buf = new StringBuilder(EMPTY_STRING + name + "{");
            for (int i = 0; i < getPropertyCount(); i++)
            {
                Object prop = properties[i];
                if (prop is PropertyInfo)
                {
                    buf.Append(EMPTY_STRING)
                        .Append(((PropertyInfo) prop).getName())
                        .Append("=")
                        .Append(getProperty(i))
                        .Append("; ");
                }
                else
                {
                    buf.Append(((SoapObject) prop).ToString());
                }
            }
            buf.Append("}");
            return buf.ToString();
        }

        public string getInnerText()
        {
            return innerText;
        }

        public void setInnerText(string innerText)
        {
            this.innerText = innerText;
        }
    }

    /**
 * A class that implements only {@link NullSoapObject#toString()}.
 * This is useful in the case where you have a {@link SoapObject} representing an optional
 * property in your SOAP response.<br/><br/>
 *
 * Example:
 * <pre>
 * <code>
 * private String getAge(SoapObject person) {
 *   return person.getPropertySafely("age").toString();
 * }
 * </code>
 * </pre>
 * <ul>
 * <li> When the person object has an {@code age} property, the {@code age} will be returned. </li>
 * <li>
 *   When the person object does not have an {@code age} property,
 *   {@link SoapObject#getPropertySafely(String)}
 *   returns a NullSoapObject, which in turn returns {@code null} for {@link NullSoapObject#toString()}.
 * </li>
 * </ul>
 * Now it is safe to always try and get the {@code age} property (assuming your downstream
 * code can handle {@code age}).
 */

    public class NullSoapObject
    {
        /**
         * Overridden specifically to always return null.
         * See the example in this class's description as to how this can be useful.
         *
         * @return {@code null}
         * @see SoapObject#getPropertySafely(String)
         */

        public override string ToString()
        {
            return null;
        }
    }
}
