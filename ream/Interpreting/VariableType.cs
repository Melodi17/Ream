using Ream.Lexing;
using Ream.SDK;

namespace Ream.Interpreting
{
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
