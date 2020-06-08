using System;
using System.IO;
using ServiceTitan.Testing.Web.Constants;
using ServiceTitan.Testing.Web.Controls;
using ServiceTitan.Testing.Web.Controls.DateRangePicker;
using ServiceTitan.Testing.Web.Framework;
using ServiceTitan.Testing.Web.Helpers;
using ServiceTitan.Testing.Web.Pages.Reports.Custom.Scheduling;
using ServiceTitan.UITests.Controls;
using ServiceTitan.UITests.Helpers;

namespace ServiceTitan.UITests.Pages.Reports.Redesign
{
    public abstract class ReactReportPage<TFilter, TRow> : DomNodeWrapper, IPage
        where TFilter : ReactReportFilter
        where TRow : Row
    {
        public abstract string PageName { get; }
        public DomNodeWrapper Title => Query.Css(".qa-report-title").FirstDomNode();
        public DomNodeWrapper ReportMenuIcon => Query.Css(".qa-ellipsis-report-menu").FirstDomNode();
        public SelectDropdown ReportActionsMenu => ReportMenuIcon.Query.FirstDomNode(q => new SelectDropdown(q));
        public DomNodeWrapper QuestionIcon => Query.Css(".qa-question-info-icon").FirstDomNode();
        public DomNodeWrapper RunButton => Query.Css(".qa-run-button").FirstDomNode();
        public TFilter Filter => Query.FirstDomNode(q => (TFilter)Activator.CreateInstance(typeof(TFilter), q));
        public SelectDropdown ExportDropdown =>
            Query.Css(".Select-wrapper:contains('Export')").FirstDomNode(q => new SelectDropdown(q));
        public EditColumnsDrawer Drawer => Query.FirstDomNode(q => new EditColumnsDrawer(q));
        public DomNodeWrapper EditColumns => Query.Css(".qa-edit-columns-button").FirstDomNode();
        public DomNodeWrapper SaveChanges => Query.Css(".qa-save-changes-button").FirstDomNode();
        public virtual DomNodeWrapperList<TRow> Table =>
            new DomNodeWrapperList<TRow>(() => Query.Css(".k-grid-table tbody"),
                parent: Query.Css("[role='grid']").FirstDomNode());
        public KendoTableHeader TableHeader => Query.FirstDomNode(q => new KendoTableHeader(q));
        public KendoGroupingHeader GroupingHeader => Query.FirstDomNode(q => new KendoGroupingHeader(q));
        public Banner Banner => Query.FirstDomNode(q => new Banner(q));
        public DomNodeWrapperList<Cell> TotalsAndAveragesRow => new DomNodeWrapperList<Cell>(() => Query.Css(".k-group-footer"));
        public DomNodeWrapperList<DomNodeWrapper> GroupingRows =>
            new DomNodeWrapperList<DomNodeWrapper>(() => Query.Css(".k-grouping-row"));
        public Drilldown Drilldown => Query.FirstDomNode(q => new Drilldown(q));
        public DomNodeWrapper KpiTooltip => Browser.Query.Css(".k-tooltip").FirstDomNode();
        public GraphContainer GraphContainer => Query.FirstDomNode(q => new GraphContainer(q));

        public void ApplyFilter(ReportFilterCriteria criteria)
        {
            Filter.Apply(criteria);
        }

        public void Run()
        {
            RunButton.Click();
            WaitDomLoad();
        }

        public void Save()
        {
            SaveChanges.Click();
            WaitDomLoad(false);
        }

        public FileInfo ExportToPdf()
        {
            ExportDropdown.SelectItem("Export to PDF");
            return FileHelper.GetDownloadedFile(Browser, ".pdf");
        }

        public FileInfo ExportToXlsx()
        {
            ExportDropdown.SelectItem("Export to XLSX");
            return FileHelper.GetDownloadedFile(Browser, ".xlsx");
        }

        public FileInfo ExportChartToPdf()
        {
            ExportDropdown.SelectItem("Export Chart to PDF");
            return FileHelper.GetDownloadedFile(Browser, ".pdf");
        }

        public EditColumnsDrawer GetEditColumnsDrawer()
        {
            return Drawer.ExistsAndDisplayed
                ? Drawer
                : EditColumns.ClickForOpen<EditColumnsDrawer>();
        }

        /// <summary>
        /// Opens Edit Columns drawer and adds/removes columns by checking/unchecking them, then hides the drawer
        /// </summary>
        /// <param name="colsToAdd">String array of columns to be selected</param>
        /// <param name="colsToRemove">String array of columns to be deselected</param>
        public void AddRemoveColumns(string[] colsToAdd = null, string[] colsToRemove = null)
        {
            var drawer = GetEditColumnsDrawer();
            if (colsToAdd != null) {
                drawer.EditColumns(true, colsToAdd);
            }
            if (colsToRemove != null) {
                drawer.EditColumns(false, colsToRemove);
            }
            drawer.ApplyAndWait();
        }

        /// <summary>
        /// Clicks on a question mark icon to open a popover with report info: template, datasource, category
        /// </summary>
        /// <returns>Popover instance</returns>
        public ReportInfoPopover GetReportInfoPopover() => QuestionIcon.ClickForOpen<ReportInfoPopover>();

        /// <summary>
        /// Unfolds the report's ellipsis menu, selects 'Edit' option
        /// </summary>
        /// <returns>'Edit Report' modal object</returns>
        public EditReportModal GetEditModal() =>
            ReportActionsMenu.SelectItemForOpen<EditReportModal>(nameof(ReportActions.Edit));

        /// <summary>
        /// Unfolds the report's ellipsis menu, selects 'Delete' option
        /// </summary>
        /// <returns>'Delete Report' modal object</returns>
        public NewModalDialog GetDeleteModal() =>
            ReportActionsMenu.SelectItemForOpen<NewModalDialog>(nameof(ReportActions.Delete));

        /// <summary>
        /// Unfolds the report's ellipsis menu, selects 'Schedule' option
        /// </summary>
        /// <returns>The scheduling flow starting page with simple/flexible scheduling types selectors</returns>
        public SelectReportTypePage GetSchedulingFlow() =>
            ReportActionsMenu.SelectItemForOpen<SelectReportTypePage>(nameof(ReportActions.Schedule));

        /// <summary>
        /// Unfolds the report's ellipsis menu, selects 'Duplicate' option
        /// </summary>
        /// <returns>Duplicate report modal</returns>
        public EditReportModal GetDuplicateModal() =>
            ReportActionsMenu.SelectItemForOpen<EditReportModal>(nameof(ReportActions.Duplicate));

        protected ReactReportPage(DomContext context)
            : base(context)
        {
            RootQuery = context.Css(".qa-report-page-container");
        }
    }

