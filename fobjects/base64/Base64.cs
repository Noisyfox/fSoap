using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cn.noisyfox.fobjects.base64
{
    public static class Base64
    {
        private static readonly char[] charTab =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"
                .ToCharArray();

        public static String encode(byte[] data)
        {
            return encode(data, 0, data.Length, null).ToString();
        }

        /** Encodes the part of the given byte array denoted by start and
    len to the Base64 format.  The encoded data is Appended to the
    given StringBuffer. If no StringBuffer is given, a new one is
    created automatically. The StringBuffer is the return value of
    this method. */

        public static StringBuilder encode(
            byte[] data,
            int start,
            int len,
            StringBuilder buf)
        {

            if (buf == null)
                buf = new StringBuilder(data.Length*3/2);

            int end = len - 3;
            int i = start;
            int n = 0;

            while (i <= end)
            {
                int d =
                    ((((int) data[i]) & 0x0ff) << 16)
                    | ((((int) data[i + 1]) & 0x0ff) << 8)
                    | (((int) data[i + 2]) & 0x0ff);

                buf.Append(charTab[(d >> 18) & 63]);
                buf.Append(charTab[(d >> 12) & 63]);
                buf.Append(charTab[(d >> 6) & 63]);
                buf.Append(charTab[d & 63]);

                i += 3;

                if (n++ >= 14)
                {
                    n = 0;
                    buf.Append("\r\n");
                }
            }

            if (i == start + len - 2)
            {
                int d =
                    ((((int) data[i]) & 0x0ff) << 16)
                    | ((((int) data[i + 1]) & 255) << 8);

                buf.Append(charTab[(d >> 18) & 63]);
                buf.Append(charTab[(d >> 12) & 63]);
                buf.Append(charTab[(d >> 6) & 63]);
                buf.Append("=");
            }
            else if (i == start + len - 1)
            {
                int d = (((int) data[i]) & 0x0ff) << 16;

                buf.Append(charTab[(d >> 18) & 63]);
                buf.Append(charTab[(d >> 12) & 63]);
                buf.Append("==");
            }

            return buf;
        }

        private static int decode(char c)
        {
            if (c >= 'A' && c <= 'Z')
                return ((int) c) - 65;
            else if (c >= 'a' && c <= 'z')
                return ((int) c) - 97 + 26;
            else if (c >= '0' && c <= '9')
                return ((int) c) - 48 + 26 + 26;
            else
                switch (c)
                {
                    case '+':
                        return 62;
                    case '/':
                        return 63;
                    case '=':
                        return 0;
                    default:
                        throw new Exception(
                            "unexpected code: " + c);
                }
        }

        /** Decodes the given Base64 encoded String to a new byte array. 
    The byte array holding the decoded data is returned. */

        public static byte[] decode(String s)
        {

            MemoryStream bos = new MemoryStream();
            try
            {
                decode(s, bos);
            }
            catch (IOException e)
            {
                throw new Exception();
            }
            return bos.ToArray();
        }

        public static void decode(String s, Stream os)
        {
            int i = 0;

            int len = s.Length;

            while (true)
            {
                while (i < len && s[i] <= ' ')
                    i++;

                if (i == len)
                    break;

                int tri =
                    (decode(s[i]) << 18)
                    + (decode(s[i + 1]) << 12)
                    + (decode(s[i + 2]) << 6)
                    + (decode(s[i + 3]));

                os.WriteByte((byte) ((tri >> 16) & 255));
                if (s[i + 2] == '=')
                    break;
                os.WriteByte((byte) ((tri >> 8) & 255));
                if (s[i + 3] == '=')
                    break;
                os.WriteByte((byte) (tri & 255));

                i += 4;
            }
        }
    }
}
