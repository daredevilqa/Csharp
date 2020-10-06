using System;
using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace W3Schools_TrySQL_Tests.Pages
{
    public class TrySqlPage
    {
        private readonly IWebDriver driver;

        public IWebElement TrySqlForm => driver.FindElement(By.Id("tryitform"));
        public IWebElement RunSqlBtn => driver.FindElement(By.CssSelector("[onclick*='SQLSubmit']"));
        public IWebElement CodeArea => TrySqlForm.FindElement(By.ClassName("CodeMirror"));
        public IWebElement RestoreDbBtn => driver.FindElement(By.Id("restoreDBBtn"));
        public IWebElement DbInfoSection => driver.FindElement(By.Id("dbInfo"));
        public IWebElement CustomersCell => DbInfoSection.FindElement(By.CssSelector("td[onclick*='Customers']"));

        public IWebElement NumOfCustomersCell =>
            DbInfoSection.FindElement(By.CssSelector("td[onclick*='Customers'] + td"));

        public IWebElement ResultDiv => driver.FindElement(By.Id("divResultSQL"));
        public IWebElement ResultTable => ResultDiv.FindElement(By.TagName("table"));
        public IWebElement ResultTableHeader => ResultTable.FindElement(By.TagName("tr"));

        public IEnumerable<IWebElement> ResultRows =>
            ResultTable.FindElements(By.TagName("tr")).Skip(1); //skip first header row

        /// <summary>
        /// Sets the new SQL query into the code editor present on the page using JS
        /// </summary>
        /// <param name="sql">String SQL script to use</param>
        public void SetNewQuery(string sql)
        {
            var js = (IJavaScriptExecutor) driver;
            js.ExecuteScript($"window.editor.setValue(\"{sql}\")");
        }

        public void RestoreDb()
        {
            RestoreDbBtn.Click();
            var alert = new WebDriverWait(driver, TimeSpan.FromSeconds(3)).Until(ExpectedConditions.AlertIsPresent());
            alert.Accept();
        }

        public TrySqlPage(IWebDriver driver)
        {
            this.driver = driver;
            if (!driver.Title.Contains("SQL Tryit Editor")) {
                throw new InvalidOperationException(
                    $"Either this isn't 'SQL Try it' page or smth went wrong. Current page is: '{driver.Url}'");
            }
        }
    }
}
