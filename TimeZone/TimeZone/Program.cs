using System;

namespace TimeZone
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"Current local time zone code: '{TimeZoneInfo.Local.Id}'");
            Console.WriteLine($"UTC time zone code: '{TimeZoneInfo.Utc.Id}'");
            Console.WriteLine();
            Console.WriteLine("System has the following info about time zones:");

            foreach (var tz in TimeZoneInfo.GetSystemTimeZones()) {
                Console.WriteLine($"{tz} has '{tz.Id}' ID");
            }
            Console.ReadLine();
        }
    }
}