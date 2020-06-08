using System;

namespace Combine_Arrays
{
    internal class Program
    {
        public static void Main()
        {
            var ints = new[] { 1, 2 };
            var strings = new[] { "A", "B" };

            Print(Combine(ints, ints));
            Print(Combine(ints, ints, ints));
            Print(Combine(ints));
            Print(Combine());
            Print(Combine(strings, strings));
            Print(Combine(ints, strings));
        }

        static Array Combine(params Array[] arrays)
        {
            if (arrays.Length == 0) {
                return null;
            }
            var type = arrays[0].GetType().GetElementType();
            var length = 0;

            foreach (var arr in arrays) {
                if (arr.GetType().GetElementType() != type) {
                    return null;
                }
                length += arr.Length;
            }
            var result = Array.CreateInstance(type, length);
            var index = 0;

            foreach (var array in arrays) {
                foreach (var item in array) {
                    result.SetValue(item, index++);
                }
            }
            return result;
        }

        static void Print(Array array)
        {
            if (array == null)
            {
                Console.WriteLine("null");
                return;
            }
            for (int i = 0; i < array.Length; i++)
                Console.Write("{0} ", array.GetValue(i));
            Console.WriteLine();
        }
    }
}