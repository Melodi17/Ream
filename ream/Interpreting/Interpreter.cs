using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using ream.Interpreting.Objects;
using Ream.Lexing;
using Ream.Parsing;

namespace Ream.Interpreting;

// NOTE: Syntax styling as follows:
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
        return obj.Type().Value;
    }
}
public class Interpreter : Expr.Visitor<ReamObject>, Stmt.Visitor<ReamObject>, IDisposable
{
    public static Interpreter Instance => instance ??= new();
    private static Interpreter instance;
    public readonly Scope Globals;
    private Scope _scope;
    private Flags _flags;

    private Interpreter(Scope defaultScope = null)
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

    public bool SetReference(Expr expr, ReamObject value)
    {
        switch (expr)
        {
            case Expr.Variable variable:
                this._scope.Set(variable.name.Raw, value);
                return true;
            
            case Expr.Get get:
                ReamObject obj = this.Evaluate(get.obj);
                obj.SetMember(get.name.Raw, value);
                return true;
            
            case Expr.Indexer indexer:
                ReamObject callee = this.Evaluate(indexer.callee);
                ReamObject index = this.Evaluate(indexer.index);
                callee.SetIndex(index, value);
                return true;
        }
        
        return false;
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
        ReamObject value = this.Evaluate(expr.value);
        this._scope.Set(expr.name.Raw, value);
        return value;
    }

    public ReamObject VisitBinaryExpr(Expr.Binary expr)
    {
        ReamObject left = this.Evaluate(expr.left);
        ReamObject right = this.Evaluate(expr.right);

        if (expr.@operator.Type is TokenType.PlusEqual or TokenType.MinusEqual or TokenType.StarEqual or TokenType.SlashEqual)
        {
            ReamObject result = expr.@operator.Type switch
            {
                TokenType.PlusEqual => left.Add(right),
                TokenType.MinusEqual => left.Subtract(right),
                TokenType.StarEqual => left.Multiply(right),
                TokenType.SlashEqual => left.Divide(right),
                _ => throw new("Invalid operator type."),
            };

            if (!this.SetReference(expr.left, result))
                throw new("Invalid assignment target.");

            return result;
        }
        else
        {
            return expr.@operator.Type switch
            {
                TokenType.Greater => left.Greater(right),
                TokenType.GreaterEqual => left.GreaterEqual(right),
                TokenType.Less => left.Less(right),
                TokenType.LessEqual => left.LessEqual(right),
                TokenType.EqualEqual => left.Equal(right),
                TokenType.NotEqual => left.NotEqual(right),
                TokenType.Plus => left.Add(right),
                TokenType.Minus => left.Subtract(right),
                TokenType.Star => left.Multiply(right),
                TokenType.Slash => left.Divide(right),
                TokenType.Modulo => left.Modulo(right),
                TokenType.Ampersand => ReamBoolean.From(left.Truthy().Value && right.Truthy().Value),
                TokenType.Pipe => ReamBoolean.From(left.Truthy().Value && right.Truthy().Value),
                _ => throw new("Invalid operator type."),
            };
        }
    }

    public ReamObject VisitTernaryExpr(Expr.Ternary expr)
    {
        bool condition = this.Evaluate(expr.left).Truthy().Value;
        return this.Evaluate(condition ? expr.middle : expr.right);
    }

    public ReamObject VisitCallExpr(Expr.Call expr)
    {
        ReamObject callee = this.Evaluate(expr.callee);
        List<ReamObject> arguments = expr.arguments.Select(this.Evaluate).ToList();
        ReamSequence sequence = ReamSequence.From(arguments);
        return callee.Call(sequence);
    }

    public ReamObject VisitIndexerExpr(Expr.Indexer expr)
    {
        ReamObject callee = this.Evaluate(expr.callee);
        ReamObject index = this.Evaluate(expr.index);
        return callee.Index(index);
    }

    public ReamObject VisitSetIndexerExpr(Expr.SetIndexer expr)
    {
        ReamObject callee = this.Evaluate(expr.indexer.callee);
        ReamObject index = this.Evaluate(expr.indexer.index);
        ReamObject value = this.Evaluate(expr.value);
        return callee.SetIndex(index, value);
    }

    public ReamObject VisitSetExpr(Expr.Set expr)
    {
        ReamObject obj = this.Evaluate(expr.obj);
        ReamObject value = this.Evaluate(expr.value);
        obj.SetMember(expr.name.Raw, value);
        return value;
    }

    public ReamObject VisitGetExpr(Expr.Get expr)
    {
        ReamObject obj = this.Evaluate(expr.obj);
        return obj.Member(expr.name.Raw);
    }

