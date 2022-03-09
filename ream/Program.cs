using Ream.Interpreting;
using Ream.Lexing;
using Ream.Parsing;
using Ream.Tools;

namespace Ream
{
    public class Program
    {
        public static readonly Interpreter Interpreter = new();
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

            if (args.Any())
                RunFile(args.First());
            else
                RunPrompt();
        }

        private static void UpdateAST()
        {
            ASTGenerator.DefineAst(Path.Join("..", "..", "..", "Parsing", "ASTExpr.cs"), "Expr", new string[]
            {
                "Assign   : Token name, Expr value",
                "Binary   : Expr left, Token @operator, Expr right",
                "Call     : Expr callee, Token paren, List<Expr> arguments",
                "Indexer  : Expr callee, Token paren, Expr index",
                "Get      : Expr obj, Token name",
                "Grouping : Expr expression",
                "Sequence : List<Expr> items",
                "Lambda   : List<Token> parameters, List<Stmt> body",
                "Literal  : Object value",
                "Logical  : Expr left, Token @operator, Expr right",
                "Set      : Expr obj, Token name, Expr value",
                "This     : Token keyword",
                "Unary    : Token @operator, Expr right",
                "Variable : Token name"
            }.ToList());

            ASTGenerator.DefineAst(Path.Join("..", "..", "..", "Interpreting", "Stmt.cs"), "Stmt", new string[]
            {
                "Block      : List<Stmt> statements",
                "Class      : Token name, List<Stmt.Function> functions",
                "Expression : Expr expression",
                "Function   : Token name, VariableType type, List<Token> parameters, List<Stmt> body",
                "If         : Expr condition, Stmt thenBranch, Stmt elseBranch",
                "Print      : Expr expression",
                "Return     : Token keyword, Expr value",
                "Typed      : Token name, Expr initializer, VariableType type",
                "While      : Expr condition, Stmt body",
                "Import     : Token name",
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

        private static void RunPrompt()
        {
            while (true)
            {
                Console.Write("> ");
                string line = Console.ReadLine();
                if (line == null) break;
                Run(line);
                ErrorOccured = false;
            }
        }

        public static void Run(string source)
        {
            Lexer lexer = new(source);
            List<Token> tokens = lexer.Lex();
            Parser parser = new(tokens);
            List<Stmt> statements = parser.Parse();

            if (ErrorOccured) return;

            Interpreter.Interpret(statements);
            //Console.WriteLine(new ASTPrinter().Print(expression));
        }
        public static void RuntimeError(RuntimeError error)
        {
            Console.Error.WriteLine($"Error on line {error.Token.Line}: {error.Message}");
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