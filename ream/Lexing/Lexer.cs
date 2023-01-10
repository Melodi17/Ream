using Ream.Interpreting;

//using Ream.Interpreting.Features;

namespace Ream.Lexing
{
    public class Lexer
    {
        public readonly string Source;
        public bool AtEnd => this._current >= this.Source.Length;
        private readonly List<Token> _tokens;
        private int _start;
        private int _current;
        private int _line;
        private static readonly Dictionary<string, TokenType> keywords = new()
        {
            { "if", TokenType.If },
            { "else", TokenType.Else },
            { "elif", TokenType.Elif },
            { "for", TokenType.For },
            { "while", TokenType.While },
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
            { "continue", TokenType.Continue },
            { "break", TokenType.Break },
            { "_", TokenType.Dispose },
        };

        public Lexer(string source)
        {
            this.Source = source;
            this._tokens = new();
            this._start = 0;
            this._current = 0;
            this._line = 1;
        }

        public List<Token> Lex()
        {
            while (!this.AtEnd)
            {
                this._start = this._current;
                this.LexToken();
            }

            this._tokens.Add(new(TokenType.End, "", null, this._line));
            return this._tokens;
        }

        private void LexToken()
        {
            char c = this.Advance();
            switch (c)
            {
                case '(':
                    this.AddToken(TokenType.LeftParenthesis);
                    break;
                case ')':
                    this.AddToken(TokenType.RightParenthesis);
                    break;
                case '{':
                    this.AddToken(TokenType.LeftBrace);
                    break;
                case '}':
                    this.AddToken(TokenType.RightBrace);
                    break;
                case '[':
                    this.AddToken(TokenType.LeftSquare);
                    break;
                case ']':
                    this.AddToken(TokenType.RightSquare);
                    break;
                case ',':
                    this.AddToken(TokenType.Comma);
                    break;
                case '.':
                    this.AddToken(TokenType.Period);
                    break;
                case '?':
                    this.AddToken(TokenType.Question);
                    break;
                case '\\':
                    if (this.Peek() == '\n')
                        this.Advance();
                    else
                        Program.Error(this._line, "Expected newline character after '\\'");
                    break;
                case '$':
                    this.HandleInterpolated(this.Advance());
                    break;

                case '&':
                    this.AddToken(this.Match('&') ? TokenType.AmpersandAmpersand : TokenType.Ampersand);
                    break;
                case '%':
                    this.AddToken(this.Match('%') ? TokenType.PercentPercent : TokenType.Percent);
                    break;
                case '|':
                    this.AddToken(this.Match('|') ? TokenType.PipePipe : TokenType.Pipe);
                    break;
                case '=':
                    this.AddToken(this.Match('=') ? TokenType.EqualEqual : TokenType.Equal);
                    break;
                case '!':
                    this.AddToken(this.Match('=') ? TokenType.NotEqual : TokenType.Not);
                    break;
                case '>':
                    this.AddToken(this.Match('=') ? TokenType.GreaterEqual : TokenType.Greater);
                    break;
                case '<':
                    this.AddToken(this.Match('=') ? TokenType.LessEqual : TokenType.Less);
                    break;
                case '+':
                    if (this.Match('='))
                        this.AddToken(TokenType.PlusEqual);
                    else if (this.Match('+'))
                        this.AddToken(TokenType.PlusPlus);
                    else
                        this.AddToken(TokenType.Plus);
                    break;
                case '-':
                    if (this.Match('='))
                        this.AddToken(TokenType.MinusEqual);
                    else if (this.Match('-'))
                        this.AddToken(TokenType.MinusMinus);
                    else if (this.Match('>'))
                        this.AddToken(TokenType.Chain);
                    else
                        this.AddToken(TokenType.Minus);
                    break;
                case '*':
                    this.AddToken(this.Match('=') ? TokenType.StarEqual : TokenType.Star);
                    break;
                case '/':
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
                        this.AddToken(TokenType.SlashEqual);
                    else
                        this.AddToken(TokenType.Slash);
                    break;

                case ':':
                    this.AddToken(this.Match(':') ? TokenType.ColonColon : TokenType.Colon);
                    break;
                case ';':
                    this.AddToken(TokenType.Newline);
                    break;

                // Trim useless characters
                case ' ':
                case '\r':
                case '\t':
                    break;

                case '\n':
                    this.AddToken(TokenType.Newline);
                    this._line++;
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
                        Program.Error(this._line, $"Unexpected character '{c}'");
                    }
                    break;
            }
        }

        private char Advance()
        {
            this._current++;
            return this.Source[this._current - 1];
        }

        private void AddToken(TokenType type)
        {
            this.AddToken(type, null);
        }

        private void AddToken(TokenType type, object value)
        {
            string text = this.Source.Between(this._start, this._current);
            this._tokens.Add(new(type, text, value, this._line));
        }

        private bool Match(char c)
        {
            if (this.AtEnd) return false;
            if (this.Source[this._current] != c) return false;

            this._current++;
            return true;
        }

        private char Peek(int n = 0)
        {
            if (this._current + n >= this.Source.Length) return '\0';
            return this.Source[this._current + n];
        }

        private void HandleInterpolated(char st)
        {
            string text = "";
            bool escaped = false;
            while (!this.AtEnd)
            {
                char ch = this.Peek();
                if (ch == '\n') this._line++;
                if (ch == '{' && !escaped)
                {
                    this._start++;
                    this.AddToken(TokenType.String, text);
                    this.Advance();
                    this._start = this._current;
                    this.AddToken(TokenType.Plus, '+');
                    this.AddToken(TokenType.LeftParenthesis, '(');
                    text = "";
                    int level = 1;
                    bool done = false;
                    while (!done)
                    {
                        this._start = this._current;
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
                                    this._start = this._current;
                                    this.AddToken(TokenType.RightParenthesis, ')');
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
                        case 'a':
                            ch = '\a';
                            break;
                        case 'b':
                            ch = '\b';
                            break;
                        case 'f':
                            ch = '\f';
                            break;
                        case 'n':
                            ch = '\n';
                            break;
                        case 'r':
                            ch = '\r';
                            break;
                        case 't':
                            ch = '\t';
                            break;
                        case 'v':
                            ch = '\v';
                            break;
                        case '0':
                            ch = '\0';
                            break;

                        case '\\':
                        case '\'':
                        case '\"':
                        case '{':
                        case '}':
                            break;

                        default:
                            Program.Error(this._line, "Unknown escape character");
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
                Program.Error(this._line, "Unterminated string");
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
                if (ch == '\n') this._line++;
                if (ch == st && !escaped) break;
                if (escaped)
                {
                    switch (ch)
                    {
                        case 'a':
                            ch = '\a';
                            break;
                        case 'b':
                            ch = '\b';
                            break;
                        case 'f':
                            ch = '\f';
                            break;
                        case 'n':
                            ch = '\n';
                            break;
                        case 'r':
                            ch = '\r';
                            break;
                        case 't':
                            ch = '\t';
                            break;
                        case 'v':
                            ch = '\v';
                            break;
                        case '0':
                            ch = '\0';
                            break;

                        case '\\':
                        case '\'':
                        case '\"':
                            break;

                        default:
                            Program.Error(this._line, "Unknown escape character");
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
                Program.Error(this._line, "Unterminated string");
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

            this.AddToken(TokenType.Integer, double.Parse(this.Source.Between(this._start, this._current).Replace("_", "")));
        }

        private void HandleIdentifier()
        {
            while (this.ValidIdentifierChar(this.Peek())) this.Advance();

            string text = this.Source.Between(this._start, this._current);
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
