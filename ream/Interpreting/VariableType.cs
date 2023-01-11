using Ream.Lexing;

namespace Ream.Interpreting
{
    [Flags]
    public enum VariableType
    {
        Normal = 0x1,
        Local = 0x2,
        Global = 0x4,
        Final = 0x8,
        Static = 0x16
    }
    public static class VariableTypeExtensions
    {
        public static VariableType ToVariableType(this TokenType type)
            => type switch
            {
                TokenType.Local => VariableType.Local,
                TokenType.Global => VariableType.Global,
                TokenType.Final => VariableType.Final,
                TokenType.Static => VariableType.Static,
                _ => VariableType.Normal
            };

        public static bool IsVariableType(this TokenType type)
            => type switch
            {
                TokenType.Local => true,
                TokenType.Global => true,
                TokenType.Final => true,
                TokenType.Static => true,
                _ => false,
            };
    }
}