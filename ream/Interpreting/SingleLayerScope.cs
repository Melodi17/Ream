using Ream.Lexing;

namespace Ream.Interpreting
{
    public class SingleLayerScope
    {
        private readonly Dictionary<string, object> Values = new();
        private readonly Dictionary<string, SingleLayerVariableData> VariableData = new();

        public SingleLayerScope()
        {

        }

        public void Define(string key, object value)
        {
            Values.Add(key, value);
        }
        public VariableType AutoDetectType(Token key, VariableType manualType)
        {
            VariableType type = manualType.HasFlag(VariableType.Normal)
                ? GetData(key)?.Type ?? manualType : manualType;

            return type;
        }
        public void Set(Token key, object value, VariableType manualType = VariableType.Normal)
        {
            string keyName = key.Raw;

            VariableType type = AutoDetectType(key, manualType);

            // Variable is not null and is readonly, so it cannot be changed
            if (Get(key) != null && GetData(key).Type.HasFlag(VariableType.Final))
                return;

            Values[keyName] = value;
            VariableData[key.Raw] = new(type, this);
        }

        public bool Has(string key)
        {
            bool has = Values.ContainsKey(key);

            return has;
        }

        public object Get(Token key)
        {
            string keyName = key.Raw;

            // If can be found locally
            if (Values.ContainsKey(keyName))
                return Values[keyName];

            return null;
        }

        public SingleLayerVariableData GetData(Token key)
        {
            string keyName = key.Raw;

            // If can be found locally
            if (VariableData.ContainsKey(keyName))
                return VariableData[keyName];

            return null;
        }
    }

    public class SingleLayerVariableData
    {
        public VariableType Type;
        public SingleLayerScope Scope;

        public SingleLayerVariableData(VariableType type, SingleLayerScope scope)
        {
            Type = type;
            Scope = scope;
        }
    }
}
