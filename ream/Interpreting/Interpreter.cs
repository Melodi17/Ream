using System.Reflection;
using Ream.Lexing;
using Ream.Parsing;
using Ream.SDK;
using Ream.Tools;

namespace Ream.Interpreting
{
    public class MainInterpret
    {
        [ExternalVariable]
        public static object Time => DateTime.Now.ToString();

        [ExternalFunction]
        public static void WriteLine(object text)
        {
            Console.WriteLine(Interpreter.Stringify(text ?? ""));
        }

        [ExternalFunction]
        public static void Write(object text)
        {
            Console.Write(Interpreter.Stringify(text ?? ""));
        }

        [ExternalFunction]
        public static object Read(object text)
        {
            Write(text);
            return Console.ReadLine();
        }

        [ExternalFunction]
        public static void Sleep(object time)
        {
            Thread.Sleep(((double)time).ToInt());
        }
    }
    public class CastInterpret
    {
        [ExternalFunction]
        public static object String(object obj)
        {
            return Interpreter.Stringify(obj);
        }

        [ExternalFunction]
        public static object Number(object obj)
        {
            if (double.TryParse(Interpreter.Stringify(obj), out double res))
                return res;

            return null;
        }
    }
    public class Interpreter : Expr.Visitor<Object>, Stmt.Visitor<Object>
    {
        public readonly Scope Globals;
        private Dictionary<long, Scope> Scope = new();
        private long CurrentThread;
        private Stack<object> Script = new();

        //private readonly Dictionary<Expr, int> Locals = new();
        public Interpreter()
        {
            Globals = new();
            CurrentThread = 0;
            Scope[CurrentThread] = new(Globals);

            Globals.Define("Main", new ExternalClass(typeof(MainInterpret), this));
            Globals.Define("Cast", new ExternalClass(typeof(CastInterpret), this));

            //Globals.Define("print", new ExternalFunction((i, j) =>
            //{
            //    Console.WriteLine(j.First());
            //    return null;
            //}, 1));
        }
        
        public void Interpret(List<Stmt> statements)
        {
            try
            {
                foreach (Stmt statement in statements)
                {
                    Execute(statement);
                }
            }
            catch (RuntimeError error)
            {
                Program.RuntimeError(error);
            }
        }
        public void Execute(Stmt stmt)
        {
            stmt.Accept(this);
        }
        private object Evaluate(Expr expr)
        {
            return expr.Accept(this);
        }
        private bool IsTruthy(object obj)
        {
            if (obj == null) return false;
            if (obj is bool) return (bool)obj;
            if (obj is double) return (double)obj > 0;
            if (obj is List<object>) return ((List<object>)obj).Any();
            return true;
        }
        private bool IsEqual(object left, object right)
        {
            if (left == null && right == null) return true;
            if (left == null) return false;

            return left.Equals(right);
        }
        private void CheckIntergerOperand(Token token, object obj)
        {
            if (obj is double) return;

            throw new RuntimeError(token, "Operand must be an interger");
        }
        private void CheckIntergerOperands(Token token, object left, object right)
        {
            if (left is double && right is double) return;

            throw new RuntimeError(token, "Operands must be an intergers");
        }
        public static string Stringify(object obj)
        {
            if (obj == null) return "null";
            if (obj is double)
            {
                string text = obj.ToString();
                if (text.EndsWith(".0"))
                {
                    text = text[..^2];
                }
                return text;
            }
            if (obj is bool) return obj.ToString().ToLower();
            if (obj is List<object>)
            {
                return string.Join(", ", ((List<object>)obj).Select(x => Stringify(x)));
            }

            return obj.ToString();
        }
        public object DeclareStmt(Token name, Expr initializer, VariableType type)
        {
            object value = null;
            VariableType autoType = Scope[CurrentThread].AutoDetectType(name, type);

            if (initializer != null && !autoType.HasFlag(VariableType.Dynamic))
                value = Evaluate(initializer);
            else
                value = initializer;

