using Ream.Lexing;

namespace Ream.Interpreting
{
    public class Resolver
    {
        private Interpreter interpreter;
        public Resolver(Interpreter interpreter)
        {
            this.interpreter = interpreter;
        }

        public const string OVERRIDE_STRINGIFY = "~string";
        public const string OVERRIDE_ITERATOR = "~iterator";
        public const string OVERRIDE_INDEX = "~index";
        public const string OVERRIDE_MIX = "~mix";
        public const string OVERRIDE_INSTANCE = "~instance";
        public const string OVERRIDE_EQUAL = "~equal";

        public const string OPERATOR_ADD = "~add";
        public const string OPERATOR_SUBTRACT = "~subtract";
        public const string OPERATOR_MULTIPLY = "~multiply";
        public const string OPERATOR_DIVIDE = "~divide";
        public const string OPERATOR_GREATER = "~greater";
        public const string OPERATOR_LESS = "~less";

        public object Compare(object left, object right, TokenType type)
        {
            switch (type)
            {
                case TokenType.Equal_Equal:
                    return Equal(left, right);
                case TokenType.Not_Equal:
                    return !Equal(left, right);

                case TokenType.Plus:
                    {
                        IPropable prop = GetPropable(left);
                        if (prop != null)
                            if (prop.Get(OPERATOR_ADD) is ICallable func)
                                return func.Call(interpreter, new() { right });

                        return null;
                    }

                case TokenType.Minus:
                    {
                        IPropable prop = GetPropable(left);
                        if (prop != null)
                            if (prop.Get(OPERATOR_SUBTRACT) is ICallable func)
                                return func.Call(interpreter, new() { right });

                        return null;
                    }

                case TokenType.Star:
                    {
                        IPropable prop = GetPropable(left);
                        if (prop != null)
                            if (prop.Get(OPERATOR_MULTIPLY) is ICallable func)
                                return func.Call(interpreter, new() { right });

                        return null;
                    }

                case TokenType.Slash:
                    {
                        IPropable prop = GetPropable(left);
                        if (prop != null)
                            if (prop.Get(OPERATOR_DIVIDE) is ICallable func)
                                return func.Call(interpreter, new() { right });

                        return null;
                    }

                case TokenType.Greater:
                    {
                        IPropable prop = GetPropable(left);
                        if (prop != null)
                            if (prop.Get(OPERATOR_GREATER) is ICallable func)
                                return Truthy(func.Call(interpreter, new() { right }));

                        return null;
                    }

                case TokenType.Greater_Equal:
                    {
                        IPropable prop = GetPropable(left);
                        if (prop != null)
                            if (prop.Get(OPERATOR_GREATER) is ICallable func)
                                return Truthy(func.Call(interpreter, new() { right })) || Equal(left, right);

                        return null;
                    }

                case TokenType.Less:
                    {
                        IPropable prop = GetPropable(left);
                        if (prop != null)
                            if (prop.Get(OPERATOR_LESS) is ICallable func)
                                return Truthy(func.Call(interpreter, new() { right }));

                        return null;
                    }

                case TokenType.Less_Equal:
                    {
                        IPropable prop = GetPropable(left);
                        if (prop != null)
                            if (prop.Get(OPERATOR_LESS) is ICallable func)
                                return Truthy(func.Call(interpreter, new() { right })) || Equal(left, right);

                        return null;
                    }
            }

            return null;
        }
        public bool Equal(object left, object right)
        {
            if (left == null && right == null) return true;
            if (left == null) return false;

            IPropable prop = GetPropable(left);
            if (prop != null)
            {
                if (prop.Get(OVERRIDE_EQUAL) is ICallable func)
                    return Truthy(func.Call(interpreter, new() { right }));
            }

            return left.Equals(right);
        }
        public object LogicalCompare(object left, object right)
        {
            return Truthy(left) ? left : right;
        }
        public IPropable GetPropable(object obj)
        {
            if (obj == null) return null;
            if (obj is IPropable p) return p;

            if (obj is string s) return new StringPropMap(s);
            if (obj is double d) return new DoublePropMap(d);
            if (obj is bool b) return new BoolPropMap(b);
            if (obj is List<object> l) return new ListPropMap(l);
            if (obj is Dictionary<object, object> dict) return new DictPropMap(dict);
            
            return new AutoPropMap(obj);
        }
        public bool Truthy(object obj)
        {
            if (obj == null) return false;
            if (obj is bool b) return b;
            if (obj is double d) return d > 0;
            if (obj is IEnumerable<object> l) return l.Any();
            if (obj is string s) return s.Length > 0;
            return true;
        }

        public string Stringify(object obj)
        {
            IPropable prop = GetPropable(obj);
            if (prop != null)
            {
                if (prop.Get(OVERRIDE_STRINGIFY) is ICallable func)
                    return func.Call(interpreter, new(0)).ToString();
            }

            return obj?.ToString();
        }

        public List<object> GetIterator(object obj)
        {
            IPropable prop = GetPropable(obj);
            if (prop != null)
            {
                if (prop.Get(OVERRIDE_ITERATOR) is ICallable func)
                    return (List<object>)func.Call(interpreter, new(0));
            }

            return null;
        }

        public object GetIndex(object obj, object ind)
        {
            IPropable prop = GetPropable(obj);
            if (prop != null)
            {
                if (prop.Get(OVERRIDE_INDEX) is ICallable func)
                    return func.Call(interpreter, new() { ind });

                if (ind is double d)
                    return GetIterator(obj)[GetInt(d)];
            }

            return null;
        }

        public object GetMix(object obj, object ind, object value)
        {
            IPropable prop = GetPropable(obj);
            if (prop != null)
            {
                if (prop.Get(OVERRIDE_MIX) is ICallable func)
                    return func.Call(interpreter, new() { ind, value });

                if (ind is double d)
                    return GetIterator(obj)[GetInt(d)];
            }

            return null;
        }

        public int GetInt(double d)
        {
            try
            {
                return Convert.ToInt32(d);
            }
            catch (OverflowException)
            {
                return 0;
            }
        }
    }
}
