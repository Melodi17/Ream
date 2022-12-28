using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libraries
{
    public class Handle
    {
        public static string Username => Environment.UserName;
        public static string HostName => Environment.MachineName;
        public static string OS => Environment.OSVersion.ToString();
        public static string OSVersion => Environment.OSVersion.VersionString;
        public static string OSArchitecture => Environment.Is64BitOperatingSystem ? "x64" : "x86";
        public static string OSPlatform => Environment.OSVersion.Platform.ToString();
        //public static double Ticks => Environment.TickCount64;
        public static object[] Arguments => Environment.GetCommandLineArgs();
        public static string Time => DateTime.Now.ToString("HH:mm:ss");
        public static string Date => DateTime.Now.ToString("dd/MM/yyyy");
        public static string GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name);
        }

        public static void SetEnvironmentVariable(string name, string value)
        {
            Environment.SetEnvironmentVariable(name, value);
        }

        //public static string RunCommand(string command)
        //{
        //    // if we are running on windows, we need to use cmd.exe 
        //    if (OSPlatform == "Win32NT")
        //    {
        //        Process process = new();
        //        process.StartInfo.FileName = "cmd.exe";
        //        process.StartInfo.Arguments = "/C " + command;
        //        process.StartInfo.UseShellExecute = false;
        //        process.StartInfo.RedirectStandardOutput = true;
        //        process.StartInfo.CreateNoWindow = true;
        //        process.Start();
        //        string output = process.StandardOutput.ReadToEnd();
        //        process.WaitForExit();
        //        return output;
        //    }
        //    else if (OSPlatform == "Unix")
        //    {
        //        Process process = new();
        //        process.StartInfo.FileName = "/bin/bash";
        //        process.StartInfo.Arguments = "-c " + command;
        //        process.StartInfo.UseShellExecute = false;
        //        process.StartInfo.RedirectStandardOutput = true;
        //        process.StartInfo.CreateNoWindow = true;
        //        process.Start();
        //        string output = process.StandardOutput.ReadToEnd();
        //        process.WaitForExit();
        //        return output;
        //    }
        //    else
        //    {
        //        return "Unsupported OS";
        //    }
        //}
    }
}
