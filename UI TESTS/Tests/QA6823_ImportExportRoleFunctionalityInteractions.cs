using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using NUnit.Framework;
using ServiceTitan.Model;
using ServiceTitan.Services;
using ServiceTitan.Services.Core.Models;
using ServiceTitan.Testing.Web.DataGenerator;
using ServiceTitan.Testing.Web.Framework;
using ServiceTitan.UITests.Entities;
using ServiceTitan.UITests.Helpers;
using ServiceTitan.UITests.Pages.Settings.Operations.ReportingSettings;
using ServiceTitan.UITests.Pages.Settings.Tools;
using ServiceTitan.Web.Controllers;
using Templates = ServiceTitan.Testing.Web.Constants.ReportingTemplates;
using Categories = ServiceTitan.Testing.Web.Constants.ReportingCategories;

namespace ServiceTitan.UITests.Tests.Reporting
{
    public class QA6823_ImportExportRoleFunctionalityInteractions : TestBase
    {
        public const string UserPermissions = "User Permissions",
            ReportingTemplates = "Reporting Templates",
            ReportingCategories = "Reporting Categories",
            ReportingBusinessUnits = "Reporting BusinessUnits",
            NameColumn = "Name",
            UserNameColumn = "Username",
            UserRoleColumn = "UserRole",
            Employees = "Employees",
            Technicians = "Technicians",
            NonExistingUser = "NonExistingUser";