    public class EditColumnsDrawer : Drawer
    {
        public DomNodeWrapperList<DomNodeWrapper> CategorySectionNames =>
            new DomNodeWrapperList<DomNodeWrapper>(() => Query.Css(".qa-category-name"));
        public DomNodeWrapperList<KpiRow> Columns => new DomNodeWrapperList<KpiRow>(() => Query);
        public DomNodeWrapperList<KpiRow> ColumnsPerCategory(string categoryName) => new DomNodeWrapperList<KpiRow>(
            () => Query.Css($".qa-columns-section-per-category:contains({categoryName})"));
        public DomNodeWrapper SearchBox => Query.Css(".qa-columns-search-input input").FirstDomNode();
        public DomNodeWrapper ClearSearchIcon => Query.Css(".qa-clear-search-results").FirstDomNode();
        public DomNodeWrapper SearchIllustrationContainer => Query.Css("[class*='search-illustration']").FirstDomNode();
        public DomNodeWrapper NoMatchesFoundMsg =>
            SearchIllustrationContainer.Query.Css("[class*='no-matches-found']").FirstDomNode();
        public DomNodeWrapper ClearSearchBtn => NoMatchesFoundMsg.Query.Css(".Link").FirstDomNode();
        public DomNodeWrapper ColumnsSelectedTag => Footer.Query.Css(".Tag").FirstDomNode();

        /// <summary>
        /// Checks or unchecks certain columns set depending on condition.
        /// </summary>
        /// <param name="condition">True - if it's needed to switch ON columns visibility; false - opposite</param>
        /// <param name="columns">String array of columns (KPIs)</param>
        public void EditColumns(bool condition, string[] columns)
        {
            foreach (var col in columns) {
                SearchBox.SetValue(col);
                Columns.GetRowByExpression(r => r.Label.GetText() == col).Kpi.CheckBox.CheckByCondition(condition);
            }
        }

        public EditColumnsDrawer(DomContext context)
            : base(context) { }
    }

    public class KpiRow : DomNodeWrapper
    {
        public CheckBoxOption Kpi => Query.FirstDomNode(q => new CheckBoxOption(q));
        public DomNodeWrapper Label => Query.Css("label.Text").FirstDomNode();

        public KpiRow(DomContext context)
            : base(context)
        {
            RootQuery = context.Css(".qa-kpi-row");
        }
    }

    public class Drilldown : DomNodeWrapper
    {
        public DomNodeWrapper KpiTitle => Query.Css(".Text").FirstDomNode();
        public KendoTableHeader DrilldownTableHeader => Query.FirstDomNode(q => new KendoTableHeader(q));
        public DomNodeWrapperList<Row> DrillDownTable => new DomNodeWrapperList<Row>(() => Query.Css(".k-grid-content tbody"),
                parent: Query.Css(".k-widget .k-grid").FirstDomNode());
        public SelectDropdown ExportDropdown => Query.Css(".Select-wrapper").FirstDomNode(q => new SelectDropdown(q));