    public ReamObject VisitChainExpr(Expr.Chain expr)
    {
        Expr.Get callee = expr.call.callee as Expr.Get;

        if (callee is null)
        {
            throw new("Invalid chain expression.");
        }

        ReamObject obj = this.Evaluate(callee.obj);
        ReamObject member = obj.Member(callee.name.Raw);
        List<ReamObject> arguments = expr.call.arguments.Select(this.Evaluate).ToList();

        member.Call(ReamSequence.From(arguments));
        return obj;
    }

    public ReamObject VisitGroupingExpr(Expr.Grouping expr)
    {
        return this.Evaluate(expr.expression);
    }

    public ReamObject VisitSequenceExpr(Expr.Sequence expr)
    {
        List<ReamObject> items = expr.items.Select(this.Evaluate).ToList();
        return ReamSequence.From(items);
    }

    public ReamObject VisitDictionaryExpr(Expr.Dictionary expr)
    {
        Dictionary<ReamObject, ReamObject> items = new();

        foreach (KeyValuePair<Expr, Expr> pair in expr.items)
        {
            ReamObject key = this.Evaluate(pair.Key);
            ReamObject value = this.Evaluate(pair.Value);
            items.Add(key, value);
        }

        return ReamDictionary.From(items);
    }

    public ReamObject VisitLambdaExpr(Expr.Lambda expr)
    {
        return ReamFunctionInternal.From(expr.parameters.Select(x => x.Raw).ToList(), expr.body, this._scope);
    }

    public ReamObject VisitLiteralExpr(Expr.Literal expr)
    {
        return ReamObjectFactory.Create(expr.value);
    }

    public ReamObject VisitLogicalExpr(Expr.Logical expr)
    {
        ReamObject left = this.Evaluate(expr.left);

        return expr.@operator.Type switch
        {
            TokenType.PipePipe when left.Truthy().Value => left,
            TokenType.AmpersandAmpersand when !left.Truthy().Value => left,
            _ => this.Evaluate(expr.right),
        };
    }

    public ReamObject VisitThisExpr(Expr.This expr)
    {
        return this._scope.Get("this").Value;
    }

    public ReamObject VisitDisposeExpr(Expr.Dispose expr)
    {
        // basically get the reamreference object and run .Dispose

        if (expr.expression is Expr.Variable variable)
        {
            ReamReference reference = this._scope.Get(variable.name.Raw);
            reference.Dispose();
        }
        else if (!this.SetReference(expr, ReamNull.Instance))
            throw new("Invalid dispose expression.");
        
        return ReamNull.Instance;
    }

    public ReamObject VisitUnaryExpr(Expr.Unary expr)
    {
        ReamObject right = this.Evaluate(expr.right);

        return expr.@operator.Type switch
        {
            TokenType.Not => right.Truthy().Not(),
            TokenType.Minus => right.Negate(),
            _ => throw new("Invalid operator type."),
        };
    }

    public ReamObject VisitVariableExpr(Expr.Variable expr)
    {
        return this._scope.Get(expr.name.Raw)?.Value ?? ReamNull.Instance;
    }

    public ReamObject VisitBlockStmt(Stmt.Block stmt)
    {
        this.ExecuteBlock(stmt.statements, new(this._scope));
        return ReamNull.Instance;
    }

    public ReamObject VisitClassStmt(Stmt.Class stmt)
    {
        Scope staticScope = new();
        Scope instanceScope = new();
        
        foreach (Stmt.Function funcDecl in stmt.functions)
        {
            bool isStatic = funcDecl.type.HasFlag(VariableType.Static);
            ReamFunctionInternal func = ReamFunctionInternal.From(funcDecl.parameters.Select(x => x.Raw).ToList(), funcDecl.body,
                isStatic ? staticScope : instanceScope);
            
            (isStatic ? staticScope : instanceScope).Set(funcDecl.name.Raw, func);
        }

        foreach (Stmt.Typed varDecl in stmt.variables)
        {
            bool isStatic = varDecl.type.HasFlag(VariableType.Static);
            
            ReamObject value = varDecl.initializer is null ? ReamNull.Instance : this.Evaluate(varDecl.initializer);
            (isStatic ? staticScope : instanceScope).Set(varDecl.name.Raw, value, varDecl.type);
        }

        ReamClassInternal clss = ReamClassInternal.From(stmt.name.Raw, staticScope, instanceScope, ReamSequence.Empty);
        this._scope.Set(stmt.name.Raw, clss);
        
        return ReamNull.Instance;
    }

    public ReamObject VisitExpressionStmt(Stmt.Expression stmt)
    {
        this.Evaluate(stmt.expression);
        return ReamNull.Instance;
    }

    public ReamObject VisitFunctionStmt(Stmt.Function stmt)
    {
        ReamFunctionInternal function = ReamFunctionInternal.From(stmt.parameters.Select(x => x.Raw).ToList(), stmt.body, this._scope);
        this._scope.Set(stmt.name.Raw, function);
        return ReamNull.Instance;
    }

