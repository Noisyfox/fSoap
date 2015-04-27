using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using cn.noisyfox.fxml.xmlpull;

namespace cn.noisyfox.fxml.io
{
    public class FXmlParser : XmlPullParser
    {
        private object location;
        private static readonly string UNEXPECTED_EOF = "Unexpected EOF";
        private static readonly string ILLEGAL_TYPE = "Wrong event type";
        private static readonly int LEGACY = 999;
        private static readonly int XML_DECL = 998;

        // general

        private string version;
        private bool? standalone;

        private bool processNsp;
        private bool relaxed;
        private Dictionary<object, object> entityMap;
        private int depth;
        private string[] elementStack = new string[16];
        private string[] nspStack = new string[8];
        private int[] nspCounts = new int[4];

        // source

        private TextReader reader;
        private string encoding;
        private char[] srcBuf;

        private int srcPos;
        private int srcCount;

        private int line;
        private int column;

        // txtbuffer

        /** Target buffer for storing incoming text (including aggregated resolved entities) */
        private char[] txtBuf = new char[128];
        /** Write position  */
        private int txtPos;

        // Event-related

        private int type;
        private bool _isWhitespace;
        private string namespace_;
        private string _prefix;
        private string name;

        private bool degenerated;
        private int attributeCount;
        private string[] attributes = new string[16];
//    private int stackMismatch = 0;
        private string _error;

        /** 
     * A separate peek buffer seems simpler than managing
     * wrap around in the first level read buffer */

        private int[] _peek = new int[2];
        private int peekCount;
        private bool wasCR;

        private bool unresolved;
        private bool token;

        public FXmlParser()
        {
            srcBuf = new char[8192];
        }

        private bool isProp(string n1, bool prop, string n2)
        {
            if (!n1.StartsWith("http://xmlpull.org/v1/doc/"))
                return false;
            if (prop)
                return n1.Substring(42).Equals(n2);
            else
                return n1.Substring(40).Equals(n2);
        }

        private bool adjustNsp()
        {

            bool any = false;
            int cut = 0;

            for (int i = 0; i < attributeCount << 2; i += 4)
            {
                // * 4 - 4; i >= 0; i -= 4) {

                string attrName = attributes[i + 2];
                cut = attrName.IndexOf(':');
                string prefix;

                if (cut != -1)
                {
                    prefix = attrName.Substring(0, cut);
                    attrName = attrName.Substring(cut + 1);
                }
                else if (attrName.Equals("xmlns"))
                {
                    prefix = attrName;
                    attrName = null;
                }
                else
                    continue;

                if (!prefix.Equals("xmlns"))
                {
                    any = true;
                }
                else
                {
                    int j = (nspCounts[depth]++) << 1;

                    nspStack = ensureCapacity(nspStack, j + 2);
                    nspStack[j] = attrName;
                    nspStack[j + 1] = attributes[i + 3];

                    if (attrName != null && attributes[i + 3].Equals(""))
                        error("illegal empty namespace");

                    //  prefixMap = new PrefixMap (prefixMap, attrName, attr.getValue ());

                    //System.out.println (prefixMap);

                    Array.Copy(
                        attributes,
                        i + 4,
                        attributes,
                        i,
                        ((--attributeCount) << 2) - i);

                    i -= 4;
                }
            }

            if (any)
            {
                for (int i = (attributeCount << 2) - 4; i >= 0; i -= 4)
                {

                    string attrName = attributes[i + 2];
                    cut = attrName.IndexOf(':');

                    if (cut == 0 && !relaxed)
                        throw new Exception(
                            "illegal attribute name: " + attrName + " at " + this);

                    else if (cut != -1)
                    {
                        string attrPrefix = attrName.Substring(0, cut);

                        attrName = attrName.Substring(cut + 1);

                        string attrNs = getNamespace(attrPrefix);

                        if (attrNs == null && !relaxed)
                            throw new Exception(
                                "Undefined Prefix: " + attrPrefix + " in " + this);

                        attributes[i] = attrNs;
                        attributes[i + 1] = attrPrefix;
                        attributes[i + 2] = attrName;

                        /*
                                        if (!relaxed) {
                                            for (int j = (attributeCount << 2) - 4; j > i; j -= 4)
                                                if (attrName.Equals(attributes[j + 2])
                                                    && attrNs.Equals(attributes[j]))
                                                    exception(
                                                        "Duplicate Attribute: {"
                                                            + attrNs
                                                            + "}"
                                                            + attrName);
                                        }
                        */
                    }
                }
            }

            cut = name.IndexOf(':');

            if (cut == 0)
                error("illegal tag name: " + name);

            if (cut != -1)
            {
                _prefix = name.Substring(0, cut);
                name = name.Substring(cut + 1);
            }

            namespace_ = getNamespace(_prefix);

            if (namespace_ == null)
            {
                if (_prefix != null)
                    error("undefined prefix: " + _prefix);
                namespace_ = NO_NAMESPACE;
            }

            return any;
        }

