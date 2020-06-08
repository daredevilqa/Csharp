using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceTitan.Services;
using ServiceTitan.Testing.Web.Framework;
using ServiceTitan.Testing.Web.Pages.Reports.Custom.Scheduling;
using ServiceTitan.UITests.DataGenerator;
using ServiceTitan.UITests.Helpers;
using ServiceTitan.UITests.Pages.Reports.Redesign;

namespace ServiceTitan.UITests.Tests.Reporting
{
    public class QA3955_SchedulingCustomReports_Cadence : TestBase
    {
        [Test]
        [Description("https://servicetitan.atlassian.net/browse/QA-3955")]
        [Category(TestCategories.Reporting)]
        public async Task QA3955_SchedulingCustomReports_Cadence_TS()
        {
            #region Data

            this.SetConfigurationProperty(RootHub.ConfigurationProperties().Reports20, true);
            this.SetConfigurationProperty(RootHub.ConfigurationProperties().Reports20Scheduler, true);
            this.SetConfigurationProperty(RootHub.ConfigurationProperties().Reports20Redesign, true);

            var technician = await UISimpleCreator.CreateSimpleTechnicianAsync(this);

            #endregion

            var browser = OpenDriver();
            browser.Login();

            var reportsPage = browser.OpenPageFromNavBar<ReportsMainPage>().OpenAllReportsTab();
            var cadenceStepPage = reportsPage
                .StartScheduling(type: SchedulingReportTypes.Flexible)
                .FillDetailsAndProceed()
                .SelectRecipientsAndProceed(recipients: technician.Name);

            // Validate if Report is Flexible than in the right low corner there is “Next” button
            Assert.True(
                cadenceStepPage.Next.ExistsAndDisplayed,
                "Next button (possibility to navigate to the 4th Filter step) should be available for flexible reports "
                + "instead of 'Schedule' button");

            // Validate user sees default values when navigates to Cadence page for the first time:
            // "Send report every [empty] [day] at [06:00 am]"
            var cadencePeriodSelect = cadenceStepPage.CadencePeriodSelect;
            var cadenceStepInput = cadenceStepPage.CadenceStepInput;
            var timeSelector = cadenceStepPage.TimeSelector;

            StringAssert.AreEqualIgnoringCase(
                string.Empty,
                cadenceStepInput.GetValue(),
                "Expected: Send report every [empty] [day] at [06:00 am]");
            StringAssert.AreEqualIgnoringCase(
                "day",
                cadencePeriodSelect.GetSelectedText(),
                "Expected: Send report every [empty] [day] at [06:00 am]");
            StringAssert.AreEqualIgnoringCase(
                "06:00 am",
                timeSelector.GetSelectedText(),
                "Expected: Send report every [empty] [day] at [06:00 am]");

            // Validate when all required fields are filled, Next button should be enabled
            cadenceStepInput.SetValue("1");
            AssertNextBtnIsEnabled(cadenceStepPage);

            // Validate the main dropdown contains: Day, Week, Month
            CollectionAssert.AreEqual(
                new[] { "day", "week", "month" },
                cadencePeriodSelect.GetAllOptionsForSelect(true),
                "Default cadence types do not match");

            // Validate user is allowed to enter only integer value into the inputs
            cadenceStepInput.SetValue("1.5");
            StringAssert.AreEqualIgnoringCase(
                "15",
                cadenceStepInput.GetValue(),
                "If a wrong non-integer value is typed in, delimiters should be omitted");

            // Validate frequency of sending is limited by 1 and 1000
            cadenceStepInput.SetValue("0");
            StringAssert.AreEqualIgnoringCase(
                "1",
                cadenceStepInput.GetValue(),
                "Expected: frequency of sending is limited by 1 (lower bound) and 1000 (upper bound) ");

            cadenceStepInput.SetValue("1001");
            StringAssert.AreEqualIgnoringCase(
                "1000",
                cadenceStepInput.GetValue(),
                "Expected: frequency of sending is limited by 1 (lower bound) and 1000 (upper bound) ");

            cadenceStepInput.SetValue("1");

            // Validate when “Day” selected, number of time options is limited by the number of 15 minute time ranges
            var startTime = DateTime.ParseExact("12:00 am", "hh:mm tt", CultureInfo.InvariantCulture);
            var expectedTimeSlots = new List<DateTime> { startTime };
            var actualTimeSlots = new List<DateTime>();

            while (startTime != DateTime.ParseExact("11:45 pm", "hh:mm tt", CultureInfo.InvariantCulture)) {
                var newTime = startTime.AddMinutes(15);
                startTime = newTime;
                expectedTimeSlots.Add(startTime);
            }

            foreach (var slot in timeSelector.GetAllOptionsForSelect(true)) {
                actualTimeSlots.Add(DateTime.ParseExact(slot, "hh:mm tt", CultureInfo.InvariantCulture));
            }

            CollectionAssert.AreEqual(
                expectedTimeSlots,
                actualTimeSlots,
                "Expected: number of time options is limited by the number of 15 minute time ranges of the whole day");

            // Validate when “Day” selected, user can add additional time
            Assert.True(
                cadenceStepPage.AddAnotherTime.ExistsAndDisplayed,
                "Expected: 'Add another time' link displayed when cadence type is Day");

            for (int i = 0; i < 3; i++) {
                cadenceStepPage.AddAnotherTime.Click();
                cadenceStepPage.AdditionalSlots[i].WaitElementByCondition();
            }

            Assert.AreEqual(
                3,
                cadenceStepPage.AdditionalSlots.Count(),
                "Expected: 3 additional slots should be added");

            cadenceStepPage.AdditionalSlots[0].AdditionalTimePicker.SelectItem("07:15 am", true);

            // Validate when “Day” selected, user cannot add time that was already used
            CollectionAssert.DoesNotContain(
                cadenceStepPage.AdditionalSlots[1].AdditionalTimePicker.GetAllOptionsForSelect(true),
                "06:00 am",
                "This time slot has been already used, thus, can't be added twice");

            // Validate when “Day” selected, user can remove additional time
            for (int i = 2; i >= 0; i--) {
                cadenceStepPage.AdditionalSlots[i].CrossBtn.ClickWithWaitDisappear();
            }
            CollectionAssert.IsEmpty(cadenceStepPage.AdditionalSlots);

            // Validate when “Week” selected, user can choose time and day of the week
            cadencePeriodSelect.SelectItem("week");

            Assert.True(
                cadenceStepPage.TimeSelector.ExistsAndDisplayed,
                "Expected: when the cadence type is week, user can choose time in addition to a day of the week");
            Assert.True(
                cadenceStepPage.DaysOfWeek.All(d => d.ExistsAndDisplayed && d.IsEnabled),
                "Expected: when the cadence type is week, user can choose any day of the week");
            Assert.AreEqual(
                7,
                cadenceStepPage.DaysOfWeek.Count(),
                "Expected: week contains 7 days");

            var expectedDaysLetters = new[] { "S", "M", "T", "W", "T", "F", "S" };

            for (int i = 0; i < 7; i++) {
                Assert.AreEqual(
                    expectedDaysLetters[i],
                    cadenceStepPage.DaysOfWeek[i].GetText(),
                    "Day's first letter doesn't match / Order of days is wrong");
            }

            // Validate when “Week” selected, days of the week are required parameter
            AssertNextBtnIsDisabled(cadenceStepPage);
            cadenceStepPage.DaysOfWeek[0].Click();
            AssertNextBtnIsEnabled(cadenceStepPage);
            cadenceStepPage.DaysOfWeek[0].Click();

            // Validate when “Week” selected, user can choose multiple days of the week and chosen days become blue
            foreach (var day in cadenceStepPage.DaysOfWeek) {
                day.Click();
                day.WaitElementByCondition();
                Assert.True(day.IsActive(), "Expected: chosen day icon should become active");
            }

            // Validate when “Week” selected, user can deselect chosen days of the week
            foreach (var day in cadenceStepPage.DaysOfWeek) {
                day.Click();
                day.WaitElementByCondition();
                Assert.False(day.IsActive(), "Expected: deselected day icon should become inactive");
            }

            // Validate when “Week” selected, user can not select additional time
            Assert.False(
                cadenceStepPage.AddAnotherTime.IsDisplayed,
                "Expected: when 'Week' selected, user can not select additional time");

            // Validate when “Month” selected, user can choose time and day of the month
            cadencePeriodSelect.SelectItem("month");
            var dayInput = cadenceStepPage.DayInput;

            Assert.True(cadenceStepPage.TimeSelector.ExistsAndDisplayed, "Expected: user can choose time");
            Assert.True(cadenceStepPage.DayInput.ExistsAndDisplayed, "Expected: user can specify day of the month");

            // Validate when “Month” selected, user can specify days from 1 to 31
            dayInput.SetValue("1.5");
            StringAssert.AreEqualIgnoringCase(
                "15",
                dayInput.GetValue(),
                "If a wrong non-integer value is typed in, delimiters should be omitted");

            dayInput.SetValue("0");
            StringAssert.AreEqualIgnoringCase(
                "1",
                dayInput.GetValue(),
                "There could only be days from 1 to 31 in the month");

            dayInput.SetValue("33");
            StringAssert.AreEqualIgnoringCase(
                "31",
                dayInput.GetValue(),
                "There could only be days from 1 to 31 in the month");

            // Validate when “Month” selected, user can choose only 4 days (1 main + 3 additional)
            Assert.True(cadenceStepPage.AddAnotherDay.ExistsAndDisplayed, "Add another day button isn't displayed");

            for (int i = 0; i < 3; i++) {
                cadenceStepPage.AddAnotherDay.Click();
                cadenceStepPage.AdditionalSlots[i].WaitElementByCondition();
            }

            Assert.AreEqual(
                3,
                cadenceStepPage.AdditionalSlots.Count(),
                "Expected: 3 additional slots should be added");

            Assert.True(
                cadenceStepPage.AddAnotherDay.Disabled(),
                "Expected: button should become disabled after adding 3 additional days");

            // Validate when “Month” selected, user cannot specify one day several times
            dayInput.SetValue("1");
            cadenceStepPage.AdditionalSlots[0].AdditionalDayInput.SetValue("1"); //main slot already has '1' specified
            cadenceStepPage.Next.WaitElementByCondition();

            AssertNextBtnIsDisabled(
                cadenceStepPage,
                "Expected: user cannot proceed to the next step since the same day has been specified twice");
            Assert.True(
                cadenceStepPage.AdditionalSlots[0].IsErrorState(),
                "Expected: input gets error state");

            // Validate when “Month” selected, user can remove additional days
            for (int i = 2; i >= 0; i--) {
                cadenceStepPage.AdditionalSlots[i].CrossBtn.ClickWithWaitDisappear();
            }
            CollectionAssert.IsEmpty(cadenceStepPage.AdditionalSlots);

            // Validate when “Month” selected, user cannot add additional time range
            Assert.False(
                cadenceStepPage.AddAnotherTime.IsDisplayed,
                "when cadence type is month, user cannot add additional time range");

            //Validate dropdown’s text changes to plural form when first field value is more than 1
            cadenceStepInput.SetValue("2");

            CollectionAssert.AreEqual(
                new[] { "days", "weeks", "months" },
                cadencePeriodSelect.GetAllOptionsForSelect(true),
                "Expected: Cadence types text changes to plural form when first field value is more than 1");

            // Validate if the Report is Flexible, then by clicking “Next” button, user goes to the next stage “Filter”
            var filterStepPage = cadenceStepPage.SelectCadenceAndProceedTo<FilterStepPage>(
                CadencePeriod.Month,
                time: "06:15 am",
                dayOfMonth: "2",
                isSimple: false);

            Assert.True(filterStepPage.ExistsAndDisplayed, "Expected: Filter 4th step page shown");

            // Validate in Flexible Report icon changes to green with a check mark when “Cadence” stage have been passed
            var progressBar = filterStepPage.ProgressBar;

            Assert.IsTrue(
                progressBar.CadenceStep.StepIsComplete(),
                "Third 'Cadence' step should be complete (icon changes to green with a check mark)");
            Assert.IsTrue(
                progressBar.FilterStep.StepIsActive(),
                "Fourth 'Filter' step should become active");

            StringAssert.Contains("4", progressBar.FilterStep.GetText(), "Fourth step should display '4'");
            StringAssert.Contains(
                "Filter",
                progressBar.FilterStep.GetText(),
                "Fourth step should display 'Filter'");

            // Validate chosen settings are saved after return from next stage
            filterStepPage.Back.ClickForOpen<CadenceStepPage>();

            StringAssert.AreEqualIgnoringCase("month", cadencePeriodSelect.GetSelectedText());
            StringAssert.AreEqualIgnoringCase("2", dayInput.GetValue());
            StringAssert.AreEqualIgnoringCase("06:15 am", timeSelector.GetSelectedText());

            cadenceStepPage.Header.BackToReports.ClickForOpen<AllReportsTab>();
        }

        private void AssertNextBtnIsDisabled(
            SchedulerPage page,
            string msg = "Expected: 'Next' button is grey and can’t be clicked while all required fields are empty")
        {
            Assert.True(page.Next.Disabled(), msg);
        }

        private void AssertNextBtnIsEnabled(
            SchedulerPage page,
            string msg = "Expected: Next button is active(blue) and can be clicked once required fields are not empty")
        {
            page.Next.WaitDomLoad();
            Assert.True(page.Next.Enabled(), msg);
        }
    }
}
