using System;
using System.Reflection;

namespace Ream.SDK
{
    public class ExternalVariableAttribute : Attribute
    {
        public string Name;
        public VariableType Type;
        public ExternalVariableAttribute(string name = "", VariableType type = VariableType.Normal)
        {
            Name = name;
            Type = type;
        }
    }

    public class ExternalFunctionAttribute : Attribute
    {
        public int ArgumentCount;
        public string Name;
        public VariableType Type;
        public ExternalFunctionAttribute(string name = "", int argumentCount = -1, VariableType type = VariableType.Normal)
        {
            ArgumentCount = argumentCount;
            Type = type;
            Name = name;
        }
    }

    public class ExternalClassAttribute : Attribute
    {

    }

    [Flags]
    public enum VariableType
    {
        Normal = 1,
        Local = 2,
        Global = 4,
        Dynamic = 8,
        Final = 16,
        Initializer = 32,
        Static = 64,
    }
}
