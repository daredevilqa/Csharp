using System;
using System.Collections.Generic;
using System.Linq;

namespace Case_Alternator
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            while (true) {
                Console.WriteLine("Input a lowercase password:");
                var word = Console.ReadLine();
                if (word != null && word == "") {
                    Console.WriteLine("Empty password entered. Let's try again.");
                    continue;
                }

                var resultList = CaseAlternatorTask.AlternateCharCases(word);
                Console.WriteLine("The result list:");
                
                foreach (var password in resultList) {
                    Console.WriteLine(password);
                }
                
                break;
            }
            Console.WriteLine("Press any key to close the program...");
            Console.ReadLine();
        }

        public class CaseAlternatorTask
        {
            //Вызывать будут этот метод
            public static List<string> AlternateCharCases(string lowercaseWord)
            {
                var result = new List<string>();
                AlternateCharCases(lowercaseWord.ToCharArray(), lowercaseWord.Length - 1, result);
                
                result.Sort();
                
                return result;
            }
            
            private static void AlternateCharCases(char[] word, int startIndex, List<string> result)
            {
                if (startIndex < 0)
                    return;

                if (result.Count == 0)
                    result.Add(new string(word));

                var initialWord = word.ToArray();

                if (char.IsLetter(word[startIndex]) && char.IsLower(word[startIndex])) {
                    word[startIndex] = char.ToUpperInvariant(word[startIndex]);
                    
                    var tempString = new string(word);
                    
                    if (result.Contains(tempString)) {
                        return;
                    }

                    result.Add(tempString);
                }
                AlternateCharCases(initialWord, startIndex - 1, result);
                AlternateCharCases(word, startIndex - 1, result);
            }
        }
    }
}