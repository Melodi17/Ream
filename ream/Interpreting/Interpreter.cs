using Ream.Lexing;
using Ream.Parsing;
using Ream.Tools;

namespace Ream.Interpreting
{
    public class Interpreter : Expr.Visitor<Object>, Stmt.Visitor<Object>
    {
        public readonly Scope Globals;
        private Scope Scope;
        public Interpreter()
        {
            Globals = new();
            Scope = Globals;

            Globals.Define("write", new ExternalFunction((i, l) =>
            {
                Console.WriteLine(Stringify(l.First()));
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
        private object Evaluate(Expr expr)
        {
            return expr.Accept(this);
        }
        private bool IsTruthy(object obj)
        {
            if (obj == null) return false;
            if (obj is bool) return (bool)obj;
            if (obj is double) return (double)obj > 0;
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
        private string Stringify(object obj)
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

            return obj.ToString();
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
        public object VisitVariableExpr(Expr.Variable expr)
        {
            return Scope.Get(expr.name);
        }
        public object VisitGlobalStmt(Stmt.Global stmt)
        {
            return DeclareStmt(stmt.name, stmt.initializer, true);
        }
        public object VisitLocalStmt(Stmt.Local stmt)
        {
            return DeclareStmt(stmt.name, stmt.initializer, false);
        }
        public object DeclareStmt(Token name, Expr initializer, bool isGlobal)
        {
            object value = null;
            if (initializer != null)
            {
                value = Evaluate(initializer);
            }

            Scope.Set(name, value, isGlobal);
            return value;
        }
        public object VisitAssignExpr(Expr.Assign expr)
        {
            object value = Evaluate(expr.value);
            Scope.Set(expr.name, value);
            return value;
        }
        public object VisitBlockStmt(Stmt.Block stmt)
        {
            ExecuteBlock(stmt.statements, new Scope(Scope));
            return null;
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
        public object VisitLogicalExpr(Expr.Logical expr)
        {
            object left = Evaluate(expr.left);

            if (expr.@operator.Type == TokenType.Pipe_Pipe)
                if (IsTruthy(left)) return left;
                else
                if (!IsTruthy(left)) return left;

            return Evaluate(expr.right);
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
        public List<object> GetIterator(Expr expression)
        {
            object obj = Evaluate(expression);
            if (obj is double d)
            {
                return Enumerable.Range(0, d.ToInt()).Select(x => (object)x).ToList();
            }
            if (obj is string s)
            {
                return s.ToCharArray().Select(x => (object)x.ToString()).ToList();
            }
            return new(1);
        }
        public object VisitCallExpr(Expr.Call expr)
        {
            object callee = Evaluate(expr.callee);

            if (callee is not ICallable)
            {
                throw new RuntimeError(expr.paren, "Only functions and classes can be called");
            }

            List<object> args = expr.arguments.Select(x => Evaluate(x)).ToList();
            ICallable function = (ICallable)callee;

            int count = function.ArgumentCount();
            if (args.Count != count)
            {
                throw new RuntimeError(expr.paren, $"Expected {count} parameters, got {args.Count}");
            }

            return function.Call(this, args);
        }
        public object VisitFunctionStmt(Stmt.Function stmt)
        {
            Function function = new(stmt);
            Scope.Set(stmt.name, function);
            return null;
        }

        public object VisitReturnStmt(Stmt.Return stmt)
        {
            object value = null;
            if (stmt.value != null) value = Evaluate(stmt.value);

            throw new Return(value);
        }
    }
}
