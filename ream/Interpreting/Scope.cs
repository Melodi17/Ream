using ream.Interpreting.Objects;
using Ream.Lexing;
using Ream.Utils;

namespace Ream.Interpreting
{
    public class ReamReference : IDisposable
    {
        public string Name { get; set; }
        public ReamObject Value
        {
            get => this._value;
            set
            {
                if (this.Type != VariableType.Final || this._value != ReamNull.Instance)
                {
                    this._value = value;
                }
            }
        }
        public VariableType Type { get; set; }
        public Scope Scope { get; set; }
        private bool _disposed;
        private ReamObject _value;

        public ReamReference(string name, ReamObject value, VariableType type, Scope scope)
        {
            this.Name = name;
            this.Value = value;
            this.Type = type;
            this.Scope = scope;
        }
        
        ~ReamReference() { this.Dispose(false); }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                }
            
                // Dispose unmanaged resources
                this.Name = null;
                this.Value = null;
                this.Scope = null;
            
                this._disposed = true;
            }
        }
    }
    public class Scope : IDisposable
    {
        public bool HasParent => this.Parent != null;
        public Scope Global => this.HasParent ? this.Parent.Global : this;
        public readonly Scope Parent;
        private readonly LinqDictionary<string, ReamReference> _variables = new(x => x.Name);
        private bool _disposed;

        public Scope()
        {
            this.Parent = null;
        }

        public Scope(Scope parent)
        {
            this.Parent = parent;
        }
        
        ~Scope() { this.Dispose(false); }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    foreach (ReamReference variable in this._variables)
                        variable.Dispose();
                }
            
                // Dispose unmanaged resources
                this._variables.Clear();
            
                this._disposed = true;
            }
        }

        public void Set(string key, ReamObject value, VariableType type = VariableType.Normal)
        {
            // Variable is not null and is readonly, so it cannot be changed
            if (this.Get(key)?.Type == VariableType.Final)
                return;

            if (type.HasFlag(VariableType.Global))
            {
                if (this.HasParent)
                {
                    this.Global.Set(key, value, type);
                }
                else
                {
                    // If variable exists, set it's .Value, otherwise instantiate a new variable
                    if (this._variables.ContainsKey(key))
                        this._variables[key].Value = value;
                    else
                        this._variables[key] = new(key, value, type, this);
                }
            }
            else if (type.HasFlag(VariableType.Local))
            {
                if (this._variables.ContainsKey(key))
                    this._variables[key].Value = value;
                else
                    this._variables[key] = new(key, value, type, this);
            }
            else
            {
                // If it exists locally
                if (this.Has(key, false))
                {
                    this._variables[key].Value = value;
                }
                else
                {
                    // If it exists anywhere
                    if (this.Has(key, true))
                    {
                        // Recall
                        this.Parent.Set(key, value, type);
                    }
                    else
                    {
                        // We need to create it
                        this._variables[key] = new(key, value, type, this);
                    }
                }
            }
        }

        public bool Has(string key, bool canCheckParent = true)
        {
            bool has = this._variables.ContainsKey(key);

            // Can't be found locally, has a parent to check and allowed to check parent
            if (!has && this.HasParent && canCheckParent)
                has = this.Parent.Has(key);
            return has;
        }

        public ReamReference Get(string key)
        {
            // If can be found locally
            ReamReference reference = this._variables[key];
            if (reference != null)
                return reference;

            // If can't be found locally and has a parent to check
            return this.HasParent ? this.Parent.Get(key) : null;
        }

        public void Remove(string key)
        {
            if (this._variables.ContainsKey(key))
                this._variables.Remove(key);
        }
        
        public LinqDictionary<string, ReamReference> GetMembers()
        {
            return this._variables;
        }
    }
}
