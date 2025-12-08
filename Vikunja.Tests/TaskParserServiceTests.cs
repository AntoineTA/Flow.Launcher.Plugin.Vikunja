using Flow.Launcher.Plugin.Vikunja;
using Xunit;

namespace Flow.Launcher.Plugin.Vikunja.Tests
{
    public class TaskParserServiceTests
    {
        private readonly TaskParserService _parser;

        public TaskParserServiceTests()
        {
            _parser = new TaskParserService();
        }

        [Fact]
        public void ParseTask_BasicTask_ReturnsCorrectTitle()
        {
            // Arrange
            var input = "buy groceries";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("buy groceries", result.Title);
            Assert.Null(result.DueDate);
            Assert.Empty(result.Labels);
            Assert.Null(result.Project);
            Assert.Equal(0, result.Priority);
        }

        [Fact]
        public void ParseTask_WithLabel_Vikunja_ExtractsLabel()
        {
            // Arrange
            var input = "task *urgent";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("task", result.Title);
            Assert.Contains("urgent", result.Labels);
        }

        [Fact]
        public void ParseTask_WithLabel_Todoist_ExtractsLabel()
        {
            // Arrange
            var input = "task @urgent";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Todoist);

            // Assert
            Assert.Equal("task", result.Title);
            Assert.Contains("urgent", result.Labels);
        }

        [Fact]
        public void ParseTask_WithProject_Vikunja_ExtractsProject()
        {
            // Arrange
            var input = "meeting +work";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("meeting", result.Title);
            Assert.Equal("work", result.Project);
        }

        [Fact]
        public void ParseTask_WithProject_Todoist_ExtractsProject()
        {
            // Arrange
            var input = "meeting #work";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Todoist);