        public FileInfo ExportToPdf()
        {
            ExportDropdown.SelectItem("Export to PDF");
            return FileHelper.GetDownloadedFile(Browser, ".pdf");
        }

        public FileInfo ExportToXlsx()
        {
            ExportDropdown.SelectItem("Export to XLSX");
            return FileHelper.GetDownloadedFile(Browser, ".xlsx");
        }

        public Drilldown(DomContext context)
            : base(context)
        {
            RootQuery = context.Css("[class*='drilldown_']");
        }
    }

    public class KendoGroupingHeader : DomNodeWrapper
    {
        public DomNodeWrapperList<GroupIndicator> Indicators => new DomNodeWrapperList<GroupIndicator>(() => Query);

        public KendoGroupingHeader(DomContext context)
            : base(context)
        {
            RootQuery = context.Css(".k-grouping-header");
        }
    }

    public class GroupIndicator : DomNodeWrapper
    {
        public DomNodeWrapper SortingIcon => Query.Css(".k-link .k-icon").FirstDomNode();
        public DomNodeWrapper DeleteIcon => Query.Css(".k-button-icon").FirstDomNode();

        public GroupIndicator(DomContext context)
            : base(context)
        {
            RootQuery = context.Css(".k-group-indicator");
        }
    }

    public class GraphContainer : DomNodeWrapper
    {
        public GraphContainer(DomContext context)
            : base(context)
        {
            RootQuery = context.Css("[class*='report-graph-container']");
        }
    }

    public class ReportInfoPopover : Popover
    {
        public DomNodeWrapper Template => Query.Css(".qa-popover-template-label").FirstDomNode();
        public DomNodeWrapper Datasource => Query.Css(".qa-popover-datasource-label").FirstDomNode();
        public DomNodeWrapper Category => Query.Css(".qa-popover-category-label").FirstDomNode();

        public ReportInfoPopover(DomContext context)
            : base(context) { }
    }

    public abstract class ReactReportFilter : DomNodeWrapper
    {
        public abstract void Apply(ReportFilterCriteria criteria);

        protected ReactReportFilter(DomContext context)
            : base(context)
        {
            RootQuery = context.Css(".qa-filters-container");
        }
    }

    public class ReactCommonReportFilter : ReactReportFilter
    {
        public NewDateRangePicker DateRangePicker => Query.FirstDomNode(q => new NewDateRangePicker(q));
        public SelectDropdown BusinessUnit =>
            Query.Css("[name*='BusinessUnitId']").FirstDomNode(q => new SelectDropdown(q));
        public SelectDropdown Technicians =>
            Query.Css("[name='TechnicianId']").FirstDomNode(q => new SelectDropdown(q));
        public CheckBoxOption IncludeAdjustmentInvoices =>
            Query.Css(".a-Checkbox:has([name=IncludeAdjustmentInvoices])").FirstDomNode(q => new CheckBoxOption(q));

        public override void Apply(ReportFilterCriteria criteria)
        {
            if (criteria.From != null) {
                DateRangePicker.From.SetDateViaJs((DateTime)criteria.From);
            }
            if (criteria.To != null) {
                DateRangePicker.To.SetDateViaJs((DateTime)criteria.To);
            }
            if (!string.IsNullOrEmpty(criteria.BusinessUnit)) {
                BusinessUnit.SelectItem(criteria.BusinessUnit);
            }
            if (criteria.Technicians != null) {
                Technicians.SelectItems(criteria.Technicians);
            }
            if (criteria.DateRange != null) {
                DateRangePicker.GetCalendar().SetDateRange(criteria.DateRange);
            }
        }

        public ReactCommonReportFilter(DomContext context)
            : base(context) { }
    }

    public class ReactAccountingReportFilter : ReactCommonReportFilter
    {
        public SelectDropdown Customer => Query.FirstDomNode(q => new SelectDropdown(q,
            ".field:contains('Customer') input",
            ".result.active",
            ".content",
            false));
        public SelectDropdown BatchNumber => Query.FirstDomNode(q => new SelectDropdown(q,
            ".field:contains('Batch') input",
            ".result.active",
            ".content",
            false));
        public SelectDropdown PaymentStatus => Query.FirstDomNode(q => new SelectDropdown(q,
            "[name*='PaymentStatus']",
            ".menu.transition.visible",
            ".item",
            false));

        public ReactAccountingReportFilter(DomContext context)
            : base(context) { }
    }
}
