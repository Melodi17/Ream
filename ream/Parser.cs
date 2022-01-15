using System.Collections.Generic;
using System.Linq;

namespace ream
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