        private string[] ensureCapacity(string[] arr, int required)
        {
            if (arr.Length >= required)
                return arr;
            string[] bigger = new string[required + 16];
            Array.Copy(arr, 0, bigger, 0, arr.Length);
            return bigger;
        }

        private void error(string desc)
        {
            if (relaxed)
            {
                if (_error == null)
                    _error = "ERR: " + desc;
            }
            else
                exception(desc);
        }

        private void exception(string desc)
        {
            throw new XmlPullParserException(
                desc.Length < 100 ? desc : desc.Substring(0, 100) + "\n",
                this,
                null);
        }

        /** 
     * common base for next and nextToken. Clears the state, except from 
     * txtPos and whitespace. Does not set the type variable */

        private void nextImpl()
        {

            if (reader == null)
                exception("No Input specified");

            if (type == END_TAG)
                depth--;

            while (true)
            {
                attributeCount = -1;

                // degenerated needs to be handled before error because of possible
                // processor expectations(!)

                if (degenerated)
                {
                    degenerated = false;
                    type = END_TAG;
                    return;
                }


                if (_error != null)
                {
                    for (int i = 0; i < _error.Length; i++)
                        push(_error[i]);
                    //				text = error;
                    _error = null;
                    type = COMMENT;
                    return;
                }


//            if (relaxed
//                && (stackMismatch > 0 || (peek(0) == -1 && depth > 0))) {
//                int sp = (depth - 1) << 2;
//                type = END_TAG;
//                namespace = elementStack[sp];
//                prefix = elementStack[sp + 1];
//                name = elementStack[sp + 2];
//                if (stackMismatch != 1)
//                    error = "missing end tag /" + name + " inserted";
//                if (stackMismatch > 0)
//                    stackMismatch--;
//                return;
//            }

                _prefix = null;
                name = null;
                namespace_ = null;
                //            text = null;

                type = peekType();

                switch (type)
                {

                    case ENTITY_REF:
                        pushEntity();
                        return;

                    case START_TAG:
                        parseStartTag(false);
                        return;

                    case END_TAG:
                        parseEndTag();
                        return;

                    case END_DOCUMENT:
                        return;

                    case TEXT:
                        pushText('<', !token);
                        if (depth == 0)
                        {
                            if (_isWhitespace)
                                type = IGNORABLE_WHITESPACE;
                            // make exception switchable for instances.chg... !!!!
                            //	else 
                            //    exception ("text '"+getText ()+"' not allowed outside root element");
                        }
                        return;

                    default:
                        type = parseLegacy(token);
                        if (type != XML_DECL)
                            return;
                        break;
                }
            }
        }

