using System.IO;
using ServiceTitan.Testing.Web.Controls;
using ServiceTitan.Testing.Web.Framework;
using ServiceTitan.Testing.Web.Pages.Reports.Custom.Scheduling;
using ServiceTitan.UITests.Controls;
using ServiceTitan.UITests.Helpers;

namespace ServiceTitan.UITests.Pages.Reports.Redesign
{
    public class ScheduledReportCard : DomNodeWrapper
    {
        public DomNodeWrapper EmailSubject => Query.Css(".qa-scheduled-card-email-subject").FirstDomNode();
        public SelectDropdown EllipsisMenu => Query.FirstDomNode(q => new SelectDropdown(q));
        public DomNodeWrapper Report => Query.Css(".c-blue .Link").FirstDomNode();
        public DomNodeWrapper Recipients => Query.Css(".qa-scheduled-card-recipients").FirstDomNode();
        public DomNodeWrapper CadenceInfo => Query.Css("[class*=interval] .Tag__body").FirstDomNode();
        public virtual DomNodeWrapper NextSendDate => Query.Css(".qa-scheduled-card-next-send-date").FirstDomNode();
        public DomNodeWrapper LegacyTag => Query.Css("[class*=legacy]:contains(Legacy)").FirstDomNode();
        public CheckBox SelectPendingReport => Query.FirstDomNode(q => new CheckBox(q));

        /// <summary>
        /// Unfolds the card's breadcrumbs menu, selects 'Edit' option
        /// </summary>
        /// <returns>The first Details step page object of the scheduling wizard flow</returns>
        public DetailsStepPage Edit() =>
            EllipsisMenu.SelectItemForOpen<DetailsStepPage>(nameof(ScheduledReportCardAction.Edit));

        /// <summary>
        /// Unfolds the card's breadcrumbs menu, selects 'Duplicate' option to create a duplicated scheduled report
        /// </summary>
        /// <returns>The first Details step page object of the scheduling wizard flow</returns>
        public DetailsStepPage Duplicate() =>
            EllipsisMenu.SelectItemForOpen<DetailsStepPage>(nameof(ScheduledReportCardAction.Duplicate));

        /// <summary>
        /// Deactivates the report this report card corresponds to
        /// </summary>
        /// <returns>Snackbar toast message object that confirms report's deactivation</returns>
        public SnackBar Deactivate() =>
            EllipsisMenu.SelectItemForOpen<SnackBar>(nameof(ScheduledReportCardAction.Deactivate));

        /// <summary>
        /// Activates the report this report card corresponds to
        /// </summary>
        /// <returns>Snackbar toast message object that confirms report's activation</returns>
        public SnackBar Activate() =>
            EllipsisMenu.SelectItemForOpen<SnackBar>(nameof(ScheduledReportCardAction.Activate));

        /// <summary>
        /// Unfolds the card's breadcrumbs menu, selects Preview option to download the attachment
        /// </summary>
        /// <param name="extension">".xlsx", ".pdf" or ".zip"</param>
        /// <returns>".zip" archive if both pdf and xlsx attachment formats were specified during scheduling of the report</returns>
        public FileInfo PreviewConvertedReport(string extension)
        {
            EllipsisMenu.SelectItem(nameof(ScheduledReportCardAction.Preview));

            return FileHelper.GetDownloadedFile(Browser, extension);
        }

        public ScheduledReportCard(DomContext context)
            : base(context) { }
    }

    public enum ScheduledReportCardAction
    {
        Activate,
        Deactivate,
        Edit,
        Duplicate,
        Preview
    }
}
