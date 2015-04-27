using System;
using System.IO;
using cn.noisyfox.fxml.fdom;
using cn.noisyfox.fxml.xmlpull;

namespace cn.noisyfox.fsoap
{
    public class SoapFault : IOException
    {
        /** The SOAP fault code */
        public string faultcode;
        /** The SOAP fault code */
        public string faultstring;
        /** The SOAP fault code */
        public string faultactor;
        /** A KDom Node holding the details of the fault */
        public Node detail;
        /** an integer that holds current soap version */
        public int version;

        public SoapFault()
        {
            version = SoapEnvelope.VER11;
        }

        public SoapFault(int version)
        {
            this.version = version;
        }

        /** Fills the fault details from the given XML stream */

        public virtual void parse(XmlPullParser parser)
        {
            parser.require(XmlPullParser.START_TAG, SoapEnvelope.ENV, "Fault");
            while (parser.nextTag() == XmlPullParser.START_TAG)
            {
                string name = parser.getName();
                if (name.Equals("detail"))
                {
                    detail = new Node();
                    detail.parse(parser);
                    // Handle case '...<detail/></soap:Fault>'
                    if (parser.getNamespace().Equals(SoapEnvelope.ENV) && parser.getName().Equals("Fault"))
                    {
                        break;
                    }
                    continue;
                }
                if (name.Equals("faultcode"))
                {
                    faultcode = parser.nextText();
                }
                else if (name.Equals("faultstring"))
                {
                    faultstring = parser.nextText();
                }
                else if (name.Equals("faultactor"))
                {
                    faultactor = parser.nextText();
                }
                else
                {
                    throw new Exception("unexpected tag:" + name);
                }
                parser.require(XmlPullParser.END_TAG, null, name);
            }
            parser.require(XmlPullParser.END_TAG, SoapEnvelope.ENV, "Fault");
            parser.nextTag();
        }

        /** Writes the fault to the given XML stream */

        public virtual void write(XmlSerializer xw)
        {
            xw.startTag(SoapEnvelope.ENV, "Fault");
            xw.startTag(null, "faultcode");
            xw.text("" + faultcode);
            xw.endTag(null, "faultcode");
            xw.startTag(null, "faultstring");
            xw.text("" + faultstring);
            xw.endTag(null, "faultstring");
            xw.startTag(null, "detail");
            if (detail != null)
            {
                detail.write(xw);
            }
            xw.endTag(null, "detail");
            xw.endTag(SoapEnvelope.ENV, "Fault");
        }

        /**
     * @see java.lang.Throwable#getMessage()
     */

        public override string Message
        {
            get { return faultstring; }
        }

        /** Returns a simple string representation of the fault */

        public override string ToString()
        {
            return "SoapFault - faultcode: '" + faultcode + "' faultstring: '"
                   + faultstring + "' faultactor: '" + faultactor + "' detail: " +
                   detail;
        }
    }

    public class SoapFault12 : SoapFault
    {

        /** Top-level nodes */
        public Node Code;
        public Node Reason;
        public Node Node;
        public Node Role;
        public Node Detail;

        public SoapFault12()
        {
            version = SoapEnvelope.VER12;
        }

        public SoapFault12(int version)
        {
            this.version = version;
        }

        /** Fills the fault details from the given XML stream */

        public override void parse(XmlPullParser parser)
        {
            parseSelf(parser);
            // done parsing, populate some of the legacy public members
            faultcode = Code.getElement(SoapEnvelope.ENV2003, "Value").getText(0);
            faultstring = Reason.getElement(SoapEnvelope.ENV2003, "Text").getText(0);
            detail = Detail;
            faultactor = null;
        }


        private void parseSelf(XmlPullParser parser)
        {
            parser.require(XmlPullParser.START_TAG, SoapEnvelope.ENV2003, "Fault");

            while (parser.nextTag() == XmlPullParser.START_TAG)
            {
                string name = parser.getName();
                string namespace_ = parser.getNamespace();
                parser.nextTag();
                if (name.ToLower().Equals("Code".ToLower()))
                {
                    Code = new Node();
                    Code.parse(parser);
                }
                else if (name.ToLower().Equals("Reason".ToLower()))
                {
                    Reason = new Node();
                    Reason.parse(parser);
                }
                else if (name.ToLower().Equals("Node".ToLower()))
                {
                    Node = new Node();
                    Node.parse(parser);
                }
                else if (name.ToLower().Equals("Role".ToLower()))
                {
                    Role = new Node();
                    Role.parse(parser);
                }
                else if (name.ToLower().Equals("Detail".ToLower()))
                {
                    Detail = new Node();
                    Detail.parse(parser);
                }
                else
                {
                    throw new Exception("unexpected tag:" + name);
                }

                parser.require(XmlPullParser.END_TAG, namespace_, name);
            }
            parser.require(XmlPullParser.END_TAG, SoapEnvelope.ENV2003, "Fault");
            parser.nextTag();

        }

        /** Writes the fault to the given XML stream */

        public override void write(XmlSerializer xw)
        {
            xw.startTag(SoapEnvelope.ENV2003, "Fault");
            //this.Code.write(xw);

            xw.startTag(SoapEnvelope.ENV2003, "Code");
            Code.write(xw);
            xw.endTag(SoapEnvelope.ENV2003, "Code");
            xw.startTag(SoapEnvelope.ENV2003, "Reason");
            Reason.write(xw);
            xw.endTag(SoapEnvelope.ENV2003, "Reason");

            if (Node != null)
            {
                xw.startTag(SoapEnvelope.ENV2003, "Node");
                Node.write(xw);
                xw.endTag(SoapEnvelope.ENV2003, "Node");
            }
            if (Role != null)
            {
                xw.startTag(SoapEnvelope.ENV2003, "Role");
                Role.write(xw);
                xw.endTag(SoapEnvelope.ENV2003, "Role");
            }

            if (Detail != null)
            {
                xw.startTag(SoapEnvelope.ENV2003, "Detail");
                Detail.write(xw);
                xw.endTag(SoapEnvelope.ENV2003, "Detail");
            }
            xw.endTag(SoapEnvelope.ENV2003, "Fault");
        }

        /**
 * @see java.lang.Throwable#getMessage()
 */

        public override string Message
        {
            get { return Reason.getElement(SoapEnvelope.ENV2003, "Text").getText(0); }
        }

        /** Returns a simple string representation of the fault */

        public override string ToString()
        {
            string reason = Reason.getElement(SoapEnvelope.ENV2003, "Text").getText(0);
            string code = Code.getElement(SoapEnvelope.ENV2003, "Value").getText(0);
            return "Code: " + code + ", Reason: " + reason;
        }
    }
}
