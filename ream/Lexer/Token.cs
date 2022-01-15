using System.Linq;

namespace Ream.Lexer
{
    public class Token
    {
        public static Token Null;
        public static string[] Operators =
        {
            "+",
            "-",
            "*",
            "/",
            "=",
            "|",
            "&",
            "!",
            ">",
            "<",
            "^",
            "%",
            ":",
            ",",

            "==",
            "!=",
            "::"
        };
        public static (string, string)[] Brackets =
        {
            ("(", ")"),
            ("[", "]"),
            ("{", "}"),
            ("/*", "*/")
        };
        static Token()
        {
            Null = new(null);
        }

        public TokenType Type;
        private object _value;
        public object Value => _value;
        public Token(string value)
        {
            var t = GetAccurateValue(value);
            Type = t.Item1;
            _value = t.Item2;
        }

        private Token(object value, TokenType type)
        {
            Type = type;
            _value = value;
        }

        public override string ToString()
        {
            return $"Token({Type}, {Value})";
        }

        public static Token ManualCreate(object value, TokenType type)
        {
            return new(value, type);
        }

        public static (TokenType, object) GetAccurateValue(string value)
        {
            if (value == null || value.Length == 0)
            {
                return (TokenType.Null, null);
            }

            if (value == "\n")
            {
                return (TokenType.Newline, value);
            }

            if (value.All(x => char.IsDigit(x)))
            {
                return (TokenType.Interger, long.Parse(value));
            }

            if (Operators.Any(x => x == value))
            {
                return (TokenType.Operator, value);
            }

            if (Brackets.Any(x => x.Item1 == value || x.Item2 == value))
            {
                return (TokenType.Bracket, value);
            }

            if ((value.StartsWith("\"") && value.EndsWith("\""))
                || (value.StartsWith("\'") && value.EndsWith("\'")))
            {
                return (TokenType.String, value.Length > 1 ? value[1..^1] : null);
            }

            if (value == "true" || value == "false")
            {
                return (TokenType.Boolean, value == "true");
            }

            return (TokenType.Value, value);
        }
    }
}
