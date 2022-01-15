using System;
using System.IO;
using System.Linq;

using Ream.Lexer;
using Ream.Parser;
using Ream.Interpreter;

namespace Ream
{
    public class Program
    {
        static void Main(string[] args)
        {
            TestInterpret();
        }
        static void TestLex()
        {
            Lexer.Lexer l = new(Console.ReadLine());
            Token[] tokens = l.Lex();
            Console.WriteLine(string.Join("\n", tokens.Select(x => ">>> " + x.ToString())));
        }

        static void TestParse()
        {
            Lexer.Lexer l = new(Console.ReadLine());
            Token[] tokens = l.Lex();

            Parser.Parser p = new(tokens);
            Node node = p.Parse();
            var dive = node.Dive();
            Console.WriteLine(node.ToString());
        }

        static void TestInterpret()
        {
            Lexer.Lexer l = new(File.ReadAllText("script.r"));
            Token[] tokens = l.Lex();

            Parser.Parser p = new(tokens);
            Node node = p.Parse();

            Interpreter.Interpreter i = new(node);
            i.Interpret();
        }
    }
}
