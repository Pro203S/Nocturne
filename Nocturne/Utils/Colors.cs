using System.Text;

namespace Nocturne.Utils
{
    public static class Colors
    {
        private const string ResetCode = "\x1b[0m";

        public static string Black(string text)
        {
            return Wrap(text, 30);
        }

        public static string Red(string text)
        {
            return Wrap(text, 31);
        }

        public static string Green(string text)
        {
            return Wrap(text, 32);
        }

        public static string Yellow(string text)
        {
            return Wrap(text, 33);
        }

        public static string Blue(string text)
        {
            return Wrap(text, 34);
        }

        public static string Magenta(string text)
        {
            return Wrap(text, 35);
        }

        public static string Cyan(string text)
        {
            return Wrap(text, 36);
        }

        public static string White(string text)
        {
            return Wrap(text, 37);
        }

        public static string Gray(string text)
        {
            return Wrap(text, 90);
        }

        public static string BrightRed(string text)
        {
            return Wrap(text, 91);
        }

        public static string BrightGreen(string text)
        {
            return Wrap(text, 92);
        }

        public static string BrightYellow(string text)
        {
            return Wrap(text, 93);
        }

        public static string BrightBlue(string text)
        {
            return Wrap(text, 94);
        }

        public static string BrightMagenta(string text)
        {
            return Wrap(text, 95);
        }

        public static string BrightCyan(string text)
        {
            return Wrap(text, 96);
        }

        public static string BrightWhite(string text)
        {
            return Wrap(text, 97);
        }

        public static string BgBlack(string text)
        {
            return Wrap(text, 40);
        }

        public static string BgRed(string text)
        {
            return Wrap(text, 41);
        }

        public static string BgGreen(string text)
        {
            return Wrap(text, 42);
        }

        public static string BgYellow(string text)
        {
            return Wrap(text, 43);
        }

        public static string BgBlue(string text)
        {
            return Wrap(text, 44);
        }

        public static string BgMagenta(string text)
        {
            return Wrap(text, 45);
        }

        public static string BgCyan(string text)
        {
            return Wrap(text, 46);
        }

        public static string BgWhite(string text)
        {
            return Wrap(text, 47);
        }

        public static string Bold(string text)
        {
            return Wrap(text, 1);
        }

        public static string Dim(string text)
        {
            return Wrap(text, 2);
        }

        public static string Italic(string text)
        {
            return Wrap(text, 3);
        }

        public static string Underline(string text)
        {
            return Wrap(text, 4);
        }

        public static string Inverse(string text)
        {
            return Wrap(text, 7);
        }

        public static string Hidden(string text)
        {
            return Wrap(text, 8);
        }

        public static string Strikethrough(string text)
        {
            return Wrap(text, 9);
        }

        public static string Rgb(int red, int green, int blue, string text)
        {
            ValidateRgb(red, green, blue);

            return string.Format(
                "\x1b[38;2;{0};{1};{2}m{3}{4}",
                red,
                green,
                blue,
                text,
                ResetCode
            );
        }

        public static string BgRgb(int red, int green, int blue, string text)
        {
            ValidateRgb(red, green, blue);

            return string.Format(
                "\x1b[48;2;{0};{1};{2}m{3}{4}",
                red,
                green,
                blue,
                text,
                ResetCode
            );
        }

        public static string Hex(string hex, string text)
        {
            int[] rgb = ParseHex(hex);
            return Rgb(rgb[0], rgb[1], rgb[2], text);
        }

        public static string BgHex(string hex, string text)
        {
            int[] rgb = ParseHex(hex);
            return BgRgb(rgb[0], rgb[1], rgb[2], text);
        }

        public static Style Create()
        {
            return new Style();
        }

        private static string Wrap(string text, int code)
        {
            return "\x1b[" + code + "m" + text + ResetCode;
        }

        private static void ValidateRgb(int red, int green, int blue)
        {
            if (red < 0 || red > 255)
            {
                throw new ArgumentOutOfRangeException("red");
            }

            if (green < 0 || green > 255)
            {
                throw new ArgumentOutOfRangeException("green");
            }

            if (blue < 0 || blue > 255)
            {
                throw new ArgumentOutOfRangeException("blue");
            }
        }

        private static int[] ParseHex(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
            {
                throw new ArgumentException("HEX 색상이 비어 있습니다.", "hex");
            }

            string normalized = hex.Trim().TrimStart('#');

            if (normalized.Length == 3)
            {
                normalized =
                    normalized[0].ToString() + normalized[0] +
                    normalized[1] + normalized[1] +
                    normalized[2] + normalized[2];
            }

            if (normalized.Length != 6)
            {
                throw new FormatException(
                    "HEX 색상은 #RGB 또는 #RRGGBB 형식이어야 합니다."
                );
            }

            return new[]
            {
            Convert.ToInt32(normalized.Substring(0, 2), 16),
            Convert.ToInt32(normalized.Substring(2, 2), 16),
            Convert.ToInt32(normalized.Substring(4, 2), 16)
        };
        }

        public sealed class Style
        {
            private readonly StringBuilder _codes;

            internal Style()
            {
                _codes = new StringBuilder();
            }

            public Style Black
            {
                get { return Add(30); }
            }

            public Style Red
            {
                get { return Add(31); }
            }

            public Style Green
            {
                get { return Add(32); }
            }

            public Style Yellow
            {
                get { return Add(33); }
            }

            public Style Blue
            {
                get { return Add(34); }
            }

            public Style Magenta
            {
                get { return Add(35); }
            }

            public Style Cyan
            {
                get { return Add(36); }
            }

            public Style White
            {
                get { return Add(37); }
            }

            public Style Gray
            {
                get { return Add(90); }
            }

            public Style Bold
            {
                get { return Add(1); }
            }

            public Style Dim
            {
                get { return Add(2); }
            }

            public Style Italic
            {
                get { return Add(3); }
            }

            public Style Underline
            {
                get { return Add(4); }
            }

            public Style Inverse
            {
                get { return Add(7); }
            }

            public Style Strikethrough
            {
                get { return Add(9); }
            }

            public string Apply(string text)
            {
                if (_codes.Length == 0)
                {
                    return text;
                }

                return _codes + text + ResetCode;
            }

            public override string ToString()
            {
                return _codes.ToString();
            }

            private Style Add(int code)
            {
                _codes.Append("\x1b[");
                _codes.Append(code);
                _codes.Append("m");

                return this;
            }
        }
    }
}