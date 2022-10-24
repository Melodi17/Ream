using System.Reflection;
using System.Text;
using Ream.Lexing;
using Ream.Parsing;

namespace Ream.Interpreting
{
    public class Flags
    {
        public static bool Strict = false;
    }
    public class Refraction
    {
        // TODO: Add various functions for self-inspection
    }

    public class Interpreter : Expr.Visitor<object>, Stmt.Visitor<object>
    {
        public readonly Scope Globals;
        private Scope scope;
        public Resolver resolver;
        public bool raiseErrors => Flags.Strict;

        public Interpreter()
        {
            Globals = new();
            scope = new(Globals);
            resolver = new(this);

            DefineObject("String", new ObjectType(StringPropMap.TypeID));
            DefineObject("Number", new ObjectType(DoublePropMap.TypeID));
            DefineObject("Boolean", new ObjectType(BoolPropMap.TypeID));
            DefineObject("Sequence", new ObjectType(ListPropMap.TypeID));
            DefineObject("Dictionary", new ObjectType(DictPropMap.TypeID));
            //DefineObject("Function", new ObjectType("function"));


            DefineClass<Flags>();
            DefineFunction("print", (i, j) =>
            {
                Console.WriteLine(j[0] is string s ? s : resolver.Stringify(j[0]));
                return null;
            }, 1);
            DefineFunction("printf", (i, j) =>
            {
                Console.Write(j[0] is string s ? s : resolver.Stringify(j[0]));
                return null;
            }, 1);
            DefineFunction("read", (i, j) =>
            {
                Console.Write(j[0] != null ? j[0] is string s ? s : resolver.Stringify(j[0]) : "");
                return Console.ReadLine();
            }, 1);
            DefineFunction("wait", (i, j) =>
            {
                Thread.Sleep(j[0] is double d ? resolver.GetInt(d) : 0);
                return null;
            }, 1);
            DefineFunction("exit", (i, j) =>
            {
                Environment.Exit(j[0] is double d ? resolver.GetInt(d) : 0);
                return null;
            }, 1);
            DefineFunction("clear", (i, j) =>
            {
                Console.Clear();
                return null;
            }, 0);
            DefineFunction("dispose", (i, j) =>
            {
                if (j[0] is Pointer p) p.Dispose();
                return null;
            }, 1);
            DefineFunction("size", (i, j) =>
            {
                return (double)Pointer.GetPointerCount();
            }, 0);
            DefineFunction("hook", (i, j) =>
            {
                if (j[0] is Pointer p && j[1] is ICallable func)
                    p.Hook(func);
                return null;
            }, 2);
        }

        public void DefineObject(string key, object value)
        {
            Globals.Define(key, value, VariableType.Global);
        }

        public void DefineFunction(string key, Func<object, List<object>, object> function, int argumentCount)
        {
            Globals.Define(key, new ExternalFunction(function, argumentCount), VariableType.Global);
        }

        public void DefineFunction(string key, MethodInfo method)
        {
            Globals.Define(key, new ExternalFunction(method), VariableType.Global);
        }

        public void DefineFunction(MethodInfo method)
        {
            DefineFunction(method.Name, method);
        }

        public void DefineClass(string key, object instance)
        {
            Globals.Define(key, instance, VariableType.Global);
        }

        public void DefineClass(string key, Type type)
        {
            Globals.Define(key, new ExternalClass(type, this), VariableType.Global);
        }

        public void DefineClass(Type type)
        {
            DefineClass(type.Name, type);
        }

        public void DefineClass<T>(string name)
        {
            DefineClass(name, typeof(T));
        }

        public void DefineClass<T>()
        {
            Type type = typeof(T);
            DefineClass(type.Name, type);
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
            catch (FlowControlError error)
            {
                if (raiseErrors)
                    Program.RuntimeError(new RuntimeError(error.SourceToken, "Unexpected flow control jump."));
            }
        }

        public object Interpret(Expr expression)
        {
            try
            {
                object value = Evaluate(expression);
                return value;
            }
            catch (RuntimeError error)
            {
                Program.RuntimeError(error);
                return null;
            }
            catch (FlowControlError error)
            {
                if (raiseErrors)
                    Program.RuntimeError(new RuntimeError(error.SourceToken, "Unexpected flow control jump."));

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
                    Execute(statement);
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
            VariableType autoType = scope.AutoDetectType(name, type);

            object value;
            if (initializer != null && !autoType.HasFlag(VariableType.Dynamic))
                value = Evaluate(initializer);
            else
                value = initializer;

            scope.Set(name, value, type);
            return value;
        }

        public void LoadAssemblyLibrary(Assembly asm)
        {
            foreach (Type type in asm.GetTypes()
                         .Where(x => x.IsPublic))
            {
                DefineClass(type);
                //Globals.Define(type.Name, new ExternalClass(type, this), VariableType.Global);
            }
        }

