using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ream.Lexer
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
                // Fix line endings
                .Replace("\r", "");

            // Remove comments
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
}
