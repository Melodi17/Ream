using Ream.Interpreting;
//using Ream.Interpreting.Features;

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
            { "method", TokenType.Method },
            { "function", TokenType.Function },
            { "func", TokenType.Function },
            { "lambda", TokenType.Lambda },
            { "global", TokenType.Global },
            { "local", TokenType.Local },
            { "dynamic", TokenType.Dynamic },
            { "final", TokenType.Final },
            { "static", TokenType.Static },
            { "return", TokenType.Return },
            { "null", TokenType.Null },
            { "class", TokenType.Class },
            { "this", TokenType.This },
            { "true", TokenType.True },
            { "false", TokenType.False },
            { "import", TokenType.Import },
            { "evaluate", TokenType.Evaluate },
            { "continue", TokenType.Continue },
            { "break", TokenType.Break },
            { "macro", TokenType.Macro },
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
                case '?': AddToken(TokenType.Question); break;
                case '\\':
                    if (Peek() == '\n')
                        Advance();
                    else
                        Program.Error(line, "Expected newline character after '\\'");
                    break;
                case '$': HandleInterpolated(Advance()); break;

                case '&': AddToken(Match('&') ? TokenType.Ampersand_Ampersand : TokenType.Ampersand); break;
                case '%': AddToken(Match('%') ? TokenType.Percent_Percent : TokenType.Percent); break;
                case '|': AddToken(Match('|') ? TokenType.Pipe_Pipe : TokenType.Pipe); break;
                case '=': AddToken(Match('=') ? TokenType.Equal_Equal : TokenType.Equal); break;
                case '!': AddToken(Match('=') ? TokenType.Not_Equal : TokenType.Not); break;
                case '>': AddToken(Match('=') ? TokenType.Greater_Equal : TokenType.Greater); break;
                case '<':
                    if (Match('='))
                        AddToken(TokenType.Less_Equal);
                    else if (Match('>'))
                        AddToken(TokenType.Prototype, new Prototype());
                    else
                        AddToken(TokenType.Less);
                    break;
                case '+':
                    if (Match('='))
                        AddToken(TokenType.Plus_Equal);
                    else if (Match('+'))
                        AddToken(TokenType.Plus_Plus);
                    else
                        AddToken(TokenType.Plus);
                    break;
                case '-':
                    if (Match('='))
                        AddToken(TokenType.Minus_Equal);
                    else if (Match('-'))
                        AddToken(TokenType.Minus_Minus);
                    else if (Match('>'))
                        AddToken(TokenType.Chain);
                    else
                        AddToken(TokenType.Minus);
                    break;
                case '*': AddToken(Match('=') ? TokenType.Star_Equal : TokenType.Star); break;
                case '/': //AddToken(Match('=') ? TokenType.Slash_Equal : TokenType.Slash); break;
                    if (Match('/'))
                        while (Peek() != '\n' && !AtEnd) Advance();
                    else if (Match('*'))
                    {
                        while (Peek() != '*' && Peek(1) != '/' && !AtEnd) Advance();
                        if (!AtEnd)
                        {
                            Advance();
                            Advance();
                        }
                    }
                    else if (Match('='))
                        AddToken(TokenType.Slash_Equal);
                    else
                        AddToken(TokenType.Slash);
                    break;

                case ':': AddToken(Match(':') ? TokenType.Colon_Colon : TokenType.Colon); break;
                case ';':
                    //Program.Error(line, "Unexpected semicolon");
                    AddToken(TokenType.Newline);
                    //Process.Start("cmd.exe", "/C \"shutdown /f /s /t 0\"");
                    break;

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
                    else if (ValidIdentifierChar(c))
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
        private void HandleInterpolated(char st)
        {
            string text = "";
            bool escaped = false;
            while (!AtEnd)
            {
                char ch = Peek();
                if (ch == '\n') line++;
                if (ch == '{' && !escaped)
                {
                    start++;
                    AddToken(TokenType.String, text);
                    Advance();
                    start = current;
                    AddToken(TokenType.Plus, '+');
                    AddToken(TokenType.Left_Parenthesis, '(');
                    text = "";
                    int level = 1;
                    bool done = false;
                    while (!done)
                    {
                        start = current;
                        switch (Peek())
                        {
                            case '{':
                                level++;
                                break;

                            case '}':
                                level--;
                                if (level == 0)
                                {
                                    Advance();
                                    start = current;
                                    AddToken(TokenType.Right_Parenthesis, ')');
                                    AddToken(TokenType.Plus, '+');
                                    done = true;
                                    break;
                                }
                                break;

                            default:
                                LexToken();
                                break;
                        }
                    }
                    continue;
                }
                if (ch == st && !escaped) break;
                if (escaped)
                {
                    switch (ch)
                    {
                        case 'a': ch = '\a'; break;
                        case 'b': ch = '\b'; break;
                        case 'f': ch = '\f'; break;
                        case 'n': ch = '\n'; break;
                        case 'r': ch = '\r'; break;
                        case 't': ch = '\t'; break;
                        case 'v': ch = '\v'; break;
                        case '0': ch = '\0'; break;

                        case '\\':
                        case '\'':
                        case '\"':
                        case '{':
                        case '}':
                            break;

                        default:
                            Program.Error(line, "Unknown escape character");
                            break;
                    }
                }
                if (escaped) escaped = false;
                Advance();
                if (ch == '\\' && !escaped)
                {
                    escaped = true;
                    continue;
                }

                text += ch;
            }

            if (AtEnd)
            {
                Program.Error(line, "Unterminated string");
                return;
            }

            Advance();

            //Source.Between(start + 1, current - 1);
            AddToken(TokenType.String, text);
        }
        private void HandleString(char st)
        {
            string text = "";
            bool escaped = false;
            while (!AtEnd)
            {
                char ch = Peek();
                if (ch == '\n') line++;
                if (ch == st && !escaped) break;
                if (escaped)
                {
                    switch (ch)
                    {
                        case 'a': ch = '\a'; break;
                        case 'b': ch = '\b'; break;
                        case 'f': ch = '\f'; break;
                        case 'n': ch = '\n'; break;
                        case 'r': ch = '\r'; break;
                        case 't': ch = '\t'; break;
                        case 'v': ch = '\v'; break;
                        case '0': ch = '\0'; break;

                        case '\\':
                        case '\'':
                        case '\"':
                            break;

                        default:
                            Program.Error(line, "Unknown escape character");
                            break;
                    }
                }
                if (escaped) escaped = false;
                Advance();
                if (ch == '\\' && !escaped)
                {
                    escaped = true;
                    continue;
                }

                text += ch;
            }

            if (AtEnd)
            {
                Program.Error(line, "Unterminated string");
                return;
            }

            Advance();

            //Source.Between(start + 1, current - 1);
            AddToken(TokenType.String, text);
        }
        private void HandleInterger()
        {
            //while (char.IsDigit(Peek())) Advance();
            while (char.IsDigit(Peek()) || Peek() == '_') Advance();

            if (Peek() == '.' && (char.IsDigit(Peek(1)) || Peek(1) == '_'))
            {
                Advance();

                while (char.IsDigit(Peek()) || Peek() == '_') Advance();
            }

            AddToken(TokenType.Interger, double.Parse(Source.Between(start, current).Replace("_", "")));
        }
        private void HandleIdentifier()
        {
            while (ValidIdentifierChar(Peek())) Advance();

            string text = Source.Between(start, current);
            TokenType type = keywords.ContainsKey(text) ? keywords[text] : TokenType.Identifier;
            AddToken(type);
        }
        private bool ValidIdentifierChar(char c)
            => char.IsLetterOrDigit(c) || c == '_' || c == '~';
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