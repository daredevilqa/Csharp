using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using ServiceTitan.Model;
using ServiceTitan.Services;
using ServiceTitan.Testing.Web.Constants;
using ServiceTitan.Testing.Web.Controls;
using ServiceTitan.Testing.Web.DataGenerator;
using ServiceTitan.Testing.Web.Entities;
using ServiceTitan.Testing.Web.Framework;
using ServiceTitan.Testing.Web.Helpers;
using ServiceTitan.Testing.Web.Pages.Reports.Custom;
using ServiceTitan.UITests.Controls;
using ServiceTitan.UITests.DataGenerator;
using ServiceTitan.UITests.Entities;
using ServiceTitan.UITests.Helpers;
using ServiceTitan.UITests.Pages.Reports;
using ServiceTitan.UITests.Pages.Reports.Redesign;

namespace ServiceTitan.UITests.Tests.Reporting
{
    public class QA3751_CustomReportsBasicDatasetsTest : Qa3751TestBase
    {
        #region Test Cases

        internal static IEnumerable<TestCaseData> TestCases()
        {
            var result = new[] {
                new TestCaseData(new Action(() => browser.CreateReport<BusinessUnitPerformanceReport>()))
                    .SetName("QA3751_Validate_BusinessUnitPerformanceReport"),

                new TestCaseData(new Action(() => browser.CreateReport<EstimatesReport>()))
                    .SetName("QA3751_Validate_EstimatesReport"),

                new TestCaseData(new Action(() => browser.CreateReport<TechnicianPerformanceReport>()))
                    .SetName("QA3751_Validate_TechnicianPerformanceReport"),

                new TestCaseData(new Action(() => browser.CreateReport<RecurringServicesReport>()))
                    .SetName("QA3751_Validate_RecurringServicesReport"),

                new TestCaseData(new Action(() => browser.CreateReport<MembershipsReport>()))
                    .SetName("QA3751_Validate_MembershipsReport"),

                new TestCaseData(new Action(() => browser.CreateReport<EquipmentReport>()))
                    .SetName("QA3751_Validate_EquipmentReport"),

                new TestCaseData(new Action(() => browser.CreateReport<ProjectJobCostingReport>()))
                    .SetName("QA3751_Validate_ProjectJobCostingReport"),

                new TestCaseData(new Action(() => browser.CreateReport<CustomersReport>()))
                    .SetName("QA3751_Validate_CustomersReport"),

                new TestCaseData(new Action(() => browser.CreateReport<TimesheetsReport>()))
                    .SetName("QA3751_Validate_TimesheetsReport"),

                new TestCaseData(new Action(() => browser.CreateReport<InvoicesCustomReport>()))
                    .SetName("QA3751_Validate_InvoicesCustomReport"),

                new TestCaseData(new Action(() => browser.CreateReport<JobsCustomReport>()))
                    .SetName("QA3751_Validate_JobsCustomReport"),

                new TestCaseData(new Action(() => browser.CreateReport<InvoiceItemsReport>()))
                    .SetName("QA3751_Validate_InvoiceItemsReport"),

                new TestCaseData(new Action(() => browser.CreateReport<InvoiceItemsByTechnicianReport>()))
                    .SetName("QA3751_Validate_InvoiceItemsByTechnicianReport"),

                new TestCaseData(new Action(() => browser.CreateReport<MasterPayFileReport>()))
                    .SetName("QA3751_Validate_MasterPayFileReport"),

                new TestCaseData(new Action(() => browser.CreateReport<CallsReport>()))
                    .SetName("QA3751_Validate_CallsReport"),

                new TestCaseData(new Action(() => browser.CreateReport<PaymentsReport>()))
                    .SetName("QA3751_Validate_PaymentsReport"),

                new TestCaseData(new Action(() => browser.CreateReport<OfficePerformanceReport>()))
                    .SetName("QA3751_Validate_OfficePerformanceReport"),

                new TestCaseData(new Action(() => browser.CreateReport<OfficeAuditTrailReport>()))
                    .SetName("QA3751_Validate_OfficeAuditTrailReport")
            };
            return result.OrderBy(t => t.TestName);
        }

        #endregion Test Cases

        [TestCaseSource(nameof(TestCases))]
        [NonParallelizable]
        [Description("https://servicetitan.atlassian.net/browse/QA-3751")]
        [Category(TestCategories.Reporting)]
        public void QA3751_CustomReportsBasicDatasetsTest_TS(Action createReport)
        {
            createReport();

            var report = browser.Query.FirstDomNode(q => new ReportPage(q));
            var warning = browser.Query.FirstDomNode(q => new ToastWarning(q));
            var criteria = new ReportFilterCriteria {
                From = DateTime.Today.AddDays(-30), //To get more results in reports output
                To = DateTime.Today.AddDays(30) //to have recurring services results
            };

            //Check that the report is created and displayed
            Assert.IsTrue(report.ExistsAndDisplayed, "Report is not created/displayed");

            //Check for Application Error warning
            Assert.IsFalse(warning.Exists, "Report created but throws an application error");

            //Run the report and verify it pulls results and there are no app errors
            if (criteria.From != null) {
                report.From.SetDate((DateTime)criteria.From);
            }
            if (criteria.To != null) {
                report.To.SetDate((DateTime)criteria.To);
            }

            report.RunButton.Click();
            report.WaitDomLoad();

            Assert.IsFalse(warning.Exists, "Report throws an application error when pulling results");

            //exclude MPF from being asserted for pulling results since it needs a million of preconditions
            if (report.GetText().Contains(ReportingTemplates.Mpf)) {
                return;
            }
            Assert.IsTrue(
                report.ResultsTable.Any(r => r.ExistsAndDisplayed),
                "Report results table doesn't contain any rows");
        }
    }