        private int parseLegacy(bool _push)
        {

            string req = "";
            int term;
            int result;
            int prev = 0;

            read(); // <
            int c = read();

            if (c == '?')
            {
                if ((peek(0) == 'x' || peek(0) == 'X')
                    && (peek(1) == 'm' || peek(1) == 'M'))
                {

                    if (_push)
                    {
                        push(peek(0));
                        push(peek(1));
                    }
                    read();
                    read();

                    if ((peek(0) == 'l' || peek(0) == 'L') && peek(1) <= ' ')
                    {

                        if (line != 1 || column > 4)
                            error("PI must not start with xml");

                        parseStartTag(true);

                        if (attributeCount < 1 || !"version".Equals(attributes[2]))
                            error("version expected");

                        version = attributes[3];

                        int pos = 1;

                        if (pos < attributeCount
                            && "encoding".Equals(attributes[2 + 4]))
                        {
                            encoding = attributes[3 + 4];
                            pos++;
                        }

                        if (pos < attributeCount
                            && "standalone".Equals(attributes[4*pos + 2]))
                        {
                            string st = attributes[3 + 4*pos];
                            if ("yes".Equals(st))
                                standalone = true;
                            else if ("no".Equals(st))
                                standalone = false;
                            else
                                error("illegal standalone value: " + st);
                            pos++;
                        }

                        if (pos != attributeCount)
                            error("illegal xmldecl");

                        _isWhitespace = true;
                        txtPos = 0;

                        return XML_DECL;
                    }
                }

                /*            int c0 = read ();
                        int c1 = read ();
                        int */

                term = '?';
                result = PROCESSING_INSTRUCTION;
            }
            else if (c == '!')
            {
                if (peek(0) == '-')
                {
                    result = COMMENT;
                    req = "--";
                    term = '-';
                }
                else if (peek(0) == '[')
                {
                    result = CDSECT;
                    req = "[CDATA[";
                    term = ']';
                    _push = true;
                }
                else
                {
                    result = DOCDECL;
                    req = "DOCTYPE";
                    term = -1;
                }
            }
            else
            {
                error("illegal: <" + c);
                return COMMENT;
            }

            for (int i = 0; i < req.Length; i++)
                read(req[i]);

            if (result == DOCDECL)
                parseDoctype(_push);
            else
            {
                while (true)
                {
                    c = read();
                    if (c == -1)
                    {
                        error(UNEXPECTED_EOF);
                        return COMMENT;
                    }

                    if (_push)
                        push(c);

                    if ((term == '?' || c == term)
                        && peek(0) == term
                        && peek(1) == '>')
                        break;

                    prev = c;
                }

                if (term == '-' && prev == '-' && !relaxed)
                    error("illegal comment delimiter: --->");

                read();
                read();

                if (_push && term != '?')
                    txtPos--;

            }
            return result;
        }

        /** precondition: &lt! consumed */

        private void parseDoctype(bool _push)
        {

            int nesting = 1;
            bool quoted = false;

            // read();

            while (true)
            {
                int i = read();
                switch (i)
                {

                    case -1:
                        error(UNEXPECTED_EOF);
                        return;

                    case '\'':
                        quoted = !quoted;
                        break;

                    case '<':
                        if (!quoted)
                            nesting++;
                        break;

                    case '>':
                        if (!quoted)
                        {
                            if ((--nesting) == 0)
                                return;
                        }
                        break;
                }
                if (_push)
                    push(i);
            }
        }

        /* precondition: &lt;/ consumed */

        private void parseEndTag()
        {

            read(); // '<'
            read(); // '/'
            name = readName();
            skip();
            read('>');

            int sp = (depth - 1) << 2;

            if (depth == 0)
            {
                error("element stack empty");
                type = COMMENT;
                return;
            }

            if (!relaxed)
            {
                if (!name.Equals(elementStack[sp + 3]))
                {
                    error("expected: /" + elementStack[sp + 3] + " read: " + name);

                    // become case insensitive in relaxed mode

//            int probe = sp;
//            while (probe >= 0 && !name.toLowerCase().Equals(elementStack[probe + 3].toLowerCase())) {
//                stackMismatch++;
//                probe -= 4;
//            }
//
//            if (probe < 0) {
//                stackMismatch = 0;
//                //			text = "unexpected end tag ignored";
//                type = COMMENT;
//                return;
//            }
                }

                namespace_ = elementStack[sp];
                _prefix = elementStack[sp + 1];
                name = elementStack[sp + 2];
            }
        }

        private int peekType()
        {
            switch (peek(0))
            {
                case -1:
                    return END_DOCUMENT;
                case '&':
                    return ENTITY_REF;
                case '<':
                    switch (peek(1))
                    {
                        case '/':
                            return END_TAG;
                        case '?':
                        case '!':
                            return LEGACY;
                        default:
                            return START_TAG;
                    }
                default:
                    return TEXT;
            }
        }

