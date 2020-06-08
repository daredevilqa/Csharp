using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceTitan.Model;
using ServiceTitan.Services;
using ServiceTitan.Testing.Web.Constants;
using ServiceTitan.Testing.Web.Controls;
using ServiceTitan.Testing.Web.DataGenerator;
using ServiceTitan.Testing.Web.Framework;
using ServiceTitan.Testing.Web.Helpers;
using ServiceTitan.Testing.Web.Pages.Reports.Custom;
using ServiceTitan.UITests.Controls;
using ServiceTitan.UITests.Entities;
using ServiceTitan.UITests.Helpers;
using ServiceTitan.UITests.Pages.Reports.Redesign;
using ServiceTitan.Util;

namespace ServiceTitan.UITests.Tests.Reporting
{
    public class QA4220_ReportingRedesign_AllReportsTab : TestBase
    {
        [Test, Description("https://servicetitan.atlassian.net/browse/QA-4220 - all reports tab " +
             "https://servicetitan.atlassian.net/browse/QA-4509 - editing modal"),
         Category(TestCategories.Reporting)]
        public async Task QA4220_QA4509_ReportingRedesign_AllReportsTab_TS()
        {
            #region Data and Feature Gates

            this.SetConfigurationProperty(RootHub.ConfigurationProperties().Reports20, true);
            this.SetConfigurationProperty(RootHub.ConfigurationProperties().Reports20Redesign, true);
            this.SetConfigurationProperty(RootHub.ConfigurationProperties().Reports20Scheduler, true);
            this.SetConfigurationProperty(RootHub.ConfigurationProperties().Reports20ReportEngine, true);

            const string ReportName = "Awesome QA4220 report";
            const string ReportName2 = "Amazing QA4220 report";
            const string Description = "QA4220 description";

            var admin1 = new EmployeeEntity {
                Name = $"Admin1_{RandomStringHelper.RandomString(3)}",
                Role = BuiltInUserRole.Admin,
                UserAccessEntity = new UserAccessEntity {Username = $"Admin1_{UserName}", Password = Password}
            };
            var admin2 = new EmployeeEntity {
                Name = $"Admin2_{RandomStringHelper.RandomString(3)}",
                Role = BuiltInUserRole.Admin,
                UserAccessEntity = new UserAccessEntity {Username = $"Admin2_{UserName}", Password = Password}
            };
            var dispatch1 = new EmployeeEntity {
                Name = $"Dispatch1_{RandomStringHelper.RandomString(3)}",
                Role = BuiltInUserRole.Dispatch,
                UserAccessEntity = new UserAccessEntity {Username = $"Dispatch1_{UserName}", Password = Password}
            };
            var dispatch2 = new EmployeeEntity {
                Name = $"Dispatch2_{RandomStringHelper.RandomString(3)}",
                Role = BuiltInUserRole.Dispatch,
                UserAccessEntity = new UserAccessEntity {Username = $"Dispatch2_{UserName}", Password = Password}
            };

            await ExecuteDataWorkActionAsync(async hub => {
                await EmployeesWorker.CreateUserWithEmployeeAsync(hub, admin1);
                await EmployeesWorker.CreateUserWithEmployeeAsync(hub, admin2);
                await EmployeesWorker.CreateUserWithEmployeeAsync(hub, dispatch1);
                await EmployeesWorker.CreateUserWithEmployeeAsync(hub, dispatch2);
            });

            #endregion

            var browser = OpenDriver();
            browser.Login(admin1.UserAccessEntity.Password, admin1.UserAccessEntity.Username);

            var reportsMainPage = browser.OpenPageFromNavBar<ReportsMainPage>();

            // Validate that user can access “All reports” page
            var allReportsTab = reportsMainPage.OpenAllReportsTab();

            // Validate that to the left of the "Create Report" button, there are two icons that could be clicked to change tiles view
            Assert.True(allReportsTab.GridView.ExistsAndDisplayed, "Expected: Grid View selector displayed");
            Assert.True(allReportsTab.ListView.ExistsAndDisplayed, "Expected: List View selector displayed");

            // Validate that card (grid) view is default type of view
            ValidateGridView(allReportsTab);

            // Validate that reports are displaying as list by clicking on List View (right icon)
            allReportsTab.GetListView();
            ValidateListView(allReportsTab);

            // Validate that reports are displayed back as cards by clicking on Grid View (left icon)
            allReportsTab.GetGridView();
            ValidateGridView(allReportsTab);

            // Validate that the chosen type of view is saved when switching tabs
            allReportsTab.GetListView();
            reportsMainPage.OpenHomeTab();
            reportsMainPage.OpenAllReportsTab();
            ValidateListView(allReportsTab);

            reportsMainPage.OpenScheduledTab();
            reportsMainPage.OpenAllReportsTab();
            ValidateListView(allReportsTab);
            allReportsTab.GetGridView();

            // Validate that new report opens immediately after creation
            var techPerfReport = browser.CreateReport<ReactTechPerfReport>(
                ReportName,
                ReportingCategories.Operations,
                Description,
                false);

            var now = DateTime.Now; // save the time of the report's creation

            StringAssert.AreEqualIgnoringCase(ReportName, techPerfReport.Title.GetText(),
                "Name of just created report doesn't match");

            // Get users list for further cases
            var modal = techPerfReport.GetEditModal();
            var users = modal.GetSharingTab().ViewOnlySelect.GetAllOptionsForSelect();
            modal.CloseModalDialog();

            // Validate that created custom reports appear in correct categories
            browser.OpenPageFromNavBar<ReportsMainPage>().OpenAllReportsTab();

            Assert.True(allReportsTab.Cards.ExistsItemByText(ReportName),
                "Test report is not displayed within report cards");

            var categoryIndex = allReportsTab
                .Categories
                .FirstIndexOf(c => c.GetText() == ReportingCategories.Operations);

            var actualIndex = allReportsTab
                .CardsSectionsPerCategory
                .FirstIndexOf(s => s.GetText().Contains(ReportName));

            Assert.AreEqual(categoryIndex, actualIndex,
                "Created test report displayed in the wrong category (category index mismatch)");

            #region QA-4509: Editing Report Modal

            // Validate that editing modal opens by clicking “Edit” option from the ellipsis menu of the report card
            var reportCard = allReportsTab.Cards.GetRowByText(ReportName);
            reportCard.Edit();

            // Validate that user-generated report's modals have 2 tabs: Details, Sharing
            CollectionAssert.AreEquivalent(new[] { "Details", "Sharing" }, modal.Tabs.Select(t => t.GetText()),
                "Expected: user-generated report's modals have 2 tabs - Details and Sharing");

            // Validate that details tab in user-generated report modals shows the following:
            // {Report Name, Report Category, Report Description, Template, Created By}.
            // Validate in user-generated report modal “Template” shows the name of the dataset from which the report was created
            // Validate in user-generated report modal “Created By” shows the name of the user who created the report
            StringAssert.AreEqualIgnoringCase(ReportName, modal.Name.GetValue(),
                "Name of the report doesn't match in the modal");
            StringAssert.AreEqualIgnoringCase(ReportingCategories.Operations, modal.CategorySelect.GetSelectedText(),
                "Name of the report's category doesn't match in the modal");
            StringAssert.AreEqualIgnoringCase(Description, modal.Description.GetText(),
                "Name of the report's Description doesn't match in the modal");
            StringAssert.Contains($"{ReportingTemplates.TechPerf}", modal.Template.GetText(),
                "Used report template(dataset)'s name doesn't match in the modal");
            StringAssert.Contains(admin1.Name, modal.CreatedBy.GetText(),
                "User name of the report's creator doesn't match in the modal");

            // Validate in user-generated report modal user can edit Report Name, Report Category, Report Description
            var changedName = ReportName + " changed";
            var changedDescription = Description + " changed";
            var changedCategory = ReportingCategories.Marketing;

            modal.EditDetails(changedName, changedCategory, changedDescription);

            // Validate sharing tab in user-generated report modal shows the following dropdowns:
            // * 'View only', 'View and edit'
            // Validate both dropdowns show the same users as in the same tab in reports edit modal
            modal.GetSharingTab();

            Assert.True(modal.ViewOnlySelect.ExistsAndDisplayed, "Expected: 'View Only' selector is displayed");
            Assert.True(modal.ViewAndEditSelect.ExistsAndDisplayed, "Expected: 'View and Edit' selector is displayed");

            CollectionAssert.AreEquivalent(users, modal.ViewOnlySelect.GetAllOptionsForSelect(true),
                "Expected: names of users with whom the report can be shared do not match");
            CollectionAssert.AreEquivalent(users, modal.ViewAndEditSelect.GetAllOptionsForSelect(true),
                "Expected: names of users with whom the report can be shared do not match");

            modal.ViewAndEditSelect.SelectItem(users.First(), true); // select 1st user for sharing

            // Validate that by clicking “Save Report” button user closes editing modal and all changes apply
            modal.SaveAndWait();
            reportCard = allReportsTab.Cards.GetRowByText(ReportName);

            StringAssert.AreEqualIgnoringCase(changedName, reportCard.ReportLink.GetText(),
                "Expected: name of the report should be changed and displayed on the card");
            StringAssert.AreEqualIgnoringCase(changedDescription, reportCard.Description.GetText(),
                "Expected: description of the report should be changed and displayed on the card");

            categoryIndex = allReportsTab
                .Categories
                .FirstIndexOf(c => c.GetText() == changedCategory);

            actualIndex = allReportsTab
                .CardsSectionsPerCategory
                .FirstIndexOf(s => s.GetText().Contains(ReportName));

            Assert.AreEqual(categoryIndex, actualIndex,
                "Edited test report displayed in the wrong category (category index mismatch)");

            // Validate all changes that are made in editing modal on all reports page are applied to modal on the very report's page
            reportCard.ReportLink.ClickForOpenElement(techPerfReport).GetEditModal();

            StringAssert.AreEqualIgnoringCase(changedName, modal.Name.GetValue(),
                "Expected: name of the report should be changed and match");
            StringAssert.AreEqualIgnoringCase(changedDescription, modal.Description.GetValue(),
                "Expected: description of the report should be changed and match");
            StringAssert.AreEqualIgnoringCase(changedCategory, modal.CategorySelect.GetSelectedText(),
                "Expected: category of the report should be changed and match");

            modal.GetSharingTab();
            StringAssert.AreEqualIgnoringCase(users.First(),
                modal.ViewAndEditSelect.GetAllSelectedOptions().Single(),
                "Expected: the only user which was granted Edit access to the report is displayed in 'Edit Access' field");
            Assert.IsEmpty(modal.ViewOnlySelect.GetAllSelectedOptions(),
                "Expected: there's no users in view only access field");

            // Validate that system-generated report modal shows the following:
            // {Report Name, Report Category, Report Description, Template, Created By}
            // Validate 'Created By' says ServiceTitan
            modal.CloseModalDialog();
            browser.OpenPageFromNavBar<ReportsMainPage>().OpenAllReportsTab();
            reportCard = allReportsTab.Cards.GetRowByText("Thank You");
            reportCard.Edit();

            Assert.True(modal.Name.ExistsAndDisplayed, "Expected: Name field is displayed");
            Assert.True(modal.CategorySelect.ExistsAndDisplayed, "Expected: Category selector is displayed");
            Assert.True(modal.Description.ExistsAndDisplayed, "Expected: Description textarea is displayed");
            Assert.True(modal.Template.ExistsAndDisplayed, "Expected: Template label is displayed");
            StringAssert.Contains("ServiceTitan", modal.CreatedBy.GetText(),
                "Expected: In system-generated report, 'Created By' should say 'ServiceTitan'");

            // Validate that in system-generated report modal user can only edit name and description
            Assert.True(modal.CategorySelect.Disabled(),
                "Expected: user can't change category of system-generated built-in report");
            Assert.True(modal.Name.Enabled(), "Expected: Name field is editable");
            Assert.True(modal.Description.Enabled(), "Expected: Description field is editable");

            modal.SaveAndWait();

            #endregion

            reportCard = allReportsTab.Cards.GetRowByText(ReportName);
            reportCard
                .Edit()
                .EditDetails(ReportName, ReportingCategories.Operations, Description)
                .SaveReport.ClickWithWaitDisappear();
            reportCard = allReportsTab.Cards.GetRowByText(ReportName);

            // Validate that in the grid view the user sees the following information on the report card:
            // * Report Name
            // * Description
            // * Last Updated Date
            // * Scheduling icon
            // * Bookmarked icon (if report is bookmarked, then Bookmark icon always displayed)
            // * Breadcrumbs menu:
            //     ** Edit
            //     ** Delete (if report was created by user)
            StringAssert.AreEqualIgnoringCase(Description, reportCard.Description.GetText(),
                "Report's description doesn't match on the card");
            StringAssert.Contains(now.ToString("MM/dd/yy"), reportCard.Updated.GetText(),
                "Report's Updated date doesn't match on the card");
            Assert.True(reportCard.ScheduleBtn.ExistsAndDisplayed, "Schedule button isn't displayed on the card");
            CollectionAssert.AreEquivalent(new[] {
                    nameof(ReportActions.Edit),
                    nameof(ReportActions.Delete),
                    nameof(ReportActions.Duplicate)
                },
                reportCard.EllipsisMenu.GetAllOptionsForSelect(),
                "Breadcrumbs menu options do not match");

            // Validate that in the list view the user sees the same info as on the report card with the only difference
            // is that instead Scheduling icon there's a 'Schedule' option in the breadcrumbs menu
            allReportsTab.GetListView();
            var reportListItem = allReportsTab.CardsListView.GetRowByText(ReportName);
            reportListItem.SetBookmark();

            StringAssert.AreEqualIgnoringCase(Description, reportListItem.Description.GetText(),
                "Report's description doesn't match on the list item");
            StringAssert.Contains(now.ToString("MM/dd/yy"), reportListItem.Updated.GetText(),
                "Report's Updated date doesn't match on the list item");
            Assert.False(reportListItem.ScheduleBtn.ExistsAndDisplayed,
                "Schedule button should not be displayed on the list item");
            CollectionAssert.AreEquivalent(new[] {
                    nameof(ReportActions.Edit),
                    nameof(ReportActions.Delete),
                    nameof(ReportActions.Duplicate),
                    nameof(ReportActions.Schedule)
                },
                reportListItem.EllipsisMenu.GetAllOptionsForSelect(),
                "Breadcrumbs menu options do not match");
            Assert.True(reportListItem.Bookmark.ExistsAndDisplayed, "Bookmark icon isn't displayed on the list item");

            allReportsTab.GetGridView();
            Assert.True(reportCard.Bookmark.ExistsAndDisplayed, "Bookmark icon isn't displayed on the card");

            // Validate that on “All Reports” page reports are grouped by Category
            var expectedCategories = new[] {
                ReportingCategories.Marketing,
                ReportingCategories.Operations,
                ReportingCategories.Accounting,
                ReportingCategories.TechnicianDashboard,
                ReportingCategories.BusinessUnitDashboard,
                ReportingCategories.Integrations,
                ReportingCategories.Technician
            };
            CollectionAssert.AreEquivalent(expectedCategories, allReportsTab.Categories.Select(c => c.GetText()),
                "Categories headers do not match on their respective sections");

            // Validate the reports sorted alphabetically within each category
            foreach (var section in allReportsTab.CardsSectionsPerCategory) {
                CollectionAssert.IsOrdered(section.Cards.Select(c => c.ReportLink.GetText()),
                    StringComparer.Ordinal,
                    "Expected: reports sorted alphabetically within each category");
            }

            // Validate that on “All Reports” page there are “Categories” and “Created By” filters
            var categoriesFilter = allReportsTab.CategoriesSelect;
            var createdByFilter = allReportsTab.CreatedBySelect;
            Assert.True(categoriesFilter.ExistsAndDisplayed, "'Categories' dropdown filter isn't shown");
            Assert.True(createdByFilter.ExistsAndDisplayed, "'Created By' dropdown filter isn't shown");

            // Validate that on “All Reports” page by clicking “Categories” a user opens multi-select list of all
            // available report categories
            CollectionAssert.AreEquivalent(expectedCategories, categoriesFilter.GetAllOptionsForSelect(),
                "Categories options do not match in the Category filter");

            // Validate that user can select any number of categories from list in "Categories" filter
            var checkboxesList = CheckOffAllOptionsAndRefresh(categoriesFilter);

            // Validate that if any category is selected in "Categories" filter,
            // then “Clear selected items” button appears at the top of the list
            Assert.True(categoriesFilter.ClearSelectedItems.ExistsAndDisplayed,
                "Expected: 'Clear selected items' option appeared");
            StringAssert.Contains("Clear selected items", categoriesFilter.ClearSelectedItems.GetText(),
                "Clear option's text doesn't match");

            // Validate that “Clear selected items” button unchecks all checked categories in "Categories" filter
            ClearSelectedOptionsAndAssert(categoriesFilter, checkboxesList);

            // Validate that if nothing is checked in "Categories" filter, then there is no number near this filter's name
            var filterName = categoriesFilter.Query.Css(".Button__content").FirstDomNode();

            StringAssert.AreEqualIgnoringCase("Categories", filterName.GetText(),
                "Expected: when no categories selected, filter displays just 'Categories' name w/o any numbers in brackets");

            // Validate that the number near “Categories” filter shows number of checked categories in this filter
            expectedCategories = new[] {
                ReportingCategories.Marketing,
                ReportingCategories.Operations
            };
            categoriesFilter.SelectItems(expectedCategories, true);

            StringAssert.AreEqualIgnoringCase("Categories (2)", filterName.GetText(),
                "Expected: when 2 categories selected, filter displays this number in its name in brackets");

            // Validate that if one or more categories are selected in “Categories” filter,
            // then shown reports should be filtered by the category
            CollectionAssert.AreEquivalent(expectedCategories, allReportsTab.Categories.Select(c => c.GetText()),
                "Expected: reports filtered by 2 categories and only these 2 categories displayed on the page");

            categoriesFilter.ClearSelectedCheckboxes();

            // Validate that by clicking “Created By” opens multi-select list, that may contain:
            // 'You', 'ServiceTitan', 'Other' (if several users created their reports)
            // Validate that if other users have no created custom reports, then “Created By” does not contain “Other”
            CollectionAssert.AreEquivalent(new[] { "You", "ServiceTitan" },
                createdByFilter.GetAllOptionsForSelect(),
                "Created By options do not match in the respective filter / 'Other' option should not be present");

            // Validate that user can select any number of items from list in “Created By” filter
            checkboxesList = CheckOffAllOptionsAndRefresh(createdByFilter);

            // Validate that number near “Created By” filter shows number of checked items in this filter
            filterName = createdByFilter.Query.Css(".Button__content").FirstDomNode();

            StringAssert.AreEqualIgnoringCase("Created By (2)", filterName.GetText(),
                "Expected: when certain options selected, filter displays this number in its name in brackets");

            // Validate that if any item is selected in “Created By” filter, then “Clear selected items” button appears
            Assert.True(createdByFilter.ClearSelectedItems.ExistsAndDisplayed,
                "Expected: 'Clear selected items' option appeared");
            StringAssert.Contains("Clear selected items", createdByFilter.ClearSelectedItems.GetText(),
                "Clear option's text doesn't match");

            // Validate that “Clear selected items” button unchecks all checked items in “Created By” filter
            ClearSelectedOptionsAndAssert(createdByFilter, checkboxesList);

            // Validate that if nothing is checked in “Created By” filter, then there is no number near this filter
            StringAssert.AreEqualIgnoringCase("Created By", filterName.GetText(),
                "Expected: when no options selected, filter displays just 'Created By' name w/o any numbers in brackets");

            // Validate that if the current user has created reports, then he is displayed as “You” in “Created By” filter
            // Validate that if “You” is selected in “Created By” filter, then all reports
            // that are created only by the current user should be shown within their categories
            createdByFilter.SelectItem("You");

            Assert.True(allReportsTab.Cards.Single(c => c.ReportLink.GetText() == ReportName).ExistsAndDisplayed,
                $"Expected: only one report named '{ReportName}' is displayed that was created by this user");

            createdByFilter.ClearSelectedCheckboxes();

            // Validate that if current user has no created custom reports, then “Created By” does not contain “You”
            browser.SignOut();
            browser.Login(admin2.UserAccessEntity.Password, admin2.UserAccessEntity.Username);
            browser.OpenPageFromNavBar<ReportsMainPage>().OpenAllReportsTab();

            CollectionAssert.AreEquivalent(new[] { "Other", "ServiceTitan" },
                createdByFilter.GetAllOptionsForSelect(),
                "Created By options do not match in the respective filter / 'You' option should not be present");

            // Validate that user can use both filters along with searching content at the same time
            // create 2nd report under same category but w/ different report name
            browser.CreateReport<ReactTechPerfReport>(ReportName2, ReportingCategories.Operations);
            browser.OpenPageFromNavBar<ReportsMainPage>().OpenAllReportsTab();
            categoriesFilter.SelectItem(ReportingCategories.Operations);
            createdByFilter.SelectItems(new[] { "You", "Other" });

            var searchBox = allReportsTab.SearchBox;
            searchBox.SetValue("QA4220");

            Assert.AreEqual(2, allReportsTab.Cards.Count(), "Expected: after filtering only 2 reports remain");
            Assert.True(allReportsTab.Cards.GetRowByText(ReportName).ExistsAndDisplayed,
                $"Expected: report named '{ReportName}' that was created by first user is displayed");
            Assert.True(allReportsTab.Cards.GetRowByText(ReportName2).ExistsAndDisplayed,
                $"Expected: report named '{ReportName2}' that was created by second user is displayed");

            createdByFilter.SelectItem("Other"); // uncheck 'Other' option so that only 'You' remains
            Assert.AreEqual(1, allReportsTab.Cards.Count(), "Expected: after filtering only 1 report remains");
            Assert.True(allReportsTab.Cards.GetRowByText(ReportName2).ExistsAndDisplayed,
                $"Expected: report named '{ReportName2}' that was created by current user is displayed");

            // Validate that all filters are saved when switching tabs
            reportsMainPage.OpenHomeTab();
            reportsMainPage.OpenAllReportsTab();

            Assert.AreEqual(1, allReportsTab.Cards.Count(),
                "Expected: after filtering and switching tabs only 1 report remains");
            Assert.True(allReportsTab.Cards.GetRowByText(ReportName2).ExistsAndDisplayed,
                $"Expected: report named '{ReportName2}' that was created by current user is displayed");

            // Validate that if “Other” is selected in “Created By” filter, then all reports
            // that are created by other users should be shown within their categories
            categoriesFilter.ClearSelectedCheckboxes();
            createdByFilter.ClearSelectedCheckboxes();
            createdByFilter.SelectItem("Other");

            Assert.True(allReportsTab.Cards.Single(c => c.ReportLink.GetText() == ReportName).ExistsAndDisplayed,
                $"Expected: only one report named '{ReportName}' is displayed that was created by another user");

            createdByFilter.ClearSelectedCheckboxes();

            // Validate that if all reports that were created by other users are deleted,
            // then “Created By” filter does not show and count "Other" category
            allReportsTab.DeleteReport(ReportName);

            CollectionAssert.AreEquivalent(new[] { "You", "ServiceTitan" },
                createdByFilter.GetAllOptionsForSelect(true),
                "Created By options do not match in the respective filter / 'Other' option should not be present");

            // Validate that report cards are automatically removed from “All Reports” page, when they are deleted
            Assert.False(allReportsTab.Cards.ExistsItemByText(ReportName),
                $"Expected: '{ReportName}' deleted and, thus, is not displayed within report cards");

            // Validate that if none of the users have created custom reports, then “Created By” filter is hidden
            allReportsTab.DeleteReport(ReportName2);
            allReportsTab.WaitDomLoad();
            Assert.False(createdByFilter.ExistsAndDisplayed,
                "Expected:'Created By' filter should be hidden after deleting all reports created by users");
        }

