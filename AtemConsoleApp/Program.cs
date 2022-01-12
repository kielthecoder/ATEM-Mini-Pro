using System;
using System.Threading;

namespace AtemConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var mini = new AtemBase())
            {
                mini.Connect("172.20.10.205", 9910);
                mini.Hello();

                Thread.Sleep(2000);
                Console.WriteLine("OK");

                mini.SetPreview(3);
                Thread.Sleep(1000);

                Console.WriteLine("Cut");
                mini.Cut();
                Thread.Sleep(1000);

                Console.WriteLine("Auto");
                mini.Auto();
                Thread.Sleep(2000);

                Console.WriteLine("Done");
                Thread.Sleep(2000);
            }
        }
    }
}
