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
        public static object Read()
        {
            return Console.ReadLine();
        }

        [ExternalFunction]
        public static void Sleep(object time)
        {
            Thread.Sleep(((double)time).ToInt());
        }
    }
    public class Interpreter : Expr.Visitor<Object>, Stmt.Visitor<Object>
    {
        public readonly Scope Globals;
        private Scope Scope;
        //private readonly Dictionary<Expr, int> Locals = new();
        public Interpreter()
        {
            Globals = new();
            Scope = new(Globals);

            Globals.Define("Main", new ExternalClass(typeof(MainInterpret), this));

            Globals.Define("print", new ExternalFunction((i, j) =>
            {
                Console.WriteLine(j.First());
                return null;
            }, 1));
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
            VariableType autoType = Scope.AutoDetectType(name, type);

            if (initializer != null && !autoType.HasFlag(VariableType.Dynamic))
                value = Evaluate(initializer);
            else
                value = initializer;

            Scope.Set(name, value, type);
            return value;
        }
        public void ExecuteBlock(List<Stmt> statements, Scope scope)
        {
            Scope previous = this.Scope;
            try
            {
                this.Scope = scope;

                foreach (Stmt statement in statements)
                {
                    Execute(statement);
                }
            }
            finally
            {
                this.Scope = previous;
            }
        }
        public List<object> GetIterator(Expr expression)
        {
            object obj = Evaluate(expression);
            if (obj is double d)
            {
                return Enumerable.Range(0, d.ToInt()).Select(x => (object)Convert.ToDouble(x)).ToList();
            }
            if (obj is List<object>)
            {
                return (List<object>)obj;
            }
            if (obj is string s)
            {
                return s.ToCharArray().Select(x => (object)x.ToString()).ToList();
            }
            return new();
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
            object obj = Scope.Get(expr.name);
            if (obj == null)
                return null;

            VariableData data = Scope.GetData(expr.name);

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
            Lambda function = new(expr, Scope);
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
                throw new RuntimeError(expr.paren, "Only functions, lambdas and classes can be called");
            }

            List<object> args = expr.arguments.Select(x => Evaluate(x)).ToList();
            ICallable function = (ICallable)callee;

            int count = function.ArgumentCount();
            if (count != -1)
                while (args.Count != count)
                    args.Add(null);

            return function.Call(this, args);
        }
        public object VisitIndexerExpr(Expr.Indexer expr)
        {
            List<object> iterator = GetIterator(expr.callee);
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
            object obj = Evaluate(expr.obj);
            if (obj is IClassInstance ci)
            {
                return ci.Get(expr.name);
            }

            throw new RuntimeError(expr.name, "Only instances have fields");
        }
        public object VisitSetExpr(Expr.Set expr)
        {
            // something overwriting original object
            object obj = Evaluate(expr.obj);

            if (obj is not IClassInstance)
            {
                throw new RuntimeError(expr.name, "Only instances have fields");
            }

            IClassInstance inst = obj as IClassInstance;

            VariableType type = inst.AutoDetectType(expr.name);

            object value = type.HasFlag(VariableType.Dynamic)
                ? expr.value
                : Evaluate(expr.value);

            inst.Set(expr.name, value, type);
            return value;
        }
        public object VisitThisExpr(Expr.This expr)
        {
            return Scope.Get(expr.keyword);
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
            ExecuteBlock(stmt.statements, new Scope(Scope));
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
            Function function = new(stmt, Scope);
            Scope.Set(stmt.name, function, stmt.type);
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
                Scope previous = this.Scope;
                try
                {
                    this.Scope = new Scope(this.Scope);
                    foreach (object item in GetIterator(stmt.iterator))
                    {
                        Scope.Set(stmt.name, item);
                        Execute(stmt.body);
                    }
                }
                finally
                {
                    this.Scope = previous;
                }
            }
            else
                foreach (object item in GetIterator(stmt.iterator))
                    Execute(stmt.body);

            return null;
        }
        public object VisitExpressionStmt(Stmt.Expression stmt)
        {
            Evaluate(stmt.expression);
            return null;
        }
        public object VisitWriteStmt(Stmt.Write stmt)
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
            Scope.Set(stmt.name, clss, VariableType.Global);

            return null;
        }
        public object VisitImportStmt(Stmt.Import stmt)
        {
            string path = stmt.name.Raw + ".dll";
            string libDataPath = Path.Join(Program.LibDataPath, path);
            if (File.Exists(path))
            {
                Assembly asm = Assembly.LoadFrom(path);
                LoadAssemblyLibrary(asm);
            }
            else if (File.Exists(libDataPath))
            {
                Assembly asm = Assembly.LoadFrom(libDataPath);
                LoadAssemblyLibrary(asm);
            }
            return null;
        }
        public void LoadAssemblyLibrary(Assembly asm)
        {
            foreach (Type type in asm.GetTypes()
                .Where(x => x.GetCustomAttribute<ExternalClassAttribute>() != null))
            {
                Globals.Define(type.Name, new ExternalClass(type, this), VariableType.Global);
            }
        }
        #endregion
    }
}