            Scope[CurrentThread].Set(name, value, type);
            return value;
        }
        public void ExecuteBlock(List<Stmt> statements, Scope scope)
        {
            Scope previous = this.Scope[CurrentThread];
            try
            {
                this.Scope[CurrentThread] = scope;

                foreach (Stmt statement in statements)
                {
                    Execute(statement);
                }
            }
            finally
            {
                this.Scope[CurrentThread] = previous;
            }
        }
        public List<object> GetIterator(Token tok, Expr expression)
        {
            object obj = Evaluate(expression);
            if (obj is IIterable i)
            {
                return i.GetIterator();
            }
            else if (obj is double d)
            {
                return Enumerable.Range(0, d.ToInt()).Select(x => (object)Convert.ToDouble(x)).ToList();
            }
            else if (obj is List<object> l)
            {
                return l;
            }
            else if (obj is string s)
            {
                return s.ToCharArray().Select(x => (object)x.ToString()).ToList();
            }
            else
            {
                throw new RuntimeError(tok, "Object was not Iterable");
            }
        }
        public IPropable GetPropable(Token tok, Expr expression)
        {
            object obj = Evaluate(expression);
            if (obj is IPropable p)
            {
                return p;
            }
            else if (obj is List<object> l)
            {
                return new ListPropMap(l);
            }
            else if (obj is string s)
            {
                return new StringPropMap(s);
            }
            else if (obj != null)
            {
                return new AutoPropMap(obj);
            }
            else
            {
                throw new RuntimeError(tok, "Object was not Propable");
            }
        }
        public void LoadAssemblyLibrary(Assembly asm)
        {
            foreach (Type type in asm.GetTypes()
                .Where(x => x.GetCustomAttribute<ExternalClassAttribute>() != null))
            {
                Globals.Define(type.Name, new ExternalClass(type, this), VariableType.Global);
            }
        }

        #region VisitExpr
        public object VisitAssignExpr(Expr.Assign expr)
        {
            //object value = Evaluate(expr.value);
            //Scope.Set(expr.name, value);
            //return value;
            return DeclareStmt(expr.name, expr.value, VariableType.Normal);
        }
        public object VisitVariableExpr(Expr.Variable expr)
        {
            object obj = Scope[CurrentThread].Get(expr.name);
            if (obj == null)
                return null;

            VariableData data = Scope[CurrentThread].GetData(expr.name);

            if (data.Type.HasFlag(VariableType.Dynamic))
            {
                // Evaluate expression now
                return Evaluate((Expr)obj);
            }
            else
            {
                // Get already evaluated expression
                return obj;
            }
        }
        public object VisitLambdaExpr(Expr.Lambda expr)
        {
            Lambda function = new(expr, Scope[CurrentThread]);
            return function;
        }
        public object VisitBinaryExpr(Expr.Binary expr)
        {
            object left = Evaluate(expr.left);
            object right = Evaluate(expr.right);

            switch (expr.@operator.Type)
            {
                case TokenType.Plus:
                    if (left is double && right is double)
                        return (double)left + (double)right;
                    else if (left is string && right is string)
                        return (string)left + (string)right;
                    else
                        throw new RuntimeError(expr.@operator, "Operands must be two intergers or two strings");
                    break;

                case TokenType.Minus:
                    CheckIntergerOperands(expr.@operator, left, right);
                    return (double)left - (double)right;

                case TokenType.Star:
                    if (left is double && right is double)
                        return (double)left * (double)right;
                    else if (left is string && right is double)
                        return ((string)left).Multiply(((double)right).ToInt());
                    else if (left is double && right is string)
                        return ((string)right).Multiply(((double)left).ToInt());
                    else
                        throw new RuntimeError(expr.@operator, "Operands must be two intergers or a string and an interger");
                    break;

                case TokenType.Slash:
                    CheckIntergerOperands(expr.@operator, left, right);
                    return (double)left / (double)right;

                case TokenType.Greater:
                    CheckIntergerOperands(expr.@operator, left, right);
                    return (double)left > (double)right;
                case TokenType.Greater_Equal:
                    CheckIntergerOperands(expr.@operator, left, right);
                    return (double)left >= (double)right;
                case TokenType.Less:
                    CheckIntergerOperands(expr.@operator, left, right);
                    return (double)left < (double)right;
                case TokenType.Less_Equal:
                    CheckIntergerOperands(expr.@operator, left, right);
                    return (double)left <= (double)right;

                case TokenType.Equal_Equal:
                    return IsEqual(left, right);
                case TokenType.Not_Equal:
                    return !IsEqual(left, right);
            }

