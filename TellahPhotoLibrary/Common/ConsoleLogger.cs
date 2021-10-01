using System;
using System.Collections.Generic;
using System.Text;

namespace TellahPhotoLibrary.Common
{
    public class ConsoleLogger : ILogger
    {
        private static object _lock = new object();

        public void Write(string msg)
        {
            Console.Write(msg);
        }

        public void WriteLine(string msg)
        {
            Console.WriteLine(msg);
        }

        public void Flush()
        {
            Console.Out.Flush();
        }

        public void WarnWrite(string msg)
        {
            lock (_lock)
            {
                ConsoleColor originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;

                Console.Write(msg);

                Console.ForegroundColor = originalColor;
            }
        }

        public void WarnWriteLine(string msg)
        {
            lock (_lock)
            {
                ConsoleColor originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;

                Console.WriteLine(msg);

                Console.ForegroundColor = originalColor;
            }
        }

        public void WarnFlush()
        {
            Console.Out.Flush();
        }

        public void ErrorWrite(string err)
        {
            lock (_lock)
            {
                ConsoleColor originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;

                Console.Error.Write(err);

                Console.ForegroundColor = originalColor;
            }
        }

        public void ErrorWriteLine(string err)
        {
            lock (_lock)
            {
                ConsoleColor originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;

                Console.Error.WriteLine(err);

                Console.ForegroundColor = originalColor;
            }
        }

        public void ErrorFlush()
        {
            Console.Error.Flush();
        }
    }
}