            // Assert
            Assert.Equal("meeting", result.Title);
            Assert.Equal("work", result.Project);
        }

        [Fact]
        public void ParseTask_WithPriority_Vikunja_ExtractsPriority()
        {
            // Arrange
            var input = "urgent task !3";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("urgent task", result.Title);
            Assert.Equal(3, result.Priority);
        }

        [Fact]
        public void ParseTask_WithPriority_Todoist_ExtractsPriority()
        {
            // Arrange
            var input = "urgent task p1";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Todoist);

            // Assert
            Assert.Equal("urgent task", result.Title);
            Assert.Equal(1, result.Priority); // p1 = priority 1 (direct mapping)
        }

        [Fact]
        public void ParseTask_WithSimpleDate_ParsesCorrectly()
        {
            // Arrange
            var input = "meeting tomorrow";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("meeting", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(DateTime.Today.AddDays(1).Date, result.DueDate.Value.Date);
        }

        [Fact]
        public void ParseTask_ComplexWithAllComponents_ParsesCorrectly()
        {
            // Arrange
            var input = "team meeting tomorrow 3pm +work !5 *urgent *meeting";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("team meeting", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(15, result.DueDate.Value.Hour); // 3pm = 15:00
            Assert.Equal("work", result.Project);
            Assert.Equal(5, result.Priority);
            Assert.Contains("urgent", result.Labels);
            Assert.Contains("meeting", result.Labels);
        }

        // Time-of-day word tests
        [Fact]
        public void ParseTask_WithMorning_SetsTimeTo9AM()
        {
            // Arrange
            var input = "call client tomorrow morning";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("call client", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(9, result.DueDate.Value.Hour);
            Assert.Equal(0, result.DueDate.Value.Minute);
        }

        [Fact]
        public void ParseTask_WithAfternoon_SetsTimeTo2PM()
        {
            // Arrange
            var input = "dentist appointment next monday afternoon";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("dentist appointment", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(14, result.DueDate.Value.Hour);
            Assert.Equal(0, result.DueDate.Value.Minute);
        }

        [Fact]
        public void ParseTask_WithEvening_SetsTimeTo6PM()
        {
            // Arrange
            var input = "dinner reservation friday evening";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("dinner reservation", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(18, result.DueDate.Value.Hour);
            Assert.Equal(0, result.DueDate.Value.Minute);
        }

        [Fact]
        public void ParseTask_WithNight_SetsTimeTo8PM()
        {
            // Arrange
            var input = "movie tonight";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("movie", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(20, result.DueDate.Value.Hour);
            Assert.Equal(0, result.DueDate.Value.Minute);
        }

        // AM/PM format tests
        [Fact]
        public void ParseTask_WithAMFormat_ParsesCorrectly()
        {
            // Arrange
            var input = "breakfast meeting tomorrow 9am";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("breakfast meeting", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(9, result.DueDate.Value.Hour);
            Assert.Equal(0, result.DueDate.Value.Minute);
        }

        [Fact]
        public void ParseTask_WithPMFormat_ParsesCorrectly()
        {
            // Arrange
            var input = "presentation next week 3pm";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("presentation", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(15, result.DueDate.Value.Hour);
            Assert.Equal(0, result.DueDate.Value.Minute);
        }

        [Fact]
        public void ParseTask_WithAMPMAndMinutes_ParsesCorrectly()
        {
            // Arrange
            var input = "doctor appointment tomorrow 10:30am";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("doctor appointment", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(10, result.DueDate.Value.Hour);
            Assert.Equal(30, result.DueDate.Value.Minute);
        }

        [Fact]
        public void ParseTask_WithPMAndMinutes_ParsesCorrectly()
        {
            // Arrange
            var input = "meeting friday 4:45pm";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("meeting", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(16, result.DueDate.Value.Hour);
            Assert.Equal(45, result.DueDate.Value.Minute);
        }

        [Fact]
        public void ParseTask_WithDottedAMFormat_ParsesCorrectly()
        {
            // Arrange
            var input = "call tomorrow 9 a.m.";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("call", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(9, result.DueDate.Value.Hour);
        }

        // 24-hour format tests
        [Fact]
        public void ParseTask_With24HourFormat_ParsesCorrectly()
        {
            // Arrange
            var input = "train departure tomorrow 14:30";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("train departure", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(14, result.DueDate.Value.Hour);
            Assert.Equal(30, result.DueDate.Value.Minute);
        }

        [Fact]
        public void ParseTask_With24HourNoMinutes_ParsesCorrectly()
        {
            // Arrange
            var input = "meeting next monday 9:00";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("meeting", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(9, result.DueDate.Value.Hour);
            Assert.Equal(0, result.DueDate.Value.Minute);
        }

        // European h-notation tests
        [Fact]
        public void ParseTask_WithEuropeanHNotation_ParsesCorrectly()
        {
            // Arrange
            var input = "conference call tomorrow 17h30";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("conference call", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(17, result.DueDate.Value.Hour);
            Assert.Equal(30, result.DueDate.Value.Minute);
        }

        [Fact]
        public void ParseTask_WithEuropeanHNoMinutes_ParsesCorrectly()
        {
            // Arrange
            var input = "lunch tomorrow 12h";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("lunch", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(12, result.DueDate.Value.Hour);
            Assert.Equal(0, result.DueDate.Value.Minute);
        }

        // Standalone time tests
        [Fact]
        public void ParseTask_WithStandaloneTime_SetsNextOccurrence()
        {
            // Arrange
            var input = "call client 3pm";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("call client", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(15, result.DueDate.Value.Hour);
            // Date should be today if time hasn't passed, or tomorrow if it has
            var now = DateTime.Now;
            if (now.Hour < 15)
            {
                Assert.Equal(DateTime.Today.Date, result.DueDate.Value.Date);
            }
            else
            {
                Assert.Equal(DateTime.Today.AddDays(1).Date, result.DueDate.Value.Date);
            }
        }

        [Fact]
        public void ParseTask_WithStandalone24HourTime_SetsNextOccurrence()
        {
            // Arrange
            var input = "submit report 17:30";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("submit report", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(17, result.DueDate.Value.Hour);
            Assert.Equal(30, result.DueDate.Value.Minute);
        }

        // Date-only tests (no time should default to midnight)
        [Fact]
        public void ParseTask_WithDateOnly_NoTime_SetsMidnight()
        {
            // Arrange
            var input = "project deadline next friday";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("project deadline", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(0, result.DueDate.Value.Hour);
            Assert.Equal(0, result.DueDate.Value.Minute);
        }

        // Todoist mode comprehensive test
        [Fact]
        public void ParseTask_TodoistMode_ComplexTask_ParsesCorrectly()
        {
            // Arrange
            var input = "review PR tomorrow afternoon #development @urgent @review p2";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Todoist);

            // Assert
            Assert.Equal("review PR", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(14, result.DueDate.Value.Hour); // afternoon = 2pm
            Assert.Equal("development", result.Project);
            Assert.Contains("urgent", result.Labels);
            Assert.Contains("review", result.Labels);
            Assert.Equal(2, result.Priority); // p2 = priority 2 (direct mapping)
        }

        // Edge cases
        [Fact]
        public void ParseTask_MultipleLabels_ExtractsAll()
        {
            // Arrange
            var input = "task *label1 *label2 *label3";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("task", result.Title);
            Assert.Equal(3, result.Labels.Count);
            Assert.Contains("label1", result.Labels);
            Assert.Contains("label2", result.Labels);
            Assert.Contains("label3", result.Labels);
        }

        [Fact]
        public void ParseTask_NoDateOrTime_ReturnsNullDueDate()
        {
            // Arrange
            var input = "simple task with no date";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("simple task with no date", result.Title);
            Assert.Null(result.DueDate);
        }

        [Fact]
        public void ParseTask_EmptyInput_ReturnsEmptyTitle()
        {
            // Arrange
            var input = "";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("", result.Title);
            Assert.Null(result.DueDate);
        }

        // Specific date format tests
        [Fact]
        public void ParseTask_WithDayMonthName_ParsesCorrectly()
        {
            // Arrange
            var input = "meeting 5 February";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("meeting", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(2, result.DueDate.Value.Month); // February
            Assert.Equal(5, result.DueDate.Value.Day);
        }

        [Fact]
        public void ParseTask_WithDayAbbreviatedMonth_ParsesCorrectly()
        {
            // Arrange
            var input = "deadline 15 Mar";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("deadline", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(3, result.DueDate.Value.Month); // March
            Assert.Equal(15, result.DueDate.Value.Day);
        }

        [Fact]
        public void ParseTask_WithMonthNameDay_ParsesCorrectly()
        {
            // Arrange
            var input = "appointment February 20";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("appointment", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(2, result.DueDate.Value.Month); // February
            Assert.Equal(20, result.DueDate.Value.Day);
        }

        [Fact]
        public void ParseTask_WithAbbreviatedMonthDay_ParsesCorrectly()
        {
            // Arrange
            var input = "task Apr 10";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("task", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(4, result.DueDate.Value.Month); // April
            Assert.Equal(10, result.DueDate.Value.Day);
        }

        [Fact]
        public void ParseTask_WithSpecificDateAndTime_ParsesCorrectly()
        {
            // Arrange
            var input = "meeting 3 Feb 2pm";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("meeting", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(2, result.DueDate.Value.Month); // February
            Assert.Equal(3, result.DueDate.Value.Day);
            Assert.Equal(14, result.DueDate.Value.Hour); // 2pm = 14:00
        }

        [Fact]
        public void ParseTask_WithMonthDayAndTime_ParsesCorrectly()
        {
            // Arrange
            var input = "presentation December 25 9:30am";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("presentation", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(12, result.DueDate.Value.Month); // December
            Assert.Equal(25, result.DueDate.Value.Day);
            Assert.Equal(9, result.DueDate.Value.Hour);
            Assert.Equal(30, result.DueDate.Value.Minute);
        }

        [Fact]
        public void ParseTask_WithNumericDate_ParsesCorrectly()
        {
            // Arrange
            var input = "report 2/15";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("report", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(2, result.DueDate.Value.Month);
            Assert.Equal(15, result.DueDate.Value.Day);
        }

        [Fact]
        public void ParseTask_WithNumericDateUnambiguous_ParsesCorrectly()
        {
            // Arrange
            var input = "deadline 15/4";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("deadline", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(4, result.DueDate.Value.Month); // April
            Assert.Equal(15, result.DueDate.Value.Day);
        }

        [Fact]
        public void ParseTask_WithFullNumericDate_ParsesCorrectly()
        {
            // Arrange
            var input = "vacation 7/4/2026";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("vacation", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(7, result.DueDate.Value.Month);
            Assert.Equal(4, result.DueDate.Value.Day);
            Assert.Equal(2026, result.DueDate.Value.Year);
        }

        [Fact]
        public void ParseTask_WithFullNumericDateWithCommas_ParsesCorrectly()
        {
            // Arrange
            var input = "meeting 12/31/2025";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("meeting", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(2025, result.DueDate.Value.Year);
            Assert.Equal(12, result.DueDate.Value.Month);
            Assert.Equal(31, result.DueDate.Value.Day);
        }

        [Fact]
        public void ParseTask_SpecificDateWithProjectAndPriority_ParsesCorrectly()
        {
            // Arrange
            var input = "launch event March 15 3pm +marketing !5 *important";

            // Act
            var result = _parser.ParseTask(input, ParsingMode.Vikunja);

            // Assert
            Assert.Equal("launch event", result.Title);
            Assert.NotNull(result.DueDate);
            Assert.Equal(3, result.DueDate.Value.Month); // March
            Assert.Equal(15, result.DueDate.Value.Day);
            Assert.Equal(15, result.DueDate.Value.Hour); // 3pm
            Assert.Equal("marketing", result.Project);
            Assert.Equal(5, result.Priority);
            Assert.Contains("important", result.Labels);
        }
    }
}
