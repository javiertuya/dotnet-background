using System;

namespace TestProcess1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Start TestProcess:"+string.Join(" ",args));
            if (args.Length > 0 && args[0] == "exit") //simulates a process that exits before being killed
            {
                Console.WriteLine("end");
                return;
            }
            for (int i=1; i<=500; i++)
            {
                System.Threading.Thread.Sleep(1000);
                Console.WriteLine("Timer " + i);
            }
        }
    }
}