            return null;
        }
        public object VisitGroupingExpr(Expr.Grouping expr)
        {
            return Evaluate(expr.expression);
        }
        public object VisitLiteralExpr(Expr.Literal expr)
        {
            return expr.value;
        }
        public object VisitUnaryExpr(Expr.Unary expr)
        {
            object right = Evaluate(expr.right);

            switch (expr.@operator.Type)
            {
                case TokenType.Not:
                    return !IsTruthy(right);
                case TokenType.Minus:
                    CheckIntergerOperand(expr.@operator, right);
                    return -(double)right;
            }

            return null;
        }
        public object VisitCallExpr(Expr.Call expr)
        {
            object callee = Evaluate(expr.callee);

            if (callee is not ICallable)
            {
                throw new RuntimeError(expr.paren, "Object was not Callable");
            }

            List<object> args = expr.arguments.Select(x => Evaluate(x)).ToList();
            ICallable function = (ICallable)callee;

            int count = function.ArgumentCount();
            if (count > 0)
                while (args.Count < count)
                    args.Add(null);

            return function.Call(this, args);
        }
        public object VisitIndexerExpr(Expr.Indexer expr)
        {
            List<object> iterator = GetIterator(expr.paren, expr.callee);
            int index = ((double)Evaluate(expr.index)).ToInt();
            if (index < 0 || index >= iterator.Count)
            {
                //throw new RuntimeError(expr.paren, $"Index {index} was out of bounds (below 0 or above/equal to {iterator.Count})");
                return null;
            }

            return iterator[index];
        }
        public object VisitSequenceExpr(Expr.Sequence expr)
        {
            List<object> items = expr.items.Select(x => Evaluate(x)).ToList();
            return items;
        }
        public object VisitLogicalExpr(Expr.Logical expr)
        {
            object left = Evaluate(expr.left);

            if (expr.@operator.Type == TokenType.Pipe_Pipe)
                if (IsTruthy(left)) return left;
                else
                if (!IsTruthy(left)) return left;

            return Evaluate(expr.right);
        }
        public object VisitGetExpr(Expr.Get expr)
        {
            IPropable prop = GetPropable(expr.name, expr.obj);
            return prop.Get(expr.name);
        }
        public object VisitSetExpr(Expr.Set expr)
        {
            IPropable prop = GetPropable(expr.name, expr.obj);

            VariableType type = prop.AutoDetectType(expr.name);
            object value = type.HasFlag(VariableType.Dynamic)
                ? expr.value
                : Evaluate(expr.value);

            prop.Set(expr.name, value, type);
            return value;
        }
        public object VisitThisExpr(Expr.This expr)
        {
            return Scope[CurrentThread].Get(expr.keyword);
        }
        #endregion

        #region VisitStmt
        public object VisitIfStmt(Stmt.If stmt)
        {
            if (IsTruthy(Evaluate(stmt.condition)))
            {
                Execute(stmt.thenBranch);
            }
            else if (stmt.elseBranch != null)
            {
                Execute(stmt.elseBranch);
            }

            return null;
        }
        public object VisitBlockStmt(Stmt.Block stmt)
        {
            ExecuteBlock(stmt.statements, new Scope(Scope[CurrentThread]));
            return null;
        }
        public object VisitTypedStmt(Stmt.Typed stmt)
        {
            return DeclareStmt(stmt.name, stmt.initializer, stmt.type);
        }
        public object VisitReturnStmt(Stmt.Return stmt)
        {
            object value = null;
            if (stmt.value != null) value = Evaluate(stmt.value);

