using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cn.noisyfox.fxml.xmlpull;

namespace cn.noisyfox.fxml.fdom
{
    public class Node
    {
        public const int DOCUMENT = 0;
        public const int ELEMENT = 2;
        public const int TEXT = 4;
        public const int CDSECT = 5;
        public const int ENTITY_REF = 6;
        public const int IGNORABLE_WHITESPACE = 7;
        public const int PROCESSING_INSTRUCTION = 8;
        public const int COMMENT = 9;
        public const int DOCDECL = 10;

        protected internal List<object> children;
        protected internal StringBuilder types;

        /** inserts the given child object of the given type at the
    given index. */

        public void addChild(int index, int type, object child)
        {

            if (child == null)
                throw new NullReferenceException();

            if (children == null)
            {
                children = new List<object>();
                types = new StringBuilder();
            }

            if (type == ELEMENT)
            {
                if (!(child
                    is Element))
                    throw new Exception("Element obj expected)");

                ((Element) child).setParent(this);
            }
            else if (!(child
                is string))
                throw new Exception("String expected");

            children.Insert(index, child);
            types.Insert(index, (char) type);
        }

        /** convenience method for addChild (getChildCount (), child) */

        public void addChild(int type, object child)
        {
            addChild(getChildCount(), type, child);
        }

        /** Builds a default element with the given properties. Elements
    should always be created using this method instead of the
    constructor in order to enable construction of specialized
    subclasses by deriving custom Document classes. Please note:
    For no namespace, please use Xml.NO_NAMESPACE, null is not a
    legal value. Currently, null is converted to Xml.NO_NAMESPACE,
    but future versions may throw an exception. */

        public virtual Element createElement(string namespace_, string name)
        {

            Element e = new Element();
            e.namespace_ = namespace_ ?? "";
            e.name = name;
            return e;
        }

        /** Returns the child object at the given index.  For child
        elements, an Element object is returned. For all other child
        types, a String is returned. */

        public object getChild(int index)
        {
            return children.ElementAt(index);
        }

        /** Returns the number of child objects */

        public int getChildCount()
        {
            return children == null ? 0 : children.Count;
        }

        /** returns the element at the given index. If the node at the
    given index is a text node, null is returned */

        public Element getElement(int index)
        {
            object child = getChild(index);
            return (child
                is Element)
                ? (Element) child
                : null;
        }

        /** Returns the element with the given namespace and name. If the
        element is not found, or more than one matching elements are
        found, an exception is thrown. */

        public Element getElement(string namespace_, string name)
        {

            int i = indexOf(namespace_,
                name,

                0)
                ;
            int j = indexOf(namespace_, name, i
                                              + 1)
                ;

            if (i == -1 || j != -1)
                throw new Exception(
                    "Element {"
                    + namespace_
                    + "}"
                    + name
                    + (i == -1 ? " not found in " : " more than once in ")
                    + this)
                    ;

            return getElement(i);
        }

        /* returns "#document-fragment". For elements, the element name is returned 
    
    public String getName() {
        return "#document-fragment";
    }
    
    /** Returns the namespace of the current element. For Node
        and Document, Xml.NO_NAMESPACE is returned. 
    
    public String getNamespace() {
        return "";
    }
    
    public int getNamespaceCount () {
    	return 0;
    }
    
    /** returns the text content if the element has text-only
    content. Throws an exception for mixed content
    
    public String getText() {
    
        StringBuffer buf = new StringBuffer();
        int len = getChildCount();
    
        for (int i = 0; i < len; i++) {
            if (isText(i))
                buf.append(getText(i));
            else if (getType(i) == ELEMENT)
                throw new RuntimeException("not text-only content!");
        }
    
        return buf.toString();
    }
    */

        /** Returns the text node with the given index or null if the node
        with the given index is not a text node. */

        public string getText(int index)
        {
            return (isText(index)) ? (string) getChild(index) : null;
        }

        /** Returns the type of the child at the given index. Possible 
    types are ELEMENT, TEXT, COMMENT, and PROCESSING_INSTRUCTION */

        public int getType(int index)
        {
            return types[index];
        }

        /** Convenience method for indexOf (getNamespace (), name,
        startIndex). 
    
    public int indexOf(String name, int startIndex) {
        return indexOf(getNamespace(), name, startIndex);
    }
    */

