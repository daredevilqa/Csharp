using System;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace W3Schools_TrySQL_Tests
{
    public class TestBase
    {
        protected IWebDriver Driver;

        [SetUp]
        public void Setup()
        {
            Driver = new ChromeDriver();
            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
            Driver.Manage().Window.Maximize();
            Driver.Navigate().GoToUrl("https://www.w3schools.com/sql/trysql.asp?filename=trysql_select_all");
        }

        [TearDown]
        public void TearDown()
        {
            Driver.Quit();
        }

        /*
        [OneTimeTearDown]
        public void CleanUp()
        {
            Driver.Quit();
        }
        */
    }
}
