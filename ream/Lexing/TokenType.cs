namespace Ream.Lexing
{
    public enum TokenType
    {
        Identifier, Integer, String,

        LeftParenthesis, RightParenthesis,
        LeftBrace, RightBrace, LeftSquare,
        RightSquare,

        Comma, Period, Plus, Minus, Star, Slash,
        Colon, Ampersand, Pipe, Equal, Not, Greater,
        Less, Question, Chain,

        NotEqual, EqualEqual, GreaterEqual, 
        LessEqual, PlusEqual, MinusEqual, StarEqual,
        SlashEqual, ColonColon, AmpersandAmpersand,
        PipePipe, PlusPlus, MinusMinus, Percent,
        PercentPercent,

        If, Else, Elif, For, While, Function, Global,
        Return, Null, Class, This, True, False, Local,
        Lambda, Dynamic, Final, Static ,Import, Continue,
        Break, Dispose,

        Newline, End,
    }
}