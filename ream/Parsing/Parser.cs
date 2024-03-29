﻿using Ream.Interpreting;
using Ream.Lexing;

namespace Ream.Parsing
{
    public class Parser
    {
        private Dictionary<string, Macro> macros;
        private bool allowExpression = false;
        private bool foundExpression = false;
        private class ParseError : Exception { }
        public bool AtEnd => Peek().Type == TokenType.End;
        private List<Token> tokens;
        private int Current = 0;

        public Parser(List<Token> tokens)
        {
            this.macros = new();
            this.tokens = new();
            Token lastToken = null;
            foreach (var item in tokens) // Remove empty lines
            {
                if (item.Type == TokenType.Newline
                    && lastToken != null && lastToken.Type == TokenType.Newline)
                    continue;

                lastToken = item;
                this.tokens.Add(item);
            }
        }
        private static IEnumerable<IEnumerable<TValue>> Chunk<TValue>(IEnumerable<TValue> values, Func<TValue, bool> pred)
        {
            using (var enumerator = values.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    yield return GetChunk(enumerator, pred).ToList();
                }
            }
        }
        private static IEnumerable<T> GetChunk<T>(IEnumerator<T> enumerator, Func<T, bool> pred)
        {
            do
            {
                yield return enumerator.Current;
            } while (!pred(enumerator.Current) && enumerator.MoveNext());
        }
        public List<Stmt> Parse()
        {
            List<Stmt> statements = new();
            while (!AtEnd)
            {
                statements.Add(Declaration());
            }

            return statements;
        }

        public object ParseREPL()
        {
            allowExpression = true;
            List<Stmt> statements = new();
            while (!AtEnd)
            {
                statements.Add(Declaration());

                if (foundExpression)
                {
                    Stmt last = statements.Last();
                    return ((Stmt.Expression)last).expression;
                }

                allowExpression = false;
            }