    public ReamObject VisitMethodStmt(Stmt.Method stmt)
    {
        ReamObject obj = this.Evaluate(stmt.obj);
        ReamFunctionInternal method = ReamFunctionInternal.From(stmt.parameters.Select(x => x.Raw).ToList(), stmt.body, this._scope);
        obj.SetMember(stmt.name.Raw, method);
        return ReamNull.Instance;
    }

    public ReamObject VisitIfStmt(Stmt.If stmt)
    {
        if (this.Evaluate(stmt.condition).Truthy().Value)
        {
            this.Execute(stmt.thenBranch);
            return ReamNull.Instance;
        }

        if (stmt.elifBranches.Any())
            foreach ((Expr cond, Stmt block) in stmt.elifBranches)
                if (this.Evaluate(cond).Truthy().Value)
                {
                    this.Execute(block);
                    return ReamNull.Instance;
                }
        
        if (stmt.elseBranch != null)
            this.Execute(stmt.elseBranch);

        return ReamNull.Instance;
    }

    public ReamObject VisitReturnStmt(Stmt.Return stmt)
    {
        ReamObject value = ReamNull.Instance;

        if (stmt.value != null)
            value = this.Evaluate(stmt.value);

        throw new Return(stmt.keyword, value);
    }

    public ReamObject VisitContinueStmt(Stmt.Continue stmt)
    {
        throw new Continue(stmt.keyword);
    }

    public ReamObject VisitBreakStmt(Stmt.Break stmt)
    {
        throw new Break(stmt.keyword);
    }

    public ReamObject VisitTypedStmt(Stmt.Typed stmt)
    {
        ReamObject value = stmt.initializer is null ? ReamNull.Instance : this.Evaluate(stmt.initializer);
        this._scope.Set(stmt.name.Raw, value, stmt.type);
        
        return ReamNull.Instance;
    }

    public ReamObject VisitWhileStmt(Stmt.While stmt)
    {
        while (this.Evaluate(stmt.condition).Truthy().Value)
        {
            try
            {
                this.Execute(stmt.body);
            }
            catch (Break)
            {
                break;
            }
            catch (Continue) { }
        }
        
        return ReamNull.Instance;
    }

    public ReamObject VisitImportStmt(Stmt.Import stmt)
    {
        if (!stmt.name.Any())
            return null;

        StringBuilder sb = new();
        foreach (Token item in stmt.name)
        {
            if (item.Type == TokenType.Identifier)
                sb.Append(item.Raw);
            else if (item.Type == TokenType.Period)
                sb.Append('.');
        }

        string displayPath = sb.ToString();
        string basePath = displayPath.Replace('.', Path.DirectorySeparatorChar);

        string dllPath = basePath + ".dll";
        string dllLibDataPath = Path.Join(Program.LibDataPath, dllPath);
        if (File.Exists(dllPath))
        {
            Assembly asm = Assembly.LoadFrom(dllPath);
            this.LoadAssemblyLibrary(asm);
        }
        else if (File.Exists(dllLibDataPath))
        {
            Assembly asm = Assembly.LoadFrom(dllLibDataPath);
            this.LoadAssemblyLibrary(asm);
        }
        else
        {
            string reamPath = basePath + ".r";
            string reamLibDataPath = Path.Join(Program.LibDataPath, reamPath);
            if (File.Exists(reamPath))
            {
                Program.RunFile(reamPath);
            }
            else if (File.Exists(reamLibDataPath))
            {
                Program.RunFile(reamLibDataPath);
            }
            else
            {
                if (this._flags.Strict)
                    throw new RuntimeError(stmt.name.First(), $"Unable to find library '{displayPath}'");
            }
        }

        return ReamNull.Instance;
    }

    public ReamObject VisitForStmt(Stmt.For stmt)
    {
        if (stmt.name != null)
        {
            Scope prev = this._scope;
            try
            {
                this._scope = new(this._scope);
                foreach (ReamObject item in this.Evaluate(stmt.iterator).Iterate().Value)
                {
                    this._scope.Set(stmt.name.Raw, item, VariableType.Local);
                    try
                    {
                        this.Execute(stmt.body);
                    }
                    catch (Break)
                    {
                        break;
                    }
                    catch (Continue) { }
                }
            }
            finally
            {
                this._scope = prev;
            }
        }
        else
        {
            foreach (ReamObject _ in this.Evaluate(stmt.iterator).Iterate().Value)
            {
                try
                {
                    this.Execute(stmt.body);
                }
                catch (Break)
                {
                    break;
                }
                catch (Continue) { }
            }
        }
        
        return ReamNull.Instance;
    }

    public ReamObject VisitEvaluateStmt(Stmt.Evaluate stmt)
    {
        this.Evaluate(stmt.value);
        return ReamNull.Instance;
    }
}
