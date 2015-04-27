using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cn.noisyfox.fsoap.serialization
{
    public class AttributeContainer : HasAttributes
    {
        protected List<object> attributes = new List<object>();

        public void getAttributeInfo(int index, AttributeInfo attributeInfo)
        {
            AttributeInfo p = (AttributeInfo) attributes[index];
            attributeInfo.name = p.name;
            attributeInfo.namespace_ = p.namespace_;
            attributeInfo.flags = p.flags;
            attributeInfo.type = p.type;
            attributeInfo.elementType = p.elementType;
            attributeInfo.value = p.getValue();
        }


        /**
         * Get the attribute at the given index
         */

        public Object getAttribute(int index)
        {
            return ((AttributeInfo) attributes[index]).getValue();
        }


        /**
     * Get the attribute's toString value.
     */

        public String getAttributeAsString(int index)
        {
            AttributeInfo attributeInfo = (AttributeInfo) attributes[index];
            return attributeInfo.getValue().ToString();
        }

        /**
     * Get the attribute with the given name
     *
     * @throws RuntimeException if the attribute does not exist
     */

        public Object getAttribute(String name)
        {
            int? i = attributeIndex(name);
            if (i != null)
            {
                return getAttribute(i.Value);
            }
            else
            {
                throw new Exception("illegal property: " + name);
            }
        }

        /**
     * Get the attribute with the given name
     *
     * @throws RuntimeException if the attribute does not exist
     */

        public Object getAttribute(String namespace_, String name)
        {
            int? i = attributeIndex(namespace_, name);
            if (i != null)
            {
                return getAttribute(i.Value);
            }
            else
            {
                throw new Exception("illegal property: " + name);
            }
        }

        /**
     * Get the toString value of the attribute with the given name.
     *
     * @throws RuntimeException if the attribute does not exist
     */

        public String getAttributeAsString(String name)
        {
            int? i = attributeIndex(name);
            if (i != null)
            {
                return getAttribute(i.Value).ToString();
            }
            else
            {
                throw new Exception("illegal property: " + name);
            }
        }

        /**
     * Get the toString value of the attribute with the given name.
     *
     * @throws RuntimeException if the attribute does not exist
     */

        public String getAttributeAsString(String namespace_, String name)
        {
            int? i = attributeIndex(namespace_, name);
            if (i != null)
            {
                return getAttribute(i.Value).ToString();
            }
            else
            {
                throw new Exception("illegal property: " + name);
            }
        }

        /**
     * Knows whether the given attribute exists
     */

        public bool hasAttribute(String name)
        {
            if (attributeIndex(name) != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /**
     * Knows whether the given attribute exists
     */

        public bool hasAttribute(String namespace_, String name)
        {
            if (attributeIndex(namespace_, name) != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /**
     * Get an attribute without chance of throwing an exception
     *
     * @param name the name of the attribute to retrieve
     * @return the value of the attribute if it exists; {@code null} if it does not exist
     */

        public Object getAttributeSafely(String name)
        {
            int? i = attributeIndex(name);
            if (i != null)
            {
                return getAttribute(i.Value);
            }
            else
            {
                return null;
            }
        }

        /**
     * Get an attribute without chance of throwing an exception
     *
     * @param name the name of the attribute to retrieve
     * @return the value of the attribute if it exists; {@code null} if it does not exist
     */

        public Object getAttributeSafely(String namespace_, String name)
        {
            int? i = attributeIndex(namespace_, name);
            if (i != null)
            {
                return getAttribute(i.Value);
            }
            else
            {
                return null;
            }
        }

        /**
     * Get an attributes' toString value without chance of throwing an
     * exception.

     * @param name
     * @return the value of the attribute,s toString method if it exists; ""
     * if it does not exist
     */

        public Object getAttributeSafelyAsString(String name)
        {
            int? i = attributeIndex(name);
            if (i != null)
            {
                return getAttribute(i.Value).ToString();
            }
            else
            {
                return "";
            }
        }

        /**
     * Get an attributes' toString value without chance of throwing an
     * exception.

     * @param name
     * @return the value of the attribute,s toString method if it exists; ""
     * if it does not exist
     */

        public Object getAttributeSafelyAsString(String namespace_, String name)
        {
            int? i = attributeIndex(namespace_, name);
            if (i != null)
            {
                return getAttribute(i.Value).ToString();
            }
            else
            {
                return "";
            }
        }

        private int? attributeIndex(String name)
        {
            for (int i = 0; i < attributes.Count; i++)
            {
                if (name.Equals(((AttributeInfo) attributes[i]).getName()))
                {
                    return i;
                }
            }
            return null;
        }

        private int? attributeIndex(String namespace_, String name)
        {
            for (int i = 0; i < attributes.Count; i++)
            {
                AttributeInfo attrInfo = (AttributeInfo) attributes[i];
                if (name.Equals(attrInfo.getName()) && namespace_.Equals(attrInfo.getNamespace()))
                {
                    return i;
                }
            }
            return null;
        }

        /**
     * Returns the number of attributes
     *
     * @return the number of attributes
     */

        public int getAttributeCount()
        {
            return attributes.Count;
        }

        /**
     * Checks that the two objects have identical sets of attributes.
     *
     * @param other
     * @return {@code true} of the attrubte sets are equal, {@code false} otherwise.
     */

        protected bool attributesAreEqual(AttributeContainer other)
        {
            int numAttributes = getAttributeCount();
            if (numAttributes != other.getAttributeCount())
            {
                return false;
            }

            for (int attribIndex = 0; attribIndex < numAttributes; attribIndex++)
            {
                AttributeInfo thisAttrib = (AttributeInfo) this.attributes[attribIndex];
                Object thisAttribValue = thisAttrib.getValue();
                if (!other.hasAttribute(thisAttrib.getName()))
                {
                    return false;
                }
                Object otherAttribValue = other.getAttributeSafely(thisAttrib.getName());
                if (!thisAttribValue.Equals(otherAttribValue))
                {
                    return false;
                }
            }
            return true;
        }

        /**
     * Adds a attribute (parameter) to the object.
     *
     * @param name  The name of the attribute
     * @param value the value of the attribute
     * @return {@code this} object.
     */

        public void addAttribute(String name, Object value)
        {
            addAttribute(null, name, value);
        }

        /**
     * Adds a attribute (parameter) to the object.
     *
     * @param namespace  The namespace of the attribute
     * @param name  The name of the attribute
     * @param value the value of the attribute
     * @return {@code this} object.
     */

        public void addAttribute(String namespace_, String name, Object value)
        {
            AttributeInfo attributeInfo = new AttributeInfo();
            attributeInfo.name = name;
            attributeInfo.namespace_ = namespace_;
            attributeInfo.type = value == null ? PropertyInfo.OBJECT_CLASS : value.GetType();
            attributeInfo.value = value;
            addAttribute(attributeInfo);
        }

        /**
     * Add an attribute if the value is not null.
     * @param name
     * @param value
     */

        public void addAttributeIfValue(String name, Object value)
        {
            if (value != null)
            {
                addAttribute(name, value);
            }
        }

        /**
     * Add an attribute if the value is not null.
     * @param namespace  The namespace of the attribute
     * @param name
     * @param value
     */

        public void addAttributeIfValue(String namespace_, String name, Object value)
        {
            if (value != null)
            {
                addAttribute(namespace_, name, value);
            }
        }

        /**
     * Add a new attribute by providing an {@link AttributeInfo} object.  {@code AttributeInfo}
     * contains all data about the attribute, including name and value.}
     *
     * @param attributeInfo the {@code AttributeInfo} object to add.
     * @return {@code this} object.
     */

        public void addAttribute(AttributeInfo attributeInfo)
        {
            attributes.Add(attributeInfo);
        }

        /**
     * Add an attributeInfo if its value is not null.
     * @param attributeInfo
     */

        public void addAttributeIfValue(AttributeInfo attributeInfo)
        {
            if (attributeInfo.value != null)
            {
                attributes.Add(attributeInfo);
            }
        }

        public void getAttribute(int index, AttributeInfo info)
        {
            throw new NotImplementedException();
        }

        public void setAttribute(AttributeInfo info)
        {
            throw new NotImplementedException();
        }
    }
}
