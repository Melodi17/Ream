using System.Collections.Generic;
using System.Linq;
using Ream.Lexer;

namespace Ream.Parser
{
    public class Node
    {
        public List<Node> ChildNodes;
        public Node Parent;
        public Token Token;
        public bool HasToken => Token != null;

        public Node(Token token = null, Node parent = null)
        {
            ChildNodes = new();
            Token = token;
            Parent = parent;
        }
        public Node CreateChild(Token token = null)
        {
            Node child = new(token, this);
            ChildNodes.Add(child);
            return child;
        }
        public override string ToString()
        {
            return string.Join('\n', Dive());
        }
        public string[] Dive(int indent = 0)
        {
            List<string> lines = new();
            foreach (Node item in ChildNodes)
            {
                if (item.ChildNodes.Any())
                    lines.AddRange(item.Dive(indent + 1));

                if (item.HasToken)
                    lines.Add(new string(' ', indent) + item.Token.ToString());
            }
            return lines.ToArray();
        }
    }
}
