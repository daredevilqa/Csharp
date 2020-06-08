using ServiceTitan.Testing.Web.Controls;
using ServiceTitan.Testing.Web.Framework;

namespace ServiceTitan.UITests.Pages.Reports.Redesign
{
    public class ScheduledReportsModal : NewModalDialog
    {
        public DomNodeWrapperList<CardSection> ScheduledReportsSections =>
            new DomNodeWrapperList<CardSection>(() => Query.Css(".Modal .Card"));
        public DomNodeWrapper ScheduleButton => Query.Css(".Button--blue").ContainsTextDomNode("Schedule Report");

        public ScheduledReportsModal(DomContext context)
            : base(context) { }
    }

    public class CardSection : ScheduledReportCard
    {
        public override DomNodeWrapper NextSendDate => Query.Css(".Tag + span").FirstDomNode();

        public CardSection(DomContext context)
            : base(context)
        {
            RootQuery = context.Css(".CardSection");
        }
    }
}
