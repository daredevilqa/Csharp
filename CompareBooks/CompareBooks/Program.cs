using System;

namespace CompareBooks
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var book1 = new Book {Title = "B", Theme = 5};
            var book2 = new Book {Title = "B", Theme = 5};
        }
    }

    class Book : IComparable
    {
        public string Title;
        public int Theme;

        public int CompareTo(object obj)
        {
            var book = (Book) obj;
            if(Theme == book.Theme) {
                return string.Compare(Title, book.Title, StringComparison.Ordinal);
            }
            return Theme < book.Theme
                ? -1
                : 1;
        }
    }
}