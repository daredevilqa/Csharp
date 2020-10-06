using System;
using System.Linq;
using NUnit.Framework;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using W3Schools_TrySQL_Tests.Helpers;
using W3Schools_TrySQL_Tests.Pages;

namespace W3Schools_TrySQL_Tests
{
    public class UiTest1 : TestBase
    {
        [Test]
        [Description("Validate the address of Giovanni Rovelli is 'Via Ludovico il Moro 22'")]
        public void Test1()
        {
            const string defaultSql = "SELECT * FROM Customers;";
            const string contactName = "Giovanni Rovelli";
            const string address = "Via Ludovico il Moro 22";
            var page = new TrySqlPage(Driver);

            Assert.AreEqual(defaultSql, page.CodeArea.Text.Trim(), "Default SQL query doesn't match");

            page.RunSqlBtn.Click();

            Assert.IsTrue(page.ResultTable.Displayed, "Expected: results table should appear");

            var row = page.ResultRows.GetItemByText(contactName);
            var addressCell = row.GetCellByHeader(page.ResultTableHeader, "Address");
            var contactNameCell = row.GetCellByHeader(page.ResultTableHeader, "ContactName");

            Assert.AreEqual(contactName, contactNameCell.Text,
                $"Expected: '{contactName}' should live exactly under ContactName column");
            Assert.AreEqual(address, addressCell.Text, $"{contactName}'s address doesn't match");
        }
    }

    public class UiTest2 : TestBase
    {
        [Test]
        [Description("Should show only 6 London customers using respective query")]
        public void Test2()
        {
            var page = new TrySqlPage(Driver);
            page.SetNewQuery("select * from Customers where city='London'");
            page.RunSqlBtn.Click();

            Assert.IsTrue(page.ResultTable.Displayed, "Expected: results table should appear");
            StringAssert.Contains(
                "Number of Records: 6",
                page.ResultDiv.Text,
                "Expected: # of records label should show 6 records count");
            Assert.AreEqual(
                6,
                page.ResultRows.Count(),
                "Expected: Result table should show only 6 rows with London customers");
        }
    }

    public class UiTest3 : TestBase
    {
        [Test]
        [Description("Validate it's possible to add a new record into Customers table")]
        public void Test3()
        {
            const string customerName = "Terminator",
                contactName = "Arni",
                address = "Venice Beach Gym",
                city = "Santa-Monica",
                postalCode = "91203",
                country = "USA";

            var sql = "insert into Customers (CustomerName, ContactName, Address, City, PostalCode, Country) "
                      + $"values ('{customerName}', '{contactName}', '{address}', '{city}', '{postalCode}', '{country}');";
            var page = new TrySqlPage(Driver);
            page.RunSqlBtn.Click();

            Assert.IsTrue(page.ResultTable.Displayed, "Expected: results table should appear");

            var numOfRecords = int.Parse(page.NumOfCustomersCell.Text);
            page.SetNewQuery(sql);
            page.RunSqlBtn.Click();

            Assert.AreEqual(
                "You have made changes to the database. Rows affected: 1",
                page.ResultDiv.Text,
                "Expected: success message should appear in the result div");

            page.CustomersCell.Click();
            StringAssert.Contains(
                $"Number of Records: {numOfRecords + 1}",
                page.ResultDiv.Text,
                "Expected: # of records label should be updated respectively");
            Assert.AreEqual(
                numOfRecords + 1,
                page.ResultRows.Count(),
                "Expected: Result table should show updated number of rows including just added row");
            Assert.AreEqual(
                $"{numOfRecords + 1}",
                page.NumOfCustomersCell.Text,
                "Expected: DB info section should show updated number of records in Customers table");

            var row = page.ResultRows.Last();
            var header = page.ResultTableHeader;

            Assert.AreEqual(customerName, row.GetCellByHeader(header, "CustomerName").Text,
                "Cell value doesn't match column name");
            Assert.AreEqual(contactName, row.GetCellByHeader(header, "ContactName").Text,
                "Cell value doesn't match column name");
            Assert.AreEqual(address, row.GetCellByHeader(header, "Address").Text,
                "Cell value doesn't match column name");
            Assert.AreEqual(city, row.GetCellByHeader(header, "City").Text,
                "Cell value doesn't match column name");
            Assert.AreEqual(postalCode, row.GetCellByHeader(header, "PostalCode").Text,
                "Cell value doesn't match column name");
            Assert.AreEqual(country, row.GetCellByHeader(header, "Country").Text,
                "Cell value doesn't match column name");
        }
    }

