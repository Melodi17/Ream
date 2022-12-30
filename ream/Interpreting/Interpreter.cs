using System.Reflection;
using System.Text;
using Ream.Lexing;
using Ream.Parsing;

namespace Ream.Interpreting
{
    public class Flags
    {
        public static bool Strict = false;
        public static void Exit(int? code)
        {
            Environment.Exit(code ?? 0);
        }
        public static void Dispose(Pointer p)
        {
            p.Dispose();
        }
        public static void Hook(Pointer p, ICallable func)
        {
            p.Hook(func);
        }
        public static double Size()
        {
            return (double)Pointer.GetPointerCount();
        }
        public static string Type(object obj)
        {
            return Program.Interpreter.resolver.GetType(obj);
        }
    }

    public class Interpreter : Expr.Visitor<object>, Stmt.Visitor<object>
    {
        public readonly Scope Globals;
        private Scope scope;
        public Resolver resolver;
        public bool raiseErrors => Flags.Strict;

        public Interpreter()
        {
            this.Globals = new();
            this.scope = new(this.Globals);
            this.resolver = new(this);

            this.DefineObject("String", new ObjectType(StringPropMap.TypeID));
            this.DefineObject("Number", new ObjectType(DoublePropMap.TypeID));
            this.DefineObject("Boolean", new ObjectType(BoolPropMap.TypeID));
            this.DefineObject("Sequence", new ObjectType(ListPropMap.TypeID));
            this.DefineObject("Dictionary", new ObjectType(DictPropMap.TypeID));
            //DefineObject("Callable", new ObjectType("callable"));


            this.DefineClass<Flags>();
        }

        public void DefineObject(string key, object value)
        {
            this.Globals.Define(key, value, VariableType.Global);
        }

        public void DefineFunction(string key, Func<object, List<object>, object> function, int argumentCount)
        {
            this.Globals.Define(key, new ExternalFunction(function, argumentCount), VariableType.Global);
        }

        public void DefineFunction(string key, MethodInfo method)
        {
            this.Globals.Define(key, new ExternalFunction(method), VariableType.Global);
        }

        public void DefineFunction(MethodInfo method)
        {
            this.DefineFunction(method.Name, method);
        }

        public void DefineClass(string key, object instance)
        {
            this.Globals.Define(key, instance, VariableType.Global);
        }

        public void DefineClass(string key, Type type)
        {
            this.Globals.Define(key, new ExternalClass(type, this), VariableType.Global);
        }

        public void DefineClass(Type type)
        {
            this.DefineClass(type.Name, type);
        }

        public void DefineClass<T>(string name)
        {
            this.DefineClass(name, typeof(T));
        }

        public void DefineClass<T>()
        {
            Type type = typeof(T);
            this.DefineClass(type.Name, type);
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
                if (this.raiseErrors)
                    Program.RuntimeError(new(error.SourceToken, "Unexpected flow control jump."));
            }
        }

        public object Interpret(Expr expression)
        {
            try
            {
                object value = this.Evaluate(expression);
                return value;
            }
            catch (RuntimeError error)
            {
                Program.RuntimeError(error);
                return null;
            }
            catch (FlowControlError error)
            {
                if (this.raiseErrors)
                    Program.RuntimeError(new(error.SourceToken, "Unexpected flow control jump."));

                return null;
            }
        }

        public object Evaluate(Expr expr)
        {
            return expr?.Accept(this);
        }

        public object Evaluate(Expr expr, Scope scope)
        {
            Scope previous = this.scope;
            object res = null;
            try
            {
                this.scope = scope;
                res = expr?.Accept(this);
                this.scope.FreeMemory();
            }
            finally
            {
                this.scope = previous;
            }
            return res;
        }

        public void ExecuteBlock(List<Stmt> statements, Scope scope)
        {
            Scope previous = this.scope;
            try
            {
                this.scope = scope;

                foreach (Stmt statement in statements)
                {
                    this.Execute(statement);
                }

                this.scope.FreeMemory();
            }
            finally
            {
                this.scope = previous;
            }
        }

        public void Execute(Stmt stmt)
        {
            stmt?.Accept(this);
        }

        public object DeclareStmt(Token name, Expr initializer, VariableType type = VariableType.Normal)
        {
            VariableType autoType = this.scope.AutoDetectType(name, type);

            object value;
            if (initializer != null && !autoType.HasFlag(VariableType.Dynamic))
                value = this.Evaluate(initializer);
            else
                value = initializer;

            this.scope.Set(name, value, type);
            return value;
        }

        public void LoadAssemblyLibrary(Assembly asm)
        {
            foreach (Type type in asm.GetTypes()
                         .Where(x => x.IsPublic))
            {
                this.DefineClass(type);
                //Globals.Define(type.Name, new ExternalClass(type, this), VariableType.Global);
            }
        }

