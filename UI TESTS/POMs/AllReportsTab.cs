using System.Linq;
using ServiceTitan.Testing.Web.Constants;
using ServiceTitan.Testing.Web.Framework;
using ServiceTitan.Testing.Web.Pages.Reports.Custom.Scheduling;
using ServiceTitan.UITests.Controls;

namespace ServiceTitan.UITests.Pages.Reports.Redesign
{
    public class AllReportsTab : ReportsTab<ReportCard>
    {
        public SelectDropdown CategoriesSelect =>
            Query.Css(".qa-reports-categories").FirstDomNode(q => new SelectDropdown(q, itemCss:".qa-dropdown-list-item label"));
        public SelectDropdown CreatedBySelect =>
            Query.Css(".qa-created-by").FirstDomNode(q => new SelectDropdown(q, itemCss:".qa-dropdown-list-item label"));

        internal void DeleteReport(string name)
        {
            Cards
                .GetRowByText(name)
                .GetDeleteModal()
                .ClickButtonByText("Delete");
        }

        /// <summary>
        /// Looks for the specified report, clicks 'schedule' button and opens specified scheduling wizard type: simple or flexible
        /// </summary>
        /// <param name="reportName">Name of the report to be scheduled</param>
        /// <param name="type">Scheduling report type: Simple or Flexible</param>
        /// <param name="firstTime">Indicates whether a certain report is being scheduled for the first time
        /// and the tenant yet has no scheduled reports of the same type</param>
        /// <returns></returns>
        public DetailsStepPage StartScheduling(
            string reportName = ReportingTemplates.Mpf,
            SchedulingReportTypes type = SchedulingReportTypes.Simple, bool firstTime = true)
        {
            var reportCard = Cards.First(c => c.ReportLink.GetText() == reportName);

            return firstTime
                ? reportCard.ScheduleBtn
                    .ClickForOpen<SelectReportTypePage>()
                    .Cards[(int)type]
                    .ClickForOpen<DetailsStepPage>()
                : reportCard.ScheduleBtn
                    .ClickForOpen<ScheduledReportsModal>()
                    .ScheduleButton
                    .ClickForOpen<SelectReportTypePage>()
                    .Cards[(int)type]
                    .ClickForOpen<DetailsStepPage>();
        }

        public AllReportsTab(DomContext context)
            : base(context)
        {
            RootQuery = context.Css(".qa-all-reports-section");
        }
    }
}
