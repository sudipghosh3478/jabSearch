using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jabSearch
{
    class Logger:IDisposable
    {
        StreamWriter outputFile;
        public Logger()
        {
            outputFile = new StreamWriter($"JabSearchLogs_{DateTime.Now.ToString().Replace(':', '_')}.txt");
            outputFile.AutoFlush = true;
            outputFile.WriteLine("Starting log");
            outputFile.WriteLine();
        }

        public void Dispose()
        {
            outputFile.Close();
            outputFile.Dispose();
        }

        internal void Log(string message)
        {
            var datedMessage = $"{DateTime.Now.ToString("hh-mm-ss tt")}: {message}";
            Console.WriteLine(datedMessage);
            outputFile.WriteLine(datedMessage);
        }

        internal void LogError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Log("Error: " + message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        internal void LogWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Log("Warning: " + message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        internal void LogSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Log("Success: " + message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        internal void EmptyLine()
        {
            Console.WriteLine();
            outputFile.WriteLine();
        }
    }
}
