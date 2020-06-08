using System;

namespace Object_Array_Print
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Print(1, 2);
            Print("a", 'b');
            Print(1, "a");
            Print(true, "a", 1);
        }

        public static void Print(params object[] arr)
        {
            for(var i = 0; i < arr.Length; i++){
                if (i > 0)
                    Console.Write(", ");
                Console.Write(arr.GetValue(i));
            }
            Console.WriteLine();
        }
    }
}