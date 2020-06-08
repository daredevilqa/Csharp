using System;
using System.Collections;

namespace ClockwiseComparer
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var array = new[]
            {
                new Point { X = 1, Y = 0 },
                new Point { X = -1, Y = 0 },
                new Point { X = 0, Y = 1 },
                new Point { X = 0, Y = -1 },
                new Point { X = 0.01, Y = 1 }
            };
            Array.Sort(array, new ClockwiseComparer());
            foreach (Point e in array)
                Console.WriteLine("{0} {1}", e.X, e.Y);
        }
    }

    public class Point
    {
        public double X;
        public double Y;
    }

    public class ClockwiseComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            var point1 = x as Point;
            var point2 = y as Point;
            if (point1.Y >= 0) {
                return point2.Y >= 0
                    ? point2.X.CompareTo(point1.X)
                    : -1;
            }
            return point2.Y >= 0
                ? 1
                : point2.X.CompareTo(point1.X);
        }
    }
}