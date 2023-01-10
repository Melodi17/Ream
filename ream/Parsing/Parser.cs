﻿using Ream.Interpreting;
using Ream.Lexing;

namespace Ream.Parsing
{
    public class Parser
    {
        private bool allowExpression = false;
        private bool foundExpression = false;
        private class ParseError : Exception { }
        public bool AtEnd => this.Peek().Type == TokenType.End;
        private List<Token> tokens;
        private int Current = 0;

        public Parser(List<Token> tokens)
        {
            this.tokens = new();
            Token lastToken = null;
            foreach (Token item in tokens) // Remove empty lines
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
            using (IEnumerator<TValue> enumerator = values.GetEnumerator())
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
            while (!this.AtEnd)
            {
                statements.Add(this.Declaration());
            }

            return statements;
        }

        public object ParseREPL()
        {
            this.allowExpression = true;
            List<Stmt> statements = new();
            while (!this.AtEnd)
            {
                statements.Add(this.Declaration());

                if (this.foundExpression)
                {
                    Stmt last = statements.Last();
                    return ((Stmt.Expression)last).expression;
                }

                this.allowExpression = false;
            }

            return statements;
        }

        private Expr Expression()
        {
            return this.ExprAssignment();
        }

        private Expr ExprAssignment()
        {
            Expr expr = this.ExprTernary();

            this.InsistEnd();

            if (this.Match(TokenType.Equal))
            {
                Token eq = this.Previous();
                Expr value = this.ExprAssignment();

                if (expr is Expr.Variable variable)
                    return new Expr.Assign(variable.name, value);
                if (expr is Expr.Get get)
                    return new Expr.Set(get.obj, get.name, value);
                if (expr is Expr.Indexer indexer)
                    return new Expr.SetIndexer(indexer, value);

                this.Error(eq, $"Invalid assignment target {expr.GetType().Name}");
            }
            else if (this.Match(TokenType.PlusEqual, TokenType.MinusEqual, TokenType.SlashEqual, TokenType.StarEqual))
            {
                Token op = this.Previous();
                Expr right = this.ExprAssignment();
                return new Expr.Binary(expr, op, right);
            }

            return expr;
        }

        private Expr ExprTernary()
        {
            Expr expr = this.ExprOr();

            this.InsistEnd();

            if (this.Match(TokenType.Question))
            {
                Token leftOperator = this.Previous();
                Expr middle = this.Expression();
                Token rightOperator = this.Consume(TokenType.Colon, "Expected ':' in ternary operator");
                Expr right = this.Expression();
                expr = new Expr.Ternary(expr, leftOperator, middle, rightOperator, right);
            }

            return expr;
        }

        private Expr ExprOr()
        {
            Expr expr = this.ExprAnd();

            this.InsistEnd();

            while (this.Match(TokenType.PipePipe))
            {
                Token op = this.Previous();
                Expr right = this.ExprAnd();
                expr = new Expr.Logical(expr, op, right);
            }

            return expr;
        }

        private Expr ExprAnd()
        {
            Expr expr = this.ExprEquality();

            this.InsistEnd();

            while (this.Match(TokenType.AmpersandAmpersand))
            {
                Token op = this.Previous();
                Expr right = this.ExprEquality();
                expr = new Expr.Logical(expr, op, right);
            }

            return expr;
        }