            throw new Return(value);
        }
        public object VisitFunctionStmt(Stmt.Function stmt)
        {
            Function function = new(stmt, Scope[CurrentThread]);
            Scope[CurrentThread].Set(stmt.name, function, stmt.type);
            return null;
        }
        public object VisitWhileStmt(Stmt.While stmt)
        {
            while (IsTruthy(Evaluate(stmt.condition)))
            {
                Execute(stmt.body);
            }
            return null;
        }
        public object VisitForStmt(Stmt.For stmt)
        {
            if (stmt.name != null)
            {
                Scope previous = this.Scope[CurrentThread];
                try
                {
                    this.Scope[CurrentThread] = new Scope(this.Scope[CurrentThread]);
                    foreach (object item in GetIterator(stmt.name, stmt.iterator))
                    {
                        Scope[CurrentThread].Set(stmt.name, item);
                        Execute(stmt.body);
                    }
                }
                finally
                {
                    this.Scope[CurrentThread] = previous;
                }
            }
            else
                foreach (object item in GetIterator(stmt.name, stmt.iterator))
                    Execute(stmt.body);

            return null;
        }
        public object VisitExpressionStmt(Stmt.Expression stmt)
        {
            Evaluate(stmt.expression);
            return null;
        }
        public object VisitPrintStmt(Stmt.Print stmt)
        {
            object value = Evaluate(stmt.expression);
            Console.WriteLine(Stringify(value));
            return null;
        }
        public object VisitClassStmt(Stmt.Class stmt)
        {
            Scope scope = new(Globals);
            Scope staticScope = new(Globals);
            foreach (Stmt.Function funStmt in stmt.functions)
            {
                Function function;
                if (funStmt.type.HasFlag(VariableType.Static))
                {
                    function = new(funStmt, staticScope);
                    staticScope.Set(funStmt.name, function, funStmt.type);
                }
                else
                {
                    function = new(funStmt, scope);
                    scope.Set(funStmt.name, function, funStmt.type);
                }
            }
            Class clss = new(stmt.name.Raw, this, scope, staticScope);
            Scope[CurrentThread].Set(stmt.name, clss, VariableType.Global);

            return null;
        }
        public object VisitImportStmt(Stmt.Import stmt)
        {
            string dllPath = stmt.name.Raw + ".dll";
            string dllLibDataPath = Path.Join(Program.LibDataPath, dllPath);
            if (File.Exists(dllPath))
            {
                Assembly asm = Assembly.LoadFrom(dllPath);
                LoadAssemblyLibrary(asm);
            }
            else if (File.Exists(dllLibDataPath))
            {
                Assembly asm = Assembly.LoadFrom(dllLibDataPath);
                LoadAssemblyLibrary(asm);
            }
            else
            {
                string reamPath = stmt.name.Raw + ".r";
                string reamLibDataPath = Path.Join(Program.LibDataPath, reamPath);
                if (File.Exists(reamPath))
                {
                    Program.RunFile(reamPath);
                }
                else if (File.Exists(reamLibDataPath))
                {
                    Program.RunFile(reamLibDataPath);
                }
            }

            return null;
        }
        public object VisitEvaluateStmt(Stmt.Evaluate stmt)
        {
            object obj = Evaluate(stmt.value);
            Program.Run(Stringify(obj));
            return null;
        }
        public object VisitThreadStmt(Stmt.Thread stmt)
        {
            // TODO: Multiple threads
            Execute(stmt.body);
            return null;
        }
        public object VisitScriptStmt(Stmt.Script stmt)
        {
            string[] splt = stmt.body.Literal.ToString().Split(" ", 2);
            string keyword = splt[0].ToLower();
            string args = splt.Length > 1 ? splt[1] : "";

            switch (keyword)
            {
                case "dispose":
                    Scope[CurrentThread].Dispose(args);
                    break;

                case "store":
                    Script.Push(Scope[CurrentThread].Get(args));
                    break;

                case "load":
                    Scope[CurrentThread].Set(args, Script.Pop());
                    break;

                case "mem":
                    Script.Push(Scope[CurrentThread]
                        .All()
                        .Select(x => (object)new KeyValuePropMap(x.Key, x.Value))
                        .ToList());
                    break;

                case "mov":
                    Script.Push(args);
                    break;

                default:
                    throw new RuntimeError(stmt.body, "Script failed, unknown keyword");
                    break;
            }

            return null;
        }
        #endregion
    }
}