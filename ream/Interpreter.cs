using System;
using System.Collections.Generic;
using System.Linq;

namespace ream
{
    public class Interpreter
    {
        public static string[] ReservedKeywords =
        {
            "global",
            "function",
            "import",
            "if",
            "for",
            "while",
            "else",
            "write",
            "null",
            "return"
        };
        public Node MainNode;
        public Scope GlobalScope;
        public Interpreter(Node mainNode)
        {
            MainNode = mainNode;
            GlobalScope = new();
        }
        public void Interpret()
        {
            Dive(MainNode, GlobalScope);
        }
        public void Dive(Node node, Scope scope)
        {
            /* TODO:
             * - Implement return values
             * - Work on import
             * - Work on statements
             * - Else + else if
             * - Investigate multi-operation algorithms
             */
            Scope localScope = scope.CreateChild();

            List<Token> tokens = new();

            tokens.AddRange(node.ChildNodes.Where(x => x.HasToken).Select(x => x.Token));

            List<Node> childNodes = new();
            foreach (Node item in node.ChildNodes)
            {
                if (item.ChildNodes.Any())
                    //Dive(item, localScope);
                    childNodes.Add(item);
            }
            Reader<Node> childNodeReader = new(childNodes.ToArray());

            List<List<Token>> lines = tokens.Split(x => x.Type == TokenType.Newline).Select(x => x.ToList()).Where(x => x.Count > 0).ToList();
            foreach (var item in lines)
            {
                Token tok = Evaluate(localScope, item.ToArray(), childNodeReader);
            }
        }
        public Token Evaluate(Scope scope, Token[] tokens, Reader<Node> childNodeReader = null)
        {
            bool childNodeSupported = childNodeReader != null;
            Reader<Token> tokenReader = new(tokens);
            InterpretFormat format = new(tokens);

            string fValue = tokenReader.Peek().Value.ToString();
            if (format.IsSimilar("V") && ReservedKeywords.Any(fValue.Equals))
            {
                tokenReader.Read(); // Remove first item
                if (fValue.Equals("global") && format.IsSimilar("VV+ "))
                {
                    Token leftToken = tokenReader.Read();

                    if (leftToken.Value.ToString() == "function")
                    {
                        Token nameToken = tokenReader.Read();
                        Node node = childNodeReader.Read();

                        string name = nameToken.Value.ToString();
                        if (format.IsSimilar("VVV+V"))
                        {
                            Token operatorToken = tokenReader.Read();
                            Token[] variableNameTokens = tokenReader.Rest();
                            string[] variableNames = variableNameTokens.Select(x => x.Value.ToString()).ToArray();

                            if (operatorToken.Value.ToString() != ":")
                                Error("TokenValueInvalid", "Specified token was not value ':'");

                            IFunction f = new Function(name, variableNames, node);
                            scope.Set(name, Token.ManualCreate(f, TokenType.Function), true);
                        }
                        else
                        {
                            IFunction f = new Function(name, Array.Empty<string>(), node);
                            scope.Set(name, Token.ManualCreate(f, TokenType.Function));
                        }
                    }
                    else
                    {
                        Token operatorToken = tokenReader.Read();
                        Token rightToken = Evaluate(scope, tokenReader.Rest());

                        if (operatorToken.Value.ToString() != "=")
                            Error("TokenValueInvalid", "Specified token was not value '='");

                        scope.Set(leftToken.Value.ToString(), rightToken, true);
                    }
                }
                if (fValue.Equals("import") && format.IsSimilar("VV"))
                {
                    string flName = tokenReader.Read().Value.ToString() + ".r";
                }
                else if (fValue.Equals("write") && format.IsSimilar("V "))
                {
                    Console.WriteLine(Evaluate(scope, tokenReader.Rest()));
                }
                else if (fValue.Equals("return") && format.IsSimilar("V "))
                {
                    return Evaluate(scope, tokenReader.Rest());
                }
                else if (fValue.Equals("if") && format.IsSimilar("V "))
                {
                    Token conditionalToken = Evaluate(scope, tokenReader.Rest());
                    Node node = childNodeReader.Read();
                    if (conditionalToken.Type == TokenType.Boolean)
                    {
                        if ((bool)conditionalToken.Value)
                            Dive(node, scope);
                    }
                    else
                        Error("TokenTypeInvalid", "Specified token was not type 'boolean'");
                }
                else if (fValue.Equals("while") && format.IsSimilar("V "))
                {
                    Token[] constantForEval = tokenReader.Rest();
                    Node node = childNodeReader.Read();
                    while (true)
                    {
                        Token conditionalToken = Evaluate(scope, constantForEval);
                        if (conditionalToken.Type == TokenType.Boolean)
                        {
                            if ((bool)conditionalToken.Value)
                                Dive(node, scope);
                            else
                                break;
                        }
                        else
                            Error("TokenTypeInvalid", "Specified token was not type 'boolean'");
                    }
                }
                else if (fValue.Equals("for") && format.IsSimilar("V "))
                {
                    Node node = childNodeReader.Read();
                    Token setToken = null;
                    if (format.IsSimilar("VV+ "))
                    {
                        setToken = tokenReader.Read();
                        Token opToken = tokenReader.Read();

                        if (opToken.Value.ToString() != ":")
                            Error("TokenValueInvalid", "Specified token was not value ':'");
                    }

                    bool useSet = setToken != null;
                    string set = setToken.Value.ToString();
                    Token iterateToken = Evaluate(scope, tokenReader.Rest());

                    if (iterateToken.Type == TokenType.Interger)
                    {
                        for (long i = 0; i < (long)iterateToken.Value; i++)
                        {
                            if (useSet)
                            {
                                Scope localScope = scope.CreateChild();
                                localScope.Set(set, Token.ManualCreate(i, TokenType.Interger));
                                Dive(node, localScope);
                            }
                            else
                                Dive(node, scope);
                        }
                    }
                    else
                        Error("TokenTypeInvalid", "Specified token was not a supported type");

                    //Dive(node, scope)
                    //Token[] collToken = tokenReader.Rest();
                    //

                    //Token iterator = Evaluate(scope, collToken);
                }
                else if (fValue.Equals("function") && format.IsSimilar("VV"))
                {
                    Token nameToken = tokenReader.Read();
                    Node node = childNodeReader.Read();

                    string name = nameToken.Value.ToString();
                    if (format.IsSimilar("VV+V"))
                    {
                        Token operatorToken = tokenReader.Read();
                        Token[] variableNameTokens = tokenReader.Rest();
                        string[] variableNames = variableNameTokens.Select(x => x.Value.ToString()).ToArray();

                        if (operatorToken.Value.ToString() != ":")
                            Error("TokenValueInvalid", "Specified token was not value ':'");

                        IFunction f = new Function(name, variableNames, node);
                        scope.Set(name, Token.ManualCreate(f, TokenType.Function));
                    }
                    else
                    {
                        IFunction f = new Function(name, Array.Empty<string>(), node);
                        scope.Set(name, Token.ManualCreate(f, TokenType.Function));
                    }
                }
            }
            else
            {
                if (format.IsMatching("V"))
                {
                    return scope.Get(fValue);
                }
                else if (format.IsMatching(" "))
                {
                    return tokenReader.Read();
                }
                else if (format.IsSimilar("V("))
                {
                    Token nameToken = tokenReader.Read();
                    Token openBracketToken = tokenReader.Read();
                    Token[] parameterTokens = tokenReader.Rest(1);
                    Token closeBracketToken = tokenReader.Read();

                    if (openBracketToken.Type == closeBracketToken.Type && openBracketToken.Type == TokenType.Bracket)
                    {
                        Token functionToken = scope.Get(nameToken.Value.ToString());
                        if (functionToken.Type != TokenType.Function)
                            Error("TokenTypeInvalid", "Specified token was not type 'function'");

                        IFunction function = (IFunction)functionToken.Value;

                        if (parameterTokens.Length > 0)
                        {
                            Token[] solvedParameters = parameterTokens
                                .Split(x => x.Type == TokenType.Operator && x.Value.ToString() == ",")
                                .Select(x => Evaluate(scope, x.ToArray()))
                                .ToArray();

                            function.Invoke(this, scope, solvedParameters);
                        }
                        else
                        {
                            function.Invoke(this, scope, Array.Empty<Token>());
                        }

                    }
                    else
                        Error("TokenTypeInvalid", "Specified token was not type 'bracket'");
                }
                else if (tokens.Any(x =>
                    { string op = x.Value.ToString(); return op == "|" || op == "&"; }))
                {
                    List<Token> leftTokens = new();
                    Token operatorToken;
                    while (true)
                    {
                        Token currentToken = tokenReader.Read();
                        if (currentToken.Type == TokenType.Operator
                            && (currentToken.Value.ToString() == "|"
                                || currentToken.Value.ToString() == "&"))
                        {
                            operatorToken = currentToken; break;
                        }
                        leftTokens.Add(currentToken);
                    }
                    Token leftToken = Evaluate(scope, leftTokens.ToArray());
                    Token rightToken = Evaluate(scope, tokenReader.Rest());

                    if (!(leftToken.Type == rightToken.Type && leftToken.Type == TokenType.Boolean))
                        Error("InvalidOperator", $"Specified operator cannot be applied to type {leftToken.Type.ToString().ToLower()} and {rightToken.Type.ToString().ToLower()}");

                    string op = operatorToken.Value.ToString();

                    if (op == "|")
                        return Token.ManualCreate((bool)leftToken.Value || (bool)rightToken.Value, TokenType.Boolean);
                    else if (op == "&")
                        return Token.ManualCreate((bool)leftToken.Value && (bool)rightToken.Value, TokenType.Boolean);
                }
                else if (format.IsSimilar(" + "))
                {
                    Token leftTokenUnEval = tokenReader.Read();
                    Token leftToken = Evaluate(scope, new Token[] { leftTokenUnEval });
                    Token operatorToken = tokenReader.Read();
                    Token rightToken = Evaluate(scope, tokenReader.Rest());

                    string op = operatorToken.Value.ToString();
                    if (op.Equals("="))
                    {
                        leftToken = leftTokenUnEval;
                        if (leftToken.Type != TokenType.Value)
                            Error("TokenTypeInvalid", "Specified token was not type value");

                        scope.Set(leftToken.Value.ToString(), rightToken);
                    }
                    else if (op.Equals("=="))
                    {
                        return Token.ManualCreate(leftToken.Value.ToString() == rightToken.Value.ToString(), TokenType.Boolean);
                    }
                    else if (op.Equals("!="))
                    {
                        return Token.ManualCreate(leftToken.Value.ToString() != rightToken.Value.ToString(), TokenType.Boolean);
                    }
                    //else if (op.Equals("==="))
                    //    return Token.ManualCreate(ObjectComparerUtility.ObjectsAreEqual(leftToken.Value, rightToken.Value), TokenType.Boolean);
                    //else if (op.Equals("!="))
                    //    return Token.ManualCreate(!ObjectComparerUtility.ObjectsAreEqual(leftToken.Value, rightToken.Value), TokenType.Boolean);

                    else if (op.Equals("&"))
                    {
                        if (leftToken.Type == rightToken.Type && leftToken.Type == TokenType.Boolean)
                            return Token.ManualCreate((bool)leftToken.Value && (bool)rightToken.Value, TokenType.Boolean);
                        else
                            Error("InvalidOperator", $"Specified operator cannot be applied to type {leftToken.Type.ToString().ToLower()} and {rightToken.Type.ToString().ToLower()}");
                    }
                    else if (op.Equals("|"))
                    {
                        if (leftToken.Type == rightToken.Type && leftToken.Type == TokenType.Boolean)
                            return Token.ManualCreate((bool)leftToken.Value || (bool)rightToken.Value, TokenType.Boolean);
                        else
                            Error("InvalidOperator", $"Specified operator cannot be applied to type {leftToken.Type.ToString().ToLower()} and {rightToken.Type.ToString().ToLower()}");
                    }

                    else if (op.Equals("+"))
                    {
                        if (leftToken.Type == rightToken.Type && leftToken.Type == TokenType.String)
                            return Token.ManualCreate(leftToken.Value.ToString() + rightToken.Value.ToString(), TokenType.String);
                        else if (leftToken.Type == rightToken.Type && leftToken.Type == TokenType.Interger)
                            return Token.ManualCreate((long)leftToken.Value + (long)rightToken.Value, TokenType.Interger);
                        else
                            Error("InvalidOperator", $"Specified operator cannot be applied to type {leftToken.Type.ToString().ToLower()} and {leftToken.Type.ToString().ToLower()}");
                    }
                    else if (op.Equals("-"))
                    {
                        if (leftToken.Type == rightToken.Type && leftToken.Type == TokenType.Interger)
                            return Token.ManualCreate((long)leftToken.Value - (long)rightToken.Value, TokenType.Interger);
                        else
                            Error("InvalidOperator", $"Specified operator cannot be applied to type {leftToken.Type.ToString().ToLower()} and {leftToken.Type.ToString().ToLower()}");
                    }
                    else if (op.Equals("*"))
                    {
                        if (leftToken.Type == TokenType.String && rightToken.Type == TokenType.Interger)
                            return Token.ManualCreate(leftToken.Value.ToString().Multiply(((long)rightToken.Value).ToInt()), TokenType.String);
                        else if (leftToken.Type == rightToken.Type && leftToken.Type == TokenType.Interger)
                            return Token.ManualCreate((long)leftToken.Value * (long)rightToken.Value, TokenType.Interger);
                        else
                            Error("InvalidOperator", $"Specified operator cannot be applied to type {leftToken.Type.ToString().ToLower()} and {leftToken.Type.ToString().ToLower()}");
                    }
                    else if (op.Equals("/"))
                    {
                        if (leftToken.Type == rightToken.Type && leftToken.Type == TokenType.Interger)
                            return Token.ManualCreate((long)leftToken.Value / (long)rightToken.Value, TokenType.Interger);
                        else
                            Error("InvalidOperator", $"Specified operator cannot be applied to type {leftToken.Type.ToString().ToLower()} and {leftToken.Type.ToString().ToLower()}");
                    }

                    else if (op.Equals("<"))
                    {
                        if (leftToken.Type == rightToken.Type && leftToken.Type == TokenType.Interger)
                            return Token.ManualCreate((long)leftToken.Value < (long)rightToken.Value, TokenType.Boolean);
                        else
                            Error("InvalidOperator", $"Specified operator cannot be applied to type {leftToken.Type.ToString().ToLower()} and {rightToken.Type.ToString().ToLower()}");
                    }
                    else if (op.Equals(">"))
                    {
                        if (leftToken.Type == rightToken.Type && leftToken.Type == TokenType.Interger)
                            return Token.ManualCreate((long)leftToken.Value > (long)rightToken.Value, TokenType.Boolean);
                        else
                            Error("InvalidOperator", $"Specified operator cannot be applied to type {leftToken.Type.ToString().ToLower()} and {rightToken.Type.ToString().ToLower()}");
                    }
                }
            }

            return Token.Null;
        }