        private string get(int pos)
        {
            return new string(txtBuf, pos, txtPos - pos);
        }

        /*
    private  String pop (int pos) {
    String result = new String (txtBuf, pos, txtPos - pos);
    txtPos = pos;
    return result;
    }
    */

        private void push(int c)
        {

            _isWhitespace &= c <= ' ';

            if (txtPos == txtBuf.Length)
            {
                char[] bigger = new char[txtPos*4/3 + 4];
                Array.Copy(txtBuf, 0, bigger, 0, txtPos);
                txtBuf = bigger;
            }

            txtBuf[txtPos++] = (char) c;
        }

        /** Sets name and attributes */

        private void parseStartTag(bool xmldecl)
        {

            if (!xmldecl)
                read();
            name = readName();
            attributeCount = 0;

            while (true)
            {
                skip();

                int c = peek(0);

                if (xmldecl)
                {
                    if (c == '?')
                    {
                        read();
                        read('>');
                        return;
                    }
                }
                else
                {
                    if (c == '/')
                    {
                        degenerated = true;
                        read();
                        skip();
                        read('>');
                        break;
                    }

                    if (c == '>' && !xmldecl)
                    {
                        read();
                        break;
                    }
                }

                if (c == -1)
                {
                    error(UNEXPECTED_EOF);
                    //type = COMMENT;
                    return;
                }

                string attrName = readName();

                if (attrName.Length == 0)
                {
                    error("attr name expected");
                    //type = COMMENT;
                    break;
                }

                int i = (attributeCount++) << 2;

                attributes = ensureCapacity(attributes, i + 4);

                attributes[i++] = "";
                attributes[i++] = null;
                attributes[i++] = attrName;

                skip();

                if (peek(0) != '=')
                {
                    if (!relaxed)
                    {
                        error("Attr.value missing f. " + attrName);
                    }
                    attributes[i] = attrName;
                }
                else
                {
                    read('=');
                    skip();
                    int delimiter = peek(0);

                    if (delimiter != '\'' && delimiter != '"')
                    {
                        if (!relaxed)
                        {
                            error("attr value delimiter missing!");
                        }
                        delimiter = ' ';
                    }
                    else
                        read();

                    int p = txtPos;
                    pushText(delimiter, true);

                    attributes[i] = get(p);
                    txtPos = p;

                    if (delimiter != ' ')
                        read(); // skip endquote
                }
            }

            int sp = depth++ << 2;

            elementStack = ensureCapacity(elementStack, sp + 4);
            elementStack[sp + 3] = name;

            if (depth >= nspCounts.Length)
            {
                int[] bigger = new int[depth + 4];
                Array.Copy(nspCounts, 0, bigger, 0, nspCounts.Length);
                nspCounts = bigger;
            }

            nspCounts[depth] = nspCounts[depth - 1];

            /*
        		if(!relaxed){
                for (int i = attributeCount - 1; i > 0; i--) {
                    for (int j = 0; j < i; j++) {
                        if (getAttributeName(i).Equals(getAttributeName(j)))
                            exception("Duplicate Attribute: " + getAttributeName(i));
                    }
                }
        		}
        */
            if (processNsp)
                adjustNsp();
            else
                namespace_ = "";

            elementStack[sp] = namespace_;
            elementStack[sp + 1] = _prefix;
            elementStack[sp + 2] = name;
        }

        /** 
     * result: isWhitespace; if the setName parameter is set,
     * the name of the entity is stored in "name" */