        public object VisitAssignExpr(Expr.Assign expr)
        {
            return this.DeclareStmt(expr.name, expr.value, VariableType.Normal);
        }

        public object VisitBinaryExpr(Expr.Binary expr)
        {
            if (expr.@operator.Type is TokenType.Plus_Equal or TokenType.Minus_Equal or TokenType.Slash_Equal or TokenType.Star_Equal)
            {
                object left = this.Evaluate(expr.left);
                object right = this.Evaluate(expr.right);
                object result = this.resolver.Compare(left, right, expr.@operator.Type switch
                {
                    TokenType.Plus_Equal => TokenType.Plus,
                    TokenType.Minus_Equal => TokenType.Minus,
                    TokenType.Slash_Equal => TokenType.Slash,
                    TokenType.Star_Equal => TokenType.Star,
                });

                if (expr.left is Expr.Variable variable)
                {
                    VariableType type = this.scope.AutoDetectType(variable.name, VariableType.Normal);
                    if (type.HasFlag(VariableType.Dynamic))
                    {
                        if (!this.raiseErrors)
                            return null;
                        throw new RuntimeError(variable.name, "Cannot assign to a dynamic variable.");
                    }
                    if (type.HasFlag(VariableType.Final))
                    {
                        if (!this.raiseErrors)
                            return null;
                        throw new RuntimeError(variable.name, "Cannot assign to a final variable.");
                    }

                    this.scope.Set(variable.name, result, type);
                }
                else if (expr.left is Expr.Get get)
                {
                    object obj = this.Evaluate(get.obj);
                    IPropable prop = this.resolver.GetPropable(obj);
                    if (prop == null)
                    {
                        if (!this.raiseErrors)
                            return null;
                        throw new RuntimeError(get.name, $"Cannot map properties of {(obj == null ? "null" : this.resolver.Stringify(obj))}");
                    }

                    VariableType type = prop.AutoDetectType(get.name);
                    if (type.HasFlag(VariableType.Dynamic))
                    {
                        if (!this.raiseErrors)
                            return null;
                        throw new RuntimeError(get.name, "Cannot assign to a dynamic variable.");
                    }
                    if (type.HasFlag(VariableType.Final))
                    {
                        if (!this.raiseErrors)
                            return null;
                        throw new RuntimeError(get.name, "Cannot assign to a final variable.");
                    }

                    prop.Set(get.name, result, type);
                }
                else if (expr.left is Expr.Indexer indexer)
                {
                    object index = this.Evaluate(indexer.index);
                    if (index == null)
                    {
                        if (!this.raiseErrors)
                            return null;
                        throw new RuntimeError(indexer.paren, "Cannot mix null");
                    }

                    return this.resolver.GetMix(this.Evaluate(indexer.callee), index, result);
                }
                return result;
            }
            else
                return this.resolver.Compare(this.Evaluate(expr.left), this.Evaluate(expr.right), expr.@operator.Type);
        }

        public object VisitBlockStmt(Stmt.Block stmt)
        {
            this.ExecuteBlock(stmt.statements, new(this.scope));
            return null;
        }

        public object VisitBreakStmt(Stmt.Break stmt)
        {
            throw new Break(stmt.keyword);
        }

        public object VisitCallExpr(Expr.Call expr)
        {
            object callee = this.Evaluate(expr.callee);

            if (callee is not ICallable function)
            {
                if (this.raiseErrors)
                    throw new RuntimeError(expr.paren, $"Cannot call {(callee == null ? "null" : this.resolver.Stringify(callee))}");
                else
                    return null;
            }

            List<object> args = expr.arguments.Select(x => this.Evaluate(x)).ToList();

            int count = function.ArgumentCount();
            if (count > 0)
                while (args.Count < count)
                    args.Add(null);

            return function.Call(this, args);
        }

