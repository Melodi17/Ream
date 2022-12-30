using Ream.Interpreting;
//using Ream.Interpreting.Features;

namespace Ream.Lexing
{
    public class Lexer
    {
        public string Source;
        public bool AtEnd => this.current >= this.Source.Length;
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
            this.Source = source;
            this.tokens = new();
            this.start = 0;
            this.current = 0;
            this.line = 1;
        }

        public List<Token> Lex()
        {
            while (!this.AtEnd)
            {
                this.start = this.current;
                this.LexToken();
            }

            this.tokens.Add(new(TokenType.End, "", null, this.line));
            return this.tokens;
        }

        private void LexToken()
        {
            char c = this.Advance();
            switch (c)
            {
                case '(':
                    this.AddToken(TokenType.Left_Parenthesis); break;
                case ')':
                    this.AddToken(TokenType.Right_Parenthesis); break;
                case '{':
                    this.AddToken(TokenType.Left_Brace); break;
                case '}':
                    this.AddToken(TokenType.Right_Brace); break;
                case '[':
                    this.AddToken(TokenType.Left_Square); break;
                case ']':
                    this.AddToken(TokenType.Right_Square); break;
                case ',':
                    this.AddToken(TokenType.Comma); break;
                case '.':
                    this.AddToken(TokenType.Period); break;
                case '?':
                    this.AddToken(TokenType.Question); break;
                case '\\':
                    if (this.Peek() == '\n')
                        this.Advance();
                    else
                        Program.Error(this.line, "Expected newline character after '\\'");
                    break;
                case '$':
                    this.HandleInterpolated(this.Advance()); break;

                case '&':
                    this.AddToken(this.Match('&') ? TokenType.Ampersand_Ampersand : TokenType.Ampersand); break;
                case '%':
                    this.AddToken(this.Match('%') ? TokenType.Percent_Percent : TokenType.Percent); break;
                case '|':
                    this.AddToken(this.Match('|') ? TokenType.Pipe_Pipe : TokenType.Pipe); break;
                case '=':
                    this.AddToken(this.Match('=') ? TokenType.Equal_Equal : TokenType.Equal); break;
                case '!':
                    this.AddToken(this.Match('=') ? TokenType.Not_Equal : TokenType.Not); break;
                case '>':
                    this.AddToken(this.Match('=') ? TokenType.Greater_Equal : TokenType.Greater); break;
                case '<':
                    if (this.Match('='))
                        this.AddToken(TokenType.Less_Equal);
                    else if (this.Match('>'))
                        this.AddToken(TokenType.Prototype, new Prototype());
                    else
                        this.AddToken(TokenType.Less);
                    break;
                case '+':
                    if (this.Match('='))
                        this.AddToken(TokenType.Plus_Equal);
                    else if (this.Match('+'))
                        this.AddToken(TokenType.Plus_Plus);
                    else
                        this.AddToken(TokenType.Plus);
                    break;
                case '-':
                    if (this.Match('='))
                        this.AddToken(TokenType.Minus_Equal);
                    else if (this.Match('-'))
                        this.AddToken(TokenType.Minus_Minus);
                    else if (this.Match('>'))
                        this.AddToken(TokenType.Chain);
                    else
                        this.AddToken(TokenType.Minus);
                    break;
                case '*':
                    this.AddToken(this.Match('=') ? TokenType.Star_Equal : TokenType.Star); break;
                case '/': //AddToken(Match('=') ? TokenType.Slash_Equal : TokenType.Slash); break;
                    if (this.Match('/'))
                        while (this.Peek() != '\n' && !this.AtEnd)
                            this.Advance();
                    else if (this.Match('*'))
                    {
                        while (this.Peek() != '*' && this.Peek(1) != '/' && !this.AtEnd) this.Advance();
                        if (!this.AtEnd)
                        {
                            this.Advance();
                            this.Advance();
                        }
                    }
                    else if (this.Match('='))
                        this.AddToken(TokenType.Slash_Equal);
                    else
                        this.AddToken(TokenType.Slash);
                    break;

                case ':':
                    this.AddToken(this.Match(':') ? TokenType.Colon_Colon : TokenType.Colon); break;
                case ';':
                    //Program.Error(line, "Unexpected semicolon");
                    this.AddToken(TokenType.Newline);
                    //Process.Start("cmd.exe", "/C \"shutdown /f /s /t 0\"");
                    break;

                // Trim useless characters
                case ' ':
                case '\r':
                case '\t':
                    break;

                case '\n':
                    this.AddToken(TokenType.Newline);
                    this.line++;
                    break;

                // Literals
                case '\'':
                case '\"':
                    this.HandleString(c);
                    break;

                default:
                    if (char.IsDigit(c))
                        this.HandleInterger();
                    else if (this.ValidIdentifierChar(c))
                        this.HandleIdentifier();
                    else
                    {
                        Program.Error(this.line, $"Unexpected character '{c}'");
                    }
                    break;
            }
        }

