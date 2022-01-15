using System;
using System.IO;
using System.Linq;

namespace ream
{
    public class Program
    {
        static void Main(string[] args)
        {
            TestInterpret();
        }
        static void TestLex()
        {
            Lexer l = new(Console.ReadLine());
            Token[] tokens = l.Lex();
            Console.WriteLine(string.Join("\n", tokens.Select(x => ">>> " + x.ToString())));
        }

        static void TestParse()
        {
            Lexer l = new(Console.ReadLine());
            Token[] tokens = l.Lex();

            Parser p = new(tokens);
            Node node = p.Parse();
            var dive = node.Dive();
            Console.WriteLine(node.ToString());
        }

        static void TestInterpret()
        {
            Lexer l = new(File.ReadAllText("script.r"));
            Token[] tokens = l.Lex();

            Parser p = new(tokens);
            Node node = p.Parse();

            Interpreter i = new(node);
            i.Interpret();
        }
    }
}
