﻿using Ream;
using Ream.Lexing;

namespace Ream.Lexing
{
    public class Lexer
    {
        public string Source;
        public bool AtEnd => current >= Source.Length;
        private readonly List<Token> tokens;
        private int start;
        private int current;
        private int line;
        private static readonly Dictionary<string, TokenType> keywords = new()
        {
            { "if", TokenType.If },
            { "else", TokenType.Else },
            { "elif", TokenType.Elif },
            { "for", TokenType.For },
            { "while", TokenType.While },
            { "function", TokenType.Function },
            { "global", TokenType.Global },
            { "return", TokenType.Return },
            { "null", TokenType.Null },
            { "class", TokenType.Class },
            { "this", TokenType.This },
            { "true", TokenType.True },
            { "false", TokenType.True },
            { "write", TokenType.Write },
        };

        public Lexer(string source)
        {
            Source = source;
            tokens = new();
            start = 0;
            current = 0;
            line = 1;
        }

        public List<Token> Lex()
        {
            while (!AtEnd)
            {
                start = current;
                LexToken();
            }

            tokens.Add(new(TokenType.End, "", null, line));
            return tokens;
        }

        private void LexToken()
        {
            char c = Advance();
            switch (c)
            {
                case '(': AddToken(TokenType.Left_Parenthesis); break;
                case ')': AddToken(TokenType.Right_Parenthesis); break;
                case '{': AddToken(TokenType.Left_Brace); break;
                case '}': AddToken(TokenType.Right_Brace); break;
                case '[': AddToken(TokenType.Left_Square); break;
                case ']': AddToken(TokenType.Right_Square); break;
                case ',': AddToken(TokenType.Comma); break;
                case '.': AddToken(TokenType.Period); break;

                case '&': AddToken(Match('&') ? TokenType.Ampersand_Ampersand : TokenType.Ampersand); break;
                case '|': AddToken(Match('|') ? TokenType.Pipe_Pipe : TokenType.Pipe); break;
                case '=': AddToken(Match('=') ? TokenType.Equal_Equal : TokenType.Equal); break;
                case '!': AddToken(Match('=') ? TokenType.Not_Equal : TokenType.Not); break;
                case '>': AddToken(Match('=') ? TokenType.Greater_Equal : TokenType.Greater); break;
                case '<': AddToken(Match('=') ? TokenType.Less_Equal : TokenType.Less); break;
                case '+': AddToken(Match('=') ? TokenType.Plus_Equal : TokenType.Plus); break;
                case '-': AddToken(Match('=') ? TokenType.Minus_Equal : TokenType.Minus); break;
                case '*': AddToken(Match('=') ? TokenType.Star_Equal : TokenType.Star); break;
                case '/': //AddToken(Match('=') ? TokenType.Slash_Equal : TokenType.Slash); break;
                    if (Match('/'))
                        while (Peek() != '\n' && !AtEnd) Advance();
                    else if (Match('*'))
                        while (Peek() != '*' && Peek(1) != '/' && !AtEnd) Advance();
                    else if (Match('='))
                        AddToken(TokenType.Slash_Equal);
                    else
                        AddToken(TokenType.Slash);
                    break;

                case ':': AddToken(Match(':') ? TokenType.Colon_Colon : TokenType.Colon); break;

                // Trim useless characters
                case ' ':
                case '\r':
                case '\t':
                    break;

                case '\n':
                    AddToken(TokenType.Newline);
                    line++;
                    break;

                // Literals
                case '\'':
                case '\"':
                    HandleString(c);
                    break;

                default:
                    if (char.IsDigit(c))
                        HandleInterger();
                    else if (char.IsLetterOrDigit(c) | c == '_')
                        HandleIdentifier();
                    else
                    {
                        Program.Error(line, $"Unexpected character '{c}'");
                    }
                    break;
            }
        }

        private char Advance()
        {
            current++;
            return Source[current - 1];
        }

        private void AddToken(TokenType type)
        {
            AddToken(type, null);
        }

        private void AddToken(TokenType type, object value)
        {
            string text = Source.Between(start, current);
            tokens.Add(new(type, text, value, line));
        }

        private bool Match(char c)
        {
            if (AtEnd) return false;
            if (Source[current] != c) return false;

            current++;
            return true;
        }
        private char Peek(int n = 0)
        {
            if (current + n >= Source.Length) return '\0';
            return Source[current + n];
        }
        private void HandleString(char st)
        {
            while (Peek() != st && !AtEnd)
            {
                if (Peek() == '\n') line++;

                Advance();
            }

            if (AtEnd)
            {
                Program.Error(line, "Unterminated string");
                return;
            }

            Advance();

            string value = Source.Between(start + 1, current - 1);
            AddToken(TokenType.String, value);
        }
        private void HandleInterger()
        {
            while (char.IsDigit(Peek())) Advance();

            if (Peek() == '.' && char.IsDigit(Peek(1)))
            {
                Advance();

                while (char.IsDigit(Peek())) Advance();
            }

            AddToken(TokenType.Interger, double.Parse(Source.Between(start, current)));
        }
        private void HandleIdentifier()
        {
            while (char.IsLetterOrDigit(Peek())) Advance();

            string text = Source.Between(start, current);
            TokenType type = keywords.ContainsKey(text) ? keywords[text] : TokenType.Identifier;
            AddToken(type);
        }
    }

    public static class Extensions
    {
        public static string Between(this string s, int startIndex, int endIndex)
        {
            int length = endIndex - startIndex;
            return s.Substring(startIndex, length);
        }
    }
}