        public object VisitAssignExpr(Expr.Assign expr)
        {
            return DeclareStmt(expr.name, expr.value, VariableType.Normal);
        }

        public object VisitBinaryExpr(Expr.Binary expr)
        {
            if (expr.@operator.Type is TokenType.Plus_Equal or TokenType.Minus_Equal or TokenType.Slash_Equal or TokenType.Star_Equal)
            {
                object left = Evaluate(expr.left);
                object right = Evaluate(expr.right);
                object result = resolver.Compare(left, right, expr.@operator.Type switch
                {
                    TokenType.Plus_Equal => TokenType.Plus,
                    TokenType.Minus_Equal => TokenType.Minus,
                    TokenType.Slash_Equal => TokenType.Slash,
                    TokenType.Star_Equal => TokenType.Star,
                });

                if (expr.left is Expr.Variable variable)
                {
                    VariableType type = scope.AutoDetectType(variable.name, VariableType.Normal);
                    if (type.HasFlag(VariableType.Dynamic))
                    {
                        if (!raiseErrors)
                            return null;
                        throw new RuntimeError(variable.name, "Cannot assign to a dynamic variable.");
                    }
                    if (type.HasFlag(VariableType.Final))
                    {
                        if (!raiseErrors)
                            return null;
                        throw new RuntimeError(variable.name, "Cannot assign to a final variable.");
                    }

                    scope.Set(variable.name, result, type);
                }
                else if (expr.left is Expr.Get get)
                {
                    object obj = Evaluate(get.obj);
                    IPropable prop = resolver.GetPropable(obj);
                    if (prop == null)
                    {
                        if (!raiseErrors)
                            return null;
                        throw new RuntimeError(get.name, $"Cannot map properties of {(obj == null ? "null" : resolver.Stringify(obj))}");
                    }

                    VariableType type = prop.AutoDetectType(get.name);
                    if (type.HasFlag(VariableType.Dynamic))
                    {
                        if (!raiseErrors)
                            return null;
                        throw new RuntimeError(get.name, "Cannot assign to a dynamic variable.");
                    }
                    if (type.HasFlag(VariableType.Final))
                    {
                        if (!raiseErrors)
                            return null;
                        throw new RuntimeError(get.name, "Cannot assign to a final variable.");
                    }

                    prop.Set(get.name, result, type);
                }
                else if (expr.left is Expr.Indexer indexer)
                {
                    object index = Evaluate(indexer.index);
                    if (index == null)
                    {
                        if (!raiseErrors)
                            return null;
                        throw new RuntimeError(indexer.paren, "Cannot mix null");
                    }

                    return resolver.GetMix(Evaluate(indexer.callee), index, result);
                }
                return result;
            }
            else
                return resolver.Compare(Evaluate(expr.left), Evaluate(expr.right), expr.@operator.Type);
        }

        public object VisitBlockStmt(Stmt.Block stmt)
        {
            ExecuteBlock(stmt.statements, new Scope(scope));
            return null;
        }

        public object VisitBreakStmt(Stmt.Break stmt)
        {
            throw new Break(stmt.keyword);
        }

        public object VisitCallExpr(Expr.Call expr)
        {
            object callee = Evaluate(expr.callee);

            if (callee is not ICallable function)
            {
                if (raiseErrors)
                    throw new RuntimeError(expr.paren, $"Cannot call {(callee == null ? "null" : resolver.Stringify(callee))}");
                else
                    return null;
            }

            List<object> args = expr.arguments.Select(x => Evaluate(x)).ToList();

            int count = function.ArgumentCount();
            if (count > 0)
                while (args.Count < count)
                    args.Add(null);

            return function.Call(this, args);
        }

        public object VisitClassStmt(Stmt.Class stmt)
        {
            Scope localScope = new(Globals);
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
                    function = new(funStmt, localScope);
                    localScope.Set(funStmt.name, function, funStmt.type);
                }
            }

            foreach (Stmt.Typed typeStmt in stmt.variables)
            {
                object value = typeStmt.type.HasFlag(VariableType.Dynamic)
                    ? typeStmt.initializer
                    : Evaluate(typeStmt.initializer);

                if (typeStmt.type.HasFlag(VariableType.Static))
                    staticScope.Set(typeStmt.name, value, typeStmt.type);
                else
                    localScope.Set(typeStmt.name, value, typeStmt.type);
            }

            Class clss = new(stmt.name.Raw, this, localScope, staticScope);
            scope.Set(stmt.name, clss, VariableType.Global);

            return null;
        }

