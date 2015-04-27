using System;
using System.Collections.Generic;
using cn.noisyfox.fxml.xmlpull;

namespace cn.noisyfox.fxml.fdom
{
    public class Element : Node
    {
        protected internal string namespace_;
        protected internal string name;
        protected internal List<object> attributes;
        protected internal Node parent;
        protected internal List<object> prefixes;


        /** 
     * called when all properties are set, but before children
     * are parsed. Please do not use setParent for initialization
     * code any longer. */

        public void init()
        {
        }




        /** 
     * removes all children and attributes */

        public void clear()
        {
            attributes = null;
            children = null;
        }

        /** 
     * Forwards creation request to parent if any, otherwise
     * calls super.createElement. */

        public override Element createElement(
            string namespace_,
            string name)
        {

            return (parent == null)
                ? base.createElement(namespace_,
                    name)
                : parent.createElement(namespace_,
                    name)
                ;
        }

        /** 
     * Returns the number of attributes of this element. */

        public int getAttributeCount()
        {
            return attributes == null ? 0 : attributes.Count;
        }

        public string getAttributeNamespace(int index)
        {
            return ((string[]) attributes[index])[0];
        }

/*	public String getAttributePrefix (int index) {
		return ((String []) attributes.elementAt (index)) [1];
	}*/

        public string getAttributeName(int index)
        {
            return ((string[]) attributes[index])[1];
        }


        public string getAttributeValue(int index)
        {
            return ((string[]) attributes[index])[2];
        }


        public string getAttributeValue(string namespace_, string name)
        {
            for (int i = 0; i < getAttributeCount(); i++)
            {
                if (name.Equals(getAttributeName(i))
                    && (namespace_ ==
                        null || namespace_.
                            Equals(getAttributeNamespace(i))))
                {
                    return getAttributeValue(i);
                }
            }
            return null;
        }

        /** 
     * Returns the root node, determined by ascending to the 
     * all parents un of the root element. */

        public Node getRoot()
        {

            Element current = this;

            while (current.parent != null)
            {
                if (!(current.parent
                    is Element))
                    return current.parent;
                current = (Element) current.parent;
            }

            return current;
        }

        /** 
     * returns the (local) name of the element */

        public string getName()
        {
            return name;
        }

        /** 
     * returns the namespace of the element */

        public string getNamespace()
        {
            return namespace_
                ;
        }


        /** 
     * returns the namespace for the given prefix */

        public string getNamespaceUri(string prefix)
        {
            int cnt = getNamespaceCount();
            for (int i = 0; i < cnt; i++)
            {
                if (prefix == getNamespacePrefix(i) ||
                    (prefix != null && prefix.Equals(getNamespacePrefix(i))))
                    return getNamespaceUri(i);
            }
            return parent
                is
                Element
                ? ((Element) parent).getNamespaceUri(prefix)
                : null;
        }


        /** 
     * returns the number of declared namespaces, NOT including
	 * parent elements */

        public int getNamespaceCount()
        {
            return (prefixes == null ? 0 : prefixes.Count);
        }


        public string getNamespacePrefix(int i)
        {
            return ((string[]) prefixes[i])[0];
        }

        public string getNamespaceUri(int i)
        {
            return ((string[]) prefixes[i])[1];
        }


        /** 
     * Returns the parent node of this element */

        public Node getParent()
        {
            return parent;
        }

        /* 
     * Returns the parent element if available, null otherwise 

    public Element getParentElement() {
        return (parent instanceof Element)
            ? ((Element) parent)
            : null;
    }
*/

        /** 
     * Builds the child elements from the given Parser. By overwriting 
     * parse, an element can take complete control over parsing its 
     * subtree. */

        public override void parse(XmlPullParser parser)
        {

            for (int i = parser.getNamespaceCount(parser.getDepth() - 1);
                i < parser.getNamespaceCount(parser.getDepth());
                i++)
            {
                setPrefix(parser.getNamespacePrefix(i), parser.getNamespaceUri(i));
            }


            for (int i = 0; i < parser.getAttributeCount(); i++)
                setAttribute(parser.getAttributeNamespace(i),
//	        			  parser.getAttributePrefix (i),
                    parser.getAttributeName(i),
                    parser.getAttributeValue(i));


            //        if (prefixMap == null) throw new RuntimeException ("!!");

            init();


            if (parser.isEmptyElementTag())
                parser.nextToken();
            else
            {
                parser.nextToken();
                base.parse(parser);

                if (getChildCount() == 0)
                    addChild(IGNORABLE_WHITESPACE, "");
            }

            parser.require(
                XmlPullParser.END_TAG,
                getNamespace(),
                getName());

            parser.nextToken();
        }


        /** 
     * Sets the given attribute; a value of null removes the attribute */

        public void setAttribute(string namespace_, string name, string value)
        {
            if (attributes == null)
                attributes = new List<object>();

            if (namespace_ ==
                null)
                namespace_ =
                    "";

            for (int i = attributes.Count - 1; i >= 0; i--)
            {
                string[] attribut = (string[]) attributes[i];
                if (attribut[0].Equals(namespace_) &&
                    attribut[1].Equals(name))
                {

                    if (value == null)
                    {
                        attributes.RemoveAt(i);
                    }
                    else
                    {
                        attribut[2] = value;
                    }
                    return;
                }
            }

            attributes.Add
                (new string[]
                {
                    namespace_, name, value
                }
                )
                ;
        }


        /** 
     * Sets the given prefix; a namespace value of null removess the 
	 * prefix */

        public void setPrefix(string prefix, string namespace_)
        {
            if (prefixes == null) prefixes = new List<object>();
            prefixes.Add(new string[]
            {
                prefix, namespace_
            }
                )
                ;
        }


        /** 
     * sets the name of the element */

        public void setName(string name)
        {
            this.name = name;
        }

        /** 
     * sets the namespace of the element. Please note: For no
     * namespace, please use Xml.NO_NAMESPACE, null is not a legal
     * value. Currently, null is converted to Xml.NO_NAMESPACE, but
     * future versions may throw an exception. */

        public void setNamespace(string namespace_)
        {
            if (namespace_ ==
                null)
                throw new NotSupportedException("Use \"\" for empty namespace");
            this.namespace_ = namespace_
                ;
        }

        /** 
     * Sets the Parent of this element. Automatically called from the
     * add method.  Please use with care, you can simply
     * create inconsitencies in the document tree structure using
     * this method!  */

        protected internal void setParent(Node parent)
        {
            this.parent = parent;
        }


        /** 
     * Writes this element and all children to the given XmlWriter. */

        public override void write(XmlSerializer writer)
        {

            if (prefixes != null)
            {
                for (int i = 0; i < prefixes.Count; i++)
                {
                    writer.setPrefix(getNamespacePrefix(i), getNamespaceUri(i));
                }
            }

            writer.startTag(
                getNamespace(),
                getName());

            int len = getAttributeCount();

            for (int i = 0; i < len; i++)
            {
                writer.attribute(
                    getAttributeNamespace(i),
                    getAttributeName(i),
                    getAttributeValue(i));
            }

            writeChildren(writer);

            writer.endTag(getNamespace(), getName());
        }
    }
}
