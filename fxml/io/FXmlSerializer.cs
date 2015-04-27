using System;
using System.IO;
using System.Text;
using cn.noisyfox.fxml.xmlpull;

namespace cn.noisyfox.fxml.io
{
    public class FXmlSerializer : XmlSerializer
    {
        private TextWriter writer;

        private bool pending;
        private int auto;
        private int depth;

        private string[] elementStack = new string[12];
        //nsp/prefix/name
        private int[] nspCounts = new int[4];
        private string[] nspStack = new string[8];
        //prefix/nsp; both empty are ""
        private bool[] indent = new bool[4];
        private bool unicode;
        private Encoding encoding;

        private void check(bool close)
        {
            if (!pending)
                return;

            depth++;
            pending = false;

            if (indent.Length <= depth)
            {
                bool[] hlp = new bool[depth + 4];
                Array.Copy(indent, 0, hlp, 0, depth);
                indent = hlp;
            }
            indent[depth] = indent[depth - 1];

            for (int i = nspCounts[depth - 1];
                i < nspCounts[depth];
                i++)
            {
                writer.Write(' ');
                writer.Write("xmlns");
                if (!"".Equals(nspStack[i*2]))
                {
                    writer.Write(':');
                    writer.Write(nspStack[i*2]);
                }
                else if ("".Equals(getNamespace()) && !"".Equals(nspStack[i*2 + 1]))
                    throw new InvalidOperationException("Cannot set default namespace for elements in no namespace");
                writer.Write("=\"");
                writeEscaped(nspStack[i*2 + 1], '"');
                writer.Write('"');
            }

            if (nspCounts.Length <= depth + 1)
            {
                int[] hlp = new int[depth + 8];
                Array.Copy(nspCounts, 0, hlp, 0, depth + 1);
                nspCounts = hlp;
            }

            nspCounts[depth + 1] = nspCounts[depth];
            //   nspCounts[depth + 2] = nspCounts[depth];

            writer.Write(close ? " />" : ">");
        }

