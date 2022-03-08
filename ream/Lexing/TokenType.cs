namespace Ream.Lexing
{
    public enum TokenType
    {
        Identifier, Interger, String, Boolean,

        Left_Parenthesis, Right_Parenthesis,
        Left_Brace, Right_Brace, Left_Square,
        Right_Square,

        Comma, Period, Plus, Minus, Star, Slash,
        Colon, Ampersand, Pipe, Equal, Not, Greater,
        Less,

        Not_Equal, Equal_Equal, Greater_Equal, 
        Less_Equal, Plus_Equal, Minus_Equal, Star_Equal,
        Slash_Equal, Colon_Colon, Ampersand_Ampersand,
        Pipe_Pipe,

        If, Else, Elif, For, While, Function, Global,
        Return, Null, Class, This, Print, True, False,
        Local, Lambda, Dynamic, Final, Initializer, Static,
        Import,

        Newline, End
    }
}