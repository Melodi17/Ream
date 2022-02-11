using Ream.Lexing;

namespace Ream.Interpreting
{
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

    public static class VariableTypeExtensions 
    {
        public static VariableType ToVariableType(this TokenType type)
            => type switch
            {
                TokenType.Local => VariableType.Local,
                TokenType.Global => VariableType.Global,
                TokenType.Dynamic => VariableType.Dynamic,
                TokenType.Final => VariableType.Final,
                TokenType.Initializer => VariableType.Initializer,
                TokenType.Static => VariableType.Static,
                _ => VariableType.Normal
            };

        public static bool IsVariableType(this TokenType type)
            => type switch
            {
                TokenType.Local => true,
                TokenType.Global => true,
                TokenType.Dynamic => true,
                TokenType.Final => true,
                TokenType.Initializer => true,
                TokenType.Static => true,
                _ => false
            };
    }
}
