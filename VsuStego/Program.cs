using System;
using VsuStego.Tasks;

namespace VsuStego
{
    class Program
    {
        static void Main()
        {
            ITask task = new Task2();
            task.Run();

            Console.WriteLine("ok");
            Console.Read();
        }
    }
}