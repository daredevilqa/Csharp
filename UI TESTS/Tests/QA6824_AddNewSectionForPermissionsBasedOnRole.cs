using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceTitan.Model;
using ServiceTitan.Services;
using ServiceTitan.Testing.Web.Constants;
using ServiceTitan.Testing.Web.DataGenerator;
using ServiceTitan.Testing.Web.Framework;
using ServiceTitan.UITests.Controls;
using ServiceTitan.UITests.Entities;
using ServiceTitan.UITests.Helpers;
using ServiceTitan.UITests.Pages.Settings.Operations.Permission;
using ServiceTitan.UITests.Pages.Settings.Operations.ReportingSettings;

namespace ServiceTitan.UITests.Tests.Reporting
{
    public class QA6824_AddNewSectionForPermissionsBasedOnRole : TestBase
    {
        #region Constants

        public const string Dispatch = "Dispatch",
            Accounting = "Accounting",
            FieldManager = "Field Manager",
            DisplayUser = "Display User",
            SalesManager = "Sales Manager",
            Csr = "CSR",
            GeneralOffice = "General Office",
            Admin = "Admin",
            Role = "Role",
            CustomRole = "Slave";

        #endregion Constants

        [Test]
        [Category(TestCategories.Reporting)]
        [Description("https://servicetitan.atlassian.net/browse/QA-6824")]
        public async Task QA6824_AddNewSectionForPermissionsBasedOnRole_TS()
        {
            #region Data and Feature Gates

            this.SetConfigurationProperty(RootHub.ConfigurationProperties().Reports20, true);
            this.SetConfigurationProperty(RootHub.ConfigurationProperties().Reports20Redesign, true);

            var dispatch = new EmployeeEntity {
                Name = "Dispatch_QA6824",
                Role = BuiltInUserRole.Dispatch,
                UserAccessEntity = new UserAccessEntity { Username = $"Dispatch_{UserName}", Password = Password }
            };
            var newDispatch = new EmployeeEntity {
                Name = "NewDispatch_QA6824",
                Role = BuiltInUserRole.Dispatch,
                UserAccessEntity = new UserAccessEntity { Username = $"Dispatch_{UserName}", Password = Password }
            };

            await ExecuteDataWorkActionAsync(
                async hub => {
                    await EmployeesWorker.CreateUserWithEmployeeAsync(hub, dispatch);
                });

            #endregion

            var browser = OpenDriver();
            browser.Login();

            //Validate the new Role Permissions section has been added to the Reporting Permissions page
            var reportingPermissionsPage = browser.OpenSettingsPage<ReportingSettingsPage>().OpenPermissionsTab();

            Assert.True(
                reportingPermissionsPage.RolesContainer.ExistsAndDisplayed,
                "Expected: new Role Permissions section has been added to the Reporting Permissions page");

            //Validate the Role Permissions table contains the following columns: A multi-select column, Role, Actions
            var header = reportingPermissionsPage.RolesTableHeader;

            Assert.True(
                header.SelectAllCheckBox.GetParent().ExistsAndDisplayed,
                "Expected: A multi-select column is present");
            Assert.True(
                header.HeaderItems.ExistsItemByText(Role),
                "Expected: Role column is present");
            Assert.True(
                header.HeaderItems.ExistsItemByText("Actions"),
                "Expected: Actions column is present");

            //Validate on the Role Permissions section, the “Edit Selection” button is unavailable if items are not selected
            Assert.True(
                reportingPermissionsPage.EditRolesSelectionBtn.ExistsAndDisplayed,
                "Expected: Edit Selection btn is present");
            Assert.True(
                reportingPermissionsPage.EditRolesSelectionBtn.Disabled(),
                "Expected: “Edit Selection” button is unavailable if no roles are selected");

            //Validate on the Role Permissions page the “Edit Selection” button is available if any item is selected
            var table = reportingPermissionsPage.RolesPermissionsTable;
            table.First().Selector.CheckBox.Check();

            Assert.True(
                reportingPermissionsPage.EditRolesSelectionBtn.Enabled(),
                "Expected: “Edit Selection” button is available if any role is selected");

            //Validate the “Edit Selection” button above Employee permissions table is unavailable
            //if any role is selected, but no employees are selected
            Assert.True(
                reportingPermissionsPage.EditEmployeesSelectionBtn.Disabled(),
                "Expected: “Edit Selection” button is unavailable in Employees section if any role is selected but no employees are selected");

            //Validate on the Role Permissions page a user can select all items by checking off the multi-select checkbox
            header.SelectAllCheckBox.Check();

            foreach (var row in table) {
                Assert.True(row.Selector.CheckBox.Checked, "Expected: each role row should be selected");
            }

            //Validate on the Role Permissions page a user can deselect all items by clicking on the checkbox
            header.SelectAllCheckBox.Uncheck();

            foreach (var row in table) {
                Assert.False(row.Selector.CheckBox.Checked, "Expected: each role row should be deselected");
            }

            //Validate on the Role Permissions page the “Role” column contains the following roles by default:
            //Dispatch, Accounting, Field Manager, Display User, Sales Manager, CSR, General Office, Admin
            CollectionAssert.AreEquivalent(
                new[] {
                    Dispatch,
                    Accounting,
                    FieldManager,
                    DisplayUser,
                    SalesManager,
                    Csr,
                    GeneralOffice,
                    Admin
                },
                table.Select(r => r.GetCellByKendoHeader(Role, parentLvl: 7).GetText()).ToList(),
                "Default roles do not match");

            //Validate on the Reporting Role Permissions page the “Role” column may contain custom roles
            var permissionRolePage = browser.OpenSettingsPage<PermissionRolePage>();
            var addRolePage = permissionRolePage.OpenAddEmployeeRole();
            addRolePage.Name.SetValue(CustomRole);
            addRolePage.SaveBtn.ClickForOpenElement(permissionRolePage);
            browser.OpenSettingsPage<ReportingSettingsPage>().OpenPermissionsTab();

            Assert.True(
                table.ExistsItemByText(CustomRole),
                $"Expected: {CustomRole} custom role appeared in the roles table");

            //Validate a user can sort/filter the Role column
            Assert.True(
                header.GetColumnHeaderByName(Role).FilterIcon.ExistsAndDisplayed,
                $"Expected: {Role} column has filter icon");

            header.SortKendoColumn(Role, SortOrder.Descending);

            Assert.AreEqual(
                SortOrder.Descending,
                header.GetColumnSortOrder(Role),
                $"Expected: {Role} column should be sorted descending");

            header.GetColumnHeaderByName(Role).OpenFilterModal().FilterByContainedValue(CustomRole);

            Assert.AreEqual(1, table.Count(), "Expected: roles table is filtered and contains 1 row only");
            Assert.True(table.ExistsItemByText(CustomRole), "Table is filtered wrongly");

            header.GetColumnHeaderByName(Role).OpenFilterModal().ClearBtn.ClickWithWaitDisappear();

            //Validate “Reporting Permissions” modal opens if a user clicked on the “Edit” button
            var modal = table.GetRowByText(Admin).OpenPermissionsEditModal();

            //Validate “Update Permissions” checkbox has been added into the “Reporting Permissions” modal
            Assert.True(
                modal.UpdateExistingEmployees.ExistsAndDisplayed,
                "Expected: “Update Permissions” checkbox has been added into the “Reporting Permissions” modal");

            //Validate the tooltip appears when a user hovers over the checkbox
            modal.UpdateExistingEmployees.AssertTooltip(
                "Update existing employees’ reporting permissions to match the edits you made to the permissions of this role");

            //Validate Edit modal opens if a user clicked the “Edit Selection” button for several selected roles
            modal.SaveAndWait();
            header.SelectAllCheckBox.Check();
            reportingPermissionsPage.EditRolesSelectionBtn.WaitElementToBeClickable();

            Assert.DoesNotThrow(
                () => reportingPermissionsPage.EditRolesSelectionBtn.ClickForOpenElement(modal),
                "Expected: Edit modal opens if a user clicked the “Edit Selection” button for several selected roles");

            modal.CloseModalDialog();
            header.SelectAllCheckBox.Uncheck();

            #region Default Roles Permissions

            //Validate the default roles’ reporting permissions are the following:
            //1. Dispatch
            //View Templates - Jobs
            //Edit Templates - None
            //View and Edit Categories - None
            table.GetRowByText(Dispatch).OpenPermissionsEditModal();

            foreach (var row in modal.PermissionsTable) {
                if (row.GetText() == ReportingTemplates.Jobs) {
                    Assert.True(
                        row.View.Checked && !row.Edit.Checked,
                        $"Expected: {Dispatch} role has only view permission for {ReportingTemplates.Jobs} template");
                    break;
                }
                Assert.False(
                    row.View.Checked && row.Edit.Checked,
                    $"Expected: {Dispatch} role has only view permission for {ReportingTemplates.Jobs} template only");
            }

            modal.OpenCategoriesTab();
            foreach (var row in modal.PermissionsTable) {
                Assert.False(
                    row.View.Checked && row.Edit.Checked,
                    $"Expected: {Dispatch} role has no permissions for categories");
            }
            modal.CloseModalDialog();

            //2. Accounting
            //View Templates - Invoices, Jobs
            //Edit Templates - None
            //View and Edit Categories - Accounting
            table.GetRowByText(Accounting).OpenPermissionsEditModal();

            foreach (var row in modal.PermissionsTable) {
                if (row.GetText() == ReportingTemplates.Jobs || row.GetText() == ReportingTemplates.Invoices) {
                    Assert.True(
                        row.View.Checked && !row.Edit.Checked,
                        $"Expected: {Accounting} role has only Invoices and Jobs view permissions");
                    break;
                }
                Assert.False(
                    row.View.Checked && row.Edit.Checked,
                    $"Expected: {Accounting} role has only Invoices and Jobs view permissions");
            }

            modal.OpenCategoriesTab();
            foreach (var row in modal.PermissionsTable) {
                if (row.GetText() == Accounting) {
                    Assert.True(
                        row.View.Checked && row.Edit.Checked,
                        $"Expected: {Accounting} role has both view/edit permissions for {Accounting} category");
                    break;
                }
                Assert.False(
                    row.View.Checked && row.Edit.Checked,
                    $"Expected: {Accounting} role has no permissions for categories except {Accounting}");
            }
            modal.CloseModalDialog();

            //3. Field Manager
            //View and Edit Templates - Tech Perf, Jobs, Estimates, PJC
            //View and Edit Categories - Tech, Tech dashboard
            table.GetRowByText(FieldManager).OpenPermissionsEditModal();

            foreach (var row in modal.PermissionsTable) {
                if (row.GetText() == ReportingTemplates.Jobs
                    || row.GetText() == ReportingTemplates.Estimates
                    || row.GetText() == ReportingTemplates.TechPerf
                    || row.GetText() == ReportingTemplates.Pjc) {
                    Assert.True(
                        row.View.Checked && row.Edit.Checked,
                        $"Expected: {FieldManager} role has only Jobs, Estimates, PJC and Tech Perf view/edit permissions");
                    break;
                }
                Assert.False(
                    row.View.Checked && row.Edit.Checked,
                    $"Expected: {FieldManager} role has only Jobs, Estimates, PJC and Tech Perf view/edit permissions");
            }

            modal.OpenCategoriesTab();
            foreach (var row in modal.PermissionsTable) {
                if (row.GetText() == ReportingCategories.Technician
                    || row.GetText() == ReportingCategories.TechnicianDashboard) {
                    Assert.True(
                        row.View.Checked && row.Edit.Checked,
                        $"Expected: {FieldManager} role has both view/edit permissions for "
                        + $"{ReportingCategories.Technician} and {ReportingCategories.TechnicianDashboard} categories");
                    break;
                }
                Assert.False(
                    row.View.Checked && row.Edit.Checked,
                    $"Expected: {FieldManager} role has no permissions for other categories");
            }
            modal.CloseModalDialog();

            //4. Display User
            //All permissions are None
            table.GetRowByText(DisplayUser).OpenPermissionsEditModal();

            foreach (var row in modal.PermissionsTable) {
                Assert.False(
                    row.View.Checked && row.Edit.Checked,
                    $"Expected: {DisplayUser} role has no templates permissions by default");
            }

            modal.OpenCategoriesTab();
            foreach (var row in modal.PermissionsTable) {
                Assert.False(
                    row.View.Checked && row.Edit.Checked,
                    $"Expected: {DisplayUser} role has no permissions for categories");
            }
            modal.CloseModalDialog();

            //5. Sales Manager
            //View and Edit Templates - Tech Perf, Jobs, Estimates
            //View and Edit Categories - Tech, Tech dashboard
            table.GetRowByText(SalesManager).OpenPermissionsEditModal();

            foreach (var row in modal.PermissionsTable) {
                if (row.GetText() == ReportingTemplates.Jobs
                    || row.GetText() == ReportingTemplates.Estimates
                    || row.GetText() == ReportingTemplates.TechPerf) {
                    Assert.True(
                        row.View.Checked && row.Edit.Checked,
                        $"Expected: {SalesManager} role has only Jobs, Estimates and Tech Perf view/edit permissions");
                    break;
                }
                Assert.False(
                    row.View.Checked && row.Edit.Checked,
                    $"Expected: {SalesManager} role has only Jobs, Estimates and Tech Perf view/edit permissions");
            }

            modal.OpenCategoriesTab();
            foreach (var row in modal.PermissionsTable) {
                if (row.GetText() == ReportingCategories.Technician
                    || row.GetText() == ReportingCategories.TechnicianDashboard) {
                    Assert.True(
                        row.View.Checked && row.Edit.Checked,
                        $"Expected: {SalesManager} role has both view/edit permissions for "
                        + $"{ReportingCategories.Technician} and {ReportingCategories.TechnicianDashboard} categories");
                    break;
                }
                Assert.False(
                    row.View.Checked && row.Edit.Checked,
                    $"Expected: {SalesManager} role has no permissions for other categories");
            }
            modal.CloseModalDialog();

            //6. CSR
            //View Templates - Jobs
            //Edit Templates - None
            //View and Edit Categories - None
            table.GetRowByText(Csr).OpenPermissionsEditModal();

            foreach (var row in modal.PermissionsTable) {
                if (row.GetText() == ReportingTemplates.Jobs) {
                    Assert.True(
                        row.View.Checked && !row.Edit.Checked,
                        $"Expected: {Csr} role has only view permission for {ReportingTemplates.Jobs} template");
                    break;
                }
                Assert.False(
                    row.View.Checked && row.Edit.Checked,
                    $"Expected: {Csr} role has only view permission for {ReportingTemplates.Jobs} template only");
            }

            modal.OpenCategoriesTab();
            foreach (var row in modal.PermissionsTable) {
                Assert.False(
                    row.View.Checked && row.Edit.Checked,
                    $"Expected: {Csr} role has no permissions for categories");
            }
            modal.CloseModalDialog();

            //7. General Office
            //All permissions are None
            table.GetRowByText(GeneralOffice).OpenPermissionsEditModal();

            foreach (var row in modal.PermissionsTable) {
                Assert.False(
                    row.View.Checked && row.Edit.Checked,
                    $"Expected: {GeneralOffice} role has no templates permissions by default");
            }

            modal.OpenCategoriesTab();
            foreach (var row in modal.PermissionsTable) {
                Assert.False(
                    row.View.Checked && row.Edit.Checked,
                    $"Expected: {GeneralOffice} role has no permissions for categories");
            }
            modal.CloseModalDialog();

            //8. Admin
            //All permissions
            table.GetRowByText(Admin).OpenPermissionsEditModal();

            foreach (var row in modal.PermissionsTable) {
                Assert.True(
                    row.View.Checked && row.Edit.Checked,
                    $"Expected: {Admin} role has all permissions by default");
            }

            modal.OpenCategoriesTab();
            foreach (var row in modal.PermissionsTable) {
                if (row.GetText() == ReportingCategories.SpecializedReports) {
                    Assert.True(
                        row.View.Checked,
                        $"Expected: {Admin} role has view permission for specialized reports");
                    break;
                }
                Assert.True(
                    row.View.Checked && row.Edit.Checked,
                    $"Expected: {Admin} role has all permissions for categories");
            }
            modal.CloseModalDialog();

            //9. Custom Role
            //View Templates - Jobs
            //Edit Templates - None
            //View and Edit Categories - None
            table.GetRowByText(CustomRole).OpenPermissionsEditModal();

            foreach (var row in modal.PermissionsTable) {
                if (row.GetText() == ReportingTemplates.Jobs) {
                    Assert.True(
                        row.View.Checked && !row.Edit.Checked,
                        $"Expected: {CustomRole} role has only view permission for {ReportingTemplates.Jobs} template");
                    break;
                }
                Assert.False(
                    row.View.Checked && row.Edit.Checked,
                    $"Expected: {CustomRole} role has only view permission for {ReportingTemplates.Jobs} template only");
            }

            modal.OpenCategoriesTab();
            foreach (var row in modal.PermissionsTable) {
                Assert.False(
                    row.View.Checked && row.Edit.Checked,
                    $"Expected: {CustomRole} role has no permissions for categories");
            }
            modal.CloseModalDialog();

            #endregion Default Roles Permissions

            //Validate all changes are applied only to the edited role if the “Update Permissions” checkbox
            //had not been checked (permissions of users with the edited role have not been changed)
            table.GetRowByText(Dispatch).OpenPermissionsEditModal();
            modal.PermissionsTable.GetRowByText(ReportingTemplates.Jobs).Edit.Check();
            modal.SaveAndWait();
            reportingPermissionsPage.EmployeesPermissionsTable.GetRowByText(dispatch.Name).OpenPermissionsEditModal();

            Assert.False(
                modal.PermissionsTable.GetRowByText(ReportingTemplates.Jobs).Edit.Checked,
                "Expected: actual dispatch user's Jobs template permission hasn't been changed");

            modal.CloseModalDialog();

            //Validate all changes are applied to the edited role and all users with the role if the
            //“Update Permissions” checkbox had been checked (permissions of users with the edited role have been changed)
            table.GetRowByText(Dispatch).OpenPermissionsEditModal();
            modal.PermissionsTable.GetRowByText(ReportingTemplates.Invoices).Edit.Check();
            modal.UpdateExistingEmployees.CheckBox.Check();
            modal.SaveAndWait();
            reportingPermissionsPage.EmployeesPermissionsTable.GetRowByText(dispatch.Name).OpenPermissionsEditModal();

            Assert.True(
                modal.PermissionsTable.GetRowByText(ReportingTemplates.Invoices).Edit.Checked,
                "Expected: existing dispatch user's Invoices template permission has been changed along with the role");

            modal.CloseModalDialog();

            //Validate new users have reporting permissions that are set for their roles
            await ExecuteDataWorkActionAsync(
                async hub => {
                    await EmployeesWorker.CreateUserWithEmployeeAsync(hub, newDispatch);
                });

            reportingPermissionsPage.FullRefreshPage();
            reportingPermissionsPage.EmployeesPermissionsTable.GetRowByText(newDispatch.Name).OpenPermissionsEditModal();

            Assert.True(
                modal.PermissionsTable.GetRowByText(ReportingTemplates.Invoices).Edit.Checked,
                "Expected: new dispatch user gets permissions that are set for their role");
        }
    }
}
