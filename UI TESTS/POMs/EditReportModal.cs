using ServiceTitan.Testing.Web.Controls;
using ServiceTitan.Testing.Web.Framework;
using ServiceTitan.UITests.Controls;

namespace ServiceTitan.UITests.Pages.Reports.Redesign
{
    public class EditReportModal : NewModalDialog
    {
        public DomNodeWrapper Name => Query.Css(".Input input").FirstDomNode();
        public SelectDropdown CategorySelect => Query.Css("[role='listbox']").FirstDomNode(q => new SelectDropdown(q));
        public DomNodeWrapper Description => Query.Css("textarea").FirstDomNode();
        public DomNodeWrapper Template => Query.Css("div:has(span:contains(Template))").LastDomNode();
        public DomNodeWrapper CreatedBy => Query.Css("div:has(span:contains(Created By))").LastDomNode();
        public SelectDropdown ViewOnlySelect =>
            Query.Css(".qa-sharing-view-only").FirstDomNode(q => new SelectDropdown(q));
        public SelectDropdown ViewAndEditSelect =>
            Query.Css(".qa-sharing-view-and-edit").FirstDomNode(q => new SelectDropdown(q));
        public Button SaveReport => Query.Css(".Button--blue").FirstDomNode(q => new Button(q));
        public CheckBoxOption ShareItWithSamePpl => Query.Css("[class*='sharing']").FirstDomNode(q => new CheckBoxOption(q));
        public DomNodeWrapper DuplicateWarning => Query.Css("[class*='duplicate-name']").FirstDomNode();

        /// <summary>
        /// Fills in report parameters on the Details tab
        /// </summary>
        /// <param name="name"></param>
        /// <param name="category"></param>
        /// <param name="description"></param>
        public EditReportModal EditDetails(string name = null, string category = null, string description = null)
        {
            if (!string.IsNullOrEmpty(description)) {
                Description.SetValue(description);
            }
            if (!string.IsNullOrEmpty(name)) {
                Name.SetValue(name);
            }
            if (!string.IsNullOrEmpty(category)) {
                CategorySelect.SelectItem(category, true);
            }
            return this;
        }

        public EditReportModal GetDetailsTab() => (EditReportModal) SwitchToTab(EditReportModalTabs.Details.ToString());
        public EditReportModal GetSharingTab() => (EditReportModal) SwitchToTab(EditReportModalTabs.Sharing.ToString());

        public EditReportModal(DomContext context)
            : base(context) { }
    }

    public enum EditReportModalTabs
    {
        Details,
        Sharing
    }
}