    public class UiTest4 : TestBase
    {
        [Test]
        [Description("Validate it's possible to edit a record of Customers table and changes persist")]
        public void Test4()
        {
            const string customerName = "Terminator",
                contactName = "Arni",
                address = "Venice Beach Gym",
                city = "Santa-Monica",
                postalCode = "91203",
                country = "USA";

            var sql = $"update Customers set CustomerName='{customerName}', ContactName='{contactName}', Address='{address}', City='{city}', PostalCode='{postalCode}', Country='{country}' where CustomerID=1";
            var page = new TrySqlPage(Driver);

            page.SetNewQuery(sql);
            page.RunSqlBtn.Click();

            new WebDriverWait(Driver, TimeSpan.FromSeconds(3)).Until(
                ExpectedConditions.TextToBePresentInElement(page.ResultDiv,
                    "You have made changes to the database. Rows affected: 1"));

            page.CustomersCell.Click();
            Assert.IsTrue(page.ResultTable.Displayed, "Expected: results table should appear");

            var row = page.ResultRows.First();
            var header = page.ResultTableHeader;

            Assert.AreEqual(customerName, row.GetCellByHeader(header, "CustomerName").Text,
                "Cell should display the updated value");
            Assert.AreEqual(contactName, row.GetCellByHeader(header, "ContactName").Text,
                "Cell should display the updated value");
            Assert.AreEqual(address, row.GetCellByHeader(header, "Address").Text,
                "Cell should display the updated value");
            Assert.AreEqual(city, row.GetCellByHeader(header, "City").Text,
                "Cell should display the updated value");
            Assert.AreEqual(postalCode, row.GetCellByHeader(header, "PostalCode").Text,
                "Cell should display the updated value");
            Assert.AreEqual(country, row.GetCellByHeader(header, "Country").Text,
                "Cell should display the updated value");

            Driver.Navigate().Refresh();
            page.RunSqlBtn.Click();
            row = page.ResultRows.First();
            header = page.ResultTableHeader;

            Assert.AreEqual(customerName, row.GetCellByHeader(header, "CustomerName").Text,
                "Expected: Changed record should still display the updated value even after the page refresh");
        }
    }

    public class UiTest5 : TestBase
    {
        [Test]
        [Description("Validate 'Restore DB' button resets the DB to initial state")]
        public void Test5()
        {
            const string sql =
                "insert into Customers (CustomerName, ContactName, Address, City, PostalCode, Country) values (1, 2, 3, 4, 5, 6);";

            var page = new TrySqlPage(Driver);
            page.RunSqlBtn.Click();
            var numOfRecords = int.Parse(page.NumOfCustomersCell.Text);
            page.SetNewQuery(sql);
            page.RunSqlBtn.Click();

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(3));
            wait.Until(ExpectedConditions.TextToBePresentInElement(page.ResultDiv,
                "You have made changes to the database. Rows affected: 1"));

            page.RestoreDb();
            wait.Until(ExpectedConditions.TextToBePresentInElement(page.ResultDiv,
                    "The database is fully restored."));

            page.CustomersCell.Click();

            StringAssert.Contains(
                $"Number of Records: {numOfRecords}",
                page.ResultDiv.Text,
                "Expected: # of records label should show initial number");
            Assert.AreEqual(
                numOfRecords,
                page.ResultRows.Count(),
                "Expected: Result table should show initial number of rows");
            Assert.AreEqual(
                $"{numOfRecords}",
                page.NumOfCustomersCell.Text,
                "Expected: DB info section should show initial number of records in Customers table");
        }
    }

    public class UiTest6 : TestBase
    {
        [Test]
        [Description("Validate running an empty SQL statement results into error alert msg")]
        public void Test6()
        {
            var page = new TrySqlPage(Driver);
            page.SetNewQuery(string.Empty);
            page.RunSqlBtn.Click();

            var alert = new WebDriverWait(Driver, TimeSpan.FromSeconds(3))
                .Until(ExpectedConditions.AlertIsPresent());
            StringAssert.Contains(
                "Error 1: could not execute statement",
                alert.Text,
                "Alert error msg doesn't match");
            alert.Accept();
        }
    }
}
