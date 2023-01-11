using Ream.Interpreting;
using ream.Interpreting.Objects;
using Ream.Lexing;
using Ream.Parsing;
using Ream.Tools;

namespace Ream
{
    public class Program
    {
        public static bool ErrorOccured = false;
        public static bool RuntimeErrorOccured = false;
        public static string DataPath;
        public static string LibDataPath;
        public static void Main(string[] args)
        {
            DataPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ream");
            LibDataPath = Path.Join(DataPath, "Libraries");

            Directory.CreateDirectory(DataPath);
            Directory.CreateDirectory(LibDataPath);

            if (args.Any(x => x == "UPDATE_AST"))
                UpdateAST();

            string keyword = args.Length > 0 ? args[0].ToLower() : "";
            if (keyword == "time")
            {
                DateTime startTime = DateTime.Now;
                RunFile(string.Join(" ", args.Skip(1)));
                DateTime endTime = DateTime.Now;
                TimeSpan duration = endTime - startTime;
                Console.WriteLine($"Execution took {duration}");
            }
            else if (args.Any())
                RunFile(string.Join(" ", args));
            else
                RunPrompt();
        }

        private static void UpdateAST()
        {
            ASTGenerator.DefineAst(Path.Join("..", "..", "..", "Parsing", "ASTExpr.cs"), "Expr", new[]
            {
                "Assign     : Token name, Expr value",
                "Binary     : Expr left, Token @operator, Expr right",
                "Ternary    : Expr left, Token leftOperator, Expr middle, Token rightOperator, Expr right",
                "Call       : Expr callee, Token paren, List<Expr> arguments",
                "Indexer    : Expr callee, Token paren, Expr index",
                "SetIndexer : Expr.Indexer indexer, Expr value",
                "Set        : Expr obj, Token name, Expr value",
                "Get        : Expr obj, Token name",
                "Chain      : Expr.Call call",
                "Grouping   : Expr expression",
                "Sequence   : List<Expr> items",
                "Dictionary : Token paren, Dictionary<Expr,Expr> items",
                "Lambda     : List<Token> parameters, List<Stmt> body",
                "Literal    : Object value",
                "Logical    : Expr left, Token @operator, Expr right",
                "This       : Token keyword",
                "Dispose    : Token keyword, Expr expression",
                "Unary      : Token @operator, Expr right",
                "Variable   : Token name",
            }.ToList());

            ASTGenerator.DefineAst(Path.Join("..", "..", "..", "Interpreting", "Stmt.cs"), "Stmt", new string[]
            {
                "Block      : List<Stmt> statements",
                "Class      : Token name, List<Stmt.Function> functions, List<Stmt.Typed> variables",
                "Expression : Expr expression",
                "Function   : Token name, VariableType type, List<Token> parameters, List<Stmt> body",
                "Method     : Expr obj, Token name, VariableType type, List<Token> parameters, List<Stmt> body",
                "If         : Expr condition, Stmt thenBranch, List<(Expr,Stmt)> elifBranches, Stmt elseBranch",
                "Return     : Token keyword, Expr value",
                "Continue   : Token keyword",
                "Break      : Token keyword",
                "Typed      : Token name, Expr initializer, VariableType type",
                "While      : Expr condition, Stmt body",
                "Import     : List<Token> name",
                "For        : Token name, Expr iterator, Stmt body",
                "Evaluate   : Expr value",
            }.ToList());

            Console.WriteLine("[ASTGenerator] AST nodes Expr and Stmt have been updated");
            Environment.Exit(0);
        }

        public static void RunFile(string path)
        {
            Run(File.ReadAllText(path));

            if (ErrorOccured) Environment.Exit(65);
            if (RuntimeErrorOccured) Environment.Exit(70);
        }
        private static int GetDepth(List<Token> tokens, TokenType opening, TokenType closing)
        {
            return Math.Max(0, tokens.Count(x => x.Type == opening) - tokens.Count(x => x.Type == closing));
        }
        private static int GetTotalDepth(List<Token> tokens)
        {
            return GetDepth(tokens, TokenType.LeftBrace, TokenType.RightBrace)
                + GetDepth(tokens, TokenType.LeftParenthesis, TokenType.RightParenthesis)
                + GetDepth(tokens, TokenType.LeftSquare, TokenType.RightSquare);
        }
        private static bool IsFinished(List<Token> tokens)
        {
            return GetDepth(tokens, TokenType.LeftBrace, TokenType.RightBrace) == 0
                && GetDepth(tokens, TokenType.LeftParenthesis, TokenType.RightParenthesis) == 0
                && GetDepth(tokens, TokenType.LeftSquare, TokenType.RightSquare) == 0;
        }
        private static void RunPrompt()
        {
            //while (true)
            //{
            //    Console.Write("> ");
            //    string line = Console.ReadLine();
            //    if (line == null) break;
            //    Run(line);
            //    ErrorOccured = false;
            //}

            Console.WriteLine("Welcome to the Ream interactive mode");
            Console.WriteLine("type exit to quit");
            Console.WriteLine("");

            while (true)
            {
                ErrorOccured = false;

                Console.Write("> ");
                string input = Console.ReadLine();
                if (input == null || input.ToLower() == "exit")
                    break;
                Lexer lexer = new(input);
                List<Token> tokens = lexer.Lex();
                while (!IsFinished(tokens))
                {
                    Console.Write(". " + string.Join("", Enumerable.Repeat(". ", GetTotalDepth(tokens))));
                    string txt = Console.ReadLine();
                    tokens.AddRange(new Lexer(txt).Lex());
                    tokens.Add(new(TokenType.Newline, "\n", '\n', tokens.Count(x => x.Type == TokenType.Newline)));
                }
                tokens.RemoveAll(x => x.Type == TokenType.End);
                tokens.Add(new(TokenType.End, "", null, tokens.Count(x => x.Type == TokenType.Newline)));

                Parser parser = new(tokens);
                object syntax = parser.ParseREPL();

                // Ignore it if there was a syntax error.
                if (ErrorOccured) continue;

                if (syntax is List<Stmt> list)
                    Interpreter.Instance.Interpret(list);
                else if (syntax is Expr expr)
                {
                    ReamObject obj = Interpreter.Instance.Interpret(expr);
                    if (obj != null)
                        Console.WriteLine(obj.RepresentAs<string>());
                }
            }
        }

        public static void Run(string source)
        {
            Lexer lexer = new(source);
            List<Token> tokens = lexer.Lex();
            Parser parser = new(tokens);
            List<Stmt> statements = parser.Parse();

            if (ErrorOccured) return;

            Interpreter.Instance.Interpret(statements);
        }
        public static void RuntimeError(RuntimeError error)
        {
            if (error.Token != null)
                Console.Error.WriteLine($"Error on line {error.Token.Line}: {error.Message}");
            else
                Console.Error.WriteLine($"Error: {error.Message}");
            RuntimeErrorOccured = true;
        }

        public static void Error(Token token, string message)
        {
            if (token.Type == TokenType.End)
                Report(token.Line, " at end", message);
            else
                Report(token.Line, $" at '{token.Raw}'", message);
        }

        public static void Error(int line, string message)
        {
            Report(line, "", message);
        }

        public static void Report(int line, string location, string message)
        {
            Console.Error.WriteLine($"Error on line {line}{location}: {message}");
        }
    }
}