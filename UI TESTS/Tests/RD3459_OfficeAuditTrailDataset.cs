using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceTitan.Configuration;
using ServiceTitan.Model;
using ServiceTitan.Services;
using ServiceTitan.Testing.Web.Framework;
using ServiceTitan.Testing.Web.DataGenerator;
using ServiceTitan.Testing.Web.Entities;
using ServiceTitan.Testing.Web.Helpers;
using ServiceTitan.Testing.Web.Pages.Reports.Custom;
using ServiceTitan.UITests.Entities;
using ServiceTitan.UITests.Helpers;
using ServiceTitan.UITests.Pages;
using ServiceTitan.UITests.Pages.Admin.PartnerPortal;
using ServiceTitan.UITests.Pages.Job;
using ServiceTitan.Util;

namespace ServiceTitan.UITests.Tests.Reporting.Datasets
{
    public class RD3459_OfficeAuditTrailDataset : TestBase
    {
        #region Constants

        public const string InboundCalls = "Inbound Calls",
            OutboundCalls = "Outbound Calls",
            JobUpdates = "Job Updates",
            EstimateUpdates = "Estimate Updates",
            InvoiceUpdates = "Invoice Updates";

        public const string DateKpi = "Date",
            TimeKpi = "Time",
            NameKpi = "Name",
            ActionTypeKpi = "Action Type",
            ActionPerformedKpi = "Action Performed",
            JobIdKpi = "Job ID",
            CallIdKpi = "Call ID";

        #endregion Constants

