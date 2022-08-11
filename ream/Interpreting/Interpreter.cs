using Ream.Lexing;
using Ream.Parsing;
using Ream.SDK;

namespace Ream.Interpreting
{
    public class Interpreter : Expr.Visitor<object>, Stmt.Visitor<object>
    {
        public readonly Scope Globals;
        private Scope scope;
        public Resolver resolver;
        public Interpreter()
        {
            Globals = new();
            scope = new(Globals);
            resolver = new(this);

            Globals.Define("print", new ExternalFunction((i, j) => { Console.WriteLine(resolver.Stringify(j[0])); return null; }, 0));
        }

        public void Interpret(List<Stmt> statements)
        {
            try
            {
                foreach (Stmt statement in statements)
                    Execute(statement);
            }
            catch (RuntimeError error)
            {
                Program.RuntimeError(error);
            }
        }
        private object Evaluate(Expr expr)
        {
            return expr.Accept(this);
        }
        public void ExecuteBlock(List<Stmt> statements, Scope scope)
        {
            Scope previous = this.scope;
            try
            {
                this.scope = scope;

                foreach (Stmt statement in statements)
                {
                    Execute(statement);
                }
            }
            finally
            {
                this.scope = previous;
            }
        }
        public void Execute(Stmt stmt)
        {
            stmt.Accept(this);
        }
        public object DeclareStmt(Token name, Expr initializer, VariableType type)
        {
            VariableType autoType = scope.AutoDetectType(name, type);

            object value;
            if (initializer != null && !autoType.HasFlag(VariableType.Dynamic))
                value = Evaluate(initializer);
            else
                value = initializer;

            scope.Set(name, value, type);
            return value;
        }

        public object VisitAssignExpr(Expr.Assign expr)
        {
            return DeclareStmt(expr.name, expr.value, VariableType.Normal);
        }
        public object VisitBinaryExpr(Expr.Binary expr)
        {
            return resolver.Compare(Evaluate(expr.left), Evaluate(expr.right), expr.@operator.Type);
        }
        public object VisitBlockStmt(Stmt.Block stmt)
        {
            ExecuteBlock(stmt.statements, new Scope(scope));
            return null;
        }
        public object VisitCallExpr(Expr.Call expr)
        {
            object callee = Evaluate(expr.callee);

            if (callee is not ICallable function)
                return null;

            List<object> args = expr.arguments.Select(x => Evaluate(x)).ToList();

            int count = function.ArgumentCount();
            if (count > 0)
                while (args.Count < count)
                    args.Add(null);

            return function.Call(this, args);
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
            scope.Set(stmt.name, clss, VariableType.Global);

            return null;
        }
        public object VisitEvaluateStmt(Stmt.Evaluate stmt)
        {
            object obj = Evaluate(stmt.value);
            Program.Run(resolver.Stringify(obj));
            return null;
        }
        public object VisitExpressionStmt(Stmt.Expression stmt)
        {
            Evaluate(stmt.expression);
            return null;
        }
        public object VisitForStmt(Stmt.For stmt)
        {
            if (stmt.name != null)
            {
                Scope previous = scope;
                try
                {
                    scope = new Scope(scope);
                    foreach (object item in resolver.GetIterator(Evaluate(stmt.iterator)))
                    {
                        scope.Set(stmt.name, item);
                        Execute(stmt.body);
                    }
                }
                finally
                {
                    scope = previous;
                }
            }
            else
                foreach (object _ in resolver.GetIterator(Evaluate(stmt.iterator)))
                    Execute(stmt.body);

            return null;
        }
        public object VisitFunctionStmt(Stmt.Function stmt)
        {
            Function function = new(stmt, scope);
            scope.Set(stmt.name, function, stmt.type);
            return null;
        }
        public object VisitGetExpr(Expr.Get expr)
        {
            IPropable prop = resolver.GetPropable(Evaluate(expr.obj));
            return prop.Get(expr.name);
        }
        public object VisitGroupingExpr(Expr.Grouping expr)
        {
            return Evaluate(expr.expression);
        }
        public object VisitIfStmt(Stmt.If stmt)
        {
            if (resolver.Truthy(Evaluate(stmt.condition)))
                Execute(stmt.thenBranch);
            else if (stmt.elseBranch != null)
                Execute(stmt.elseBranch);

            return null;
        }
        public object VisitImportStmt(Stmt.Import stmt)
        {
            throw new NotImplementedException();
        }
        public object VisitIndexerExpr(Expr.Indexer expr)
        {
            return resolver.GetIndex(Evaluate(expr.callee), Evaluate(expr.index));
        }
        public object VisitLambdaExpr(Expr.Lambda expr)
        {
            Lambda function = new(expr, scope);
            return function;
        }
        public object VisitLiteralExpr(Expr.Literal expr)
        {
            return expr.value;
        }
        public object VisitLogicalExpr(Expr.Logical expr)
        {
            object left = Evaluate(expr.left);

            if (expr.@operator.Type == TokenType.Pipe_Pipe)
            {
                if (resolver.Truthy(left))
                    return left;
            }
            else
            {
                if (!resolver.Truthy(left))
                    return left;
            }

            return Evaluate(expr.right);
        }
        public object VisitReturnStmt(Stmt.Return stmt)
        {
            object value = null;
            if (stmt.value != null) value = Evaluate(stmt.value);

            throw new Return(value);
        }
        public object VisitScriptStmt(Stmt.Script stmt)
        {
            throw new NotImplementedException();
        }
        public object VisitSequenceExpr(Expr.Sequence expr)
        {
            List<object> items = expr.items.Select(x => Evaluate(x)).ToList();
            return items;
        }
        public object VisitSetExpr(Expr.Set expr)
        {
            IPropable prop = resolver.GetPropable(Evaluate(expr.obj));

            VariableType type = prop.AutoDetectType(expr.name);
            object value = type.HasFlag(VariableType.Dynamic)
                ? expr.value
                : Evaluate(expr.value);

            prop.Set(expr.name, value, type);
            return value;
        }
        public object VisitThisExpr(Expr.This expr)
        {
            return scope.Get(expr.keyword);
        }
        public object VisitTypedStmt(Stmt.Typed stmt)
        {
            return DeclareStmt(stmt.name, stmt.initializer, stmt.type);
        }
        public object VisitUnaryExpr(Expr.Unary expr)
        {
            object right = Evaluate(expr.right);

            return expr.@operator.Type switch
            {
                TokenType.Not => !resolver.Truthy(right),
                TokenType.Minus => right is double d ? -(double)d : null,
                _ => null,
            };
        }
        public object VisitVariableExpr(Expr.Variable expr)
        {
            object obj = scope.Get(expr.name);
            if (obj == null)
                return null;

            VariableData data = scope.GetData(expr.name);

            if (data.Type.HasFlag(VariableType.Dynamic))
                return Evaluate((Expr)obj);
            else
                return obj;
        }
        public object VisitWhileStmt(Stmt.While stmt)
        {
            while (resolver.Truthy(Evaluate(stmt.condition)))
                Execute(stmt.body);
            return null;
        }
    }
}
