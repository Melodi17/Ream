using System.Runtime.Serialization.Formatters.Binary;
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

            string keyword = args.Length > 0 ? args[0].ToLower() : "";
            if (keyword == "build")
            {
                string sourceFile = string.Join(" ", args.Skip(1));
                string destfile = Path.Join(Path.GetDirectoryName(sourceFile),
                    Path.GetFileNameWithoutExtension(sourceFile) + ".cr");
                Compile(sourceFile, destfile);
            }
            else if (keyword == "time")
            {
                var startTime = DateTime.Now;
                RunFile(string.Join(" ", args.Skip(1)));
                var endTime = DateTime.Now;
                var duration = endTime - startTime;
                Console.WriteLine($"Execution took {duration}");
            }
            else if (args.Any())
                RunFile(string.Join(" ", args));
            else
                RunPrompt();
        }

        private static void UpdateAST()
        {
            ASTGenerator.DefineAst(Path.Join("..", "..", "..", "Parsing", "ASTExpr.cs"), "Expr", new string[]
            {
                "Assign    : Token name, Expr value",
                "Binary    : Expr left, Token @operator, Expr right",
                "Call      : Expr callee, Token paren, List<Expr> arguments",
                "Indexer   : Expr callee, Token paren, Expr index",
                "Mixer     : Expr callee, Token paren, Expr index, Expr value",
                "Get       : Expr obj, Token name",
                "Grouping  : Expr expression",
                "Sequence  : List<Expr> items",
                "Lambda    : List<Token> parameters, List<Stmt> body",
                "Literal   : Object value",
                "Logical   : Expr left, Token @operator, Expr right",
                "Set       : Expr obj, Token name, Expr value",
                "This      : Token keyword",
                "Unary     : Token @operator, Expr right",
                "Translate : Token @operator, Token name",
                "Variable  : Token name"
            }.ToList());

            ASTGenerator.DefineAst(Path.Join("..", "..", "..", "Interpreting", "Stmt.cs"), "Stmt", new string[]
            {
                "Block      : List<Stmt> statements",
                "Class      : Token name, List<Stmt.Function> functions",
                "Expression : Expr expression",
                "Function   : Token name, VariableType type, List<Token> parameters, List<Stmt> body",
                "If         : Expr condition, Stmt thenBranch, Stmt elseBranch",
                "Return     : Token keyword, Expr value",
                "Typed      : Token name, Expr initializer, VariableType type",
                "While      : Expr condition, Stmt body",
                "Import     : Token name",
                "For        : Token name, Expr iterator, Stmt body",
                "Evaluate   : Expr value",
                "Script     : Token body",
            }.ToList());

            Console.WriteLine("[ASTGenerator] AST nodes Expr and Stmt have been updated");
            Environment.Exit(0);
        }

        public static void RunFile(string path)
        {
            string ext = Path.GetExtension(path);
            if (ext == ".cr")
                RunCompiled(File.ReadAllBytes(path));
            else
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
            //Console.WriteLine("Pointer count: " + Pointer.GetPointerCount());
            //Console.WriteLine(new ASTPrinter().Print(expression));
        }
        public static void Compile(string sourceFile, string destinationFile)
        {
            Lexer lexer = new(File.ReadAllText(sourceFile));
            List<Token> tokens = lexer.Lex();
            Parser parser = new(tokens);
            List<Stmt> statements = parser.Parse();

            if (ErrorOccured) return;

            File.WriteAllBytes(destinationFile, SerializeBinary(statements));
        }
        public static void RunCompiled(byte[] source)
        {
            List<Stmt> statements = DeserializeBinary<List<Stmt>>(source);

            Interpreter.Interpret(statements);
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

        public static T DeserializeBinary<T>(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                var formatter = new BinaryFormatter();
                stream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
            }
        }
        public static byte[] SerializeBinary<T>(T data)
        {
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, data);
                stream.Flush();
                stream.Position = 0;
                return stream.ToArray();
            }
        }
    }
}