    public class Qa3751TestBase : TestBase
    {
        protected static Browser browser;

        public override Task SetUp()
        {
            return Task.CompletedTask;
        }

        public override Task TearDown()
        {
            ScreenShotOnError();

            if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed) {
                browser.OpenPageFromNavBar<ReportsMainPage>().FullRefreshPage();
            }

            return Task.CompletedTask;
        }

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            var sw = Stopwatch.StartNew();
            try {
                Trace.WriteLine("Start local OneTime SetUp");
                Runner = Settings.Enviroment.Runner;
                RunnerInfo = await Runner.RunAsync();
                if (Runner.IsNeedCustomSetup) {
                    await CustomSetUp();
                }
                ExecuteHelper.SetExecuteHelper(30, Settings.TestConfig.DefaultTrySeconds);
            }
            catch (Exception e) {
                Console.WriteLine($"Error in SetUp: {e.Message}");
                throw;
            }

            TempLog.WriteLine("UITestFixtureSetUp", sw.Elapsed);

            #region Feature Gates

            this.SetConfigurationProperty(RootHub.ConfigurationProperties().Reports20, true);
            this.SetConfigurationProperty(RootHub.ConfigurationProperties().Reports20Redesign, true);

            #endregion

            #region Necessary Data For Certain DataSources

            var customerEnt = new CustomerEntity("QA3751");

            var recSvcType = new RecurringServiceTypeEntity {
                Name = "Type: Continuous Weekly",
                RecurrenceInterval = 1,
                RecurrenceType = "Weekly",
                DurationType = "Continuous"
            };
            var recSvc = new RecurringServiceEntity {
                DateFrom = DateTime.Today,
                RecurringServiceName = "Clean the toilet",
                Name = recSvcType.Name
            };
            var membershipType = new MembershipTypeEntity {
                Name = "Reporting Smoke",
                Duration = 0,
                NewRecurringServiceEntity = new[] { recSvcType }
            };

            // Tech with non-zero HourlyRate and completed job should exist in order for MPF report to pull any results
            var tech1Entity = new TechnicianEntity {
                Name = "QA3751_Tech1",
                Biography = "Biography",
                Memo = "Memo",
                HourlyRate = 10
            };

            await ExecuteDataWorkActionAsync(
                async hub => {
                    await TechniciansWorker.CreateTechnicianAsync(hub, tech1Entity);
                    CustomersWorker.CreateCustomerWithLocation(hub, customerEnt);
                    await MembershipsWorker.CreateMembershipTypeForCustomerAsync(
                        hub,
                        membershipType,
                        new MembershipEntity {
                            DateFrom = DateTime.Today,
                            DateTo = DateTime.Today.AddMonths(1),
                            RecurringServiceAction = "Single"
                        },
                        customerEnt.Name,
                        recSvc);
                });

            // Add an estimate for corresponding dataset since the test tenant has no estimates by default
            var estimateEntity = new EstimateEntity {
                FollowUpDate = DateTime.Today.AddDays(2),
                Subtotal = 1000,
                Tax = 100,
                Name = "QA3751_Estimate",
                Summary = "QA3751_Estimate Summary"
            };
            var jobEntity = await UISimpleCreator.CreateSimpleJobAsync(this, tech1Entity.Name, arrive: true);

            // Equipment
            var equipment = new EquipmentEntity("QA3751") { Type = "Smoke_Equip_Type" };

            await ExecuteDataWorkActionAsync(
                async hub => {
                    var tech1 = hub.GetSession().Query.All<Technician>().First(t => t.Name == tech1Entity.Name);
                    var job1 = hub.GetSession().Query.All<Job>().First(j => j.Number == jobEntity.JobNumber);
                    EstimatesWorker.AddEstimate(hub, estimateEntity, jobEntity.JobId);
                    PricebookWorker.CreateEquipmentType(hub, equipment.Type, 1);
                    PricebookWorker.CreateEquipment(hub, equipment);
                    await InvoicesWorker.AddOrUpdateEquipmentToInvoiceAsync(hub, equipment, job1.Invoice.Id, 0);
                    await JobsWorker.CompleteJobAsync(hub, job1, tech1, job1.Start.AddHours(2));
                });

            #endregion Necessary Data For Certain DataSources

            Trace.WriteLine($"Stop OneTime SetUp. It took {sw.Elapsed}");
            Trace.WriteLine("Opening browser...");

            browser = OpenDriver();
            browser.Login();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            var sw = Stopwatch.StartNew();

            if (!browser.IsClosed) {
                browser.BeforeClose(TestContext.CurrentContext);
                browser.CloseBrowser();
            }

            await Runner.StopAsync(RunnerInfo);
            TempLog.WriteLine("UITestFixtureTearDown", sw.Elapsed);
        }
    }

    internal class ReportPage : DomNodeWrapper
    {
        public DatePicker From =>
            Query.Css(".custom-report-filter:has([name='From'])")
                .FirstDomNode(q => new DatePicker(q, ".calendar-control"));

        public DatePicker To =>
            Query.Css(".custom-report-filter:has([name='To'])")
                .FirstDomNode(q => new DatePicker(q, ".calendar-control"));

        public DomNodeWrapper RunButton => Query.Css(".ui.menu button[data-bind*='click: Run']").FirstDomNode();

        public DomNodeWrapperList<Row> ResultsTable =>
            new DomNodeWrapperList<Row>(() => Query.Css("[role='grid'] tbody"));

        public ReportPage(DomContext context)
            : base(context)
        {
            RootQuery = Query.Css(".custom-report");
        }
    }
}