        private void pushEntity()
        {

            push(read()); // &


            int pos = txtPos;

            while (true)
            {
                int c = peek(0);
                if (c == ';')
                {
                    read();
                    break;
                }
                if (c < 128
                    && (c < '0' || c > '9')
                    && (c < 'a' || c > 'z')
                    && (c < 'A' || c > 'Z')
                    && c != '_'
                    && c != '-'
                    && c != '#')
                {
                    if (!relaxed)
                    {
                        error("unterminated entity ref");
                    }

                    //System.out.println("broken entitiy: "+get(pos-1));

                    //; ends with:"+(char)c);           
//                if (c != -1)
//                    push(c);
                    return;
                }

                push(read());
            }

            string code = get(pos);
            txtPos = pos - 1;
            if (token && type == ENTITY_REF)
            {
                name = code;
            }

            if (code[0] == '#')
            {
                int c =
                    (code[1] == 'x'
                        ? int.Parse(code.Substring(2), NumberStyles.HexNumber)
                        : int.Parse(code.Substring(2), NumberStyles.Number));
                push(c);
                return;
            }

            string result = (string) entityMap[code];

            unresolved = result == null;

            if (unresolved)
            {
                if (!token)
                    error("unresolved: &" + code + ";");
            }
            else
            {
                for (int i = 0; i < result.Length; i++)
                    push(result[i]);
            }
        }

        /** types:
    '<': parse to any token (for nextToken ())
    '"': parse to quote
    ' ': parse to whitespace or '>'
    */

        private void pushText(int delimiter, bool resolveEntities)
        {

            int next = peek(0);
            int cbrCount = 0;

            while (next != -1 && next != delimiter)
            {
                // covers eof, '<', '"'

                if (delimiter == ' ')
                    if (next <= ' ' || next == '>')
                        break;

                if (next == '&')
                {
                    if (!resolveEntities)
                        break;

                    pushEntity();
                }
                else if (next == '\n' && type == START_TAG)
                {
                    read();
                    push(' ');
                }
                else
                    push(read());

                if (next == '>' && cbrCount >= 2 && delimiter != ']')
                    error("Illegal: ]]>");

                if (next == ']')
                    cbrCount++;
                else
                    cbrCount = 0;

                next = peek(0);
            }
        }

        private void read(char c)
        {
            int a = read();
            if (a != c)
                error("expected: '" + c + "' actual: '" + ((char) a) + "'");
        }

        private int read()
        {
            int result;

            if (peekCount == 0)
                result = peek(0);
            else
            {
                result = _peek[0];
                _peek[0] = _peek[1];
            }
            //		else {
            //			result = peek[0]; 
            //			Array.Copy (peek, 1, peek, 0, peekCount-1);
            //		}
            peekCount--;

            column++;

            if (result == '\n')
            {

                line++;
                column = 1;
            }

            return result;
        }

        /** Does never read more than needed */

        private int peek(int pos)
        {

            while (pos >= peekCount)
            {

                int nw;

                if (srcBuf.Length <= 1)
                    nw = reader.Read();
                else if (srcPos < srcCount)
                    nw = srcBuf[srcPos++];
                else
                {
                    srcCount = reader.Read(srcBuf, 0, srcBuf.Length);
                    if (srcCount <= 0)
                        nw = -1;
                    else
                        nw = srcBuf[0];

                    srcPos = 1;
                }

                if (nw == '\r')
                {
                    wasCR = true;
                    _peek[peekCount++] = '\n';
                }
                else
                {
                    if (nw == '\n')
                    {
                        if (!wasCR)
                            _peek[peekCount++] = '\n';
                    }
                    else
                        _peek[peekCount++] = nw;

                    wasCR = false;
                }
            }

            return _peek[pos];
        }

        private string readName()
        {

            int pos = txtPos;
            int c = peek(0);
            if ((c < 'a' || c > 'z')
                && (c < 'A' || c > 'Z')
                && c != '_'
                && c != ':'
                && c < 0x0c0
                && !relaxed)
                error("name expected");

            do
            {
                push(read());
                c = peek(0);
            } while ((c >= 'a' && c <= 'z')
                     || (c >= 'A' && c <= 'Z')
                     || (c >= '0' && c <= '9')
                     || c == '_'
                     || c == '-'
                     || c == ':'
                     || c == '.'
                     || c >= 0x0b7);

            string result = get(pos);
            txtPos = pos;
            return result;
        }

        private void skip()
        {

            while (true)
            {
                int c = peek(0);
                if (c > ' ' || c == -1)
                    break;
                read();
            }
        }

        //  public part starts here...

