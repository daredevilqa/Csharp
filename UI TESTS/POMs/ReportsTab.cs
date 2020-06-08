using NUnit.Framework;
using OpenQA.Selenium;
using ServiceTitan.Testing.Web.Constants;
using ServiceTitan.Testing.Web.Controls;
using ServiceTitan.Testing.Web.Framework;

namespace ServiceTitan.UITests.Pages.Reports.Redesign
{
    public abstract class ReportsTab<TCard> : DomNodeWrapper
        where TCard : DomNodeWrapper
    {
        public DomNodeWrapper SearchBox => Query.Css("input[placeholder='Search']").FirstDomNode();
        public DomNodeWrapper ClearSearchIcon => Query.Css(".qa-clear-search-results").FirstDomNode();
        public Button CreateReport => Query.Css(".qa-create-report").FirstDomNode(q => new Button(q));
        public Button GridView => Query.Css("[aria-label='Grid view']").FirstDomNode(q => new Button(q));
        public Button ListView => Query.Css("[aria-label='List view']").FirstDomNode(q => new Button(q));
        public virtual DomNodeWrapperList<TCard> Cards =>
            new DomNodeWrapperList<TCard>(() => Query.Css(".Card"));
        public virtual DomNodeWrapperList<TCard> CardsListView =>
            new DomNodeWrapperList<TCard>(() => Query.Css(".qa-listview-report-cell"));
        public DomNodeWrapperList<DomNodeWrapper> Categories =>
            new DomNodeWrapperList<DomNodeWrapper>(() => Query.Css(".qa-category-name"));
        public DomNodeWrapperList<CardsSection> CardsSectionsPerCategory =>
            new DomNodeWrapperList<CardsSection>(() => Query);

        internal void GetGridView()
        {
            GridView.Click();
            WaitDomLoad();
        }

        internal void GetListView()
        {
            ListView.Click();
            WaitDomLoad();
        }

        /// <summary>
        /// Clicks 'Create Report' button and selects specified dataset(template). Also checks whether the template
        /// is available or not or even exists
        /// </summary>
        /// <param name="dataset"></param>
        /// <returns>'Edit Report' modal object</returns>
        internal EditReportModal GetCreateReportModal(string dataset = ReportingTemplates.Mpf)
        {
            var selectTemplatePage = Browser.Query.FirstDomNode(q => new SelectTemplatePage(q));
            ExecuteHelper.ExecuteAgainIfError(delegate
                {
                    CreateReport.Click(waitForClickability: true);
                    selectTemplatePage.WaitElementByCondition(longWait: false);
                }
            );
            try {
                var template = selectTemplatePage.Templates.GetRowByText(dataset);

                if (template.ContainsAttribute("disabled")) {
                    Assert.Fail("The dataset is not yet available (greyed out) within the list of templates");
                }
                return template.ClickForOpen<EditReportModal>();
            }
            catch (NotFoundException) {
                Assert.Fail("The dataset is not found within the list of templates");
            }
            return null;
        }

        /// <summary>
        /// If the search box is empty, sets a search value into it and waits for the hints list to appear.
        /// If the search box already contains value, just clicks the box to unfold the hints list
        /// </summary>
        /// <param name="searchValue"></param>
        /// <returns>Hints list object</returns>
        internal HintsList GetHints(string searchValue = null)
        {
            var list = Query.FirstDomNode(q => new HintsList(q));

            if (string.IsNullOrEmpty(SearchBox.GetValue())) {
                SearchBox.SetValue(searchValue);
                list.WaitDomLoad();
                return list;
            }
            return SearchBox.ClickForOpenElement(list);
        }

        protected ReportsTab(DomContext context)
            : base(context) { }
    }

    public class HintsList : DomNodeWrapper
    {
        public DomNodeWrapperList<DomNodeWrapper> Hints =>
            new DomNodeWrapperList<DomNodeWrapper>(() => Query.Css(".qa-search-results-item"));
        public DomNodeWrapperList<DomNodeWrapper> Categories =>
            new DomNodeWrapperList<DomNodeWrapper>(() => Query.Css(".qa-search-results-category"));

        internal void SelectItem(string name)
        {
            var item = Hints.GetRowByText(name);
            item.ScrollToThisItem();
            item.ClickWithWaitDisappear();
        }

        public HintsList(DomContext context)
            : base(context)
        {
            RootQuery = context.Css(".qa-search-results-list" + NotDisplayNone);
        }
    }

    public class CardsSection : DomNodeWrapper
    {
        public DomNodeWrapperList<ReportCard> Cards =>
            new DomNodeWrapperList<ReportCard>(() => Query.Css(".Card"));

        public CardsSection(DomContext context)
            : base(context)
        {
            RootQuery = context.Css(".qa-cards-section");
        }
    }
}
