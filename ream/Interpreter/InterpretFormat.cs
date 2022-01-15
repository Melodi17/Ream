using System;
using System.Collections.Generic;
using System.Linq;
using Ream.Lexer;

namespace Ream.Interpreter
{
    public class InterpretFormat
    {
        private readonly static Dictionary<TokenType, char> dict = new()
        {
            { TokenType.String, 's' },
            { TokenType.Interger, '1' },
            { TokenType.Boolean, '?' },
            { TokenType.Operator, '+' },
            { TokenType.Value, 'V' },
            { TokenType.Bracket, '(' },
            { TokenType.Sequence, '[' },
        };
        private readonly TokenType[] _types;
        public InterpretFormat(Token[] tokens)
        {
            _types = tokens.Select(t => t.Type).ToArray();
        }

        /// <summary>
        /// Checks if format matches <paramref name="format"/>
        /// </summary>
        /// <param name="format">The format to used to scan</param>
        /// <returns>Format is matching</returns>
        public bool IsMatching(string format)
        {
            if (_types.Length != format.Length)
                return false;
            for (int i = 0; i < format.Length; i++)
            {
                char ch = format[i];
                TokenType type = _types[i];

                if (dict[type] == ch || ch == ' ') continue;
                else return false;
            }
            return true;
        }

        /// <summary>
        /// Checks if format starts with the <paramref name="format"/>
        /// </summary>
        /// <param name="format">The format to used to scan</param>
        /// <returns>Format is similar</returns>
        public bool IsSimilar(string format)
        {
            if (format.Length > _types.Length)
                return false;
            for (int i = 0; i < Math.Min(format.Length, _types.Length); i++)
            {
                char ch = format[i];
                TokenType type = _types[i];

                if (dict[type] == ch || ch == ' ') continue;
                else return false;
            }
            return true;
        }
    }
}