        public object VisitClassStmt(Stmt.Class stmt)
        {
            Scope localScope = new(this.Globals);
            Scope staticScope = new(this.Globals);
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
                    function = new(funStmt, localScope);
                    localScope.Set(funStmt.name, function, funStmt.type);
                }
            }

            foreach (Stmt.Typed typeStmt in stmt.variables)
            {
                object value = typeStmt.type.HasFlag(VariableType.Dynamic)
                    ? typeStmt.initializer
                    : this.Evaluate(typeStmt.initializer);

                if (typeStmt.type.HasFlag(VariableType.Static))
                    staticScope.Set(typeStmt.name, value, typeStmt.type);
                else
                    localScope.Set(typeStmt.name, value, typeStmt.type);
            }

            Class clss = new(stmt.name.Raw, this, localScope, staticScope);
            this.scope.Set(stmt.name, clss, VariableType.Global);

            return null;
        }

        public object VisitContinueStmt(Stmt.Continue stmt)
        {
            throw new Continue(stmt.keyword);
        }

        public object VisitEvaluateStmt(Stmt.Evaluate stmt)
        {
            object obj = this.Evaluate(stmt.value);
            Program.Run(this.resolver.Stringify(obj));
            return null;
        }

        public object VisitExpressionStmt(Stmt.Expression stmt)
        {
            this.Evaluate(stmt.expression);
            return null;
        }

        public object VisitForStmt(Stmt.For stmt)
        {
            if (stmt.name != null)
            {
                Scope previous = this.scope;
                try
                {
                    this.scope = new(this.scope);
                    foreach (object item in this.resolver.GetIterator(this.Evaluate(stmt.iterator)))
                    {
                        this.scope.Set(stmt.name, item);
                        try
                        {
                            this.Execute(stmt.body);
                        }
                        catch (Break)
                        {
                            break;
                        }
                        catch (Continue)
                        {
                            /* Don't care */
                        }
                    }

                    this.scope.FreeMemory();
                }
                finally
                {
                    this.scope = previous;
                }
            }
            else
                foreach (object _ in this.resolver.GetIterator(this.Evaluate(stmt.iterator)))
                {
                    try
                    {
                        this.Execute(stmt.body);
                    }
                    catch (Break)
                    {
                        break;
                    }
                    catch (Continue)
                    {
                        /* Don't care */
                    }
                }

            return null;
        }

        public object VisitFunctionStmt(Stmt.Function stmt)
        {
            Function function = new(stmt, this.scope);
            this.scope.Set(stmt.name, function, stmt.type);
            return null;
        }

        public object VisitMethodStmt(Stmt.Method stmt)
        {
            object obj = this.Evaluate(stmt.obj);
            IPropable prop = this.resolver.GetPropable(obj);
            if (prop == null)
            {
                if (this.raiseErrors)
                    throw new RuntimeError(stmt.name, $"Cannot map properties of {(obj == null ? "null" : this.resolver.Stringify(obj))}");
                else
                    return null;
            }

            Function func = new(stmt, this.scope);

            prop.Set(stmt.name, func, stmt.type);
            return null;
        }

        public object VisitGetExpr(Expr.Get expr)
        {
            object obj = this.Evaluate(expr.obj);
            IPropable prop = this.resolver.GetPropable(obj);
            if (prop == null)
            {
                if (this.raiseErrors)
                    throw new RuntimeError(expr.name, $"Cannot map properties of {(obj == null ? "null" : this.resolver.Stringify(obj))}");
                else
                    return null;
            }
            object res = prop.Get(expr.name);
            bool isDynamic = prop.AutoDetectType(expr.name).HasFlag(VariableType.Dynamic);
            return isDynamic ? this.Evaluate(res as Expr) : res;
        }

        public object VisitGroupingExpr(Expr.Grouping expr)
        {
            return this.Evaluate(expr.expression);
        }

        public object VisitIfStmt(Stmt.If stmt)
        {
            if (this.resolver.Truthy(this.Evaluate(stmt.condition)))
            {
                this.Execute(stmt.thenBranch);
                return null;
            }
            
            if (stmt.elifBranches.Any())
            {
                foreach ((Expr condition, Stmt body) in stmt.elifBranches)
                {
                    if (this.resolver.Truthy(this.Evaluate(condition)))
                    {
                        this.Execute(body);
                        return null;
                    }
                }
            }
            
            if (stmt.elseBranch != null)
            {
                this.Execute(stmt.elseBranch);
                return null;
            }

            return null;
        }

        public object VisitImportStmt(Stmt.Import stmt)
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
                    if (this.raiseErrors)
                        throw new RuntimeError(stmt.name.First(), $"Unable to find library '{displayPath}'");
                }
            }

            return null;
        }

        public object VisitIndexerExpr(Expr.Indexer expr)
        {
            object index = this.Evaluate(expr.index);
            if (index == null)
            {
                if (!this.raiseErrors)
                    return null;
                throw new RuntimeError(expr.paren, "Cannot index null");
            }
            return this.resolver.GetIndex(this.Evaluate(expr.callee), index);
        }

        public object VisitDictionaryExpr(Expr.Dictionary expr)
        {
            Dictionary<object, object> items = new();
            foreach (KeyValuePair<Expr, Expr> item in expr.items)
            {
                object key = this.Evaluate(item.Key);
                if (key == null)
                {
                    if (this.raiseErrors)
                        throw new RuntimeError(expr.paren, "Dictionary key cannot be null");
                    else
                        continue;
                }
                items[key] = this.Evaluate(item.Value);
            }

            return items;
        }

        public object VisitLambdaExpr(Expr.Lambda expr)
        {
            Function function = new(expr, this.scope);
            return function;
        }

        public object VisitLiteralExpr(Expr.Literal expr)
        {
            return expr.value;
        }

        public object VisitLogicalExpr(Expr.Logical expr)
        {
            object left = this.Evaluate(expr.left);

            if (expr.@operator.Type == TokenType.Pipe_Pipe)
            {
                if (this.resolver.Truthy(left))
                    return left;
            }
            else
            {
                if (!this.resolver.Truthy(left))
                    return left;
            }

            return this.Evaluate(expr.right);
        }

        public object VisitSetIndexerExpr(Expr.SetIndexer expr)
        {
            object index = this.Evaluate(expr.indexer.index);
            object value = this.Evaluate(expr.value);
            if (index == null)
            {
                if (!this.raiseErrors)
                    return null;
                throw new RuntimeError(expr.indexer.paren, "Cannot mix null");
            }

            return this.resolver.GetMix(this.Evaluate(expr.indexer.callee), index, value);
        }

        public object VisitTernaryExpr(Expr.Ternary expr)
        {
            if (expr.leftOperator.Type == TokenType.Question &&
                expr.rightOperator.Type == TokenType.Colon)
            {
                return this.Evaluate(this.resolver.Truthy(this.Evaluate(expr.left)) ? expr.middle : expr.right);
            }
            else
            {
                // Should be unreachable
                return null;
            }
        }

        public object VisitReturnStmt(Stmt.Return stmt)
        {
            object value = null;
            if (stmt.value != null) value = this.Evaluate(stmt.value);

            throw new Return(stmt.keyword, value);
        }

        public object VisitSequenceExpr(Expr.Sequence expr)
        {
            List<object> items = expr.items.Select(x => this.Evaluate(x)).ToList();
            return items;
        }

        public object VisitSetExpr(Expr.Set expr)
        {
            object obj = this.Evaluate(expr.obj);
            IPropable prop = this.resolver.GetPropable(obj);
            if (prop == null)
            {
                if (this.raiseErrors)
                    throw new RuntimeError(expr.name, $"Cannot map properties of {(obj == null ? "null" : this.resolver.Stringify(obj))}");
                else
                    return null;
            }

            VariableType type = prop.AutoDetectType(expr.name);
            object value = type.HasFlag(VariableType.Dynamic)
                ? expr.value
                : this.Evaluate(expr.value);

            prop.Set(expr.name, value, type);
            return value;
        }

        public object VisitThisExpr(Expr.This expr)
        {
            return this.scope.Get(expr.keyword);
        }

        public object VisitTypedStmt(Stmt.Typed stmt)
        {
            return this.DeclareStmt(stmt.name, stmt.initializer, stmt.type);
        }

        public object VisitUnaryExpr(Expr.Unary expr)
        {
            object right = this.Evaluate(expr.right);

            return expr.@operator.Type switch
            {
                TokenType.Not => !this.resolver.Truthy(right),
                TokenType.Minus => right is double d ? -(double)d : null,
                _ => null,
            };
        }

        public object VisitVariableExpr(Expr.Variable expr)
        {
            object obj = this.scope.Get(expr.name);
            if (obj == null)
                return null;

            VariableData data = this.scope.GetData(expr.name);

            if (data.Type.HasFlag(VariableType.Dynamic))
                return this.Evaluate((Expr)obj);
            else
                return obj;
        }

        public object VisitWhileStmt(Stmt.While stmt)
        {
            while (this.resolver.Truthy(this.Evaluate(stmt.condition)))
            {
                try
                {
                    this.Execute(stmt.body);
                }
                catch (Break)
                {
                    break;
                }
                catch (Continue)
                {
                    /* Don't care */
                }
            }

            return null;
        }

        public object VisitChainExpr(Expr.Chain expr)
        {
            Expr.Get getExpr = (Expr.Get)expr.call.callee;

            object obj = this.Evaluate(getExpr.obj);
            IPropable prop = this.resolver.GetPropable(obj);
            if (prop == null)
            {
                if (this.raiseErrors)
                    throw new RuntimeError(getExpr.name, $"Cannot map properties of {(obj == null ? "null" : this.resolver.Stringify(obj))}");
                else
                    return null;
            }
            object res = prop.Get(getExpr.name);
            bool isDynamic = prop.AutoDetectType(getExpr.name).HasFlag(VariableType.Dynamic);

            object callee = isDynamic ? this.Evaluate(res as Expr) : res;

            if (callee is not ICallable function)
            {
                if (this.raiseErrors)
                    throw new RuntimeError(expr.call.paren, $"Cannot call {(callee == null ? "null" : this.resolver.Stringify(callee))}");
                else
                    return callee;
            }

            List<object> args = expr.call.arguments.Select(x => this.Evaluate(x)).ToList();

            int count = function.ArgumentCount();
            if (count > 0)
                while (args.Count < count)
                    args.Add(null);

            function.Call(this, args);
            return obj;
        }
    }
}