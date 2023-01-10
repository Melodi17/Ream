using System.Reflection;
using ream.Interpreting.Objects;
using Ream.Lexing;
using Ream.Parsing;

namespace Ream.Interpreting;

// TODO: Syntax styling as follows:
/*
 * 1. Classes: MyClass - Capital camel case
 * 2. Methods: myMethod - Lower camel case
 * 3. Variables: myVariable - Lower camel case
 * 4. Constants: MY_CONSTANT - Upper snake case
 * 5. Core methods: ~myMethod - Lower camel case, prefixed with ~
 * 6. Braces start on the same line as the statement
 * 7. Chained statements, eg else if, are on the different lines to the previous statement
 */

public class Flags
{
    public bool Strict = false;
    public void Exit(int? code)
    {
        Environment.Exit(code ?? 0);
    }
    public string Type(ReamObject obj)
    {
        return obj.Type().RepresentAs<string>();
    }
}

public class Interpreter : Expr.Visitor<ReamObject>, Stmt.Visitor<ReamObject>, IDisposable
{
    public readonly Scope Globals;
    private Scope _scope;
    private Flags _flags;

    public Interpreter(Scope defaultScope = null)
    {
        this.Globals = defaultScope ?? new Scope();
        this._scope = new(this.Globals);
        this._flags = new();
    }

    public void Interpret(List<Stmt> statements)
    {
        try
        {
            foreach (Stmt statement in statements) this.Execute(statement);
        }
        catch (RuntimeError error)
        {
            Program.RuntimeError(error);
        }
        catch (FlowControlError error)
        {
            if (this._flags.Strict)
                Program.RuntimeError(new(error.SourceToken, "Unexpected flow control jump."));
        }
    }

    public ReamObject Interpret(Expr expression)
    {
        try
        {
            return this.Evaluate(expression);
        }
        catch (RuntimeError error)
        {
            Program.RuntimeError(error);
            return ReamNull.Instance;
        }
        catch (FlowControlError error)
        {
            if (this._flags.Strict)
                Program.RuntimeError(new(error.SourceToken, "Unexpected flow control jump."));

            return ReamNull.Instance;
        }
    }

    public ReamObject Evaluate(Expr expr)
    {
        return expr?.Accept(this);
    }

    public ReamReference EvaluateReference(Expr expr)
    {
        throw new NotImplementedException();
    }

    public ReamObject Evaluate(Expr expr, Scope scope)
    {
        Scope previous = this._scope;
        ReamObject result;
        try
        {
            this._scope = scope;
            result = expr?.Accept(this);
            this._scope.Dispose();
        }
        finally
        {
            this._scope = previous;
        }
        return result;
    }

    public void ExecuteBlock(List<Stmt> statements, Scope scope)
    {
        Scope previous = this._scope;
        try
        {
            this._scope = scope;

            foreach (Stmt statement in statements)
            {
                this.Execute(statement);
            }

            this._scope.Dispose();
        }
        finally
        {
            this._scope = previous;
        }
    }

    public void Execute(Stmt stmt)
    {
        stmt?.Accept(this);
    }

    public void LoadAssemblyLibrary(Assembly asm)
    {
        foreach (Type type in asm.GetTypes()
                     .Where(x => x.IsPublic))
        {
            ReamClassExternal reamClass = ReamClassExternal.From(type);
            this.Globals.Set(type.Name, reamClass, VariableType.Global);
        }
    }

    public void Dispose()
    {
        this.Globals.Dispose();
        this._scope.Dispose();
        this._flags = null;
    }

    public ReamObject VisitAssignExpr(Expr.Assign expr)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitBinaryExpr(Expr.Binary expr)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitTernaryExpr(Expr.Ternary expr)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitCallExpr(Expr.Call expr)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitIndexerExpr(Expr.Indexer expr)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitSetIndexerExpr(Expr.SetIndexer expr)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitSetExpr(Expr.Set expr)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitGetExpr(Expr.Get expr)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitChainExpr(Expr.Chain expr)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitGroupingExpr(Expr.Grouping expr)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitSequenceExpr(Expr.Sequence expr)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitDictionaryExpr(Expr.Dictionary expr)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitLambdaExpr(Expr.Lambda expr)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitLiteralExpr(Expr.Literal expr)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitLogicalExpr(Expr.Logical expr)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitThisExpr(Expr.This expr)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitDisposeExpr(Expr.Dispose expr)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitUnaryExpr(Expr.Unary expr)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitVariableExpr(Expr.Variable expr)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitBlockStmt(Stmt.Block stmt)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitClassStmt(Stmt.Class stmt)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitExpressionStmt(Stmt.Expression stmt)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitFunctionStmt(Stmt.Function stmt)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitMethodStmt(Stmt.Method stmt)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitIfStmt(Stmt.If stmt)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitReturnStmt(Stmt.Return stmt)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitContinueStmt(Stmt.Continue stmt)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitBreakStmt(Stmt.Break stmt)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitTypedStmt(Stmt.Typed stmt)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitWhileStmt(Stmt.While stmt)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitImportStmt(Stmt.Import stmt)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitForStmt(Stmt.For stmt)
    {
        throw new NotImplementedException();
    }

    public ReamObject VisitEvaluateStmt(Stmt.Evaluate stmt)
    {
        throw new NotImplementedException();
    }
}