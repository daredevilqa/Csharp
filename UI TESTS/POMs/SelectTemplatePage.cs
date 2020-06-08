using ServiceTitan.Testing.Web.Controls;
using ServiceTitan.Testing.Web.Framework;

namespace ServiceTitan.UITests.Pages.Reports.Redesign
{
    public class SelectTemplatePage : DomNodeWrapper
    {
        public DomNodeWrapperList<DomNodeWrapper> Templates =>
            new DomNodeWrapperList<DomNodeWrapper>(() => Query.Css(".Card--hoverable"));
        public Button Cancel => Query.Css(":contains(Cancel)").FirstDomNode(q => new Button(q));

        public SelectTemplatePage(DomContext context)
            : base(context)
        {
            RootQuery = context.Css(".qa-select-template-page");
        }
    }
}
