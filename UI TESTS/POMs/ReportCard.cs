using NUnit.Framework;
using ServiceTitan.Testing.Web.Constants;
using ServiceTitan.Testing.Web.Controls;
using ServiceTitan.Testing.Web.Framework;
using ServiceTitan.UITests.Controls;

namespace ServiceTitan.UITests.Pages.Reports.Redesign
{
    public class ReportCard : DomNodeWrapper
    {
        public DomNodeWrapper ReportLink => Query.Css("a.Link--blue").FirstDomNode();
        public DomNodeWrapper Updated => Query.Css(".qa-report-updated, .qa-listview-updated-date").FirstDomNode();
        public Button ScheduleBtn => Query.Css(".qa-schedule-button").FirstDomNode(q => new Button(q));
        public DomNodeWrapper Bookmark => Query.Css("i[class*='bookmark']").FirstDomNode();
        public bool IsBookmarked => Bookmark.HasClass("active");
        public SelectDropdown EllipsisMenu => Query.FirstDomNode(q => new SelectDropdown(q));
        public DomNodeWrapper Description => Query.Css("div:has(.Link) + div").FirstDomNode();

        /// <summary>
        /// Sets a report's bookmark by clicking on the bookmark icon
        /// </summary>
        internal void SetBookmark()
        {
            ExecuteHelper.ExecuteAgainIfError(
                () => {
                    if (IsBookmarked) {
                        return;
                    }
                    MoveToElement();
                    Bookmark.WaitElementToBeClickable();
                    Bookmark.Click();
                    Bookmark.WaitElementByCondition();
                    Assert.True(
                        IsBookmarked,
                        "Expected: bookmark icon should be displayed in active state");
                },
                actionAfterError: () => Bookmark.ExecuteJavaScript(
                    "arguments[0].click()",
                    new object[] { Bookmark.WebElement }));
        }

        /// <summary>
        /// Removes a report's bookmark by clicking on the bookmark icon
        /// </summary>
        /// <param name="isHomeTab">Indicates whether the action is being done on the home tab's card.
        /// False means we are on the All Reports tab</param>
        internal void RemoveBookmark(bool isHomeTab = false)
        {
            Bookmark.MoveToElement();
            Bookmark.Click();

            if (isHomeTab) {
                WaitDisappearElement();
            }
            else {
                Bookmark.WaitElementByCondition();
            }
        }

        /// <summary>
        /// Unfolds the card's breadcrumbs menu, selects 'Edit' option
        /// </summary>
        /// <returns>'Edit Report' modal object</returns>
        internal EditReportModal Edit() =>
            EllipsisMenu.SelectItemForOpen<EditReportModal>(nameof(ReportActions.Edit));

        /// <summary>
        /// Unfolds the card's breadcrumbs menu, selects 'Delete' option
        /// </summary>
        /// <returns>'Delete Report' modal object</returns>
        internal NewModalDialog GetDeleteModal() =>
            EllipsisMenu.SelectItemForOpen<NewModalDialog>(nameof(ReportActions.Delete));

        /// <summary>
        /// Unfolds the card's breadcrumbs menu, selects 'Duplicate' option
        /// </summary>
        /// <returns>Duplicate report modal</returns>
        public EditReportModal GetDuplicateModal() =>
            EllipsisMenu.SelectItemForOpen<EditReportModal>(nameof(ReportActions.Duplicate));

        /// <summary>
        /// Unfolds the card's breadcrumbs menu, selects 'Hide' option
        /// </summary>
        /// <returns>'Hide Recommended Report' modal object</returns>
        internal HideRecommendedReportModal HideRecommendedReport() =>
            EllipsisMenu.SelectItemForOpen<HideRecommendedReportModal>(nameof(ReportActions.Hide));

        public ReportCard(DomContext context)
            : base(context) { }
    }
}
