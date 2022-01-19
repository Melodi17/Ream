using Ream.Lexing;

namespace Ream.Parsing
{
    public class Parser
    {
        private class ParseError : Exception { }
        public bool AtEnd => Peek().Type == TokenType.End;
        private readonly List<Token> Tokens;
        private int Current = 0;

        public Parser(List<Token> tokens)
        {
            this.Tokens = new();
            Token lastToken = null;
            foreach (var item in tokens) // Remove empty lines
            {
                if (item.Type == TokenType.Newline && lastToken != null && lastToken.Type == TokenType.Newline)
                    continue;

                lastToken = item;
                this.Tokens.Add(item);
            }
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
        private Expr Expression()
        {
            return ExprAssignment();
        }
        private Expr ExprAssignment()
        {
            Expr expr = ExprOr();

            if (Match(TokenType.Equal))
            {
                Token eq = Previous();
                Expr value = ExprAssignment();

                if (expr is Expr.Variable)
                {
                    Token name = ((Expr.Variable)expr).name;
                    return new Expr.Assign(name, value);
                }

                Error(eq, "Invalid assignment target");
            }

            return expr;
        }
        private Expr ExprOr()
        {
            Expr expr = ExprAnd();

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
            Expr expr = ExprUnary();

            while (Match(TokenType.Slash, TokenType.Star))
            {
                Token op = Previous();
                Expr right = ExprUnary();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }
        private Expr ExprUnary()
        {
            if (Match(TokenType.Not, TokenType.Minus))
            {
                Token op = Previous();
                Expr right = ExprUnary();
                return new Expr.Unary(op, right);
            }

            return ExprCall();
        }
        private Expr ExprCall()
        {
            Expr expr = ExprPrimary();

            while (true)
            {
                if (Match(TokenType.Left_Parenthesis))
                {
                    expr = ExprFinishCall(expr);
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
                do
                {
                    if (arguments.Count >= 255)
                        Error(Peek(), "Maximum of 255 arguments allowed");

                    arguments.Add(Expression());
                } while (Match(TokenType.Comma));
            }

            Token paren = Consume(TokenType.Right_Parenthesis, "Expected ')' after arguments");
            //ExpectEnd();

            return new Expr.Call(callee, paren, arguments);
        }
        private Expr ExprLambda()
        {
            Expr expr = ExprCall();

            if (Match(TokenType.Lambda))
            {
                expr = ExprFinishLambda();
            }

            return expr;
        }
        private Expr ExprFinishLambda()
        {
            List<Token> parameters = new();
            if (!(Check(TokenType.Newline) || Check(TokenType.Left_Brace)))
            {
                do
                {
                    if (parameters.Count >= 255)
                        Error(Peek(), "Maximum of 255 arguments allowed");

                    parameters.Add(Consume(TokenType.Identifier, "Expected parameter name"));
                } while (!Check(TokenType.Newline));
            }

            InsistEnd();

            Consume(TokenType.Left_Brace, "Expected '{' before lambda body");
            List<Stmt> body = Block();
            return new Expr.Lambda(parameters, body);
        }
        private Expr ExprPrimary()
        {
            if (Match(TokenType.True)) return new Expr.Literal(true);
            if (Match(TokenType.False)) return new Expr.Literal(false);
            if (Match(TokenType.Null)) return new Expr.Literal(null);
            if (Match(TokenType.String, TokenType.Interger))
                return new Expr.Literal(Previous().Literal);
            if (Match(TokenType.Left_Parenthesis))
            {
                Expr expr = Expression();
                Consume(TokenType.Right_Parenthesis, "Expected ')' after expression");
                return new Expr.Grouping(expr);
            }
            if (Match(TokenType.Identifier))
            {
                return new Expr.Variable(Previous());
            }
            if (Match(TokenType.Lambda))
            {
                return ExprFinishLambda();
            }

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
            => Tokens[Current + n];
        private Token Previous()
            => Tokens[Current - 1];
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
                if (Match(TokenType.Global)) return VariableDeclaration(true);
                if (Match(TokenType.Local)) return VariableDeclaration(false);
                if (Match(TokenType.Function)) return Function();

                return Statement();
            }
            catch (ParseError error)
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
            if (Match(TokenType.Write)) return PrintStatement();
            if (Match(TokenType.Return)) return ReturnStatement();
            if (Match(TokenType.Left_Brace)) return new Stmt.Block(Block());

            return ExpressionStatement();
        }
        private Stmt ReturnStatement()
        {
            Token keyword = Previous();
            Expr value = null;
            if (!Check(TokenType.Newline))
            {
                value = Expression();
            }

            InsistEnd();
            return new Stmt.Return(keyword, value);
        }
        private List<Stmt> Block()
        {
            List<Stmt> statements = new();
            InsistEnd();
            while (!Check(TokenType.Right_Brace))
            {
                statements.Add(Declaration());
            }

            Consume(TokenType.Right_Brace, "Expected '}' after block");
            InsistEnd();
            return statements;
        }
        private Stmt Function()
        {
            Token name = Consume(TokenType.Identifier, "Expected function name");
            List<Token> parameters = new();
            if (!(Check(TokenType.Newline) || Check(TokenType.Left_Brace)))
            {
                Consume(TokenType.Colon, "Expected ':' after function name");

                do
                {
                    if (parameters.Count >= 255)
                        Error(Peek(), "Maximum of 255 arguments allowed");

                    parameters.Add(Consume(TokenType.Identifier, "Expected parameter name"));
                } while (!Check(TokenType.Newline));
            }

            InsistEnd();

            Consume(TokenType.Left_Brace, "Expected '{' before function body");
            List<Stmt> body = Block();
            return new Stmt.Function(name, parameters, body);
        }
        private Stmt VariableDeclaration(bool isGlobal)
        {
            Token name = Consume(TokenType.Identifier, "Expected variable name");

            Expr initializer = null;
            if (Match(TokenType.Equal))
            {
                initializer = Expression();
            }

            InsistEnd();
            return isGlobal ? new Stmt.Global(name, initializer) : new Stmt.Local(name, initializer);
        }
        private Stmt ExpressionStatement()
        {
            Expr expr = Expression();
            InsistEnd();
            return new Stmt.Expression(expr);
        }
        private Stmt PrintStatement()
        {
            Expr value = Expression();
            InsistEnd();
            return new Stmt.Write(value);
        }
        private Stmt IfStatement()
        {
            Expr condition = Expression();
            InsistEnd();

            Stmt thenBranch = Statement();

            Stmt elseBranch = null;
            if (Match(TokenType.Else))
            {
                InsistEnd();
                elseBranch = Statement();
            }

            return new Stmt.If(condition, thenBranch, elseBranch);
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
