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
using ServiceTitan.UITests.Pages.Settings.Operations.ReportingSettings;

namespace ServiceTitan.UITests.Tests.Reporting
{
    public class QA6412_UpgradeReportInterface : TestBase
    {
        [Test]
        [Category(TestCategories.Reporting)]
        [Description("https://servicetitan.atlassian.net/browse/QA-6412")]
        public async Task QA6412_UpgradeReportInterface_TS()
        {
            #region Data and Feature Gates

            this.SetConfigurationProperty(RootHub.ConfigurationProperties().Reports20, true);
            this.SetConfigurationProperty(RootHub.ConfigurationProperties().Reports20Redesign, true);
            this.SetConfigurationProperty(RootHub.ConfigurationProperties().Reports20Scheduler, true);
            this.SetConfigurationProperty(RootHub.ConfigurationProperties().Reports20ReportEngine, true);

            const string UserReport = "User Report",
                UserReportChanged = "User Report Changed",
                UserReportDuplicateShared = "User Report Duplicate Shared",
                UserReportDuplicateNotShared = "User Report Duplicate Not Shared",
                Description = "lmao",
                PendoStep1 = "To edit, duplicate, schedule or delete a report, click More ⋮ next to the report name",
                PendoStep2 =
                    "After you sort and add filters to your report, click Save Changes to save those settings for the next time you run the report",
                PendoStep3 =
                    "Hover over a column name for more details in both the report table and the edit columns section",
                UnavailableCategory = "Accounting",
                Name = "Name",
                Zone = "Zone",
                TechBusUnit = "Technician Business Unit",
                CompletedJobs = "Completed Jobs",
                CompletedRevenue = "Completed Revenue";

            var from = DateTime.Today.AddDays(-30);
            var to = DateTime.Today;

            var admin1 = new EmployeeEntity {
                Name = "Admin_1",
                Role = BuiltInUserRole.Admin,
                UserAccessEntity = new UserAccessEntity { Username = $"Admin1_{UserName}", Password = Password }
            };
            var admin2 = new EmployeeEntity {
                Name = "Admin_2",
                Role = BuiltInUserRole.Admin,
                UserAccessEntity = new UserAccessEntity { Username = $"Admin2_{UserName}", Password = Password }
            };
            List<string> techs = null;

            await ExecuteDataWorkActionAsync(
                async hub => {
                    await EmployeesWorker.CreateUserWithEmployeeAsync(hub, admin1);
                    await EmployeesWorker.CreateUserWithEmployeeAsync(hub, admin2);
                    techs = hub.GetSession().Query.All<Technician>().Where(t => t.Active).Select(t => t.Name).ToList();
                });

            #endregion

            var browser = OpenDriver(options: BrowserOptions.Create().SetDefaultOptions());
            browser.Login(admin1.UserAccessEntity.Password, admin1.UserAccessEntity.Username);

            var reportModal = browser.OpenPageFromNavBar<ReportsMainPage>()
                .HomeTab.GetCreateReportModal(ReportingTemplates.TechPerf)
                .EditDetails(UserReport, ReportingCategories.Marketing)
                .GetSharingTab();
            reportModal.ViewOnlySelect.SelectItem(admin2.Name); //share with Admin_2 (view only)
            var userReport = reportModal.SaveReport.ClickForOpen<ReactTechPerfReport>(waitForClickability: true);

            //Validate “Run Report” button is unavailable if the date range filter does not set
            var dateRangePicker = userReport.Filter.DateRangePicker;
            dateRangePicker.From.ClearDate();
            userReport.RunButton.WaitElementByCondition();

            Assert.True(
                userReport.RunButton.Disabled(),
                "Expected: 'Run Report' button is disabled if the date range filter does not set");

            //Validate there are picture and default text inside the grid if the report is not run
            StringAssert.AreEqualIgnoringCase(
                "Run a Report",
                browser.ExecuteJavaScript(".k-grid-container", "before", "content").Trim('"'),
                "Expected: 'Run a Report' text is displayed inside the grid if the report hasn't been run yet");
            StringAssert.Contains(
                "url",
                browser.ExecuteJavaScript(".k-grid-container", "before", "background-image"),
                "Expected: image is displayed inside the grid if the report hasn't been run yet => background-image prop has 'url'");

            #region Tutorial Banner

            //Validate the tutorial banner is available for the user in the new design after the first access
            var banner = userReport.Banner;

            Assert.True(
                banner.ExistsAndDisplayed,
                "Expected: tutorial banner is available for the user in the new design after the first access");

            //Validate the tutorial banner contains the “X” (cross) and “Learn More” buttons
            Assert.True(
                banner.CloseIcon.ExistsAndDisplayed,
                "Expected: the tutorial banner contains the 'X' (cross) button");
            Assert.True(
                banner.ActionBtn.ExistsAndDisplayed && banner.ActionBtn.GetText().Contains("Learn More"),
                "Expected: the tutorial banner contains the 'Learn More' button");

            //Validate the tutorial banner appears in all custom reports if the user did not close the banner
            var builtInReport = browser.OpenReport<ReactThankYouReport>(true);

            banner = builtInReport.Banner;

            Assert.True(
                banner.ExistsAndDisplayed,
                "Expected: tutorial banner appears in all custom reports if the user did not close the banner");

            //Validate the walkthrough modal appears if the user clicks the “Learn More” button inside the banner
            var pendoModal = banner.ActionBtn.ClickForOpen<PendoModal>();

            //Validate the walkthrough modal contains 3 steps and a title: 'What’s New in Reports'
            Assert.AreEqual(3, pendoModal.StepsDots.Count(), "Expected: walkthrough modal contains 3 steps");
            StringAssert.AreEqualIgnoringCase(
                "What’s New in Reports",
                pendoModal.Header.GetText(),
                "Walkthrough's title doesn't match");

            //Validate a user can navigate between steps in the walkthrough modal by clicking on the dots
            pendoModal.StepsDots[1].Click();
            pendoModal.Body.WaitElementByCondition(PendoStep2);

            pendoModal.StepsDots.Last().Click();
            pendoModal.Body.WaitElementByCondition(PendoStep3);

            StringAssert.AreEqualIgnoringCase(
                "Done",
                pendoModal.NextOrDone.GetText(),
                "Expected: if user navigates to the last step, then “Next” button changes to “Done”");

            //Validate a user can navigate between steps in the walkthrough modal back and forth using Back/Next buttons
            pendoModal.Back.Click();
            pendoModal.Body.WaitElementByCondition(PendoStep2);
            pendoModal.Back.Click();
            pendoModal.Body.WaitElementByCondition(PendoStep1);
            pendoModal.NextOrDone.Click();
            pendoModal.Body.WaitElementByCondition(PendoStep2);
            pendoModal.NextOrDone.Click();
            pendoModal.Body.WaitElementByCondition(PendoStep3);

            //Validate a user can close the walkthrough modal by clicking on the “X” (cross) button
            pendoModal.CloseBtn.ClickWithWaitDisappear(reprocess: false);

            //Validate the banner does not disappear if a user clicks on the “X” (cross) button inside the walkthrough modal
            Assert.True(
                banner.ExistsAndDisplayed,
                "Expected: banner does not disappear if a user clicks on the 'X' (cross) button inside the walkthrough modal");

            //Validate by clicking on the “Done” button on the third step in the walkthrough modal, the walkthrough modal closes and the banner disappears
            banner.ActionBtn.ClickForOpenElement(pendoModal);
            pendoModal.StepsDots.Last().Click();
            pendoModal.NextOrDone.WaitElementByCondition("Done");
            pendoModal.NextOrDone.ClickWithWaitDisappear(reprocess: false);

            Assert.False(banner.ExistsAndDisplayed, "Expected: banner disappears");

            //Validate the banner will not appear even after refreshing the page if it has been closed
            builtInReport.FullRefreshPage();

            Assert.False(
                banner.ExistsAndDisplayed,
                "Expected: banner will not appear even after refreshing the page if it has been closed");

            //Validate the tutorial banner disappears from all custom reports
            browser.OpenReport<ReactTechPerfReport>(true, UserReport);
            banner = userReport.Banner;

            Assert.False(banner.ExistsAndDisplayed, "Expected: banner disappears from all custom reports");

            //Validate if a user closes the banner, it does not affect other users (the banner is hidden only for users who already closed it)
            browser.SignOut();
            browser.Login(admin2.UserAccessEntity.Password, admin2.UserAccessEntity.Username);
            browser.OpenReport<ReactTechPerfReport>(customName: UserReport);

            Assert.True(banner.ExistsAndDisplayed, "Expected: banner still displays for another user");

            #endregion Tutorial Banner

            //Validate near report’s name there is an ellipsis icon
            Assert.True(
                userReport.ReportMenuIcon.ExistsAndDisplayed,
                "Expected: near report’s name there is an ellipsis icon");

            //Validate the ellipsis icon has a tooltip with the text: More Actions
            userReport.ReportMenuIcon.AssertTooltip("More Actions");

            //Validate the overflow menu contains the following options: Edit, Duplicate, Schedule, Delete
            CollectionAssert.AreEquivalent(
                new[] {
                    ReportActions.Edit.ToString(),
                    ReportActions.Duplicate.ToString(),
                    ReportActions.Schedule.ToString(),
                    ReportActions.Delete.ToString()
                },
                userReport.ReportActionsMenu.GetAllOptionsForSelect(),
                "Report's overflow menu options do not match");

            //Validate the overflow menu does not contain the “Delete” option if the user cannot delete it (e.g. built-in)
            //Validate the “Schedule” option is unavailable if the “Allow scheduling of reports 2.0” FG is disabled
            browser.OpenReport<ReactThankYouReport>(true);
            this.SetConfigurationProperty(RootHub.ConfigurationProperties().Reports20Scheduler, false);
            builtInReport.FullRefreshPage();

            CollectionAssert.AreEquivalent(
                new[] {
                    ReportActions.Edit.ToString(),
                    ReportActions.Duplicate.ToString()
                },
                builtInReport.ReportActionsMenu.GetAllOptionsForSelect(),
                "Report's overflow menu options do not match");

            this.SetConfigurationProperty(RootHub.ConfigurationProperties().Reports20Scheduler, true);
            builtInReport.FullRefreshPage();

            //Validate the warning message appears if the user without edit permissions tries to edit, duplicate or delete the report
            browser.OpenReport<ReactTechPerfReport>(true, UserReport);
            var toast = browser.Query.FirstDomNode(q => new ToastWarning(q));

            userReport.ReportActionsMenu.SelectItem(ReportActions.Edit.ToString());
            ExecuteHelper.ExecuteAgainIfError(
                () => {
                    toast.WaitDomLoad(false);
                    StringAssert.Contains(
                        "Authorization Error",
                        toast.Title.GetText(),
                        "Either toast warning hasn't showed up or its title doesn't match");
                    StringAssert.Contains(
                        "You do not have permission to edit this custom report",
                        toast.Message.GetText(),
                        "Toast warning message doesn't match");
                },
                30,
                () => userReport.ReportActionsMenu.SelectItem(ReportActions.Edit.ToString()));
            toast.Dismiss();

            userReport.ReportActionsMenu.SelectItem(ReportActions.Duplicate.ToString());
            ExecuteHelper.ExecuteAgainIfError(
                () => {
                    toast.WaitDomLoad(false);
                    StringAssert.Contains(
                        "Authorization Error",
                        toast.Title.GetText(),
                        "Either toast warning hasn't showed up or its title doesn't match");
                    StringAssert.Contains(
                        "You do not have permission to duplicate this report",
                        toast.Message.GetText(),
                        "Toast warning message doesn't match");
                },
                30,
                () => userReport.ReportActionsMenu.SelectItem(ReportActions.Duplicate.ToString()));
            toast.Dismiss();

            userReport.ReportActionsMenu.SelectItem(ReportActions.Delete.ToString());
            ExecuteHelper.ExecuteAgainIfError(
                () => {
                    toast.WaitDomLoad(false);
                    StringAssert.Contains(
                        "Authorization Error",
                        toast.Title.GetText(),
                        "Either toast warning hasn't showed up or its title doesn't match");
                    StringAssert.Contains(
                        "You do not have permissions to delete this report",
                        toast.Message.GetText(),
                        "Toast warning message doesn't match");
                },
                30,
                () => userReport.ReportActionsMenu.SelectItem(ReportActions.Delete.ToString()));
            toast.Dismiss();

            browser.SignOut();
            browser.Login(admin1.UserAccessEntity.Password, admin1.UserAccessEntity.Username);

            var permissionsModal = browser.OpenSettingsPage<ReportingSettingsPage>()
                .OpenPermissionsTab()
                .EmployeesPermissionsTable.GetRowByText(admin1.Name)
                .OpenPermissionsEditModal();
            permissionsModal.OpenCategoriesTab()
                .PermissionsTable.GetRowByText(UnavailableCategory)
                .View.Uncheck();
            permissionsModal.SaveAndWait();

            //Validate if a user clicks “Edit” in the overflow menu, then appears a modal that is the same that on the All Reports screen
            browser.OpenReport<ReactTechPerfReport>(customName: UserReport);

            Assert.DoesNotThrow(
                () => userReport.GetEditModal(),
                "Expected: edit report modal appears that is the same that on the All Reports page");

            //Validate user closes editing modal by clicking “Save Changes” button and all the changes apply
            reportModal.EditDetails(UserReportChanged, ReportingCategories.Operations, Description);
            reportModal.SaveAndWait();

            StringAssert.AreEqualIgnoringCase(
                UserReportChanged,
                userReport.Title.GetText(),
                "Expected: changed report name displayed");
            userReport.GetEditModal();
            StringAssert.AreEqualIgnoringCase(
                Description,
                reportModal.Description.GetText(),
                "Description doesn't match");
            StringAssert.AreEqualIgnoringCase(
                ReportingCategories.Operations,
                reportModal.CategorySelect.GetSelectedText(),
                "Changed category doesn't match");

            reportModal.SaveAndWait();

            //Validate the “Schedule” option is available in the ellipsis if the “Allow scheduling of reports 2.0” FG is ON
            //Validate the scheduling workflow starts if a user clicks “Schedule”
            var selectReportTypePage = userReport.GetSchedulingFlow();

            //Validate a user returns into the report if they click “Back to Reports” button in the scheduling workflow
            selectReportTypePage.Header.BackToReports.ClickForOpenElement(userReport);

            #region Drawer

            //Validate the new drawer opens if a user clicks the “Edit Columns” button
            var drawer = userReport.GetEditColumnsDrawer();

            //Validate a user cannot close the “Edit Columns” drawer by clicking on a backdrop
            drawer.ClickWithOffsets(-3, 10);

            Assert.True(
                drawer.ExistsAndDisplayed,
                "Expected: user cannot close 'Edit Columns' drawer by clicking on a backdrop => drawer should remain visible");

            //Validate tooltips are shown if a user hovers over KPIs
            drawer.Columns.GetRowByText(Name)
                .Label.AssertTooltip("Name of the technician set on the technician settings page.");

            //Validate “Apply” button is unavailable until a column is added to or removed from the report
            Assert.True(
                drawer.ApplyBtn.Disabled(),
                "Expected: 'Apply' button is unavailable until a column is added to or removed from the report");

            //Validate “Apply” button is available if a user selected/deselected a column
            //Validate “Apply” button is unavailable if a user selected(deselected) and then deselected(selected) the same columns
            drawer.EditColumns(false, new[] { Name });
            Assert.True(
                drawer.ApplyBtn.Enabled(),
                "Expected: 'Apply' button is available if a user deselected a column");

            drawer.EditColumns(true, new[] { Name });
            Assert.True(
                drawer.ApplyBtn.Disabled(),
                "Expected: 'Apply' button is unavailable again if a user deselected and selected the same column");

            drawer.EditColumns(true, new[] { Zone });
            Assert.True(
                drawer.ApplyBtn.Enabled(),
                "Expected: 'Apply' button is available if a user selected a column");

            drawer.EditColumns(false, new[] { Zone });
            Assert.True(
                drawer.ApplyBtn.Disabled(),
                "Expected: 'Apply' button is unavailable again if a user selected and deselected the same column");

            //Validate “Edit Columns” drawer closes without saving changes if a user clicked the “Cancel” button
            drawer.EditColumns(true, new[] { Zone });
            drawer.ApplyAndWait("Cancel");

            CollectionAssert.DoesNotContain(
                userReport.TableHeader.HeaderItems.Select(h => h.GetText()).ToList(),
                Zone,
                $"Expected: {Zone} column hasn't been added since user cancelled the drawer");

            //Validate the “X” (cross) button appears, if the search bar is not empty
            userReport.GetEditColumnsDrawer().SearchBox.SetValue("qwerty");

            Assert.True(
                drawer.ClearSearchIcon.ExistsAndDisplayed,
                "Expected: 'X' (cross) icon appears, if the search bar is not empty");

            //Validate that by clicking cross (X) button search bar clears and becomes inactive
            drawer.ClearSearchIcon.ClickWithWaitDisappear(reprocess: false);

            Assert.True(
                string.IsNullOrEmpty(drawer.SearchBox.GetValue()) && !drawer.SearchBox.GetParent().IsActive(),
                "Expected: after clicking cross (X) icon, search bar clears and becomes inactive");

            //Validate the entered text does not disappear from the search bar if a user clicks out of it
            drawer.SearchBox.SetValue("qwerty");
            drawer.SearchBox.ClickWithOffsets(-10, -10);

            StringAssert.AreEqualIgnoringCase(
                "qwerty",
                drawer.SearchBox.GetValue(),
                "Expected: entered text does not disappear from the search bar if a user clicks out of it");

            //Validate if search has no results, then the user will see the illustration with the text:
            //“No matches found. Try a different search or clear search.”
            Assert.True(
                drawer.SearchIllustrationContainer.Query.TagName("img").FirstDomNode().ExistsAndDisplayed,
                "Expected: search illustration container with img is shown");
            StringAssert.Contains(
                "No matches found. Try a different search or clear search",
                drawer.SearchIllustrationContainer.GetText(),
                "No matches text should be shown");
            CollectionAssert.IsEmpty(
                drawer.Columns,
                "Expected: no columns should be displayed since nonsense string 'qwerty' has been entered");

            //Validate a user can clear the search query by clicking on the “clear search.” under the illustration
            Assert.True(drawer.ClearSearchBtn.ExistsAndDisplayed, "Expected: 'clear search' button is displayed");

            drawer.ClearSearchBtn.ClickWithWaitDisappear(reprocess: false);

            Assert.True(string.IsNullOrEmpty(drawer.SearchBox.GetValue()), "Expected: search box is cleared");
            CollectionAssert.IsNotEmpty(
                drawer.Columns,
                "Expected: columns appeared back since the search has been cleared");

            //Validate entering “Avg” returns all column names that contain “Average”
            drawer.SearchBox.SetValue("avg");
            drawer.Columns.WaitDomLoad(false);

            foreach (var kpi in drawer.Columns.Select(c => c.Label.GetText()).ToList()) {
                Assert.True(
                    kpi.Contains("Average") || kpi.Contains("Avg"),
                    "Expected: entering 'Avg' into the search box returns all column names that contain 'Average' or 'Avg'");
            }

            //Validate entering “%” returns all column names that contain the % character, as well as any column names contain “Rate”
            drawer.ClearSearchIcon.ClickWithWaitDisappear(reprocess: false);
            drawer.SearchBox.SetValue("%");
            drawer.Columns.WaitDomLoad(false);

            foreach (var kpi in drawer.Columns.Select(c => c.Label.GetText()).ToList()) {
                StringAssert.Contains(
                    "Rate",
                    kpi,
                    "Expected: entering '%' into the search box returns all column names that contain 'Rate'");
            }

            //Validate if the search is used, a tag at the bottom of the “Edit Column” menu shows the number of columns that are currently selected in the report
            Assert.True(
                drawer.ColumnsSelectedTag.ExistsAndDisplayed,
                "Expected: 'columns selected' tag is displayed when the search is used");
            StringAssert.AreEqualIgnoringCase(
                "4 columns selected",
                drawer.ColumnsSelectedTag.GetText(),
                "Expected: tag should say '4 columns selected'");

            //Validate a tag at the bottom is hidden if the search is not used
            drawer.ClearSearchIcon.ClickWithWaitDisappear(reprocess: false);

            Assert.False(
                drawer.ColumnsSelectedTag.ExistsAndDisplayed,
                "Expected: 'columns selected' tag is hidden if the search is not used");

            //Validate “Edit Columns” drawer closes and all changes are applied if a user clicked the “Apply” button
            drawer.EditColumns(true, new[] { Zone }); //add Zone KPI
            drawer.ApplyAndWait();
            userReport.Run();

            Assert.DoesNotThrow(
                () => userReport.TableHeader.GetColumnHeaderByName(Zone),
                $"Expected: columns changes should persist after applying changes in drawer => '{Zone}' column should be found");

            #endregion Drawer

            //Validate “Save Changes” button has a tooltip with the text: 'Save changes made to report columns and filters'
            userReport.SaveChanges.AssertTooltip("Save changes made to report columns and filters.");

            //Validate “Save Changes” button disappears after saving
            dateRangePicker.From.SetDateViaJs(from);
            dateRangePicker.To.SetDateViaJs(to);
            userReport.Run();
            userReport.Save();

            Assert.False(
                userReport.SaveChanges.ExistsAndDisplayed,
                "Expected: “Save Changes” button disappears after saving");

            //Validate the current date range does not change after saving a report
            StringAssert.AreEqualIgnoringCase(
                from.ToString("M/d/yyyy"),
                dateRangePicker.From.GetValue(),
                "Expected: date range persist after saving a report");

            StringAssert.AreEqualIgnoringCase(
                to.ToString("M/d/yyyy"),
                dateRangePicker.To.GetValue(),
                "Expected: date range persist after saving a report");

            #region Grouping Header

            //Validate a user can group a report by dragging and dropping a column header into the purple bar
            var groupingHeader = userReport.GroupingHeader;
            var nameCell = userReport.TableHeader.GetColumnHeaderByName(Name);
            nameCell.DragAndDrop(groupingHeader);

            Assert.True(
                groupingHeader.Indicators.ExistsItemByText(Name),
                $"Expected: After grouping by tech name, there should be a grouping indicator with '{Name}' text on the grouping row");
            Assert.True(
                userReport.GroupingRows.ExistsItemByText("Adam"),
                "Expected: After grouping by tech name, there should be a grouping row with Adam name");
            Assert.True(
                userReport.GroupingRows.ExistsItemByText("Bob"),
                "Expected: After grouping by tech name, there should be a grouping row with Bob name");

            //Validate if the purple bar is not empty it has the text: "Grouped by:"
            StringAssert.AreEqualIgnoringCase(
                "Grouped by:",
                browser.ExecuteJavaScript(".k-grouping-header", "before", "content").Trim('"'),
                "Expected: if the grouping header is not empty, it has the text 'Grouped by:'");

            //Validate a user can sort the grouped report by clicking on the column header inside the purple bar
            var sortingIcon = groupingHeader.Indicators.GetRowByText(Name).SortingIcon;

            Assert.True(
                sortingIcon.ExistsAndDisplayed,
                "Expected: sorting icon is displayed within the grouping indicator");
            Assert.True(
                sortingIcon.ContainsAttribute("sort-asc"),
                "Expected: grouped report sorted ascending by default");

            sortingIcon.ClickForOpenElement(sortingIcon);
            Assert.True(sortingIcon.ContainsAttribute("sort-desc"), "Expected: sorting changed from asc to desc");
            sortingIcon.ClickForOpenElement(sortingIcon);
            Assert.True(sortingIcon.ContainsAttribute("sort-asc"), "Expected: sorting changed from desc to asc");

            //Validate a user can group a report by several columns
            userReport.TableHeader.GetColumnHeaderByName(TechBusUnit).DragAndDrop(groupingHeader);

            Assert.True(
                groupingHeader.Indicators.ExistsItemByText(TechBusUnit),
                $"Expected: After grouping by '{TechBusUnit}', there should be a grouping indicator with '{TechBusUnit}' text on the grouping row");
            Assert.True(
                groupingHeader.Indicators.ExistsItemByText(Name),
                $"Expected: After grouping by '{TechBusUnit}', there should remain a grouping indicator with '{Name}' text on the grouping row"
                + "since user should be able to group a report by several columns");

            //Validate a user can remove grouping by clicking on the 'X'
            Assert.True(
                groupingHeader.Indicators.GetRowByText(TechBusUnit).DeleteIcon.ExistsAndDisplayed
                && groupingHeader.Indicators.GetRowByText(Name).DeleteIcon.ExistsAndDisplayed,
                "Expected: grouping indicators have delete icons");

            groupingHeader.Indicators.GetRowByText(TechBusUnit).DeleteIcon.ClickWithWaitDisappear();
            groupingHeader.Indicators.GetRowByText(Name).DeleteIcon.ClickWithWaitDisappear();

            Assert.False(userReport.GroupingRows.Any(), "Expected: grouping is removed => no grouping rows displayed");

            #endregion Grouping Header

            //Validate tooltips are shown when a user hovers over the column name
            var tooltipText = "";
            ExecuteHelper.ExecuteAgainIfError(
                () => {
                    nameCell.Label.MoveToElement();
                    userReport.KpiTooltip.WaitElementByCondition(longWait: false);
                    tooltipText = userReport.KpiTooltip.GetText();
                });
            StringAssert.AreEqualIgnoringCase(
                "Name of the technician set on the technician settings page.",
                tooltipText,
                "Tooltip text doesn't match");

            #region Sorting/Filtering

            //Validate columns can be sorted by clicking on their headers
            userReport.TableHeader.SortKendoColumn(Name, SortOrder.Descending);

            Assert.AreEqual(
                SortOrder.Descending,
                userReport.TableHeader.GetColumnSortOrder(Name),
                "Expected: 'Name' column is sorted descending");

            //Validate the column filter has “Is one of” option by default
            var filterModal = nameCell.OpenFilterModal();

            StringAssert.AreEqualIgnoringCase(
                KendoFilterOperators.IsOneOf,
                filterModal.FilterOperator.GetText(),
                "Expected: filter operator has 'Is one of' option by default");

            //Validate a user gets a list of all available values for “Is one of” option by clicking on the input below
            filterModal.SubOptionsMultiSelect.Click();
            filterModal.SubOptions.WaitDomLoad(false);

            CollectionAssert.AreEquivalent(
                techs,
                filterModal.SubOptions.Select(o => o.GetText()).ToList(),
                "Sub-options for 'IsOneOf' filter do not match");

            //Validate selected values create tags that could be removed by clicking on the “X”
            filterModal.SubOptions.GetRowByText("Adam").Click();
            filterModal.SelectedSubOptions.WaitDomLoad(false);
            var tag = filterModal.SelectedSubOptions.Single();

            Assert.True(tag.ExistsAndDisplayed, "Expected: selected values create tags");
            StringAssert.AreEqualIgnoringCase(
                "Adam",
                tag.GetText(),
                "Tag's name doesn't match");

            tag.CloseIcon.Click();
            tag.WaitDisappearElement(false);

            //Validate a user can remove all tags by clicking on the “X” inside the field
            filterModal.SubOptionsMultiSelect.Click();
            filterModal.SubOptions.WaitDomLoad(false);
            filterModal.SubOptions.GetRowByText("Adam").Click();
            filterModal.SubOptionsMultiSelect.Click();
            filterModal.SubOptions.WaitDomLoad(false);
            filterModal.SubOptions.GetRowByText("Bob").Click();
            filterModal.SelectedSubOptions.WaitDomLoad(false);

            Assert.True(
                filterModal.ClearSelectedValuesIcon.ExistsAndDisplayed,
                "Expected: 'X' Clear Selected Values icon is displayed");

            filterModal.ClearSelectedValuesIcon.ClickWithWaitDisappear(reprocess: false);

            CollectionAssert.IsEmpty(filterModal.SelectedSubOptions, "Expected: all tags have been removed");

            //Validate “Filter” icon changes if filter is applied
            filterModal.SubOptionsMultiSelect.Click();
            filterModal.SubOptions.WaitDomLoad(false);
            filterModal.SubOptions.GetRowByText("Adam").Click();
            filterModal.FilterBtn.ClickWithWaitDisappear(reprocess: false);

            Assert.True(
                nameCell.ContainsAttribute("Filter--active"),
                "Expected: 'Filter' icon changes if filter is applied");

            #endregion Sorting/Filtering

            #region Drilldown

            //Validate a user can select drilldown export format by clicking on the “Export” button
            //Validate a user can export drilldowns into PDF and XLSX
            var drilldown = userReport.Drilldown;
            userReport.Table.First().GetCellByKendoHeader(CompletedJobs, parentLvl: 7).GetDrilldown();
            drilldown.WaitDomLoad();
            drilldown.ExportDropdown.ScrollToThisItem(); //extra scrolling just for small screen builders
            drilldown.ScrollToDown();

            StringAssert.AreEqualIgnoringCase(
                CompletedJobs,
                drilldown.KpiTitle.GetText(),
                "Drilldown title doesn't match");
            CollectionAssert.AreEquivalent(
                new[] { "Export to PDF", "Export to XLSX" },
                drilldown.ExportDropdown.GetAllOptionsForSelect(true),
                "Expected: user can export drilldowns into PDF and XLSX");

            drilldown.ExportToPdf();
            browser.ClearDownloadCatalog();
            drilldown.ExportToXlsx();

            #endregion Drilldown

            #region Calendar

            dateRangePicker.To.SetDateViaJs(to.AddDays(-1)); //set not today deliberately

            //Validate the calendar opens no matter on which field a user clicks - the "From" or "To"
            dateRangePicker.From.ClickForOpenElement(dateRangePicker.Calendar);
            dateRangePicker.HideCalendar();
            dateRangePicker.To.ClickForOpenElement(dateRangePicker.Calendar);

            //Validate the “Today” button fills a correct (selected) date field and focus goes to another field
            //Validate the “From” and “To” fields automatically set today’s date if a user clicked “Today” twice
            dateRangePicker.Calendar.TodayLink.Click();
            dateRangePicker.To.WaitElementByCondition();

            StringAssert.AreEqualIgnoringCase(
                DateTime.Today.ToString("M/d/yyyy"),
                dateRangePicker.To.GetValue(),
                "Expected: today has been set into To field");

            dateRangePicker.Calendar.TodayLink.Click();
            dateRangePicker.From.WaitElementByCondition();

            StringAssert.AreEqualIgnoringCase(
                DateTime.Today.ToString("M/d/yyyy"),
                dateRangePicker.From.GetValue(),
                "Expected: today has been set into From field");

            dateRangePicker.HideCalendar();

            #endregion Calendar

            userReport.Run();
            userReport.Save();

            #region Duplicate/Delete Options

            //Validate if a user clicks “Duplicate” in the overflow menu, then appears the duplicate modal
            var dupModal = userReport.GetDuplicateModal();

            //Validate Duplicate modal contains one tab with the following fields: Name, Category, Description
            Assert.True(dupModal.Name.ExistsAndDisplayed, "Name input is not displayed");
            Assert.True(dupModal.CategorySelect.ExistsAndDisplayed, "Category select is not present");
            Assert.True(dupModal.Description.ExistsAndDisplayed, "Description input is not displayed");

            //Validate Duplicate modal contains the “Share it with the same people” checkbox
            Assert.True(
                dupModal.ShareItWithSamePpl.ExistsAndDisplayed,
                "Expected: Duplicate modal contains “Share it with the same people” checkbox");
            StringAssert.AreEqualIgnoringCase(
                "Share it with the same people",
                dupModal.ShareItWithSamePpl.Label.GetText(),
                "Checkbox label text doesn't match");

            //Validate Duplicate modal contains “Duplicate Report” button
            StringAssert.AreEqualIgnoringCase(
                "Duplicate Report",
                dupModal.SaveReport.GetText(),
                "Expected: instead of 'Save Report', the button says 'Duplicate Report'");

            //Validate in the Duplicate modal the category field does not contain categories that are unavailable for the user
            CollectionAssert.DoesNotContain(
                dupModal.CategorySelect.GetAllOptionsForSelect(true),
                UnavailableCategory,
                "Expected: category field does not contain categories that are unavailable for the user");

            //Validate in Duplicate modal the name field contains the following text: 'Copy of %name of the current report%'
            StringAssert.AreEqualIgnoringCase(
                $"Copy of {UserReportChanged}",
                dupModal.Name.GetValue(),
                "Expected: name field contains the following text: 'Copy of %name of the current report%'");

            //Validate the following text displays under the name field if user sets the name that matches an existing report’s name: 'Name is already in use'
            dupModal.Name.SetValue(UserReportChanged);
            dupModal.WaitElementByCondition(longWait: false);

            Assert.True(
                dupModal.DuplicateWarning.ExistsAndDisplayed,
                "Expected: duplicate warning text appears when user sets a name of the already existing report");
            StringAssert.AreEqualIgnoringCase(
                "Name is already in use.",
                dupModal.DuplicateWarning.GetText(),
                "Duplicate name warning text doesn't match");

            //Validate the duplicate option creates a report with the same KPIs, filtering, grouping and sorting as the original one
            dupModal.Name.SetValue(UserReportDuplicateNotShared);
            dupModal.SaveAndWait("Duplicate");
            userReport.WaitDomLoad();
            userReport.Run();

            StringAssert.AreEqualIgnoringCase(
                UserReportDuplicateNotShared,
                userReport.Title.GetText(),
                "Expected: just created duplicate report has proper name");
            StringAssert.Contains(
                "Adam",
                userReport.Table.First().GetText(),
                "Expected: duplicate should be filtered so that it pulls only one row containing Adam name");
            Assert.AreEqual(
                2,
                userReport.Table.Count(),
                "Expected: report contains 2 rows: 1 filtered + 1 footer");
            Assert.AreEqual(
                SortOrder.Descending,
                userReport.TableHeader.GetColumnSortOrder(Name),
                "Expected: 'Name' column of a duplicate report is sorted descending ");

            foreach (var columnName in new[] { Name, Zone, TechBusUnit, CompletedJobs, CompletedRevenue }) {
                Assert.DoesNotThrow(
                    () => userReport.TableHeader.GetColumnHeaderByName(columnName),
                    $"Expected: duplicate has the same columns set the original report had => '{columnName}' column should be found");
            }

            //Validate the report’s copy is not shared to anyone if “Share it with the same people” checkbox has not been selected during duplication
            userReport.GetEditModal();
            reportModal.GetSharingTab();

            Assert.IsEmpty(
                reportModal.ViewOnlySelect.GetAllSelectedOptions(),
                "Expected: duplicate report is not shared to anyone");
            Assert.IsEmpty(
                reportModal.ViewAndEditSelect.GetAllSelectedOptions(),
                "Expected: duplicate report is not shared to anyone");

            //Validate if user clicks “Delete” in overflow menu, appears a modal that is the same that on the All Reports screen
            //Validate if user clicked “Delete” button, then the user navigates to the /new/reports/home page and the report is deleted
            reportModal.CloseModalDialog();
            ReportsMainPage reportsMainPage = null;

            Assert.DoesNotThrow(
                () => reportsMainPage = userReport.GetDeleteModal().Delete.ClickForOpen<ReportsMainPage>(),
                "Expected: if user clicks “Delete” button, then user navigates to the /new/reports/home page");

            var allReportsTab = reportsMainPage.OpenAllReportsTab();

            Assert.False(
                allReportsTab.Cards.ExistsItemByText(UserReportDuplicateNotShared),
                $"Expected: '{UserReportDuplicateNotShared}' report is deleted => not present among cards");

            //Validate the duplication option has been added into ellipsis of reports on the reports page
            CollectionAssert.Contains(
                allReportsTab.Cards.GetRowByText(UserReportChanged).EllipsisMenu.GetAllOptionsForSelect(true),
                nameof(ReportActions.Duplicate),
                "Expected: duplication option has been added into ellipsis of reports on the reports page");

            //Validate the report’s copy is shared to the same people if “Share it with the same people” checkbox has been selected during duplication
            allReportsTab.Cards.GetRowByText(UserReportChanged).GetDuplicateModal();
            dupModal.Name.SetValue(UserReportDuplicateShared);
            dupModal.ShareItWithSamePpl.CheckBox.Check();
            dupModal.SaveAndWait("Duplicate");
            userReport.WaitDomLoad();
            userReport.GetEditModal();
            reportModal.GetSharingTab();

            StringAssert.AreEqualIgnoringCase(
                admin2.Name,
                reportModal.ViewOnlySelect.GetSelectedText(),
                "Expected: duplicate report is shared to Admin_2");

            #endregion Duplicate/Delete Options
        }
    }
}
