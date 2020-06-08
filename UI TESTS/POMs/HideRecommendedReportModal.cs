using ServiceTitan.Testing.Web.Controls;
using ServiceTitan.Testing.Web.Framework;

namespace ServiceTitan.UITests.Pages.Reports.Redesign
{
    public class HideRecommendedReportModal : NewModalDialog
    {
        public DomNodeWrapper HideReportBtn => Query.Css(".Button--blue").ContainsTextDomNode("Hide Report");
        public DomNodeWrapperList<CheckBoxOption> Options => new DomNodeWrapperList<CheckBoxOption>(() => Query);
        public DomNodeWrapper OtherOptionTextArea => Query.Css("textarea").FirstDomNode();

        public HideRecommendedReportModal(DomContext context)
            : base(context)
        { }
    }
}