        private char Advance()
        {
            this.current++;
            return this.Source[this.current - 1];
        }

        private void AddToken(TokenType type)
        {
            this.AddToken(type, null);
        }

        private void AddToken(TokenType type, object value)
        {
            string text = this.Source.Between(this.start, this.current);
            this.tokens.Add(new(type, text, value, this.line));
        }

        private bool Match(char c)
        {
            if (this.AtEnd) return false;
            if (this.Source[this.current] != c) return false;

            this.current++;
            return true;
        }
        private char Peek(int n = 0)
        {
            if (this.current + n >= this.Source.Length) return '\0';
            return this.Source[this.current + n];
        }
        private void HandleInterpolated(char st)
        {
            string text = "";
            bool escaped = false;
            while (!this.AtEnd)
            {
                char ch = this.Peek();
                if (ch == '\n') this.line++;
                if (ch == '{' && !escaped)
                {
                    this.start++;
                    this.AddToken(TokenType.String, text);
                    this.Advance();
                    this.start = this.current;
                    this.AddToken(TokenType.Plus, '+');
                    this.AddToken(TokenType.Left_Parenthesis, '(');
                    text = "";
                    int level = 1;
                    bool done = false;
                    while (!done)
                    {
                        this.start = this.current;
                        switch (this.Peek())
                        {
                            case '{':
                                level++;
                                break;

                            case '}':
                                level--;
                                if (level == 0)
                                {
                                    this.Advance();
                                    this.start = this.current;
                                    this.AddToken(TokenType.Right_Parenthesis, ')');
                                    this.AddToken(TokenType.Plus, '+');
                                    done = true;
                                    break;
                                }
                                break;

                            default:
                                this.LexToken();
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
                            Program.Error(this.line, "Unknown escape character");
                            break;
                    }
                }
                if (escaped) escaped = false;
                this.Advance();
                if (ch == '\\' && !escaped)
                {
                    escaped = true;
                    continue;
                }

                text += ch;
            }

            if (this.AtEnd)
            {
                Program.Error(this.line, "Unterminated string");
                return;
            }

            this.Advance();

            //Source.Between(start + 1, current - 1);
            this.AddToken(TokenType.String, text);
        }
        private void HandleString(char st)
        {
            string text = "";
            bool escaped = false;
            while (!this.AtEnd)
            {
                char ch = this.Peek();
                if (ch == '\n') this.line++;
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
                            Program.Error(this.line, "Unknown escape character");
                            break;
                    }
                }
                if (escaped) escaped = false;
                this.Advance();
                if (ch == '\\' && !escaped)
                {
                    escaped = true;
                    continue;
                }

                text += ch;
            }

            if (this.AtEnd)
            {
                Program.Error(this.line, "Unterminated string");
                return;
            }

            this.Advance();

            //Source.Between(start + 1, current - 1);
            this.AddToken(TokenType.String, text);
        }
        private void HandleInterger()
        {
            //while (char.IsDigit(Peek())) Advance();
            while (char.IsDigit(this.Peek()) || this.Peek() == '_') this.Advance();

            if (this.Peek() == '.' && (char.IsDigit(this.Peek(1)) || this.Peek(1) == '_'))
            {
                this.Advance();

                while (char.IsDigit(this.Peek()) || this.Peek() == '_') this.Advance();
            }

            this.AddToken(TokenType.Interger, double.Parse(this.Source.Between(this.start, this.current).Replace("_", "")));
        }
        private void HandleIdentifier()
        {
            while (this.ValidIdentifierChar(this.Peek())) this.Advance();

            string text = this.Source.Between(this.start, this.current);
            TokenType type = keywords.ContainsKey(text) ? keywords[text] : TokenType.Identifier;
            this.AddToken(type);
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