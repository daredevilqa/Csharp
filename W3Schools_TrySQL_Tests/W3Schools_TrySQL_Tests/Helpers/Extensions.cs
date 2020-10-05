using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium;

namespace W3Schools_TrySQL_Tests.Helpers
{
    public static class WebElementListExtensions
    {
        /// <summary>
        /// Seeks for the item within a collection by containing text
        /// </summary>
        /// <param name="list">Collection of IWebElements, e.g. a list of rows or cells</param>
        /// <param name="text"></param>
        /// <returns>IWebElement containing the text</returns>
        /// <exception cref="NoSuchElementException"></exception>
        public static IWebElement GetItemByText(this IEnumerable<IWebElement> list, string text)
        {
            var item = list.FirstOrDefault(el => el.Text.Contains(text));
            if (item is null) {
                throw new NoSuchElementException($"Item with the text '{text}' hasn't been found");
            }

            return item;
        }
    }

    public static class WebElementExtensions
    {
        /// <summary>
        /// Retrieves a particular cell by the corresponding table column header name
        /// </summary>
        /// <param name="row"></param>
        /// <param name="header"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static IWebElement GetCellByHeader(this IWebElement row, IWebElement header, string columnName)
        {
            var tds = row.FindElements(By.TagName("td"));
            var ths = header.FindElements(By.TagName("th"));
            var index = ths.IndexOf(ths.GetItemByText(columnName));

            return tds[index];
        }
    }
}