        [Test]
        [Category(TestCategories.Reporting)]
        [Description("https://servicetitan.atlassian.net/browse/RD-3459" +
                     "https://servicetitan.atlassian.net/browse/RD-3458")]
        public async Task RD3459_RD3458_OfficeAuditTrailDataset_TS()
        {
            #region Setup

            this.SetConfigurationProperty(RootHub.ConfigurationProperties().Reports20, true);
            this.SetConfigurationProperty(RootHub.ConfigurationProperties().Reports20Redesign, true);
            this.SetConfigurationProperty(RootHub.ConfigurationProperties().Reports20Scheduler, true);
            this.SetConfigurationProperty(RootHub.ConfigurationProperties().Reports20ReportEngine, true);

            var adminModel = new EmployeeEntity {
                Name = "Admin_RD3459",
                Role = BuiltInUserRole.Admin,
                UserAccessEntity = new UserAccessEntity { Username = $"Admin_{UserName}", Password = Password }
            };
            var inactiveEmployeeModel = new EmployeeEntity {
                Name = "Inactive_Employee",
                Role = BuiltInUserRole.Admin,
                UserAccessEntity = new UserAccessEntity { Username = $"Inactive_{UserName}", Password = Password }
            };
            var buModel = new BusinessUnitEntity("RD3459");
            var jobTypeModel = new JobTypeEntity("RD3459");
            var jobModel = new JobExtEntity {
                JobType = jobTypeModel.Name,
                Priority = "Normal",
                BusinessUnit = buModel.Name,
                Summary = "RD3459 Job",
                ScheduledDate = DateTime.Today.AddDays(1),
                Customer = new CustomerEntity { Name = "QACustomerWithLocationOnly" },
                Location = "QACustomerWithLocationOnly"
            };
            long inboundCallId = 0;
            long outboundCallId = 0;
            var activeEmployees = new List<string>();
            var activeJobTypes = new List<string>();

            var portal = new PartnerPortalEntity {
                Name = "RD3459_" + RandomGenerator.Next(),
                Url = "RD3459" + RandomGenerator.Next(),
                OwnerName = "Owner_" + RandomGenerator.Next(),
                UserName = "User_" + RandomGenerator.Next(),
                Phone = TitanSettings.Telecom.Constants.TestPhoneNumbers.Number1,
                Email = TitanSettings.Email.Success,
                Password = Password,
                CallCenterQA = true,
                BypassTenantAccess = true
            };
            var portalUser = new PartnerPortalUserEntity {
                Name = "RD3459_PortalUser",
                UserName = "RD3459_PortalUser",
                Phone = TitanSettings.Telecom.Constants.TestPhoneNumbers.Number2,
                Email = TitanSettings.Email.Success,
                Password = Password
            };
            var globalUser = AdminHelper.GlobalUser;

            await ExecuteDataWorkActionAsync(
                async hub => {
                    var user = await EmployeesWorker.CreateUserWithEmployeeAsync(hub, adminModel);
                    adminModel.Id = user.Employee.Id;
                    var inactiveUser = await EmployeesWorker.CreateUserWithEmployeeAsync(hub, inactiveEmployeeModel);
                    inactiveUser.Employee.Active = false;

                    var fromPhone = await PhonesWorker.GetValidPhoneNumberAsync("747");
                    var toPhone = await PhonesWorker.GetValidPhoneNumberAsync("747");
                    var outboundCall = CallsWorker.CreateCall(
                        hub,
                        null,
                        user,
                        null,
                        DateTime.Now.Add(-TimeSpan.FromMinutes(5)),
                        false,
                        CallType.Booked.ToString(),
                        fromPhone,
                        toPhone);
                    outboundCall.SetDirection(CallDirection.Outbound);
                    outboundCallId = outboundCall.Id;

                    var inboundCall = CallsWorker.CreateCall(
                        hub,
                        null,
                        user,
                        null,
                        DateTime.Now.Add(-TimeSpan.FromMinutes(2)),
                        false,
                        CallType.Unbooked.ToString(),
                        fromPhone,
                        toPhone);
                    inboundCall.SetDirection(CallDirection.Inbound);
                    inboundCallId = inboundCall.Id;

                    buModel.Id = BusinessUnitsWorker.CreateBusinessUnit(hub, buModel).Id;
                    JobTypesWorker.CreateJobType(hub, jobTypeModel);
                    var job = await JobsWorker.CreateJobsAsync(hub, jobModel, outboundCall);
                    jobModel.Id = job.Id;

                    activeEmployees = hub.GetSession().Query.All<Employee>().Active().Select(e => e.Name).ToList();
                    activeJobTypes = hub.GetSession().Query.All<JobType>().Active().Select(jt => jt.Name).ToList();
                });

            #endregion Setup

            var browser = OpenDriver(options: BrowserOptions.Create().SetDefaultOptions());
            browser.Login(adminModel.UserAccessEntity.Password, adminModel.UserAccessEntity.Username);

            //Validate a report that uses Office Audit Trail can be created
            var report = browser.CreateReport<ReactOfficeAuditTrailReport>();
            report.Run();

            //Validate a report that uses Office Audit Trail dataset has the following top-level filters:
            //Employee, Job Business Unit, Job Type, Action Type, From, To, Employee Business Unit (optional)
            //Validate “Employee Business Unit” top-level filter is hidden if none of the employees is assigned to any BU
            var filter = report.Filter;

            Assert.True(filter.Employee.ExistsAndDisplayed, "Expected: 'Employee' filter is present");
            Assert.True(filter.JobBusinessUnit.ExistsAndDisplayed, "Expected: 'JobBusinessUnit' filter is present");
            Assert.True(filter.JobType.ExistsAndDisplayed, "Expected: 'JobType' filter is present");
            Assert.True(filter.ActionType.ExistsAndDisplayed, "Expected: 'ActionType' filter is present");
            Assert.True(filter.DateRangePicker.ExistsAndDisplayed, "Expected: 'DateRangePicker' filter is present");
            Assert.False(
                filter.EmployeeBusinessUnit.ExistsAndDisplayed,
                "Expected: 'EmployeeBusinessUnit' filter is hidden if none of the employees is assigned to any BU");

            //Validate “Employee” top-level filter contains only active employees
            CollectionAssert.AreEquivalent(
                activeEmployees,
                filter.Employee.GetAllOptionsForSelect(true),
                "Expected: 'Employee' top-level filter contains only active employees");

            //Validate “Employee Business Unit” filter is not hidden if any of the employees is assigned to any BU
            await ExecuteDataWorkActionAsync(
                async hub => {
                    var editEmployeeModel = hub.GetEmployeeService().GetEmployee(adminModel.Id);
                    editEmployeeModel.BusinessUnitId = buModel.Id;
                    await hub.GetEmployeeService().UpdateEmployeeAsync(editEmployeeModel);
                });
            report.RefreshPage();

            Assert.True(
                filter.EmployeeBusinessUnit.ExistsAndDisplayed,
                "Expected: 'EmployeeBusinessUnit' filter is not hidden if any of the employees is assigned to any BU");

            //Validate “Employee Business Unit” filter contains only BUs that are assigned to the employees
            CollectionAssert.AreEquivalent(
                new[] { buModel.Name },
                filter.EmployeeBusinessUnit.GetAllOptionsForSelect(true),
                "Expected: 'Employee Business Unit' filter contains only BUs that are assigned to the employees");

            //Validate “Job Type” top-level filter contains all active job types
            CollectionAssert.AreEquivalent(
                activeJobTypes,
                filter.JobType.GetAllOptionsForSelect(true),
                "Expected: 'Job Type' top-level filter contains all active Job Types");

            //Validate the “Action Type” top-level filter contains the following values:
            //Inbound Calls, Outbound Calls, Job Updates, Estimate Updates, Invoice Updates
            CollectionAssert.AreEquivalent(
                new[] { InboundCalls, OutboundCalls, JobUpdates, EstimateUpdates, InvoiceUpdates },
                filter.ActionType.GetAllOptionsForSelect(true),
                "'ActionType' top-level filter options do not match");

            //Validate all KPIs are enabled by default
            var drawer = report.GetEditColumnsDrawer();

            CollectionAssert.AreEquivalent(
                new[] { DateKpi, TimeKpi, NameKpi, ActionTypeKpi, ActionPerformedKpi, JobIdKpi, CallIdKpi },
                drawer.Columns.Select(c => c.Label.GetText()).ToList(),
                "KPI names do not match");

            foreach (var column in drawer.Columns) {
                Assert.True(column.Kpi.CheckBox.Checked, "Expected: all KPIs are enabled by default");
            }

            drawer.CloseDrawer();

            //Validate “Employee” filter filters the report by names of chosen employees
            report.Filter.Employee.SelectItem(adminModel.Name);
            report.Run();

            foreach (var row in report.Table) {
                if (row.GetText().Contains("None")) {
                    break;
                }
                StringAssert.Contains(
                    adminModel.Name,
                    row.GetText(),
                    $"Expected: report should be filtered by {adminModel.Name} employee");
            }
            report.Filter.Employee.DeselectHiddenOption(adminModel.Name);

            //Validate “Job Type” filter filters the report by actions taken against selected job types
            report.Filter.JobType.SelectItem(jobTypeModel.Name);
            report.Run();

            foreach (var row in report.Table) {
                if (row.GetText().Contains("None")) {
                    break;
                }
                StringAssert.Contains(
                    jobModel.Id.ToString(),
                    row.GetText(),
                    "Expected: report should be filtered by actions taken against the selected job type");
            }
            report.Filter.JobType.DeselectHiddenOption(jobTypeModel.Name);

            //Validate “Action Type” filter filters the report by selected action types
            report.Filter.ActionType.SelectItem(OutboundCalls);
            report.Run();

            foreach (var row in report.Table) {
                if (row.GetText().Contains("None")) {
                    break;
                }
                StringAssert.Contains(
                    OutboundCalls,
                    row.GetText(),
                    $"Expected: report should be filtered by {OutboundCalls} action");
            }
            report.Filter.ActionType.DeselectHiddenOption(OutboundCalls);

            //Validate “Employee Business Unit” filter filters the report by employees which are assigned to the chosen BUs
            report.Filter.EmployeeBusinessUnit.SelectItem(buModel.Name);
            report.Run();

            foreach (var row in report.Table) {
                if (row.GetText().Contains("None")) {
                    break;
                }
                StringAssert.Contains(
                    adminModel.Name,
                    row.GetText(),
                    $"Expected: report should be filtered by {adminModel.Name} employee that's assigned to {buModel.Name}");
            }

            //Validate “Action Performed” KPI shows the following text for inbound calls
            //Receive - {Employee} received a call
            //Finish - {Employee} finished an inbound call
            Assert.True(
                report.Table.ExistsItemByText($"{adminModel.Name} received a call"),
                $"{ActionPerformedKpi} text doesn't match for receiving a call action");
            Assert.True(
                report.Table.ExistsItemByText($"{adminModel.Name} finished an inbound call"),
                $"{ActionPerformedKpi} text doesn't match for finishing an inbound call action");

            //Validate “Action Performed” KPI shows the following text for outbound calls
            //Creation - {Employee} made a call
            //Finish - {Employee} finished an outbound call
            Assert.True(
                report.Table.ExistsItemByText($"{adminModel.Name} made a call"),
                $"{ActionPerformedKpi} text doesn't match for making a call action");
            Assert.True(
                report.Table.ExistsItemByText($"{adminModel.Name} finished an outbound call"),
                $"{ActionPerformedKpi} text doesn't match for finishing an outbound call action");

            //Validate the call playback window opens when a user clicked on any Action Performed record that is connected with a call
            var cell = report.Table.GetRowByText(InboundCalls).GetCellByKendoHeader(ActionPerformedKpi, parentLvl: 6);
            var playBackModal = browser.Query.FirstDomNode(q => new CallPlaybackModal(q));

            Assert.DoesNotThrow(
                () => cell.Link.ClickForOpenElement(playBackModal),
                "Expected: call playback modal opens when a user clicked on any Action Performed record that's connected with a call");

            playBackModal.Close.ClickWithWaitDisappear();

            //Validate 'Call ID' KPIs match
            cell = report.Table.GetRowByText(InboundCalls).GetCellByKendoHeader(CallIdKpi, parentLvl: 6);

            StringAssert.AreEqualIgnoringCase(
                inboundCallId.ToString(),
                cell.GetText(),
                "Inbound call ID doesn't match");

            cell = report.Table.GetRowByText(OutboundCalls).GetCellByKendoHeader(CallIdKpi, parentLvl: 6);

            StringAssert.AreEqualIgnoringCase(
                outboundCallId.ToString(),
                cell.GetText(),
                "Outbound call ID doesn't match");

            //Validate “Call Id” KPI opens the call playback window upon clicking on the ID
            Assert.DoesNotThrow(
                () => cell.Link.ClickForOpenElement(playBackModal),
                "Expected: call playback modal opens when a user clicked on Call ID");

            playBackModal.Close.ClickWithWaitDisappear();

            //Validate “Job Id” KPI contains links to the jobs
            cell = report.Table.GetRowByText(OutboundCalls).GetCellByKendoHeader(JobIdKpi, parentLvl: 6);
            JobDetailPage jobPage = null;

            Assert.DoesNotThrow(
                () => jobPage = cell.Link.ClickLinkForOpenInNewTab<JobDetailPage>(false),
                "Expected: “Job Id” KPI contains links to the jobs");

            //Validate the report contains logs about Jobs, Invoices, Estimates
            jobPage.History.AddNote("lmao", false);
            var invoicePage = jobPage.OpenInvoicePage();
            invoicePage.GetInvoiceDetailPage().FillInvoiceDetails(new InvoiceEntity { Summary = "RD3459" }).Save();
            invoicePage.OpenAddEstimate().SetData(new EstimateEntity()).Save();
            browser.CloseTab(1);
            browser.OpenTab(0);
            report.Run();

            cell = report.Table.GetRowByText(JobUpdates).GetCellByKendoHeader(ActionPerformedKpi, parentLvl: 6);
            StringAssert.AreEqualIgnoringCase(
                $"{adminModel.Name} added a note",
                cell.GetText(),
                $"Expected:{ActionPerformedKpi} cell shows a log about adding a note to the job");

            cell = report.Table.GetRowByText(InvoiceUpdates).GetCellByKendoHeader(ActionPerformedKpi, parentLvl: 6);
            StringAssert.AreEqualIgnoringCase(
                $"{adminModel.Name} edited the invoice summary",
                cell.GetText(),
                $"Expected:{ActionPerformedKpi} cell shows a log about editing the invoice summary action");

            cell = report.Table.GetRowByText(EstimateUpdates).GetCellByKendoHeader(ActionPerformedKpi, parentLvl: 6);
            StringAssert.AreEqualIgnoringCase(
                $"{adminModel.Name} created an estimate",
                cell.GetText(),
                $"Expected:{ActionPerformedKpi} cell shows a log about creating of an estimate");

            browser.SignOut();

            #region RD-3458: Include Partner Portal Users into Office Audit Trail

            browser.OpenUrl(Settings.App.AdminUrl);
            browser.Login(globalUser.Password, globalUser.Name);

            var tenantsPage = browser.Query.FirstDomNode(r => new TenantsPage(r));
            var partnerPortalOverviewPage = tenantsPage.TopNavbar.OpenPartnerPortalOverviewPage();

            partnerPortalOverviewPage.CreatePortal(portal);

            var rowBypassAccess = partnerPortalOverviewPage.PortalsTable.GetRowByText(portal.Name);
            var portalPage = rowBypassAccess.PortalUrl.ClickForOpen<PortalPage>();

            portalPage.AssignTenants(new[] { UserName });
            portalPage.AddUser(portalUser);

            browser.SignOut();
            browser.Login(portalUser.Password, portalUser.UserName, false);

            portalPage.WaitDomLoad();
            portalPage.TenantSwitcher.Click();
            portalPage.WaitDomLoad();
            portalPage.SearchForTenant.SetValue(UserName);

            var tenantRow = portalPage.TenantsList.GetRowByText(UserName);
            tenantRow.Impersonate.ClickLinkForOpenInNewTab<DashboardPage>();

            #endregion RD-3458: Include Partner Portal Users into Office Audit Trail
        }
    }
}