        private void writeEscaped(string s, int quot)
        {

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '\n':
                    case '\r':
                    case '\t':
                        if (quot == -1)
                            writer.Write(c);
                        else
                            writer.Write("&#" + ((int) c) + ';');
                        break;
                    case '&':
                        writer.Write("&amp;");
                        break;
                    case '>':
                        writer.Write("&gt;");
                        break;
                    case '<':
                        writer.Write("&lt;");
                        break;
                    case '"':
                    case '\'':
                        if (c == quot)
                        {
                            writer.Write(
                                c == '"' ? "&quot;" : "&apos;");
                        }
                        else
                        {
                            //if(c < ' ')
                            //	throw new IllegalArgumentException("Illegal control code:"+((int) c));

                            if (c >= ' ' && c != '@' && (c < 127 || unicode))
                                writer.Write(c);
                            else
                                writer.Write("&#" + ((int) c) + ";");
                        }
                        break;
                    default:
                        //if(c < ' ')
                        //	throw new IllegalArgumentException("Illegal control code:"+((int) c));

                        if (c >= ' ' && c != '@' && (c < 127 || unicode))
                            writer.Write(c);
                        else
                            writer.Write("&#" + ((int) c) + ";");
                        break;

                }
            }
        }


        public void setFeature(string name, bool value)
        {
            if ("http://xmlpull.org/v1/doc/features.html#indent-output"
                .Equals(name))
            {
                indent[depth] = value;
            }
            else
                throw new Exception("Unsupported Feature");
        }

        public bool getFeature(string name)
        {
            //return false;
            return (
                "http://xmlpull.org/v1/doc/features.html#indent-output"
                    .Equals(
                        name))
                ? indent[depth]
                : false;
        }

        public void setProperty(string name, object value)
        {
            throw new NotImplementedException();
        }

        public object getProperty(string name)
        {
            throw new NotImplementedException();
        }

        public void setOutput(Stream os, Encoding encoding)
        {
            if (os == null)
                throw new ArgumentException();
            setOutput(
                encoding == null
                    ? new StreamWriter(os)
                    : new StreamWriter(os, encoding));
            this.encoding = encoding;
            if (encoding != null
                && encoding.WebName.ToLower().StartsWith("utf"))
                unicode = true;
        }

        public void setOutput(TextWriter writer)
        {
            this.writer = writer;

            // elementStack = new string[12]; //nsp/prefix/name
            //nspCounts = new int[4];
            //nspStack = new string[8]; //prefix/nsp
            //indent = new boolean[4];

            nspCounts[0] = 2;
            nspCounts[1] = 2;
            nspStack[0] = "";
            nspStack[1] = "";
            nspStack[2] = "xml";
            nspStack[3] = "http://www.w3.org/XML/1998/namespace";
            pending = false;
            auto = 0;
            depth = 0;

            unicode = false;
        }

        public void startDocument(Encoding encoding, bool? standalone)
        {
            writer.Write("<?xml version='1.0' ");

            if (encoding != null)
            {
                this.encoding = encoding;
                if (encoding.WebName.ToLower().StartsWith("utf"))
                    unicode = true;
            }

            if (this.encoding != null)
            {
                writer.Write("encoding='");
                writer.Write(this.encoding.WebName);
                writer.Write("' ");
            }

            if (standalone != null)
            {
                writer.Write("standalone='");
                writer.Write(
                    standalone.Value ? "yes" : "no");
                writer.Write("' ");
            }
            writer.Write("?>");
        }

        public void endDocument()
        {
            while (depth > 0)
            {
                endTag(
                    elementStack[depth*3 - 3],
                    elementStack[depth*3 - 1]);
            }
            flush();
        }

        public void setPrefix(string prefix, string namespace_)
        {

            check(false);
            if (prefix == null)
                prefix = "";
            if (namespace_ == null)
                namespace_ = "";

            string defined = getPrefix(namespace_,
                true,
                false);

            // boil out if already defined

            if (prefix.Equals(defined))
                return;

            int pos = (nspCounts[depth + 1]++) << 1;

            if (nspStack.Length < pos + 1)
            {
                string[] hlp = new string[nspStack.Length + 16];
                Array.Copy(nspStack, 0, hlp, 0, pos);
                nspStack = hlp;
            }

            nspStack[pos++] = prefix;
            nspStack[pos] = namespace_;
        }

        public string getPrefix(string namespace_, bool create)
        {
            return getPrefix(namespace_, false, create);
        }

        private string getPrefix(
            string _namespace,
            bool includeDefault,
            bool create)
        {

            for (int i = nspCounts[depth + 1]*2 - 2;
                i >= 0;
                i -= 2)
            {
                if (nspStack[i + 1].Equals(_namespace)
                    && (includeDefault || !nspStack[i].Equals("")))
                {
                    string cand = nspStack[i];
                    for (int j = i + 2;
                        j < nspCounts[depth + 1]*2;
                        j++)
                    {
                        if (nspStack[j].Equals(cand))
                        {
                            cand = null;
                            break;
                        }
                    }
                    if (cand != null)
                        return cand;
                }
            }

            if (!create)
                return null;

            string prefix;

            if ("".Equals(_namespace))
                prefix = "";
            else
            {
                do
                {
                    prefix = "n" + (auto++);
                    for (int i = nspCounts[depth + 1]*2 - 2;
                        i >= 0;
                        i -= 2)
                    {
                        if (prefix.Equals(nspStack[i]))
                        {
                            prefix = null;
                            break;
                        }
                    }
                } while (prefix == null);
            }

            bool p = pending;
            pending = false;
            setPrefix(prefix, _namespace);
            pending = p;
            return prefix;
        }

        public int getDepth()
        {
            return pending ? depth + 1 : depth;
        }

        public string getNamespace()
        {
            return getDepth() == 0 ? null : elementStack[getDepth()*3 - 3];
        }

        public string getName()
        {
            return getDepth() == 0 ? null : elementStack[getDepth()*3 - 1];
        }

        public XmlSerializer startTag(string namespace_, string name)
        {
            check(false);

            //        if (namespace == null)
            //            namespace = "";

            if (indent[depth])
            {
                writer.Write("\r\n");
                for (int i = 0; i < depth; i++)
                    writer.Write("  ");
            }

            int esp = depth*3;

            if (elementStack.Length < esp + 3)
            {
                string[] hlp = new string[elementStack.Length + 12];
                Array.Copy(elementStack, 0, hlp, 0, esp);
                elementStack = hlp;
            }

            string prefix =
                namespace_ == null
                    ? ""
                    : getPrefix(namespace_,
                        true,
                        true)
                ;

            if ("".Equals(namespace_))
            {
                for (int i = nspCounts[depth];
                    i < nspCounts[depth + 1];
                    i++)
                {
                    if ("".Equals(nspStack[i*2]) && !"".Equals(nspStack[i*2 + 1]))
                    {
                        throw new InvalidOperationException("Cannot set default namespace for elements in no namespace");
                    }
                }
            }

            elementStack[esp++] = namespace_;
            elementStack[esp++] = prefix;
            elementStack[esp] = name;

            writer.Write('<');
            if (!"".Equals(prefix))
            {
                writer.Write(prefix);
                writer.Write(':');
            }

            writer.Write(name);

            pending = true;

            return this;
        }

        public XmlSerializer attribute(string namespace_, string name, string value)
        {
            if (!pending)
                throw new InvalidOperationException("illegal position for attribute");

            //        int cnt = nspCounts[depth];

            if (namespace_ == null)
                namespace_ = "";

            //		depth--;
            //		pending = false;

            string prefix =
                "".Equals(namespace_)
                    ? ""
                    : getPrefix(namespace_, false, true);

            //		pending = true;
            //		depth++;

            /*        if (cnt != nspCounts[depth]) {
                    writer.write(' ');
                    writer.write("xmlns");
                    if (nspStack[cnt * 2] != null) {
                        writer.write(':');
                        writer.write(nspStack[cnt * 2]);
                    }
                    writer.write("=\"");
                    writeEscaped(nspStack[cnt * 2 + 1], '"');
                    writer.write('"');
                }
                */

            writer.Write(' ');
            if (!"".Equals(prefix))
            {
                writer.Write(prefix);
                writer.Write(':');
            }
            writer.Write(name);
            writer.Write('=');
            char q = value.IndexOf('"') == -1 ? '"' : '\'';
            writer.Write(q);
            writeEscaped(value, q);
            writer.Write(q);

            return this;
        }

        public XmlSerializer endTag(string namespace_, string name)
        {
            if (!pending)
                depth--;
            //        if (namespace == null)
            //          namespace = "";

            if ((namespace_ == null
                 && elementStack[depth*3] != null)
                || (namespace_ != null
                    && !namespace_.Equals(elementStack[depth*3]))
                || !elementStack[depth*3 + 2].Equals(name))
                throw new ArgumentException("</{" + namespace_ + "}" + name + "> does not match start");

            if (pending)
            {
                check(true);
                depth--;
            }
            else
            {
                if (indent[depth + 1])
                {
                    writer.Write("\r\n");
                    for (int i = 0; i < depth; i++)
                        writer.Write("  ");
                }

                writer.Write("</");
                string prefix = elementStack[depth*3 + 1];
                if (!"".Equals(prefix))
                {
                    writer.Write(prefix);
                    writer.Write(':');
                }
                writer.Write(name);
                writer.Write('>');
            }

            nspCounts[depth + 1] = nspCounts[depth];
            return this;
        }

        public XmlSerializer startTag(string prefix, string namespace_, string name)
        {
            throw new NotImplementedException();
        }

        public XmlSerializer attribute(string prefix, string namespace_, string name, string value)
        {
            throw new NotImplementedException();
        }

        public XmlSerializer endTag(string prefix, string namespace_, string name)
        {
            throw new NotImplementedException();
        }

        public XmlSerializer text(string text)
        {
            check(false);
            indent[depth] = false;
            writeEscaped(text, -1);
            return this;
        }

        public XmlSerializer text(char[] buf, int start, int len)
        {
            return text(new string(buf, start, len));
        }

        public void cdsect(string data)
        {
            check(false);
            writer.Write("<![CDATA[");
            writer.Write(data);
            writer.Write("]]>");
        }

        public void entityRef(string name)
        {
            check(false);
            writer.Write('&');
            writer.Write(name);
            writer.Write(';');
        }

        public void processingInstruction(string pi)
        {
            check(false);
            writer.Write("<?");
            writer.Write(pi);
            writer.Write("?>");
        }

        public void comment(string text)
        {
            check(false);
            writer.Write("<!--");
            writer.Write(text);
            writer.Write("-->");
        }

        public void docdecl(string dd)
        {
            writer.Write("<!DOCTYPE");
            writer.Write(dd);
            writer.Write(">");
        }

        public void ignorableWhitespace(string s)
        {
            text(s);
        }

        public void flush()
        {
            check(false);
            writer.Flush();
        }
    }
}