        public override void setFeature(string feature, bool value)
        {
            if (FEATURE_PROCESS_NAMESPACES.Equals(feature))
                processNsp = value;
            else if (isProp(feature, false, "relaxed"))
                relaxed = value;
            else
                exception("unsupported feature: " + feature);
        }

        public override bool getFeature(string feature)
        {
            if (FEATURE_PROCESS_NAMESPACES.Equals(feature))
                return processNsp;
            if (isProp(feature, false, "relaxed"))
                return relaxed;
            return false;
        }

        public override void setProperty(string property, object value)
        {
            if (isProp(property, true, "location"))
                location = value;
            else
                throw new XmlPullParserException("unsupported property: " + property);
        }

        public override object getProperty(string property)
        {
            if (isProp(property, true, "xmldecl-version"))
                return version;
            if (isProp(property, true, "xmldecl-standalone"))
                return standalone;
            if (isProp(property, true, "location"))
                return location ?? reader.ToString();
            return null;
        }

        public override void setInput(TextReader reader)
        {
            this.reader = reader;

            line = 1;
            column = 0;
            type = START_DOCUMENT;
            name = null;
            namespace_ = null;
            degenerated = false;
            attributeCount = -1;
            encoding = null;
            version = null;
            standalone = null;

            if (reader == null)
                return;

            srcPos = 0;
            srcCount = 0;
            peekCount = 0;
            depth = 0;

            entityMap = new Dictionary<object, object>();
            entityMap["amp"] = "&";
            entityMap["apos"] = "'";
            entityMap["gt"] = ">";
            entityMap["lt"] = "<";
            entityMap["quot"] = "\"";
        }

        public override void setInput(Stream inputStream, string _enc)
        {
            srcPos = 0;
            srcCount = 0;
            string enc = _enc;

            if (inputStream == null)
                throw new ArgumentException();

            try
            {

                if (enc == null)
                {
                    // read four bytes 

                    long chk = 0;

                    while (srcCount < 4)
                    {
                        int i = inputStream.ReadByte();
                        if (i == -1)
                            break;
                        chk = ((int)(chk << 8)) | i;
                        srcBuf[srcCount++] = (char) i;
                    }

                    if (srcCount == 4)
                    {
                        switch (chk)
                        {
                            case 0x00000FEFF:
                                enc = "UTF-32BE";
                                srcCount = 0;
                                break;

                            case 0x0FFFE0000:
                                enc = "UTF-32LE";
                                srcCount = 0;
                                break;

                            case 0x03c:
                                enc = "UTF-32BE";
                                srcBuf[0] = '<';
                                srcCount = 1;
                                break;

                            case 0x03c000000:
                                enc = "UTF-32LE";
                                srcBuf[0] = '<';
                                srcCount = 1;
                                break;

                            case 0x0003c003f:
                                enc = "UTF-16BE";
                                srcBuf[0] = '<';
                                srcBuf[1] = '?';
                                srcCount = 2;
                                break;

                            case 0x03c003f00:
                                enc = "UTF-16LE";
                                srcBuf[0] = '<';
                                srcBuf[1] = '?';
                                srcCount = 2;
                                break;

                            case 0x03c3f786d:
                                while (true)
                                {
                                    int i = inputStream.ReadByte();
                                    if (i == -1)
                                        break;
                                    srcBuf[srcCount++] = (char) i;
                                    if (i == '>')
                                    {
                                        string s = new string(srcBuf, 0, srcCount);
                                        int i0 = s.IndexOf("encoding");
                                        if (i0 != -1)
                                        {
                                            while (s[i0] != '"'
                                                   && s[i0] != '\'')
                                                i0++;
                                            char deli = s[i0++];
                                            int i1 = s.IndexOf(deli, i0);
                                            enc = s.Substring(i0, i1 - i0);
                                        }
                                        break;
                                    }
                                }
                                break;
                            default:
                                if ((chk & 0x0ffff0000) == 0x0FEFF0000)
                                {
                                    enc = "UTF-16BE";
                                    srcBuf[0] =
                                        (char) ((srcBuf[2] << 8) | srcBuf[3]);
                                    srcCount = 1;
                                }
                                else if ((chk & 0x0ffff0000) == 0x0fffe0000)
                                {
                                    enc = "UTF-16LE";
                                    srcBuf[0] =
                                        (char) ((srcBuf[3] << 8) | srcBuf[2]);
                                    srcCount = 1;
                                }
                                else if ((chk & 0x0ffffff00) == 0x0EFBBBF00)
                                {
                                    enc = "UTF-8";
                                    srcBuf[0] = srcBuf[3];
                                    srcCount = 1;
                                }
                                break;
                        }
                    }
                }

                if (enc == null)
                    enc = "UTF-8";

                int sc = srcCount;
                setInput(new StreamReader(inputStream, Encoding.GetEncoding(enc)));
                encoding = _enc;
                srcCount = sc;
            }
            catch (Exception e)
            {
                throw new XmlPullParserException(
                    "Invalid stream or encoding: " + e.ToString(),
                    this,
                    e);
            }
        }

