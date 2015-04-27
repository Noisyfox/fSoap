using System;

namespace cn.noisyfox.fxml.xmlpull
{
    public class XmlPullParserException : Exception
    {
        protected Exception detail;
        protected int row = -1;
        protected int column = -1;

        public XmlPullParserException(string s) : base(s)
        {
        }

        public XmlPullParserException(string msg, XmlPullParser parser, Exception chain)
            : base((msg == null ? "" : msg + " ")
                + (parser == null ? "" : "(position:" + parser.getPositionDescription() + ") ")
                + (chain == null ? "" : "caused by: " + chain))
        {
            if (parser != null)
            {
                row = parser.getLineNumber();
                column = parser.getColumnNumber();
            }
            detail = chain;
        }

        public Exception GetDetail {
            get { return detail; }
        }

        public int GetLineNumber { get { return row;} }

        public int GetColumnNumber { get { return column; } }

        public override string StackTrace {
            get {
                if (detail == null)
                {
                    return base.StackTrace;
                }
                else
                {
                    return Message + "; nested exception is:" + detail.StackTrace;
                }
            }
        }
    }
}
