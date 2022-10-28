using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;

namespace Libraries
{
    public class IO
    {
        public static void Println(string obj)
        {
            Console.WriteLine(obj);
        }

        public static void Print(string obj)
        {
            Console.Write(obj);
        }

        public static void Printc(string obj)
        {
            for (int i = 0; i < obj.Length; i++)
            {
                char c = obj[i];
                if (c == '%')
                {
                    //string color = obj[(i + 1)..obj.IndexOf('%', i + 1)].ToLower();
                    string color = obj.Substring(i + 1, obj.IndexOf('%', i + 1) - i - 1).ToLower();
                    bool isBackground = color.StartsWith("#");
                    //if (isBackground) color = color[1..];
                    if (isBackground) color = color.Substring(1);


                    if (color.Length == 0)
                        Console.Write('%');
                    else if (color == "reset")
                        Console.ResetColor();
                    else if (Enum.TryParse(color, true, out ConsoleColor cc))
                    {
                        if (isBackground) Console.BackgroundColor = cc;
                        else Console.ForegroundColor = cc;
                    }
                    else
                        throw new FormatException("Invalid color '" + color + "'");

                    i += color.Length + 1;
                    if (isBackground) i++;
                }
                else
                    Console.Write(c);
            }

            Console.ResetColor();
        }

        public static string Readln(string prompt)
        {
            Console.Write(prompt);
            return Console.ReadLine();
        }

        public static string Read(bool show)
        {
            return Console.ReadKey(!show).KeyChar.ToString();
        }

        public static void Clear()
        {
            Console.Clear();
        }

        public static File Open(string path)
        {
            return new File(path);
        }

        public static Directory Enumerate(string path)
        {
            return new Directory(path);
        }
    }

    public class File
    {
        private string path;
        public File(string path)
        {
            this.path = path;
        }

        public string Read()
        {
            return System.IO.File.ReadAllText(path);
        }

        public List<object> ReadLines()
        {
            return System.IO.File.ReadAllLines(path).ToList<object>();
        }

        public void Write(string content)
        {
            System.IO.File.WriteAllText(path, content);
        }

        public void WriteLines(List<object> lines)
        {
            System.IO.File.WriteAllLines(path, lines.Select(x => x.ToString()).ToArray());
        }

        public void Append(string content)
        {
            System.IO.File.AppendAllText(path, content);
        }

        public void Delete()
        {
            System.IO.File.Delete(path);
        }

        public bool Exists()
        {
            return System.IO.File.Exists(path);
        }

        public string GetPath()
        {
            return path;
        }
    }
    public class Directory
    {
        private string path;
        public Directory(string path)
        {
            this.path = path;
        }

        public void Create()
        {
            System.IO.Directory.CreateDirectory(path);
        }

        public void Delete()
        {
            System.IO.Directory.Delete(path);
        }

        public bool Exists()
        {
            return System.IO.Directory.Exists(path);
        }

        public string GetPath()
        {
            return path;
        }
    }
}
