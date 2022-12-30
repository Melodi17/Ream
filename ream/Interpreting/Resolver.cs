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
        public const string OPERATOR_MODULUS = "~modulus";
        public const string OPERATOR_GREATER = "~greater";
        public const string OPERATOR_LESS = "~less";

        public object Compare(object left, object right, TokenType type)
        {
            switch (type)
            {
                case TokenType.Equal_Equal:
                    return this.Equal(left, right);
                case TokenType.Not_Equal:
                    return !this.Equal(left, right);

                case TokenType.Plus:
                    {
                        IPropable prop = this.GetPropable(left);
                        if (prop != null)
                            if (prop.Get(OPERATOR_ADD) is ICallable func)
                                return func.Call(this.interpreter, new() { right });

                        return null;
                    }

                case TokenType.Minus:
                    {
                        IPropable prop = this.GetPropable(left);
                        if (prop != null)
                            if (prop.Get(OPERATOR_SUBTRACT) is ICallable func)
                                return func.Call(this.interpreter, new() { right });

                        return null;
                    }

                case TokenType.Star:
                    {
                        IPropable prop = this.GetPropable(left);
                        if (prop != null)
                            if (prop.Get(OPERATOR_MULTIPLY) is ICallable func)
                                return func.Call(this.interpreter, new() { right });

                        return null;
                    }

                case TokenType.Slash:
                    {
                        IPropable prop = this.GetPropable(left);
                        if (prop != null)
                            if (prop.Get(OPERATOR_DIVIDE) is ICallable func)
                                return func.Call(this.interpreter, new() { right });

                        return null;
                    }

                case TokenType.Percent:
                    {
                        IPropable prop = this.GetPropable(left);
                        if (prop != null)
                            if (prop.Get(OPERATOR_MODULUS) is ICallable func)
                                return func.Call(this.interpreter, new() { right });

                        return null;
                    }

                case TokenType.Greater:
                    {
                        IPropable prop = this.GetPropable(left);
                        if (prop != null)
                            if (prop.Get(OPERATOR_GREATER) is ICallable func)
                                return this.Truthy(func.Call(this.interpreter, new() { right }));

                        return null;
                    }

                case TokenType.Greater_Equal:
                    {
                        IPropable prop = this.GetPropable(left);
                        if (prop != null)
                            if (prop.Get(OPERATOR_GREATER) is ICallable func)
                                return this.Truthy(func.Call(this.interpreter, new() { right })) || this.Equal(left, right);

                        return null;
                    }

                case TokenType.Less:
                    {
                        IPropable prop = this.GetPropable(left);
                        if (prop != null)
                            if (prop.Get(OPERATOR_LESS) is ICallable func)
                                return this.Truthy(func.Call(this.interpreter, new() { right }));

                        return null;
                    }

                case TokenType.Less_Equal:
                    {
                        IPropable prop = this.GetPropable(left);
                        if (prop != null)
                            if (prop.Get(OPERATOR_LESS) is ICallable func)
                                return this.Truthy(func.Call(this.interpreter, new() { right })) || this.Equal(left, right);

                        return null;
                    }
            }

            return null;
        }
        public bool Equal(object left, object right)
        {
            if (left == null && right == null) return true;
            if (left == null) return false;

            IPropable prop = this.GetPropable(left);
            if (prop != null)
            {
                if (prop.Get(OVERRIDE_EQUAL) is ICallable func)
                    return this.Truthy(func.Call(this.interpreter, new() { right }));
            }

            return left.Equals(right);
        }
        public object LogicalCompare(object left, object right)
        {
            return this.Truthy(left) ? left : right;
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
            if (obj is List<object> l) return l.Any();
            if (obj is Dictionary<object, object> dic) return dic.Any();
            if (obj is string s) return s.Length > 0;
            if (obj is Prototype pt) return pt.Truthy();
            return true;
        }

        public string GetType(object obj)
        {
            return obj switch
            {
                null => "null",
                bool _ => "Boolean",
                double _ => "Number",
                string _ => "String",
                List<object> _ => "Sequence",
                Dictionary<object, object> _ => "Dictionary",
                Prototype _ => "Prototype",
                Pointer _ => "Pointer",
                Class c => c.Name,
                ClassInstance c => c.Class.Name,
                ExternalClass c => c.Type.Name,
                ExternalClassInstance c => c.Class.Type.Name,
                ICallable _ => "Callable",
                _ => "Object",
            };
        }

        public string Stringify(object obj)
        {
            IPropable prop = this.GetPropable(obj);
            if (prop != null)
            {
                if (prop.Get(OVERRIDE_STRINGIFY) is ICallable func)
                    return func.Call(this.interpreter, new(0)).ToString();
            }

            return obj?.ToString();
        }

        public object ToNative(Type t, object obj)
        {
            if (obj == null) return null;
            if (t == typeof(bool) && obj is not bool) return this.Truthy(obj);
            if (t == typeof(string) && obj is not string) return this.Stringify(obj);
            if (t == typeof(byte)
                && t == typeof(short)
                && t == typeof(int)
                && t == typeof(long)
                && obj is double d) return this.GetInt(d);
            return obj;
        }

        public List<object> GetIterator(object obj)
        {
            IPropable prop = this.GetPropable(obj);
            if (prop != null)
            {
                if (prop.Get(OVERRIDE_ITERATOR) is ICallable func)
                    return (List<object>)func.Call(this.interpreter, new(0));
            }

            return null;
        }

        public object GetIndex(object obj, object ind)
        {
            IPropable prop = this.GetPropable(obj);
            if (prop != null)
            {
                if (prop.Get(OVERRIDE_INDEX) is ICallable func)
                    return func.Call(this.interpreter, new() { ind });

                if (ind is double d)
                    return this.GetIterator(obj)[this.GetInt(d)];
            }

            return null;
        }

        public object GetMix(object obj, object ind, object value)
        {
            IPropable prop = this.GetPropable(obj);
            if (prop != null)
            {
                if (prop.Get(OVERRIDE_MIX) is ICallable func)
                    return func.Call(this.interpreter, new() { ind, value });

                if (ind is double d)
                    return this.GetIterator(obj)[this.GetInt(d)];
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
