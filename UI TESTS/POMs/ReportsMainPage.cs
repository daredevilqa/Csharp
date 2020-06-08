using ServiceTitan.Testing.Web.Framework;

namespace ServiceTitan.UITests.Pages.Reports.Redesign
{
    public class ReportsMainPage : MainPage
    {
        public override string PageName => "Reports";
        public DomNodeWrapper NavBar => Query.Css(".qa-left-navbar").FirstDomNode();
        public DomNodeWrapper HomeBtn => NavBar.Query.Css("div:contains(Home)").LastDomNode();
        public DomNodeWrapper AllReportsBtn => NavBar.Query.Css("div:contains(All Reports)").LastDomNode();
        public DomNodeWrapper ScheduledBtn => NavBar.Query.Css("div:contains(Scheduled)").LastDomNode();
        public DomNodeWrapper LegacyReportsLink => Query.Css("a.qa-legacy-reports").FirstDomNode();
        public HomeTab HomeTab => Query.FirstDomNode(q => new HomeTab(q));

        internal HomeTab OpenHomeTab()
        {
            return HomeTab.ExistsAndDisplayed
                ? HomeTab
                : HomeBtn.ClickForOpen<HomeTab>();
        }

        internal AllReportsTab OpenAllReportsTab()
        {
            return AllReportsBtn.ClickForOpen<AllReportsTab>();
        }

        internal ScheduledTab OpenScheduledTab()
        {
            return ScheduledBtn.ClickForOpen<ScheduledTab>();
        }

        internal LegacyReportsPage OpenLegacyReports()
        {
            return LegacyReportsLink.ClickForOpen<LegacyReportsPage>();
        }

        public ReportsMainPage(DomContext context)
            : base(context)
        {
            RootQuery = context.Css(".qa-reporting-home-page");
        }
    }
}
