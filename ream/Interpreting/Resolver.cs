using Ream.Lexing;
using Ream.Parsing;

namespace Ream.Interpreting
{
    //public class Resolver : Expr.Visitor<object>, Stmt.Visitor<object>
    //{
    //    private readonly Interpreter Interpreter;
    //    private readonly Stack<Dictionary<string, bool>> Scopes = new();

    //    public Resolver(Interpreter interpreter)
    //    {
    //        Interpreter = interpreter;
    //    }

    //    public object VisitBlockStmt(Stmt.Block stmt)
    //    {
    //        BeginScope();
    //        Resolve(stmt.statements);
    //        EndScope();
    //        return null;
    //    }
    //    public object VisitLocalStmt(Stmt.Local stmt)
    //    {
    //        Declare(stmt.name);
    //        if (stmt.initializer != null)
    //        {
    //            Resolve(stmt.initializer);
    //        }
    //        Define(stmt.name);
    //        return null;
    //    }
    //    public object VisitGlobalStmt(Stmt.Global stmt)
    //    {
    //        Declare(stmt.name);
    //        if (stmt.initializer != null)
    //        {
    //            Resolve(stmt.initializer);
    //        }
    //        Define(stmt.name);
    //        return null;
    //    }
    //    public object VisitFunctionStmt(Stmt.Function stmt)
    //    {
    //        Declare(stmt.name);
    //        Define(stmt.name);

    //        ResolveFunction(stmt);
    //        return null;
    //    }
    //    public object VisitLambdaExpr(Expr.Lambda expr)
    //    {
    //        ResolveLambda(expr);
    //        return null;
    //    }
    //    public object VisitExpressionStmt(Stmt.Expression stmt)
    //    {
    //        Resolve(stmt.expression);
    //        return null;
    //    }
    //    public object VisitIfStmt(Stmt.If stmt)
    //    {
    //        Resolve(stmt.condition);
    //        Resolve(stmt.thenBranch);
    //        if (stmt.elseBranch != null) Resolve(stmt.elseBranch);
    //        return null;
    //    }
    //    public object VisitWriteStmt(Stmt.Write stmt)
    //    {
    //        Resolve(stmt.expression);
    //        return null;
    //    }
    //    public object VisitReturnStmt(Stmt.Return stmt)
    //    {
    //        if (stmt.value != null) Resolve(stmt.value);
    //        return null;
    //    }
    //    public object VisitWhileStmt(Stmt.While stmt)
    //    {
    //        Resolve(stmt.condition);
    //        Resolve(stmt.body);
    //        return null;
    //    }
    //    public object VisitBinaryExpr(Expr.Binary expr)
    //    {
    //        Resolve(expr.left);
    //        Resolve(expr.right);
    //        return null;
    //    }
    //    public object VisitCallExpr(Expr.Call expr)
    //    {
    //        Resolve(expr.callee);
    //        foreach (Expr arg in expr.arguments)
    //            Resolve(arg);

    //        return null;
    //    }
    //    public object VisitGroupingExpr(Expr.Grouping expr)
    //    {
    //        Resolve(expr.expression);
    //        return null;
    //    }
    //    public object VisitLiteralExpr(Expr.Literal expr)
    //    {
    //        return null;
    //    }
    //    public object VisitLogicalExpr(Expr.Logical expr)
    //    {
    //        Resolve(expr.left);
    //        Resolve(expr.right);
    //        return null;
    //    }
    //    public object VisitUnaryExpr(Expr.Unary expr)
    //    {
    //        Resolve(expr.right);
    //        return null;
    //    }
    //    public object VisitVariableExpr(Expr.Variable expr)
    //    {
    //        if (Scopes.Any() && Scopes.Peek()[expr.name.Raw] == false)
    //        {
    //            Program.Error(expr.name, "Can't read self while in initializer");
    //        }

    //        ResolveLocal(expr, expr.name);
    //        return null;
    //    }
    //    public object VisitAssignExpr(Expr.Assign expr)
    //    {
    //        Resolve(expr.value);
    //        ResolveLocal(expr, expr.name);
    //        return null;
    //    }
    //    public object VisitForStmt(Stmt.For stmt)
    //    {
    //        Resolve(stmt.iterator);
    //        Resolve(stmt.body);
    //        return null;
    //    }
    //    private void ResolveFunction(Stmt.Function stmt)
    //    {
    //        BeginScope();
    //        foreach (Token tok in stmt.parameters)
    //        {
    //            Declare(tok);
    //            Define(tok);
    //        }
    //        Resolve(stmt.body);
    //        EndScope();
    //    }
    //    private void ResolveLambda(Expr.Lambda expr)
    //    {
    //        BeginScope();
    //        foreach (Token tok in expr.parameters)
    //        {
    //            Declare(tok);
    //            Define(tok);
    //        }
    //        Resolve(expr.body);
    //        EndScope();
    //    }
    //    private void ResolveLocal(Expr expr, Token name)
    //    {
    //        for (int i = Scopes.Count - 1; i >= 0; i--)
    //        {
    //            if (Scopes.ElementAt(i).ContainsKey(name.Raw))
    //            {
    //                Interpreter.Resolve(expr, Scopes.Count - 1 - i);
    //                return;
    //            }
    //        }
    //    }

    //    private void Declare(Token name)
    //    {
    //        if (!Scopes.Any()) return;

    //        Dictionary<string, bool> scope = Scopes.Peek();
    //        scope[name.Raw] = false;
    //    }
    //    private void Define(Token name)
    //    {
    //        if (!Scopes.Any()) return;

    //        Dictionary<string, bool> scope = Scopes.Peek();
    //        scope[name.Raw] = true;
    //    }
    //    private void Resolve(List<Stmt> statements)
    //    {
    //        foreach (Stmt stmt in statements)
    //            Resolve(stmt);
    //    }
    //    private void Resolve(Stmt statement)
    //    {
    //        statement.Accept(this);
    //    }
    //    private void Resolve(Expr expression)
    //    {
    //        expression.Accept(this);
    //    }
    //    private void BeginScope()
    //    {
    //        Scopes.Push(new());
    //    }
    //    private void EndScope()
    //    {
    //        Scopes.Pop();
    //    }
    //}
}