        public object VisitContinueStmt(Stmt.Continue stmt)
        {
            throw new Continue(stmt.keyword);
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
                        try
                        {
                            Execute(stmt.body);
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

                    scope.FreeMemory();
                }
                finally
                {
                    scope = previous;
                }
            }
            else
                foreach (object _ in resolver.GetIterator(Evaluate(stmt.iterator)))
                {
                    try
                    {
                        Execute(stmt.body);
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
            Function function = new(stmt, scope);
            scope.Set(stmt.name, function, stmt.type);
            return null;
        }

        public object VisitMethodStmt(Stmt.Method stmt)
        {
            object obj = Evaluate(stmt.obj);
            IPropable prop = resolver.GetPropable(obj);
            if (prop == null)
            {
                if (raiseErrors)
                    throw new RuntimeError(stmt.name, $"Cannot map properties of {(obj == null ? "null" : resolver.Stringify(obj))}");
                else
                    return null;
            }

            Function func = new(stmt, scope);

            prop.Set(stmt.name, func, stmt.type);
            return null;
        }

        public object VisitGetExpr(Expr.Get expr)
        {
            object obj = Evaluate(expr.obj);
            IPropable prop = resolver.GetPropable(obj);
            if (prop == null)
            {
                if (raiseErrors)
                    throw new RuntimeError(expr.name, $"Cannot map properties of {(obj == null ? "null" : resolver.Stringify(obj))}");
                else
                    return null;
            }
            object res = prop.Get(expr.name);
            bool isDynamic = prop.AutoDetectType(expr.name).HasFlag(VariableType.Dynamic);
            return isDynamic ? Evaluate(res as Expr) : res;
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
                LoadAssemblyLibrary(asm);
            }
            else if (File.Exists(dllLibDataPath))
            {
                Assembly asm = Assembly.LoadFrom(dllLibDataPath);
                LoadAssemblyLibrary(asm);
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
                    if (raiseErrors)
                        throw new RuntimeError(stmt.name.First(), $"Unable to find library '{displayPath}'");
                }
            }

            return null;
        }

        public object VisitIndexerExpr(Expr.Indexer expr)
        {
            object index = Evaluate(expr.index);
            if (index == null)
            {
                if (!raiseErrors)
                    return null;
                throw new RuntimeError(expr.paren, "Cannot index null");
            }
            return resolver.GetIndex(Evaluate(expr.callee), index);
        }

        public object VisitDictionaryExpr(Expr.Dictionary expr)
        {
            Dictionary<object, object> items = new();
            foreach (KeyValuePair<Expr, Expr> item in expr.items)
            {
                object key = Evaluate(item.Key);
                if (key == null)
                {
                    if (raiseErrors)
                        throw new RuntimeError(expr.paren, "Dictionary key cannot be null");
                    else
                        continue;
                }
                items[key] = Evaluate(item.Value);
            }

            return items;
        }

        public object VisitLambdaExpr(Expr.Lambda expr)
        {
            Function function = new(expr, scope);
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

        public object VisitMixerExpr(Expr.Mixer expr)
        {
            object index = Evaluate(expr.index);
            object value = Evaluate(expr.value);
            if (index == null)
            {
                if (!raiseErrors)
                    return null;
                throw new RuntimeError(expr.paren, "Cannot mix null");
            }

            return resolver.GetMix(Evaluate(expr.callee), index, value);
        }

        public object VisitTernaryExpr(Expr.Ternary expr)
        {
            if (expr.leftOperator.Type == TokenType.Question &&
                expr.rightOperator.Type == TokenType.Colon)
            {
                return Evaluate(resolver.Truthy(Evaluate(expr.left)) ? expr.middle : expr.right);
            }
            else
            {
                // Should be unreachable
                return null;
            }
        }

        public object VisitTranslateExpr(Expr.Translate expr)
        {
            return expr.@operator.Type switch
            {
                TokenType.Ampersand => scope.GetPointer(expr.name),
                _ => null
            };
        }

        public object VisitReturnStmt(Stmt.Return stmt)
        {
            object value = null;
            if (stmt.value != null) value = Evaluate(stmt.value);

            throw new Return(stmt.keyword, value);
        }

        public object VisitSequenceExpr(Expr.Sequence expr)
        {
            List<object> items = expr.items.Select(x => Evaluate(x)).ToList();
            return items;
        }

        public object VisitSetExpr(Expr.Set expr)
        {
            object obj = Evaluate(expr.obj);
            IPropable prop = resolver.GetPropable(obj);
            if (prop == null)
            {
                if (raiseErrors)
                    throw new RuntimeError(expr.name, $"Cannot map properties of {(obj == null ? "null" : resolver.Stringify(obj))}");
                else
                    return null;
            }

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
                TokenType.Pipe => right is Pointer p ? p.Get() : null,
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
            {
                try
                {
                    Execute(stmt.body);
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
    }
}