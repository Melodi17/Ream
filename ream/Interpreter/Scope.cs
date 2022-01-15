using System.Collections.Generic;
using Ream.Lexer;

namespace Ream.Interpreter
{
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
}