            return statements;
        }
        private Expr Expression()
        {
            return ExprAssignment();
        }
        private Expr ExprAssignment()
        {
            Expr expr = ExprTernary();

            InsistEnd();

            if (Match(TokenType.Equal))
            {
                Token eq = Previous();
                Expr value = ExprAssignment();

                if (expr is Expr.Variable variable)
                {
                    return new Expr.Assign(variable.name, value);
                }
                else if (expr is Expr.Get get)
                {
                    return new Expr.Set(get.obj, get.name, value);
                }
                else if (expr is Expr.Indexer indexer)
                {
                    return new Expr.Mixer(indexer, value);
                }

                Error(eq, $"Invalid assignment target {expr.GetType().Name}");
            }
            else if (Match(TokenType.Plus_Equal, TokenType.Minus_Equal, TokenType.Slash_Equal, TokenType.Star_Equal))
            {
                Token op = Previous();
                Expr right = ExprAssignment();
                return new Expr.Binary(expr, op, right);
            }

            return expr;
        }
        private Expr ExprTernary()
        {
            Expr expr = ExprOr();

            InsistEnd();

            if (Match(TokenType.Question))
            {
                Token leftOperator = Previous();
                Expr middle = Expression();
                Token rightOperator = Consume(TokenType.Colon, "Expected ':' in ternary operator");
                Expr right = Expression();
                expr = new Expr.Ternary(expr, leftOperator, middle, rightOperator, right);
            }

            return expr;
        }
        private Expr ExprOr()
        {
            Expr expr = ExprAnd();

            InsistEnd();

            while (Match(TokenType.Pipe_Pipe))
            {
                Token op = Previous();
                Expr right = ExprAnd();
                expr = new Expr.Logical(expr, op, right);
            }

            return expr;
        }
        private Expr ExprAnd()
        {
            Expr expr = ExprEquality();

            InsistEnd();

            while (Match(TokenType.Ampersand_Ampersand))
            {
                Token op = Previous();
                Expr right = ExprEquality();
                expr = new Expr.Logical(expr, op, right);
            }

            return expr;
        }
        private Expr ExprEquality()
        {
            Expr expr = ExprComparison();

            InsistEnd();

            while (Match(TokenType.Not_Equal, TokenType.Equal_Equal))
            {
                Token op = Previous();
                Expr right = ExprComparison();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }
        private Expr ExprComparison()
        {
            Expr expr = ExprTerm();

            InsistEnd();

            while (Match(TokenType.Greater, TokenType.Greater_Equal, TokenType.Less, TokenType.Less_Equal))
            {
                Token op = Previous();
                Expr right = ExprTerm();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }
        private Expr ExprTerm()
        {
            Expr expr = ExprFactor();

            InsistEnd();

            while (Match(TokenType.Plus, TokenType.Minus))
            {
                Token op = Previous();
                Expr right = ExprFactor();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }
        private Expr ExprFactor()
        {
            Expr expr = ExprIncrement();

            InsistEnd();

            while (Match(TokenType.Slash, TokenType.Star, TokenType.Percent))
            {
                Token op = Previous();
                Expr right = ExprIncrement();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }
        private Expr ExprIncrement()
        {
            Expr expr = ExprUnary();

            InsistEnd();

            if (Match(TokenType.Plus_Plus, TokenType.Minus_Minus))
            {
                Token op = Previous();
                //return new Expr.Unary(op, expr);
                return new Expr.Binary(expr,
                    new Token(op.Type == TokenType.Plus_Plus ? TokenType.Plus_Equal : TokenType.Minus_Equal, op.Raw, op.Literal, op.Line),
                    new Expr.Literal(1D));
            }

            return expr;

        }
        private Expr ExprUnary()
        {
            InsistEnd();
            
            if (Match(TokenType.Not, TokenType.Minus, TokenType.Pipe))
            {
                Token op = Previous();
                Expr right = ExprUnary();
                return new Expr.Unary(op, right);
            }

            InsistEnd();

            if (Match(TokenType.Ampersand))
            {
                Token op = Previous();
                Token name = Consume(TokenType.Identifier, "Expected identifier after translator");
                return new Expr.Translate(op, name);
            }

            return ExprIndexer();
        }
        private Expr ExprCall()
        {
            Expr expr = ExprPrimary();

            InsistEnd();

            while (true)
            {
                if (Match(TokenType.Left_Parenthesis))
                {
                    expr = ExprFinishCall(expr);
                }
                else if (Match(TokenType.Period))
                {
                    Token name = Consume(TokenType.Identifier, "Expected property name after '.'");
                    expr = new Expr.Get(expr, name);
                }
                else if (Match(TokenType.Chain))
                {
                    // Chainable method calls, like this, a->b()->c(), where a is the object, b is the method, and c is the method
                    Token name = Consume(TokenType.Identifier, "Expected method name after '->'");
                    Consume(TokenType.Left_Parenthesis, "Expected '(' after method name");
                    Expr.Call call = (Expr.Call)ExprFinishCall(new Expr.Get(expr, name));
                    expr = new Expr.Chain(call);
                }
                else
                {
                    break;
                }
            }

            return expr;
        }
        private Expr ExprIndexer()
        {
            Expr expr = ExprCall();


            while (true)
            {
                InsistEnd();

                if (Match(TokenType.Left_Square))
                {
                    Expr index = Expression();
                    //Expr index = ExprIndexer();

                    Token paren = Consume(TokenType.Right_Square, "Expected ']' after index");
                    InsistEnd();

                    expr = new Expr.Indexer(expr, paren, index);

                }
                else if (Match(TokenType.Period))
                {
                    Token name = Consume(TokenType.Identifier, "Expected property name after '.'");
                    expr = new Expr.Get(expr, name);
                }
                else if (Match(TokenType.Chain))
                {
                    // Chainable method calls, like this, a->b()->c(), where a is the object, b is the method, and c is the method
                    Token name = Consume(TokenType.Identifier, "Expected method name after '->'");
                    Consume(TokenType.Left_Parenthesis, "Expected '(' after method name");
                    Expr.Call call = (Expr.Call)ExprFinishCall(new Expr.Get(expr, name));
                    expr = new Expr.Chain(call);
                }
                else
                {
                    break;
                }
            }

            return expr;
        }
        private Expr ExprFinishCall(Expr callee)
        {
            List<Expr> arguments = new();
            if (!Check(TokenType.Right_Parenthesis))
            {
                InsistEnd();
                do
                {
                    Match(TokenType.Newline);
                    if (Check(TokenType.Right_Parenthesis))
                        break;
                    if (arguments.Count >= 255)
                        Error(Peek(), "Maximum of 255 arguments allowed");

                    if (Check(TokenType.Comma))
                        arguments.Add(new Expr.Literal(null));
                    else
                        arguments.Add(Expression());

                } while (Match(TokenType.Newline, TokenType.Comma));
            }

            Token paren = Consume(TokenType.Right_Parenthesis, "Expected ')' after arguments");
            InsistEnd();

            return new Expr.Call(callee, paren, arguments);
        }
        private Expr ExprFinishLambda()
        {
            List<Token> parameters = new();
            if (!(Check(TokenType.Newline) || Check(TokenType.Left_Brace) || Check(TokenType.Colon)))
            {
                do
                {
                    if (parameters.Count >= 255)
                        Error(Peek(), "Maximum of 255 arguments allowed");

                    parameters.Add(Consume(TokenType.Identifier, "Expected parameter name"));
                } while (!(Check(TokenType.Newline) || Check(TokenType.Left_Brace) || Check(TokenType.Colon)));
            }

            InsistEnd();

            List<Stmt> body;
            if (Match(TokenType.Colon))
            {
                Expr expr = Expression();
                body = new() { new Stmt.Return(Previous(), expr) };
            }
            else
            {
                Consume(TokenType.Left_Brace, "Expected '{' after parameters");
                body = Block();
            }

            return new Expr.Lambda(parameters, body);
        }
        private Expr ExprFinishSequence()
        {
            List<Expr> items = new();
            if (!Check(TokenType.Right_Square))
            {
                InsistEnd();
                do
                {
                    Match(TokenType.Newline);
                    if (Check(TokenType.Right_Square))
                        break;

                    if (Check(TokenType.Comma))
                        items.Add(new Expr.Literal(null));
                    else
                        items.Add(Expression());
                } while (Match(TokenType.Comma, TokenType.Newline));
            }

            Token paren = Consume(TokenType.Right_Square, "Expected ']' after sequence");

            return new Expr.Sequence(items);
        }
        private Expr ExprFinishDictionary()
        {
            Dictionary<Expr, Expr> items = new();
            if (!Check(TokenType.Right_Brace))
            {
                InsistEnd();
                do
                {
                    Match(TokenType.Newline);
                    if (Check(TokenType.Right_Brace))
                        break;

                    if (Check(TokenType.Comma))
                        items.Add(new Expr.Literal(null), new Expr.Literal(null));
                    else if (Check(TokenType.Colon))
                    {
                        Advance();
                        items.Add(new Expr.Literal(null), Expression());
                    }
                    else
                    {
                        Expr key = Expression();
                        Consume(TokenType.Colon, "Expected ':' after key");
                        if (Check(TokenType.Comma) || Check(TokenType.Newline) || Check(TokenType.Right_Brace))
                        {
                            items.Add(key, new Expr.Literal(null));
                        }
                        else
                        {
                            Expr value = Expression();
                            items.Add(key, value);
                        }
                    }
                } while (Match(TokenType.Comma, TokenType.Newline));
            }

            Token paren = Consume(TokenType.Right_Brace, "Expected '}' after dictionary");

            return new Expr.Dictionary(paren, items);
        }
        private Expr ExprPrimary()
        {
            if (Match(TokenType.This)) return new Expr.This(Previous());
            if (Match(TokenType.True)) return new Expr.Literal(true);
            if (Match(TokenType.False)) return new Expr.Literal(false);
            if (Match(TokenType.Null)) return new Expr.Literal(null);
            if (Match(TokenType.Prototype)) return new Expr.Literal(Previous().Literal);
            if (Match(TokenType.String, TokenType.Interger))
                return new Expr.Literal(Previous().Literal);
            if (Match(TokenType.Left_Parenthesis))
            {
                Expr expr = Expression();
                Consume(TokenType.Right_Parenthesis, "Expected ')' after expression");
                return new Expr.Grouping(expr);
            }
            if (Match(TokenType.Left_Square)) return ExprFinishSequence();
            if (Match(TokenType.Left_Brace)) return ExprFinishDictionary();
            if (Match(TokenType.Colon_Colon))
            {
                // Force into expression
                return ExprPrimary();
            }
            if (Match(TokenType.Identifier))
            {
                return new Expr.Variable(Previous());
            }
            if (Match(TokenType.Lambda)) return ExprFinishLambda();

            if (Match(TokenType.Newline)) return null;

            throw Error(Peek(), "Expected expression");
        }
        private bool Match(params TokenType[] types)
        {
            foreach (TokenType type in types)
            {
                if (Check(type))
                {
                    Advance();
                    return true;
                }
            }

            return false;
        }
        private bool Check(TokenType type)
        {
            if (AtEnd) return false;
            return Peek().Type == type;
        }
        private Token Consume(TokenType type, string message, bool allowPrematureEnd = false)
        {
            if (Check(type) || (allowPrematureEnd && AtEnd)) return Advance();

            throw Error(Peek(), message);
        }
        private Token Advance()
        {
            if (!AtEnd) Current++;
            return Previous();
        }
        private Token Peek(int n = 0)
            => tokens[Current + n];
        private Token Previous()
            => tokens[Current - 1];
        private ParseError Error(Token token, string message)
        {
            Program.Error(token, message);
            return new ParseError();
        }
        private void Synchronize()
        {
            Advance();

            while (!AtEnd)
            {
                if (Previous().Type == TokenType.Newline) return;

                switch (Peek().Type)
                {
                    case TokenType.If:
                    case TokenType.Else:
                    case TokenType.Elif:
                    case TokenType.For:
                    case TokenType.While:
                    case TokenType.Function:
                    case TokenType.Global:
                    case TokenType.Return:
                    case TokenType.Break:
                    case TokenType.Continue:
                    case TokenType.Class:
                        return;
                }

                Advance();
            }
        }
        private Stmt Declaration()
        {
            try
            {
                if (Peek().Type.IsVariableType())
                {
                    VariableType dat = Advance().Type.ToVariableType();
                    while (Peek().Type.IsVariableType())
                    {
                        dat |= Advance().Type.ToVariableType();
                    }
                    if (Match(TokenType.Function))
                        return FunctionDeclaration(dat);
                    else if (Match(TokenType.Method))
                        return FunctionDeclaration(dat, isMethod: true);
                    else
                        return VariableDeclaration<Stmt.Typed>(dat);
                }
                if (Match(TokenType.Function)) return FunctionDeclaration(VariableType.Normal);
                if (Match(TokenType.Method)) return FunctionDeclaration(VariableType.Normal, isMethod: true);
                if (Match(TokenType.Class)) return ClassDeclaration();

                return Statement();
            }
            catch (ParseError)
            {
                Synchronize();
                return null;
            }
        }
        private Stmt Statement()
        {
            if (Match(TokenType.If)) return IfStatement();
            if (Match(TokenType.While)) return WhileStatement();
            if (Match(TokenType.For)) return ForStatement();
            if (Match(TokenType.Evaluate)) return EvaluateStatement();
            if (Match(TokenType.Import)) return ImportStatement();
            if (Match(TokenType.Return)) return ReturnStatement();
            if (Match(TokenType.Continue)) return ContinueStatement();
            if (Match(TokenType.Break)) return BreakStatement();
            if (Match(TokenType.Macro)) return MacroStatement();
            if (Match(TokenType.Left_Brace)) return new Stmt.Block(Block());

            return ExpressionStatement();
        }
        private Stmt ReturnStatement()
        {
            Token keyword = Previous();
            Expr value = null;
            if (!(Check(TokenType.Newline) || Check(TokenType.Right_Brace)))
            {
                value = Expression();
            }

            InsistEnd();
            return new Stmt.Return(keyword, value);
        }
        private Stmt ContinueStatement()
        {
            Token keyword = Previous();
            InsistEnd();

            return new Stmt.Continue(keyword);
        }
        private Stmt BreakStatement()
        {
            Token keyword = Previous();
            InsistEnd();

            return new Stmt.Break(keyword);
        }
        private Stmt MacroStatement()
        {
            //Token keyword = Previous();
            Token name = Consume(TokenType.Identifier, "Expected identifier after 'macro'");
            Consume(TokenType.Equal, "Expected '=' before macro body");

            List<Token> body = new();

            while (!Check(TokenType.Newline) && !AtEnd)
                body.Add(Advance());

            InsistEnd();

            macros[name.Raw] = new Macro(body);

            List<Token> newTokens = new();
            foreach (Token token in tokens.ToList())
            {
                if (token.Type == TokenType.Identifier && token.Raw == name.Raw)
                    newTokens.AddRange(body);
                else
                    newTokens.Add(token);
            }
            tokens = newTokens;

            return null;
        }
        public List<Token> Between(TokenType start, TokenType end)
        {
            List<Token> tokens = new();
            int level = 0;

            while (true)
            {
                Token current = Advance();
                if (current.Type == start)
                {
                    tokens.Add(current);
                    level++;
                }
                else if (current.Type == end)
                {
                    level--;
                    if (level <= 0)
                        break;
                    else
                        tokens.Add(current);
                }
                else
                    tokens.Add(current);
            }

            return tokens;
        }
        private List<Stmt> Block()
        {
            List<Stmt> statements = new();
            InsistEnd();
            while (!(Check(TokenType.Right_Brace)))
            {
                statements.Add(Declaration());
            }

            Consume(TokenType.Right_Brace, "Expected '}' after block");
            InsistEnd();
            return statements;
        }
        private Stmt FunctionDeclaration(VariableType type, bool isMethod = false)
        {
            Expr obj = null;
            Token name;

            // Work out whether it is just a token or not
            if (!isMethod
                && Peek().Type == TokenType.Identifier
                && Peek(1).Type is TokenType.Left_Brace or TokenType.Colon or TokenType.Newline)
            {
                name = Consume(TokenType.Identifier, "Expected function name");
            }
            // Its a method
            else
            {
                obj = Expression();
                if (obj is Expr.Get get)
                {
                    name = get.name;
                    obj = get.obj;
                }
                else
                    throw Error(Peek(), "Expected method name");
            }

            //if (defaultName.Length == 0)
            //else
            //    name = new Token(TokenType.Identifier, defaultName, null, 0);

            List<Token> parameters = new();

            if (!(Check(TokenType.Newline) || Check(TokenType.Left_Brace)))
            {
                //if (defaultName.Length == 0)
                Consume(TokenType.Colon, "Expected ':' after function name");

                do
                {
                    if (parameters.Count >= 255)
                        Error(Peek(), "Maximum of 255 arguments allowed");

                    parameters.Add(Consume(TokenType.Identifier, "Expected parameter name"));
                } while (!(Check(TokenType.Newline) || Check(TokenType.Left_Brace)));
            }

            InsistEnd();

            Consume(TokenType.Left_Brace, "Expected '{' before function body");
            List<Stmt> body = Block();

            if (obj == null)
                return new Stmt.Function(name, type, parameters, body);
            else
                return new Stmt.Method(obj, name, type, parameters, body);
        }
        private Stmt VariableDeclaration<T>(VariableType type)
        {
            Token name = Consume(TokenType.Identifier, "Expected variable name");

            Expr initializer = null;
            if (Match(TokenType.Equal))
            {
                initializer = Expression();
            }

            InsistEnd();
            return (Stmt)Activator.CreateInstance(typeof(T), name, initializer, type);
        }
        private Stmt VariableDeclaration<T>()
        {
            Token name = Consume(TokenType.Identifier, "Expected variable name");

            Expr initializer = null;
            if (Match(TokenType.Equal))
            {
                initializer = Expression();
            }

            InsistEnd();
            return (Stmt)Activator.CreateInstance(typeof(T), name, initializer);
        }
        private Stmt ClassDeclaration()
        {
            Token name = Consume(TokenType.Identifier, "Expected class name");
            InsistEnd();
            Consume(TokenType.Left_Brace, "Expected '{' before class body");
            InsistEnd();

            List<Stmt.Function> functions = new();
            List<Stmt.Typed> variables = new();
            while (!Check(TokenType.Right_Brace) && !AtEnd)
            {
                if (Peek().Type.IsVariableType())
                {
                    VariableType dat = Advance().Type.ToVariableType();
                    while (Peek().Type.IsVariableType())
                    {
                        dat |= Advance().Type.ToVariableType();
                    }

                    if (Match(TokenType.Function))
                    {
                        Stmt stmt = FunctionDeclaration(dat);
                        if (stmt is Stmt.Function func)
                            functions.Add(func);
                        else if (stmt is Stmt.Method)
                            Error(Previous(), "Expected function, got method");
                        else
                            Error(Previous(), "Expected function");
                    }
                    else if (Match(TokenType.Method))
                    {
                        Error(Previous(), "Methods are not allowed in classes");
                    }
                    else
                        variables.Add(VariableDeclaration<Stmt.Typed>(dat) as Stmt.Typed);
                }
                else
                {
                    if (Match(TokenType.Function))
                    {
                        Stmt stmt = FunctionDeclaration(VariableType.Normal);
                        if (stmt is Stmt.Function func)
                            functions.Add(func);
                        else if (stmt is Stmt.Method)
                            Error(Previous(), "Expected function, got method");
                        else
                            Error(Previous(), "Expected function");
                    }
                    else
                        variables.Add(VariableDeclaration<Stmt.Typed>(VariableType.Normal) as Stmt.Typed);
                }
            }

            Consume(TokenType.Right_Brace, "Expected '}' after class body");
            InsistEnd();

            return new Stmt.Class(name, functions, variables);
        }
        private Stmt ExpressionStatement()
        {
            Expr expr = Expression();

            if (allowExpression && AtEnd)
                foundExpression = true;
            else
                InsistEnd();

            return new Stmt.Expression(expr);
        }
        private Stmt EvaluateStatement()
        {
            Expr value = Expression();
            InsistEnd();
            return new Stmt.Evaluate(value);
        }
        private Stmt ImportStatement()
        {
            List<Token> name = new();
            while (Check(TokenType.Identifier) || Check(TokenType.Period))
                name.Add(Advance());

            InsistEnd();
            return new Stmt.Import(name);
        }
        private Stmt IfStatement()
        {
            Expr condition = Expression();
            InsistEnd();

            Stmt thenBranch = Statement();

            List<(Expr, Stmt)> elifBranches = new();
            while (Match(TokenType.Elif))
            {
                InsistEnd();
                Expr elifCondition = Expression();
                InsistEnd();
                Stmt elifBranch = Statement();
                elifBranches.Add((elifCondition, elifBranch));
            }

            Stmt elseBranch = null;
            if (Match(TokenType.Else))
            {
                InsistEnd();
                elseBranch = Statement();
            }

            return new Stmt.If(condition, thenBranch, elifBranches, elseBranch);
        }
        private Stmt WhileStatement()
        {
            Expr condition = Expression();
            InsistEnd();

            Stmt body = Statement();

            return new Stmt.While(condition, body);
        }
        private Stmt ForStatement()
        {
            Token name = null;
            if (Peek(1).Type == TokenType.Colon)
            {
                name = Consume(TokenType.Identifier, "Expected identifier after 'for'");
                Consume(TokenType.Colon, "Expected ':' after 'for' identifier");
            }
            Expr iterator = Expression();
            InsistEnd();

            Stmt body = Statement();

            return new Stmt.For(name, iterator, body);
        }
        private void ExpectEnd()
        {
            Consume(TokenType.Newline, "Expected line to end", true);
        }
        private bool InsistEnd()
        {
            bool ended = Check(TokenType.Newline) || AtEnd;
            if (ended) Advance();
            return ended;
        }
    }
}
