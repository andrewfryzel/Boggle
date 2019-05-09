using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            int time = 60;
            Timer sw = new Timer();
            sw.Interval = 1000;
            sw.Elapsed += tick;
            sw.Start();

            while (time != 0) ;

            sw.Stop();
            Console.ReadKey();

            void tick(object sender, ElapsedEventArgs e)
            {
                Console.WriteLine("Time: " + time);
                time--;
            }
        }

    }
}
