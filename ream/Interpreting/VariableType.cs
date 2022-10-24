using Ream.Lexing;

namespace Ream.Interpreting
{
    [Flags]
    public enum VariableType
    {
        Normal = 0x1,
        Local = 0x2,
        Global = 0x4,
        Dynamic = 0x8,
        Final = 0x16,
        Static = 0x32
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
                TokenType.Static => true,
                _ => false
            };
    }
}