        [Test]
        [Category(TestCategories.Reporting)]
        [Description("https://servicetitan.atlassian.net/browse/QA-6823")]
        public async Task QA6823_ImportExportRoleFunctionalityInteractions_TS()
        {
            #region Data and Feature Gates

            this.SetConfigurationProperty(RootHub.ConfigurationProperties().Reports20, true);
            this.SetConfigurationProperty(RootHub.ConfigurationProperties().Reports20Redesign, true);
            this.SetConfigurationProperty(RootHub.ConfigurationProperties().EnableBudgeting, true);
            this.SetConfigurationProperty(
                RootHub.ConfigurationProperties().ReportingBusinessUnitUserRestrictions,
                true);

            var admin = new EmployeeEntity {
                Name = "Admin_QA6823",
                Role = BuiltInUserRole.Admin,
                UserAccessEntity = new UserAccessEntity { Username = $"Admin_{UserName}", Password = Password }
            };
            var admin2 = new EmployeeEntity {
                Name = "Admin2",
                Role = BuiltInUserRole.Admin,
                UserAccessEntity = new UserAccessEntity { Username = $"Admin2_{UserName}", Password = Password }
            };
            var inactiveEmployee = new EmployeeEntity {
                Name = "Inactive_Employee",
                Role = BuiltInUserRole.Dispatch,
                UserAccessEntity = new UserAccessEntity { Username = "Inactive_Employee", Password = Password }
            };
            var bU = new BusinessUnitEntity("QA6823");

            await ExecuteDataWorkActionAsync(
                async hub => {
                    await EmployeesWorker.CreateUserWithEmployeeAsync(hub, admin);
                    await EmployeesWorker.CreateUserWithEmployeeAsync(hub, admin2);
                    await EmployeesWorker.CreateUserWithEmployeeAsync(hub, inactiveEmployee);
                    hub.GetSession().Query.All<Employee>().First(e => e.Name == inactiveEmployee.Name).Active = false;
                    BusinessUnitsWorker.CreateBusinessUnit(hub, bU);

                    TenantsWorker.SetDwyerTenantType(hub, TenantType.MrRooter); //To have Dwyer reports category

                    var testController = hub.Resolve<TestController>();
                    testController.EnsureBuiltInReportsExist(); //Add specialized reports
                });

            #endregion

            var browser = OpenDriver(options: BrowserOptions.Create().SetDefaultOptions());
            browser.Login(admin.UserAccessEntity.Password, admin.UserAccessEntity.Username);

            var permissionsPage = browser.OpenSettingsPage<ReportingSettingsPage>().OpenPermissionsTab();
            var permissionsModal = permissionsPage
                .EmployeesPermissionsTable.GetRowByText(admin.Name)
                .OpenPermissionsEditModal();

            var templatesList = permissionsModal
                .OpenTemplatesTab()
                .PermissionsTable.Select(r => r.GetText())
                .ToList();
            permissionsModal.PermissionsTable.GetRowByText(Templates.TechPerf).Edit.Uncheck();

            var categoriesList = permissionsModal
                .OpenCategoriesTab()
                .PermissionsTable.Select(r => r.GetText())
                .ToList();
            permissionsModal.PermissionsTable.GetRowByText(Categories.Marketing).Edit.Uncheck();

            var buTab = permissionsModal.OpenBusinessUnitsDataTab();
            var businessUnitsList = buTab.BusinessUnitRestrictionsTable.Select(r => r.GetText()).ToList();
            buTab.BusinessUnitRestrictionsTable.GetRowByText(bU.Name).View.Uncheck();

            permissionsModal.SaveAndWait();

            var importExportPage = browser.OpenSettingsPage<ImportExportDataSettingsPage>();
            importExportPage.ExportBtn.ClickForOpen<ExportSettingsPage>();
            importExportPage.ExportUserPermissions();
            var clk = new Clock();
            var timestamp = clk.Now;
            var file = FileHelper.GetDownloadedFile(browser, ".xlsx");

            //Validate on the Import/Export page there is a timestamp that shows details of the last export:
            //* Last successful export date
            //* Username of the user who exported the file
            StringAssert.Contains(
                timestamp.ToString("MMMM dd"),
                importExportPage.LastRunInfo.CreatedOn.GetText(),
                "Expected: Last successful export date is shown");

            StringAssert.Contains(
                timestamp.ToString("hh:mm tt"),
                importExportPage.LastRunInfo.CreatedOn.GetText(),
                "Expected: Last successful export timestamp is shown");

            StringAssert.Contains(
                admin.UserAccessEntity.Username,
                importExportPage.LastRunInfo.CreatedBy.GetText(),
                "Expected: details section should show created by data");

            using var workbook = new XLWorkbook(file.FullName);

            //Validate in the User Permissions file the following reporting permissions tabs have been added:
            //Reporting Templates, Reporting Categories, Reporting BusinessUnits
            CollectionAssert.IsSubsetOf(
                new[] { ReportingTemplates, ReportingCategories, ReportingBusinessUnits },
                workbook.Worksheets.Select(w => w.Name),
                "Expected: 3 reporting permissions sheets have been added");

            //Validate the User Permissions file lists only active employees
            foreach (var worksheet in workbook.Worksheets) {
                if (!worksheet.Name.Contains("Reporting")) {
                    break;
                }
                var namesCells = worksheet.GetColumnCellsByName(NameColumn);

                CollectionAssert.DoesNotContain(
                    namesCells.Select(c => c.Value.ToString()).ToList(),
                    inactiveEmployee.Name,
                    "Expected: new reporting sheets contain only active employees");
            }

            //Validate “Reporting Templates” tab contains the following columns:
            //Name, Username, UserRole, View and Edit columns for Templates
            //Validate “Reporting Templates” tab does not contain read-only columns (Edit DMR and Praxis templates)
            var sheet = workbook.Worksheet(ReportingTemplates);
            var commonColumns = new[] { NameColumn, UserNameColumn, UserRoleColumn };

            CollectionAssert.IsSubsetOf(
                commonColumns,
                sheet.FirstRow().Cells().Select(c => c.GetString()).ToList(),
                "Expected: first row should contain common columns names");
            CollectionAssert.IsSubsetOf(
                templatesList.SelectMany(
                        t =>
                            new[] { $"View {t}", $"Edit {t}" })
                    .Where(t => !t.Contains("Edit DMR") && !t.Contains("Edit Praxis"))
                    .ToList(),
                sheet.FirstRow().Cells().Select(c => c.GetString()).ToList(),
                "Expected: Templates sheet contains view/edit permissions");

            //Validate “Reporting Categories” tab contains the following columns
            //Name, Username, UserRole, View and Edit columns for Categories
            //Validate “Reporting Categories” tab does not contain read-only columns ('Edit Specialized Reports' category)
            sheet = workbook.Worksheet(ReportingCategories);

            CollectionAssert.IsSubsetOf(
                commonColumns,
                sheet.FirstRow().Cells().Select(c => c.GetString()).ToList(),
                "Expected: first row should contain common columns names");
            CollectionAssert.IsSubsetOf(
                categoriesList.SelectMany(
                        c =>
                            new[] { $"View {c}", $"Edit {c}" })
                    .Where(c => !c.Contains("Edit Specialized Reports"))
                    .ToList(),
                sheet.FirstRow().Cells().Select(c => c.GetString()).ToList(),
                "Expected: Categories sheet contains view/edit permissions");

            //Validate “Reporting BusinessUnit” tab contains the following columns
            //Name, Username, UserRole, View columns for BusinessUnits
            sheet = workbook.Worksheet(ReportingBusinessUnits);

            CollectionAssert.IsSubsetOf(
                commonColumns,
                sheet.FirstRow().Cells().Select(c => c.GetString()).ToList(),
                "Expected: first row should contain common columns names");
            CollectionAssert.IsSubsetOf(
                businessUnitsList.Select(bu => $"View {bu}")
                    .ToList(),
                sheet.FirstRow().Cells().Select(c => c.GetString()).ToList(),
                "Expected: BUs sheet contains view permissions");

            //Validate the User Permissions file shows “X” for enabled permissions
            //Validate the User Permissions file shows blank cells for disabled permissions
            var rowIndex = sheet.GetRowIndexByCellValue(admin.Name);

            StringAssert.AreEqualIgnoringCase(
                string.Empty,
                sheet.Column(sheet.GetColumnIndexByName($"View {bU.Name}").Value).Cell(rowIndex).GetString(),
                $"Expected: user doesn't have permission for '{bU.Name}' => respective cell is blank");
            StringAssert.AreEqualIgnoringCase(
                "X",
                sheet.Column(sheet.GetColumnIndexByName("View Main").Value).Cell(rowIndex).GetString(),
                "Expected: user has permission for 'Main' BU => respective cell has 'X'");

            sheet = workbook.Worksheet(ReportingCategories);
            rowIndex = sheet.GetRowIndexByCellValue(admin.Name);
            var editCell = sheet.Column(sheet.GetColumnIndexByName($"Edit {Categories.Marketing}").Value)
                .Cell(rowIndex);
            var viewCell = sheet.Column(sheet.GetColumnIndexByName($"View {Categories.Marketing}").Value)
                .Cell(rowIndex);

            StringAssert.AreEqualIgnoringCase(
                string.Empty,
                editCell.GetString(),
                $"Expected: user doesn't have permission for 'Edit {Categories.Marketing}' => respective cell is blank");
            StringAssert.AreEqualIgnoringCase(
                "X",
                viewCell.GetString(),
                $"Expected: user has 'View {Categories.Marketing}' category permission => respective cell has 'X'");

            sheet = workbook.Worksheet(ReportingTemplates);
            rowIndex = sheet.GetRowIndexByCellValue(admin.Name);
            editCell = sheet.Column(sheet.GetColumnIndexByName($"Edit {Templates.TechPerf}").Value).Cell(rowIndex);
            viewCell = sheet.Column(sheet.GetColumnIndexByName($"View {Templates.TechPerf}").Value).Cell(rowIndex);

            StringAssert.AreEqualIgnoringCase(
                string.Empty,
                editCell.GetString(),
                $"Expected: user doesn't have permission for 'Edit {Templates.TechPerf}' => respective cell is blank");
            StringAssert.AreEqualIgnoringCase(
                "X",
                viewCell.GetString(),
                $"Expected: user has 'View {Templates.TechPerf}' template permission => respective cell has 'X'");

            //Validate user has view and edit permissions if the corresponding view cell is blank and the edit cell has “X”
            viewCell.Value = string.Empty;
            editCell.Value = "X";

            //Validate user has only view permission if the corresponding view cell has “X” and the edit cell is blank
            editCell = sheet.Column(sheet.GetColumnIndexByName($"Edit {Templates.Invoices}").Value).Cell(rowIndex);
            viewCell = sheet.Column(sheet.GetColumnIndexByName($"View {Templates.Invoices}").Value).Cell(rowIndex);
            viewCell.Value = "X";
            editCell.Value = string.Empty;

            //Validate user permissions can be enabled only by “X” or “x”
            sheet = workbook.Worksheet(ReportingCategories);
            rowIndex = sheet.GetRowIndexByCellValue(admin.Name);
            editCell = sheet.Column(sheet.GetColumnIndexByName($"Edit {Categories.Marketing}").Value).Cell(rowIndex);
            editCell.Value = "V";

            //Validate User Permissions file w/o Employees and Technicians tabs can be imported w/o errors and the permissions are changed
            workbook.Worksheet(Technicians).Delete();
            workbook.Worksheet(Employees).Delete();

            //Validate user does not lose permissions if the imported file has no rows with the user
            //Validate non-existing users do not appear on the reporting permissions page if the imported file has rows
            //with the non-existing users
            foreach (var worksheet in workbook.Worksheets) {
                worksheet.Cells().First(c => c.GetString() == admin2.Name).Value = NonExistingUser;
            }

            workbook.Save();

            //Validate users’ reporting permissions can be edited via the imported User Permissions file
            importExportPage.ImportUserPermissions(file.FullName);
            importExportPage.FullRefreshPage();

            //Validate all the excel changes apply correctly in UI
            browser.OpenSettingsPage<ReportingSettingsPage>().OpenPermissionsTab();

            Assert.False(
                permissionsPage.EmployeesPermissionsTable.ExistsItemByText(NonExistingUser),
                $"Expected: {NonExistingUser} should not appear on the reporting permissions page as a result of import");

            permissionsPage.EmployeesPermissionsTable.GetRowByText(admin2.Name).OpenPermissionsEditModal();

            Assert.True(
                permissionsModal.PermissionsTable.Any()
                && permissionsModal.PermissionsTable.First().Edit.Checked,
                "Expected: existing user does not lose permissions if the imported file has no rows with the user");

            permissionsModal.SaveAndWait();
            permissionsPage.EmployeesPermissionsTable.GetRowByText(admin.Name).OpenPermissionsEditModal();
            var row = permissionsModal.PermissionsTable.GetRowByText(Templates.TechPerf);

            Assert.True(
                row.Edit.Checked && row.View.Checked,
                "Expected: user has view and edit permissions if respective view cell is blank and the edit cell has 'X' in imported file");

            row = permissionsModal.PermissionsTable.GetRowByText(Templates.Invoices);

            Assert.True(
                !row.Edit.Checked && row.View.Checked,
                "Expected: user has only view permission if respective view cell has 'X' and the edit cell is blank in imported file");

            permissionsModal.OpenCategoriesTab();
            row = permissionsModal.PermissionsTable.GetRowByText(Categories.Marketing);

            Assert.False(row.Edit.Checked, "Expected: user permissions can be enabled only by 'X' or 'x'");
        }
    }
}
