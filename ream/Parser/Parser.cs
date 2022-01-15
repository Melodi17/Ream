using Ream.Lexer;

namespace Ream.Parser
{
    public class Parser
    {
        public Token[] Tokens;
        public Parser(Token[] tokens)
        {
            Tokens = tokens;
        }
        public Node Parse()
        {
            int depth = 0;
            Node mainNode = new();
            Node currentNode = mainNode;
            foreach (var item in Tokens)
            {
                if (item.Type == TokenType.Bracket 
                    && (item.Value.ToString() == "{" || item.Value.ToString() == "}"))
                {
                    if (item.Value.ToString() == "{") // Token.Brackets.Any(x => x.Item1 == item.Value.ToString())
                    {
                        depth++;
                        currentNode = currentNode.CreateChild();
                    }
                    else if (item.Value.ToString() == "}") // Token.Brackets.Any(x => x.Item2 == item.Value.ToString())
                    {
                        depth--;
                        currentNode = currentNode.Parent;
                    }
                }
                else
                {
                    currentNode.CreateChild(item);
                }
            }
            return mainNode;
        }
    }
}
