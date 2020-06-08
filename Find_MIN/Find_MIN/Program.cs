using System;

namespace Find_MIN
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(Min(new[] { 3, 6, 2, 4 }));
            Console.WriteLine(Min(new[] { "B", "A", "C", "D" }));
            Console.WriteLine(Min(new[] { '4', '2', '7' }));
        }

        private static object Min(Array array)
        {
            object result = null;
            foreach (var item1 in array) {
                var counter = 0;
                var comparableItem1 = (IComparable) item1;
                foreach (var item2 in array) {
                    if (comparableItem1.CompareTo(item2) < 0) {
                        counter++;
                    }
                }
                if (counter == array.Length - 1) {
                    result = item1;
                    break;
                }
            }
            return result;
        }
    }
}