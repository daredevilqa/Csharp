using NUnit.Framework;
using ServiceTitan.Testing.Web.Controls;
using ServiceTitan.Testing.Web.Entities;
using ServiceTitan.Testing.Web.Framework;
using ServiceTitan.UITests.Controls;

namespace ServiceTitan.UITests.Pages.Reports.Redesign
{
    public class ScheduledTab : ReportsTab<ScheduledReportCard>
    {
        public DomNodeWrapper Active => Query.Css(".ButtonToggle__Item .Button").ContainsTextDomNode("Active");
        public DomNodeWrapper Inactive => Query.Css(".ButtonToggle__Item .Button").ContainsTextDomNode("Inactive");
        public DomNodeWrapper Pending => Query.Css(".ButtonToggle__Item .Button").ContainsTextDomNode("Pending");
        public SelectDropdown SortDropdown =>
            Query.Css(".qa-sort-scheduled-reports").FirstDomNode(q => new SelectDropdown(q));
        public DomNodeWrapper LearnMore => Query.Css(".Button--outline").ContainsTextDomNode("Learn More");
        public DomNodeWrapper ConvertedReportsBanner => Query.Css(".qa-converted-banner-review").FirstDomNode();
        public Button ActivatePendingReportBtn =>
            Query.Css(".qa-activate-pending-report-button").FirstDomNode(q => new Button(q));
        public Button DeactivatePendingReportBtn =>
            Query.Css(".qa-deactivate-pending-report-button").FirstDomNode(q => new Button(q));

        public void SortBy(SchedulingSortOptions option)
        {
            switch (option) {
                case SchedulingSortOptions.LastUpdated:
                    SortDropdown.SelectItem("Last Updated");
                    break;
                case SchedulingSortOptions.Subject:
                    SortDropdown.SelectItem("Subject");
                    break;
                case SchedulingSortOptions.Created:
                    SortDropdown.SelectItem("Created");
                    break;
                case SchedulingSortOptions.NextSendDate:
                    SortDropdown.SelectItem("Next Send Date");
                    break;
                case SchedulingSortOptions.Legacy:
                    SortDropdown.SelectItem("Legacy");
                    break;
            }
        }

        public void OpenActiveTab()
        {
            ExecuteHelper.ExecuteAgainIfError(() => {
                Active.Click(waitForClickability: true);
                Assert.True(Active.ContainsAttribute("solid"),
                    "Expected: active 'Active' tab contains 'solid' attribute");
            });
        }

        public void OpenInactiveTab()
        {
            ExecuteHelper.ExecuteAgainIfError(() => {
                Inactive.Click(waitForClickability: true);
                Assert.True(Inactive.ContainsAttribute("solid"),
                    "Expected: active 'Inactive' tab contains 'solid' attribute");
            });
        }

        public void OpenPendingTab()
        {
            ExecuteHelper.ExecuteAgainIfError(() => {
                Pending.Click();
                Assert.True(Pending.ContainsAttribute("solid"),
                    "Expected: active 'Pending' tab contains 'solid' attribute");
            });
        }

        public ScheduledTab(DomContext context)
            : base(context)
        {
            RootQuery = context.Css(".qa-scheduled-reports-section");
        }
    }
}