        private void ValidateGridView(AllReportsTab tab)
        {
            CollectionAssert.IsNotEmpty(tab.Cards, "Expected: reports presented as cards (grid view)");
            CollectionAssert.IsEmpty(tab.CardsListView, "Expected: reports should not be displayed as list");
        }

        private void ValidateListView(AllReportsTab tab)
        {
            CollectionAssert.IsNotEmpty(tab.CardsListView, "Expected: reports presented in a list view");
        }

        private DomNodeWrapperList<CheckBox> CheckOffAllOptionsAndRefresh(SelectDropdown filter)
        {
            var checkboxesList = new DomNodeWrapperList<CheckBox>(() => filter.Dropdown.Query);

            foreach (var checkBox in checkboxesList) {
                checkBox.Check();
            }
            filter.Fold();
            filter.Unfold();

            return checkboxesList;
        }

        private void ClearSelectedOptionsAndAssert(SelectDropdown filter, IEnumerable<CheckBox> checkboxesList)
        {
            filter.ClearSelectedCheckboxes();
            filter.Unfold();

            Assert.False(filter.ClearSelectedItems.ExistsAndDisplayed,
                "Expected: 'Clear selected items' option disappeared since no options are selected");

            foreach (var checkBox in checkboxesList) {
                Assert.False(checkBox.Checked, "Expected: every checkbox is unchecked");
            }
        }
    }
}