        public void Error(string type, string msg)
        {
            throw new Exception($"Interpreter {type} error, {msg}");
        }
    }
    public class Scope
    {
        public Scope ParentScope;
        public bool HasParent => ParentScope != null;
        private Dictionary<string, Token> _content;
        public Scope()
        {
            ParentScope = null;
            _content = new();
        }
        public Scope(Scope parent)
        {
            ParentScope = parent;
            _content = new();
        }
        public Scope CreateChild()
        {
            Scope child = new(this);
            return child;
        }
        public Token this[string key]
        {
            get { return Get(key); }
            set { Set(key, value); }
        }
        public bool Has(string key, bool canCheckParent = true)
        {
            bool has = _content.ContainsKey(key);
            if (HasParent && !has && canCheckParent)
            {
                has = ParentScope.Has(key);
            }
            return has;
        }
        public Token Get(string key)
        {
            if (Has(key, false))
                return _content[key];
            else
            {
                if (HasParent)
                    return ParentScope.Get(key);
                else
                    return Token.Null;
            }
        }
        public void Set(string key, Token value, bool globalCreate = false)
        {
            //if (Has(key, false))
            //{
            //    _content[key] = value;
            //}
            //else
            //{
            //    if (HasParent)
            //        if (globalCreate)
            //            ParentScope.Set(key, value, globalCreate);
            //        else
            //            _content[key] = value;
            //    else
            //        if (globalCreate) _content[key] = value;
            //}
            if (Has(key, false))
            {
                _content[key] = value;
            }
            else
            {
                if (HasParent && Has(key, true))
                    ParentScope.Set(key, value, globalCreate);
                else
                    if (globalCreate && HasParent)
                    ParentScope.Set(key, value, globalCreate);
                else
                    _content[key] = value;
            }
        }
    }
    public class InterpretFormat
    {
        private readonly static Dictionary<TokenType, char> dict = new()
        {
            { TokenType.String, 's' },
            { TokenType.Interger, '1' },
            { TokenType.Boolean, '?' },
            { TokenType.Operator, '+' },
            { TokenType.Value, 'V' },
            { TokenType.Bracket, '(' }
        };
        private readonly TokenType[] _types;
        public InterpretFormat(Token[] tokens)
        {
            _types = tokens.Select(t => t.Type).ToArray();
        }

