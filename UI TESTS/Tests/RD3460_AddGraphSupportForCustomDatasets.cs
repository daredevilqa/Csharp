using System;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceTitan.Model;
using ServiceTitan.Module.Reporting.CustomReports.EmployeePermissions;
using ServiceTitan.Services;
using ServiceTitan.Services.Core.Models;
using ServiceTitan.Services.Core.Models.Common;
using ServiceTitan.Testing.Web.Constants;
using ServiceTitan.Testing.Web.Framework;
using ServiceTitan.Testing.Web.DataGenerator;
using ServiceTitan.Testing.Web.Entities;
using ServiceTitan.Testing.Web.Helpers;
using ServiceTitan.Testing.Web.Pages.Reports.Custom;
using ServiceTitan.UITests.Entities;
using ServiceTitan.UITests.Helpers;

namespace ServiceTitan.UITests.Tests.Reporting.Datasets
{
    public class RD3460_AddGraphSupportForCustomDatasets : TestBase
    {
        [Test]
        [Category(TestCategories.Reporting)]
        [Description("https://servicetitan.atlassian.net/browse/RD-3460")]
        public async Task RD3460_AddGraphSupportForCustomDatasets_TS()
        {
            #region Setup

            this.SetConfigurationProperty(RootHub.ConfigurationProperties().Reports20, true);
            this.SetConfigurationProperty(RootHub.ConfigurationProperties().Reports20Redesign, true);
            this.SetConfigurationProperty(RootHub.ConfigurationProperties().Reports20ReportEngine, true);

            var userModel = new EmployeeEntity {
                Name = "Csr_RD3460",
                Role = BuiltInUserRole.CSR,
                UserAccessEntity = new UserAccessEntity { Username = $"Csr_{UserName}", Password = Password }
            };
            var permissionsModel = new ReportingPermissionsEntity {
                Datasources = new [] {
                    new AccessPermissionModel {
                        Name = ReportingTemplates.YoY,
                        DisplayName = ReportingTemplates.YoY,
                        View = true,
                        Edit = true
                    },
                    new AccessPermissionModel {
                        Name = ReportingTemplates.MoM,
                        DisplayName = ReportingTemplates.MoM,
                        View = true,
                        Edit = true
                    }
                },
                Categories = new [] {
                    new AccessPermissionModel {
                        Name = ReportingCategories.Marketing,
                        DisplayName = ReportingCategories.Marketing,
                        View = true,
                        Edit = true
                    }
                }
            };

            await ExecuteDataWorkActionAsync(
                async hub => {
                    hub.SetTenantConfiguration(new CommonTenantConfiguration {
                        Common = new TenantConfigurationCommon {
                            IsDemoMode = true
                        }
                    });
                    var user = await EmployeesWorker.CreateUserWithEmployeeAsync(hub, userModel);
                    ReportsWorker.UpdateReportingPermissions(hub, user.Id, permissionsModel);
                });

            #endregion Setup

            var browser = OpenDriver(options: BrowserOptions.Create().SetDefaultOptions());
            browser.Login(userModel.UserAccessEntity.Password, userModel.UserAccessEntity.Username);

            #region YoY Report

            //Validate a report that uses Year-over-Year Analysis DS can be created
            YoYReport yoyReport = null;

            Assert.DoesNotThrow(() => yoyReport = browser.CreateReport<YoYReport>(),
                "Expected: report that uses Year-over-Year Analysis DS can be created");

            //Validate YoY report contains the following top-level filters:
            //Period, From, To, Business Unit
            Assert.True(yoyReport.Filter.Period.ExistsAndDisplayed, "Expected: Period filter is present");
            Assert.True(yoyReport.Filter.BusinessUnit.ExistsAndDisplayed, "Expected: BU filter is present");
            Assert.True(yoyReport.Filter.DateRangePicker.From.ExistsAndDisplayed, "Expected: 'From' filter is present");
            Assert.True(yoyReport.Filter.DateRangePicker.To.ExistsAndDisplayed, "Expected: 'To' filter is present");

            //Validate a report that uses Year-over-Year Analysis DS the “Period” filter contains the following values:
            //Day, Week, Month
            CollectionAssert.AreEquivalent(
                new[] { "Day", "Week", "Month" },
                yoyReport.Filter.Period.GetAllOptionsForSelect(true),
                "Available Period options do not match");

            //Validate the graph appears only when a user clicked the “Run Report” button
            Assert.False(
                yoyReport.GraphContainer.ExistsAndDisplayed,
                "Expected: graph doesn't appear unless Run button has been clicked");

            //Validate users with permissions can see graphs
            yoyReport.Filter.Period.SelectItem("Month");
            yoyReport.Run();

            Assert.True(
                yoyReport.GraphContainer.ExistsAndDisplayed,
                "Expected: graph appears once the report has been run && non-admin with a respective permission sees graph");

            //Validate values in graphs are aggregated by the selected value from the “Period” filter
            StringAssert.Contains(
                "January",
                yoyReport.GraphContainer.GetText(),
                "Expected: values in graphs are aggregated by month => 'January' string should be present");

            //Validate the graph contains line graphs with data points for the every year in the date range
            var thisYear = DateTime.Today.Year;

            StringAssert.Contains(
                $"Income {thisYear}",
                yoyReport.GraphContainer.GetText(),
                "Expected: graph contains line graphs with data points for the every year in the date range");
            StringAssert.Contains(
                $"Income {thisYear - 1}",
                yoyReport.GraphContainer.GetText(),
                "Expected: graph contains line graphs with data points for the every year in the date range");

            //Validate each line graph has a personal color
            string Query(int year) => $"g[transform]:contains('Income {year}') path";

            StringAssert.AreNotEqualIgnoringCase(
                yoyReport.GraphContainer.Query.Css(Query(thisYear)).FirstDomNode().GetCssValue("fill"),
                yoyReport.GraphContainer.Query.Css(Query(thisYear - 1)).FirstDomNode().GetCssValue("fill"),
                "Expected: each line graph has a personal color");

            //Validate the chart can be exported to PDF
            Assert.DoesNotThrow(() => yoyReport.ExportChartToPdf(), "Expected: the chart can be exported to PDF");
            browser.ClearDownloadCatalog();

            #endregion YoY Report

            #region MoM Report

            //Validate a report that uses Month-over-Month Analysis DS can be created
            var momReport = browser.CreateReport<MoMReport>();

            //Validate MoM report contains the following top-level filters:
            //Period, From, To, Business Unit
            Assert.True(momReport.Filter.Period.ExistsAndDisplayed, "Expected: Period filter is present");
            Assert.True(momReport.Filter.BusinessUnit.ExistsAndDisplayed, "Expected: BU filter is present");
            Assert.True(momReport.Filter.DateRangePicker.From.ExistsAndDisplayed, "Expected: 'From' filter is present");
            Assert.True(momReport.Filter.DateRangePicker.To.ExistsAndDisplayed, "Expected: 'To' filter is present");

            //Validate a report that uses Month-over-Month Analysis DS the “Period” filter contains the following values:
            //Day, Week
            CollectionAssert.AreEquivalent(
                new[] { "Day", "Week" },
                momReport.Filter.Period.GetAllOptionsForSelect(true),
                "Available Period options do not match");

            //Validate the graph appears only when a user clicked the “Run Report” button
            Assert.False(
                momReport.GraphContainer.ExistsAndDisplayed,
                "Expected: graph doesn't appear unless Run button has been clicked");

            //Validate users with permissions can see graphs
            momReport.Filter.Period.SelectItem("Week");
            momReport.Run();

            Assert.True(
                momReport.GraphContainer.ExistsAndDisplayed,
                "Expected: graph appears once the report has been run && non-admin with a respective permission sees graph");

            //Validate the chart can be exported to PDF
            Assert.DoesNotThrow(() => momReport.ExportChartToPdf(), "Expected: the chart can be exported to PDF");

            #endregion MoM Report
        }
    }
}