        /** Performs search for an element with the given namespace and
    name, starting at the given start index. A null namespace
    matches any namespace, please use Xml.NO_NAMESPACE for no
    namespace).  returns -1 if no matching element was found. */

        public int indexOf(string namespace_, string name, int startIndex)
        {

            int len = getChildCount();

            for (int i = startIndex; i < len; i++)
            {

                Element child = getElement(i);

                if (child != null
                    && name.Equals(child.getName())
                    && (namespace_ ==
                        null || namespace_.
                            Equals(child.getNamespace())))
                    return i;
            }
            return -1;
        }

        public bool isText(int i)
        {
            int t = getType(i);
            return t == TEXT || t == IGNORABLE_WHITESPACE || t == CDSECT;
        }

        /** Recursively builds the child elements from the given parser
    until an end tag or end document is found. 
        The end tag is not consumed. */

        public virtual void parse(XmlPullParser parser)
        {

            bool leave = false;

            do
            {
                int type = parser.getEventType();

                //         System.out.println(parser.getPositionDescription());

                switch (type)
                {

                    case XmlPullParser.START_TAG:
                    {
                        Element child =
                            createElement(
                                parser.getNamespace(),
                                parser.getName());
                        //    child.setAttributes (event.getAttributes ());
                        addChild(ELEMENT, child);

                        // order is important here since 
                        // setparent may perform some init code!

                        child.parse(parser);
                        break;
                    }

                    case XmlPullParser.END_DOCUMENT:
                    case XmlPullParser.END_TAG:
                        leave = true;
                        break;

                    default:
                        if (parser.getText() != null)
                            addChild(
                                type == XmlPullParser.ENTITY_REF ? TEXT : type,
                                parser.getText());
                        else if (
                            type == XmlPullParser.ENTITY_REF
                            && parser.getName() != null)
                        {
                            addChild(ENTITY_REF, parser.getName());
                        }
                        parser.nextToken();
                        break;
                }
            } while (!leave);
        }

        /** Removes the child object at the given index */

        public void removeChild(int idx)
        {
            children.RemoveAt(idx);

            /***  Modification by HHS - start ***/
            //      types.deleteCharAt (index);
            /***/
            int n = types.Length - 1;

            for (int i = idx; i < n; i++)
                types[i] = types[i + 1];

            types.Length = n;

            /***  Modification by HHS - end   ***/
        }

        /* returns a valid XML representation of this Element including
    	attributes and children. 
    public String toString() {
        try {
            ByteArrayOutputStream bos =
                new ByteArrayOutputStream();
            XmlWriter xw =
                new XmlWriter(new OutputStreamWriter(bos));
            write(xw);
            xw.close();
            return new String(bos.toByteArray());
        }
        catch (IOException e) {
            throw new RuntimeException(e.toString());
        }
    }
    */

        /** Writes this node to the given XmlWriter. For node and document,
        this method is identical to writeChildren, except that the
        stream is flushed automatically. */

        public virtual void write(XmlSerializer writer)
        {
            writeChildren(writer);
            writer.flush();
        }

        /** Writes the children of this node to the given XmlWriter. */

        public void writeChildren(XmlSerializer writer)
        {
            if (children == null)
                return;

            int len = children.Count;

            for (int i = 0; i < len; i++)
            {
                int type = getType(i);
                object child = children[i];
                switch (type)
                {
                    case ELEMENT:
                        ((Element) child).write(writer);
                        break;

                    case TEXT:
                        writer.text((string) child);
                        break;

                    case IGNORABLE_WHITESPACE:
                        writer.ignorableWhitespace((string) child);
                        break;

                    case CDSECT:
                        writer.cdsect((string) child);
                        break;

                    case COMMENT:
                        writer.comment((string) child);
                        break;

                    case ENTITY_REF:
                        writer.entityRef((string) child);
                        break;

                    case PROCESSING_INSTRUCTION:
                        writer.processingInstruction((string) child);
                        break;

                    case DOCDECL:
                        writer.docdecl((string) child);
                        break;

                    default:
                        throw new Exception("Illegal type: " + type);
                }
            }

        }
    }

}