        public override string getInputEncoding()
        {
            return encoding;
        }

        public override void defineEntityReplacementText(string entity, string value)
        {
        if (entityMap == null)
            throw new Exception("entity replacement text must be defined after setInput!");
        entityMap[entity] = value;
        }

        public override int getNamespaceCount(int depth)
        {
            if (depth > this.depth)
                throw new IndexOutOfRangeException();
            return nspCounts[depth];
        }

        public override string getNamespacePrefix(int pos)
        {
            return nspStack[pos << 1];
        }

        public override string getNamespaceUri(int pos)
        {
            return nspStack[(pos << 1) + 1];
        }

        public override string getNamespace(string prefix)
        {
            if ("xml".Equals(prefix))
                return "http://www.w3.org/XML/1998/namespace";
            if ("xmlns".Equals(prefix))
                return "http://www.w3.org/2000/xmlns/";

            for (int i = (getNamespaceCount(depth) << 1) - 2; i >= 0; i -= 2)
            {
                if (prefix == null)
                {
                    if (nspStack[i] == null)
                        return nspStack[i + 1];
                }
                else if (prefix.Equals(nspStack[i]))
                    return nspStack[i + 1];
            }
            return null;
        }

        public override int getDepth()
        {
            return depth;
        }

        public override string getPositionDescription()
        {
            StringBuilder buf =
                new StringBuilder(type < TYPES.Length ? TYPES[type] : "unknown");
            buf.Append(' ');

            if (type == START_TAG || type == END_TAG)
            {
                if (degenerated)
                    buf.Append("(empty) ");
                buf.Append('<');
                if (type == END_TAG)
                    buf.Append('/');

                if (_prefix != null)
                    buf.Append("{" + namespace_ + "}" + _prefix + ":")
                        ;
                buf.Append(name);

                int cnt = attributeCount << 2;
                for (int i = 0; i < cnt; i += 4)
                {
                    buf.Append(' ');
                    if (attributes[i + 1] != null)
                        buf.Append(
                            "{" + attributes[i] + "}" + attributes[i + 1] + ":");
                    buf.Append(attributes[i + 2] + "='" + attributes[i + 3] + "'");
                }

                buf.Append('>');
            }
            else if (type == IGNORABLE_WHITESPACE) {}
            else if (type != TEXT)
                buf.Append(getText());
            else if (_isWhitespace)
                buf.Append("(whitespace)");
            else
            {
                string text = getText();
                if (text.Length > 16)
                    text = text.Substring(0, 16) + "...";
                buf.Append(text);
            }

            buf.Append("@" + line + ":" + column);
            if (location != null)
            {
                buf.Append(" in ");
                buf.Append(location);
            }
            else if (reader != null)
            {
                buf.Append(" in ");
                buf.Append(reader.ToString());
            }
            return buf.ToString();
        }

        public override int getLineNumber()
        {
            return line;
        }

        public override int getColumnNumber()
        {
            return column;
        }

        public override bool isWhitespace()
        {
            if (type != TEXT && type != IGNORABLE_WHITESPACE && type != CDSECT)
                exception(ILLEGAL_TYPE);
            return _isWhitespace;
        }

