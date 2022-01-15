using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ream
{
    public class Lexer
    {
        public string Text;
        public int Pos;
        public Token CurrentToken;
        public bool WithinBounds => Pos < Text.Length;
        public Lexer(string text)
        {
            Text = text
                .Replace("\r", "") /* Fix line endings */;

            Text = Regex.Replace(Text, @"(\/\/.*)|(\/\*(?:.|\n)*?\*\/)", "");
        }
        public Token[] Lex()
        {
            List<Token> tokens = new();

            while (true)
            {
                Token t = GetNextToken();
                if (t.Type == TokenType.Null) break;

                tokens.Add(t);
            }

            return tokens.ToArray();
        }
        public Token GetNextToken()
        {
            SkipSpaces();

            if (!WithinBounds)
                return Token.Null;

            string tokenStr = "";
            TokenType prevTokenType;
            TokenType currTokenType;
            bool inStr = false;

            while (WithinBounds)
            {
                char ch = Text[Pos];
                currTokenType = Token.GetAccurateValue(tokenStr + ch).Item1;
                prevTokenType = Token.GetAccurateValue(tokenStr).Item1;

                if (ch == '\"' || ch == '\'') inStr = !inStr;
                if (ch == ' ' && !inStr) break;
                if (currTokenType != TokenType.Operator && Token.Operators.Contains(ch.ToString()) && !inStr) break;
                if (prevTokenType == TokenType.Operator && currTokenType != TokenType.Operator) break;
                if (currTokenType != TokenType.Bracket && Token.Brackets.Any(x => x.Item1 == ch.ToString() || x.Item2 == ch.ToString()) && !inStr) break;
                if (prevTokenType == TokenType.Bracket && currTokenType != TokenType.Bracket) break;
                if (currTokenType != TokenType.Newline && ch == '\n' && !inStr) break;
                if (prevTokenType == TokenType.Newline) break;

                Pos++;
                tokenStr += ch;
            }

            return new(tokenStr);
        }
        public void SkipSpaces()
        {
            while (WithinBounds && Text[Pos] == ' ')
                Pos++;
        }
        public char Eat()
        {
            char ret = Text[Pos];
            Pos++;
            return ret;
        }
    }

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
    public enum TokenType
    {
        String,
        Interger,
        Boolean,
        Operator,
        Value,
        Bracket,
        Function,
        Newline,
        Null
    }
}
