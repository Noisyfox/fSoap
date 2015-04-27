using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cn.noisyfox.fobjects.isodate
{
    public class IsoDate
    {

        public static readonly int DATE = 1;
        public static readonly int TIME = 2;
        public static readonly int DATE_TIME = 3;

        private static void dd(StringBuilder buf, int i)
        {
            buf.Append((char) (((int) '0') + i/10));
            buf.Append((char) (((int) '0') + i%10));
        }


        public static String dateToString(DateTime date, int type)
        {
            date = date.ToUniversalTime();

            StringBuilder buf = new StringBuilder();

            if ((type & DATE) != 0)
            {
                int year = date.Year;
                dd(buf, year/100);
                dd(buf, year%100);
                buf.Append('-');
                dd(
                    buf,
                    date.Month);
                buf.Append('-');
                dd(buf, date.Day);

                if (type == DATE_TIME)
                    buf.Append("T");
            }

            if ((type & TIME) != 0)
            {
                dd(buf, date.Hour);
                buf.Append(':');
                dd(buf, date.Minute);
                buf.Append(':');
                dd(buf, date.Second);
                buf.Append('.');
                int ms = date.Millisecond;
                buf.Append((char) (((int) '0') + (ms/100)));
                dd(buf, ms%100);
                buf.Append('Z');
            }

            return buf.ToString();
        }

        private static DateTime buildDateTime(int year = 0, int month = 0, int day = 0, int hour = 0, int minute = 0,
            int second = 0,
            int millisecond = 0, DateTimeKind kind = DateTimeKind.Unspecified)
        {
            return new DateTime(year, month, day, hour, minute, second, millisecond, kind);
        }

        public static DateTime stringToDate(String text, int type)
        {
            int year = 0, month = 0, day = 0, hour = 0, minute = 0, second = 0, millisecond = 0;

            if ((type & DATE) != 0)
            {
                year =
                    int.Parse(text.Substring(0, 4));
                month =
                    int.Parse(text.Substring(5, 2));
                day =
                    int.Parse(text.Substring(8, 2));

                if (type != DATE_TIME || text.Length < 11)
                {
                    return buildDateTime(year, month, day, hour, minute, second, millisecond, DateTimeKind.Local);
                }
                text = text.Substring(11);
            }


            hour =
                int.Parse(text.Substring(0, 2));
            // -11
            minute =
                int.Parse(text.Substring(3, 2));
            second =
                int.Parse(text.Substring(6, 2));

            int pos = 8;
            if (pos < text.Length && text[pos] == '.')
            {
                int ms = 0;
                int f = 100;
                while (true)
                {
                    char d = text[++pos];
                    if (d < '0' || d > '9')
                        break;
                    ms += (d - '0')*f;
                    f /= 10;
                }
                millisecond = ms;
            }
            else
                millisecond = 0;

            DateTimeKind kind = DateTimeKind.Local;

            if (pos < text.Length)
            {

                if (text[pos] == '+'
                    || text[pos] == '-')
                {
                    //c.setTimeZone(
                    //     TimeZone.getTimeZone(
                    //         "GMT" + text.Substring(pos)));
                    // TODO:support GMT timezone
                    DateTime c = buildDateTime(year, month, day, hour, minute, second, millisecond, DateTimeKind.Utc);

                    return c;
                }
                else if (text[pos] == 'Z')
                    kind = DateTimeKind.Utc;
                else
                    throw new Exception("illegal time format!");
            }

            return buildDateTime(year, month, day, hour, minute, second, millisecond, kind);
        }
    }
}