        private Expr ExprEquality()
        {
            Expr expr = this.ExprComparison();

            this.InsistEnd();

            while (this.Match(TokenType.NotEqual, TokenType.EqualEqual))
            {
                Token op = this.Previous();
                Expr right = this.ExprComparison();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }

        private Expr ExprComparison()
        {
            Expr expr = this.ExprTerm();

            this.InsistEnd();

            while (this.Match(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual))
            {
                Token op = this.Previous();
                Expr right = this.ExprTerm();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }

        private Expr ExprTerm()
        {
            Expr expr = this.ExprFactor();

            this.InsistEnd();

            while (this.Match(TokenType.Plus, TokenType.Minus))
            {
                Token op = this.Previous();
                Expr right = this.ExprFactor();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }

        private Expr ExprFactor()
        {
            Expr expr = this.ExprIncrement();

            this.InsistEnd();

            while (this.Match(TokenType.Slash, TokenType.Star, TokenType.Percent))
            {
                Token op = this.Previous();
                Expr right = this.ExprIncrement();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }

        private Expr ExprIncrement()
        {
            Expr expr = this.ExprUnary();

            this.InsistEnd();

            if (this.Match(TokenType.PlusPlus, TokenType.MinusMinus))
            {
                Token op = this.Previous();
                //return new Expr.Unary(op, expr);
                return new Expr.Binary(expr,
                    new(op.Type == TokenType.PlusPlus ? TokenType.PlusEqual : TokenType.MinusEqual, op.Raw, op.Literal, op.Line),
                    new Expr.Literal(1D));
            }

            return expr;

        }

        private Expr ExprUnary()
        {
            this.InsistEnd();

            if (this.Match(TokenType.Not, TokenType.Minus))
            {
                Token op = this.Previous();
                Expr right = this.ExprUnary();
                return new Expr.Unary(op, right);
            }

            this.InsistEnd();

            return this.ExprIndexer();
        }

        private Expr ExprCall()
        {
            Expr expr = this.ExprPrimary();

            this.InsistEnd();

            while (true)
            {
                if (this.Match(TokenType.LeftParenthesis))
                {
                    expr = this.ExprFinishCall(expr);
                }
                else if (this.Match(TokenType.Period))
                {
                    Token name = this.Consume(TokenType.Identifier, "Expected property name after '.'");
                    expr = new Expr.Get(expr, name);
                }
                else if (this.Match(TokenType.Chain))
                {
                    // Chainable method calls, like this, a->b()->c(), where a is the object, b is the method, and c is the method
                    Token name = this.Consume(TokenType.Identifier, "Expected method name after '->'");
                    this.Consume(TokenType.LeftParenthesis, "Expected '(' after method name");
                    Expr.Call call = (Expr.Call)this.ExprFinishCall(new Expr.Get(expr, name));
                    expr = new Expr.Chain(call);
                }
                else
                    break;
            }

            return expr;
        }

        private Expr ExprIndexer()
        {
            Expr expr = this.ExprCall();


            while (true)
            {
                this.InsistEnd();

                if (this.Match(TokenType.LeftSquare))
                {
                    Expr index = this.Expression();
                    //Expr index = ExprIndexer();

                    Token paren = this.Consume(TokenType.RightSquare, "Expected ']' after index");
                    this.InsistEnd();

                    expr = new Expr.Indexer(expr, paren, index);

                }
                else if (this.Match(TokenType.Period))
                {
                    Token name = this.Consume(TokenType.Identifier, "Expected property name after '.'");
                    expr = new Expr.Get(expr, name);
                }
                else if (this.Match(TokenType.Chain))
                {
                    // Chainable method calls, like this, a->b()->c(), where a is the object, b is the method, and c is the method
                    Token name = this.Consume(TokenType.Identifier, "Expected method name after '->'");
                    this.Consume(TokenType.LeftParenthesis, "Expected '(' after method name");
                    Expr.Call call = (Expr.Call)this.ExprFinishCall(new Expr.Get(expr, name));
                    expr = new Expr.Chain(call);
                }
                else
                    break;
            }

            return expr;
        }

        private Expr ExprFinishCall(Expr callee)
        {
            List<Expr> arguments = new();
            if (!this.Check(TokenType.RightParenthesis))
            {
                this.InsistEnd();
                do
                {
                    this.Match(TokenType.Newline);
                    if (this.Check(TokenType.RightParenthesis))
                        break;
                    if (arguments.Count >= 255) this.Error(this.Peek(), "Maximum of 255 arguments allowed");

                    if (this.Check(TokenType.Comma))
                        arguments.Add(new Expr.Literal(null));
                    else
                        arguments.Add(this.Expression());

                } while (this.Match(TokenType.Newline, TokenType.Comma));
            }

            Token paren = this.Consume(TokenType.RightParenthesis, "Expected ')' after arguments");
            this.InsistEnd();

            return new Expr.Call(callee, paren, arguments);
        }

        private Expr ExprFinishLambda()
        {
            List<Token> parameters = new();
            if (!(this.Check(TokenType.Newline) || this.Check(TokenType.LeftBrace) || this.Check(TokenType.Colon)))
            {
                do
                {
                    if (parameters.Count >= 255) this.Error(this.Peek(), "Maximum of 255 arguments allowed");

                    parameters.Add(this.Consume(TokenType.Identifier, "Expected parameter name"));
                } while (!(this.Check(TokenType.Newline) || this.Check(TokenType.LeftBrace) || this.Check(TokenType.Colon)));
            }

            this.InsistEnd();

            List<Stmt> body;
            if (this.Match(TokenType.Colon))
            {
                Expr expr = this.Expression();
                body = new() { new Stmt.Return(this.Previous(), expr) };
            }
            else
            {
                this.Consume(TokenType.LeftBrace, "Expected '{' after parameters");
                body = this.Block();
            }

            return new Expr.Lambda(parameters, body);
        }

        private Expr ExprFinishSequence()
        {
            List<Expr> items = new();
            if (!this.Check(TokenType.RightSquare))
            {
                this.InsistEnd();
                do
                {
                    this.Match(TokenType.Newline);
                    if (this.Check(TokenType.RightSquare))
                        break;

                    if (this.Check(TokenType.Comma))
                        items.Add(new Expr.Literal(null));
                    else
                        items.Add(this.Expression());
                } while (this.Match(TokenType.Comma, TokenType.Newline));
            }

            Token paren = this.Consume(TokenType.RightSquare, "Expected ']' after sequence");

            return new Expr.Sequence(items);
        }

        private Expr ExprFinishDictionary()
        {
            Dictionary<Expr, Expr> items = new();
            if (!this.Check(TokenType.RightBrace))
            {
                this.InsistEnd();
                do
                {
                    this.Match(TokenType.Newline);
                    if (this.Check(TokenType.RightBrace))
                        break;

                    if (this.Check(TokenType.Comma))
                        items.Add(new Expr.Literal(null), new Expr.Literal(null));
                    else if (this.Check(TokenType.Colon))
                    {
                        this.Advance();
                        items.Add(new Expr.Literal(null), this.Expression());
                    }
                    else
                    {
                        Expr key = this.Expression();
                        this.Consume(TokenType.Colon, "Expected ':' after key");
                        if (this.Check(TokenType.Comma) || this.Check(TokenType.Newline) || this.Check(TokenType.RightBrace))
                        {
                            items.Add(key, new Expr.Literal(null));
                        }
                        else
                        {
                            Expr value = this.Expression();
                            items.Add(key, value);
                        }
                    }
                } while (this.Match(TokenType.Comma, TokenType.Newline));
            }

            Token paren = this.Consume(TokenType.RightBrace, "Expected '}' after dictionary");

            return new Expr.Dictionary(paren, items);
        }

        private Expr ExprPrimary()
        {
            if (this.Match(TokenType.This)) return new Expr.This(this.Previous());
            if (this.Match(TokenType.True)) return new Expr.Literal(true);
            if (this.Match(TokenType.False)) return new Expr.Literal(false);
            if (this.Match(TokenType.Null)) return new Expr.Literal(null);
            if (this.Match(TokenType.String, TokenType.Integer))
                return new Expr.Literal(this.Previous().Literal);
            if (this.Match(TokenType.LeftParenthesis))
            {
                Expr expr = this.Expression();
                this.Consume(TokenType.RightParenthesis, "Expected ')' after expression");
                return new Expr.Grouping(expr);
            }
            if (this.Match(TokenType.LeftSquare)) return this.ExprFinishSequence();
            if (this.Match(TokenType.LeftBrace)) return this.ExprFinishDictionary();
            if (this.Match(TokenType.ColonColon))
                return this.ExprPrimary(); // Force into expression
            if (this.Match(TokenType.Identifier))
                return new Expr.Variable(this.Previous());
            if (this.Match(TokenType.Lambda))
                return this.ExprFinishLambda();
            if (this.Match(TokenType.Newline))
                return null;
            if (this.Match(TokenType.Dispose))
            {
                // Looks like this: _ = <expr>
                Token kw = this.Previous();
                Token eq = this.Consume(TokenType.Equal, "Expected '=' after '_'");
                Expr expr = this.Expression();
                return new Expr.Dispose(kw, expr);
            }

            throw this.Error(this.Peek(), "Expected expression");
        }

        private bool Match(params TokenType[] types)
        {
            foreach (TokenType type in types)
            {
                if (this.Check(type))
                {
                    this.Advance();
                    return true;
                }
            }

            return false;
        }

        private bool Check(TokenType type)
        {
            if (this.AtEnd) return false;
            return this.Peek().Type == type;
        }

        private Token Consume(TokenType type, string message, bool allowPrematureEnd = false)
        {
            if (this.Check(type) || (allowPrematureEnd && this.AtEnd)) return this.Advance();

            throw this.Error(this.Peek(), message);
        }

        private Token Advance()
        {
            if (!this.AtEnd) this.Current++;
            return this.Previous();
        }

        private Token Peek(int n = 0)
            => this.tokens[this.Current + n];

        private Token Previous()
            => this.tokens[this.Current - 1];

        private ParseError Error(Token token, string message)
        {
            Program.Error(token, message);
            return new();
        }

        private void Synchronize()
        {
            this.Advance();

            while (!this.AtEnd)
            {
                if (this.Previous().Type == TokenType.Newline) return;

                switch (this.Peek().Type)
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

                this.Advance();
            }
        }

        private Stmt Declaration()
        {
            try
            {
                if (this.Peek().Type.IsVariableType())
                {
                    VariableType dat = this.Advance().Type.ToVariableType();
                    while (this.Peek().Type.IsVariableType())
                    {
                        dat |= this.Advance().Type.ToVariableType();
                    }
                    if (this.Match(TokenType.Function))
                        return this.FunctionDeclaration(dat);
                    else
                        return this.VariableDeclaration<Stmt.Typed>(dat);
                }
                if (this.Match(TokenType.Function)) return this.FunctionDeclaration(VariableType.Normal);
                if (this.Match(TokenType.Class)) return this.ClassDeclaration();

                return this.Statement();
            }
            catch (ParseError)
            {
                this.Synchronize();
                return null;
            }
        }

        private Stmt Statement()
        {
            if (this.Match(TokenType.If)) return this.IfStatement();
            if (this.Match(TokenType.While)) return this.WhileStatement();
            if (this.Match(TokenType.For)) return this.ForStatement();
            if (this.Match(TokenType.Import)) return this.ImportStatement();
            if (this.Match(TokenType.Return)) return this.ReturnStatement();
            if (this.Match(TokenType.Continue)) return this.ContinueStatement();
            if (this.Match(TokenType.Break)) return this.BreakStatement();
            if (this.Match(TokenType.LeftBrace)) return new Stmt.Block(this.Block());

            return this.ExpressionStatement();
        }

        private Stmt ReturnStatement()
        {
            Token keyword = this.Previous();
            Expr value = null;
            if (!(this.Check(TokenType.Newline) || this.Check(TokenType.RightBrace)))
            {
                value = this.Expression();
            }

            this.InsistEnd();
            return new Stmt.Return(keyword, value);
        }

        private Stmt ContinueStatement()
        {
            Token keyword = this.Previous();
            this.InsistEnd();

            return new Stmt.Continue(keyword);
        }

        private Stmt BreakStatement()
        {
            Token keyword = this.Previous();
            this.InsistEnd();

            return new Stmt.Break(keyword);
        }

        public List<Token> Between(TokenType start, TokenType end)
        {
            List<Token> tokens = new();
            int level = 0;

            while (true)
            {
                Token current = this.Advance();
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
            this.InsistEnd();
            while (!(this.Check(TokenType.RightBrace)))
            {
                statements.Add(this.Declaration());
            }

            this.Consume(TokenType.RightBrace, "Expected '}' after block");
            this.InsistEnd();
            return statements;
        }

        private Stmt FunctionDeclaration(VariableType type)
        {
            Token name;

            Expr obj = this.Expression();
            if (obj is Expr.Get get)
            {
                name = get.name;
                obj = get.obj;
            }
            else if (obj is Expr.Variable var)
            {
                name = var.name;
                obj = null;
            }
            else
            {
                throw this.Error(this.Previous(), "Expected function name");
            }

            List<Token> parameters = new();

            if (!(this.Check(TokenType.Newline) || this.Check(TokenType.LeftBrace)))
            {
                this.Consume(TokenType.Colon, "Expected ':' after function name");

                do
                {
                    if (parameters.Count >= 255) this.Error(this.Peek(), "Maximum of 255 arguments allowed");

                    parameters.Add(this.Consume(TokenType.Identifier, "Expected parameter name"));
                } while (!(this.Check(TokenType.Newline) || this.Check(TokenType.LeftBrace)));
            }

            this.InsistEnd();

            this.Consume(TokenType.LeftBrace, "Expected '{' before function body");
            List<Stmt> body = this.Block();

            if (obj == null)
                return new Stmt.Function(name, type, parameters, body);
            else
                return new Stmt.Method(obj, name, type, parameters, body);
        }

        private Stmt VariableDeclaration<T>(VariableType type)
        {
            Token name = this.Consume(TokenType.Identifier, "Expected variable name");

            Expr initializer = null;
            if (this.Match(TokenType.Equal))
            {
                initializer = this.Expression();
            }

            this.InsistEnd();
            return (Stmt)Activator.CreateInstance(typeof(T), name, initializer, type);
        }

        private Stmt VariableDeclaration<T>()
        {
            Token name = this.Consume(TokenType.Identifier, "Expected variable name");

            Expr initializer = null;
            if (this.Match(TokenType.Equal))
            {
                initializer = this.Expression();
            }

            this.InsistEnd();
            return (Stmt)Activator.CreateInstance(typeof(T), name, initializer);
        }

        private Stmt ClassDeclaration()
        {
            Token name = this.Consume(TokenType.Identifier, "Expected class name");
            this.InsistEnd();
            this.Consume(TokenType.LeftBrace, "Expected '{' before class body");
            this.InsistEnd();

            List<Stmt.Function> functions = new();
            List<Stmt.Typed> variables = new();
            while (!this.Check(TokenType.RightBrace) && !this.AtEnd)
            {
                if (this.Peek().Type.IsVariableType())
                {
                    VariableType dat = this.Advance().Type.ToVariableType();
                    while (this.Peek().Type.IsVariableType())
                    {
                        dat |= this.Advance().Type.ToVariableType();
                    }

                    if (this.Match(TokenType.Function))
                    {
                        Stmt stmt = this.FunctionDeclaration(dat);
                        if (stmt is Stmt.Function func)
                            functions.Add(func);
                        else if (stmt is Stmt.Method)
                            this.Error(this.Previous(), "Expected function, got method");
                        else
                            this.Error(this.Previous(), "Expected function");
                    }
                    else
                        variables.Add(this.VariableDeclaration<Stmt.Typed>(dat) as Stmt.Typed);
                }
                else
                {
                    if (this.Match(TokenType.Function))
                    {
                        Stmt stmt = this.FunctionDeclaration(VariableType.Normal);
                        if (stmt is Stmt.Function func)
                            functions.Add(func);
                        else if (stmt is Stmt.Method)
                            this.Error(this.Previous(), "Expected function, got method");
                        else
                            this.Error(this.Previous(), "Expected function");
                    }
                    else
                        variables.Add(this.VariableDeclaration<Stmt.Typed>(VariableType.Normal) as Stmt.Typed);
                }
            }

            this.Consume(TokenType.RightBrace, "Expected '}' after class body");
            this.InsistEnd();

            return new Stmt.Class(name, functions, variables);
        }

        private Stmt ExpressionStatement()
        {
            Expr expr = this.Expression();

            if (this.allowExpression && this.AtEnd)
                this.foundExpression = true;
            else
                this.InsistEnd();

            return new Stmt.Expression(expr);
        }

        private Stmt EvaluateStatement()
        {
            Expr value = this.Expression();
            this.InsistEnd();
            return new Stmt.Evaluate(value);
        }

        private Stmt ImportStatement()
        {
            List<Token> name = new();
            while (this.Check(TokenType.Identifier) || this.Check(TokenType.Period))
                name.Add(this.Advance());

            this.InsistEnd();
            return new Stmt.Import(name);
        }

        private Stmt IfStatement()
        {
            Expr condition = this.Expression();
            this.InsistEnd();

            Stmt thenBranch = this.Statement();

            List<(Expr, Stmt)> elifBranches = new();
            while (this.Match(TokenType.Elif))
            {
                this.InsistEnd();
                Expr elifCondition = this.Expression();
                this.InsistEnd();
                Stmt elifBranch = this.Statement();
                elifBranches.Add((elifCondition, elifBranch));
            }

            Stmt elseBranch = null;
            if (this.Match(TokenType.Else))
            {
                this.InsistEnd();
                elseBranch = this.Statement();
            }

            return new Stmt.If(condition, thenBranch, elifBranches, elseBranch);
        }

        private Stmt WhileStatement()
        {
            Expr condition = this.Expression();
            this.InsistEnd();

            Stmt body = this.Statement();

            return new Stmt.While(condition, body);
        }

        private Stmt ForStatement()
        {
            Token name = null;
            if (this.Peek(1).Type == TokenType.Colon)
            {
                name = this.Consume(TokenType.Identifier, "Expected identifier after 'for'");
                this.Consume(TokenType.Colon, "Expected ':' after 'for' identifier");
            }
            Expr iterator = this.Expression();
            this.InsistEnd();

            Stmt body = this.Statement();

            return new Stmt.For(name, iterator, body);
        }

        private void ExpectEnd()
        {
            this.Consume(TokenType.Newline, "Expected line to end", true);
        }

        private bool InsistEnd()
        {
            bool ended = this.Check(TokenType.Newline) || this.AtEnd;
            if (ended) this.Advance();
            return ended;
        }
    }
}
