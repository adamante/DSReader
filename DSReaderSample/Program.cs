using System;
using System.Collections.Generic;
using System.Text;
using DSReader;

namespace DSReaderSample
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Press any key to get iButton ID");
            Console.ReadKey();

            var reader = new Reader();

        getID:
            Console.WriteLine();
            Console.WriteLine("Touch your iButton device to a receptor. You have 10 seconds");
            
            var ID = reader.GetID();

            switch (ID)
            {
                case 0:
                    Console.WriteLine("iButton device wasn't found");
                    break;
                case -1:
                    Console.WriteLine("1-Wire Net isn't availiable. Please set up default 1-Wire device in driver settings.");
                    break;
                default:
                    Console.WriteLine("ID of presented iButton is {0:X}", ID);
                    break;
            }

            Console.Write("Try again? (y/N) ");

            if (Console.ReadKey().Key == ConsoleKey.Y)
                goto getID;
        }
    }
}
