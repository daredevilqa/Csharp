using System;
using NUnit.Framework;

namespace CustomAssert
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var expected = "Don't Worry";
            Console.WriteLine($"Let's try it out. Type in: {expected}");
            var actual = Console.ReadLine();
            StAssert.AreEqual(expected, actual, "Seems like you made a typo. Try again");
        }
    }

    public static class StAssert
    {
        public static void AreEqual(string expected, string actual, string msg)
        {
            try {
                Assert.AreEqual(expected, actual, msg);
                //StringAssert.AreEqualIgnoringCase(expected, actual, msg);
            }
            catch (AssertionException e) {
                Console.WriteLine("------------------------------------");
                Console.WriteLine("RESULT");
                Console.WriteLine("------------------------------------");
                Console.WriteLine("Exception message:");
                Console.WriteLine(e.Message);
                Console.WriteLine("------------------------------------");
                Console.WriteLine("Stack Trace:");
                Console.WriteLine(e.StackTrace);
                throw e;
            }
        }
    }
}