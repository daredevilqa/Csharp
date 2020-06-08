using System;

namespace Fix_recursion
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var input = Console.ReadLine()?.ToCharArray();
            WriteReversed(input);
        }
        
        public static void WriteReversed(char[] items, int startIndex = 0)
        {
            if (startIndex == items.Length) {
                return;
            }
            
            WriteReversed(items, startIndex + 1); 
            
            Console.Write(items[startIndex]); 
        }
    }
}