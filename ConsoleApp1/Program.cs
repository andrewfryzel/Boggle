using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            int timeLimit = 140;
            DateTime start = DateTime.Now;
            Thread.Sleep(5000);

            int remaining = timeLimit - (DateTime.Now.Second - start.Second);
            Console.WriteLine(remaining);
            Console.ReadKey();
        }
    }
}
