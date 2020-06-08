using ServiceTitan.Testing.Web.Controls;
using ServiceTitan.Testing.Web.Framework;

namespace ServiceTitan.UITests.Pages.Reports.Redesign
{
    public class HomeTab : ReportsTab<ReportCard>
    {
        public DomNodeWrapper WelcomeBanner => Query.Css(".Banner").FirstDomNode();
        public DomNodeWrapper CloseBannerIcon => Query.Css(".Banner__close").FirstDomNode();
        public Button LearnMore => Query.Css(".Button--outline").FirstDomNode(q => new Button(q));
        public DomNodeWrapperList<ReportCard> RecommendedCards =>
            new DomNodeWrapperList<ReportCard>(() => Query.Css(".Card:has(.item:contains(Hide))"));
        public DomNodeWrapperList<ReportCard> RecommendedCardsListView =>
            new DomNodeWrapperList<ReportCard>(() => Query.Css(".qa-listview-report-cell:has(.item:contains(Hide))"));
        public MigratedReportsBanner MigratedReportsBanner => Query.FirstDomNode(q => new MigratedReportsBanner(q));

        public HomeTab(DomContext context)
            : base(context)
        {
            RootQuery = context.Css(".qa-home-page-section");
        }
    }

    public class MigratedReportsBanner : DomNodeWrapper
    {
        public Button GetStartedBtn => Query.ContainsTextDomNode(q => new Button(q), "Get Started");

        public MigratedReportsBanner(DomContext context)
            : base(context)
        {
            RootQuery = context.Css(".qa-migrated-banner");
        }
    }
}