        /// <summary>
        /// Checks if format matches <paramref name="format"/>
        /// </summary>
        /// <param name="format">The format to used to scan</param>
        /// <returns>Format is matching</returns>
        public bool IsMatching(string format)
        {
            if (_types.Length != format.Length)
                return false;
            for (int i = 0; i < format.Length; i++)
            {
                char ch = format[i];
                TokenType type = _types[i];

                if (dict[type] == ch || ch == ' ') continue;
                else return false;
            }
            return true;
        }

        /// <summary>
        /// Checks if format starts with the <paramref name="format"/>
        /// </summary>
        /// <param name="format">The format to used to scan</param>
        /// <returns>Format is similar</returns>
        public bool IsSimilar(string format)
        {
            if (format.Length > _types.Length)
                return false;
            for (int i = 0; i < Math.Min(format.Length, _types.Length); i++)
            {
                char ch = format[i];
                TokenType type = _types[i];

                if (dict[type] == ch || ch == ' ') continue;
                else return false;
            }
            return true;
        }
    }

    public interface IFunction
    {
        public void Invoke(Interpreter interpreter, Scope scope, Token[] parameters);
    }
    public class Function : IFunction
    {
        public string Name => _name;
        private string _name;
        public string[] ParameterNames => _parameterNames;
        private string[] _parameterNames;
        public Node Node => _node;
        private Node _node;

        public Function(string name, string[] parameterNames, Node node)
        {
            _name = name;
            _parameterNames = parameterNames;
            _node = node;
        }

        public void Invoke(Interpreter interpreter, Scope scope, Token[] parameters)
        {
            Scope localScope = scope.CreateChild();
            for (int i = 0; i < Math.Min(parameters.Length, _parameterNames.Length); i++)
            {
                localScope.Set(_parameterNames[i], parameters[i]);
            }
            interpreter.Dive(_node, localScope);
        }
    }
    public class ExternalFunction : IFunction
    {
        public string Name => _name;
        private string _name;
        public Func<Token[], Token> Action => _action;
        private Func<Token[], Token> _action;

        public ExternalFunction(string name, Func<Token[], Token> action)
        {
            _name = name;
            _action = action;
        }

        public void Invoke(Interpreter interpreter, Scope scope, Token[] parameters)
        {
            Token res = _action.Invoke(parameters);
        }
    }
}