        public override string getText()
        {
            return type < TEXT
                || (type == ENTITY_REF && unresolved) ? null : get(0);
        }

        public override char[] getTextCharacters(int[] poslen)
        {
            if (type >= TEXT)
            {
                if (type == ENTITY_REF)
                {
                    poslen[0] = 0;
                    poslen[1] = name.Length;
                    return name.ToCharArray();
                }
                poslen[0] = 0;
                poslen[1] = txtPos;
                return txtBuf;
            }

            poslen[0] = -1;
            poslen[1] = -1;
            return null;
        }

        public override string getNamespace()
        {
        return namespace_;
        }

        public override string getName()
        {
            return name;
        }

        public override string getPrefix()
        {
            return _prefix;
        }

        public override bool isEmptyElementTag()
        {
            if (type != START_TAG)
                exception(ILLEGAL_TYPE);
            return degenerated;
        }

        public override int getAttributeCount()
        {
            return attributeCount;
        }

        public override string getAttributeNamespace(int index)
        {
            if (index >= attributeCount)
                throw new IndexOutOfRangeException();
            return attributes[index << 2];
        }

        public override string getAttributeName(int index)
        {
            if (index >= attributeCount)
                throw new IndexOutOfRangeException();
            return attributes[(index << 2) + 2];
        }

        public override string getAttributePrefix(int index)
        {
            if (index >= attributeCount)
                throw new IndexOutOfRangeException();
            return attributes[(index << 2) + 1];
        }

        public override string getAttributeType(int index)
        {
            return "CDATA";
        }

        public override bool isAttributeDefault(int index)
        {
            return false;
        }

        public override string getAttributeValue(int index)
        {
            if (index >= attributeCount)
                throw new IndexOutOfRangeException();
            return attributes[(index << 2) + 3];
        }

        public override string getAttributeValue(string namespace_, string name)
        {
        for (int i = (attributeCount << 2) - 4; i >= 0; i -= 4) {
            if (attributes[i + 2].Equals(name)
                && (namespace_ == null || attributes[i].Equals(namespace_)))
                return attributes[i + 3];
        }

        return null;
        }

        public override int getEventType()
        {
            return type;
        }

        public override int next()
        {
            txtPos = 0;
            _isWhitespace = true;
            int minType = 9999;
            token = false;

            do
            {
                nextImpl();
                if (type < minType)
                    minType = type;
                //	    if (curr <= TEXT) type = curr; 
            }
            while (minType > ENTITY_REF // ignorable
                || (minType >= TEXT && peekType() >= TEXT));

            type = minType;
            if (type > TEXT)
                type = TEXT;

            return type;
        }

        public override int nextToken()
        {
            _isWhitespace = true;
            txtPos = 0;

            token = true;
            nextImpl();
            return type;
        }

        //
        // utility methods to make XML parsing easier ...
        public override void require(int type, string namespace_, string name)
        {
        if (type != this.type
            || (namespace_ != null && !namespace_.Equals(getNamespace()))
            || (name != null && !name.Equals(getName())))
            exception(
                "expected: " + TYPES[type] + " {" + namespace_ + "}" + name);
        }

        public override string nextText()
        {
            if (type != START_TAG)
                exception("precondition: START_TAG");

            next();

            string result;

            if (type == TEXT)
            {
                result = getText();
                next();
            }
            else
                result = "";

            if (type != END_TAG)
                exception("END_TAG expected");

            return result;
        }

        public override int nextTag()
        {
            next();
            if (type == TEXT && _isWhitespace)
                next();

            if (type != END_TAG && type != START_TAG)
                exception("unexpected type");

            return type;
        }

        /**
          * Skip sub tree that is currently porser positioned on.
          * <br>NOTE: parser must be on START_TAG and when funtion returns
          * parser will be positioned on corresponding END_TAG. 
          */

        //	Implementation copied from Alek's mail... 
        public override void skipSubTree()
        {
            require(START_TAG, null, null);
            int level = 1;
            while (level > 0)
            {
                int eventType = next();
                if (eventType == END_TAG)
                {
                    --level;
                }
                else if (eventType == START_TAG)
                {
                    ++level;
                }
            }
        }
    }
}
