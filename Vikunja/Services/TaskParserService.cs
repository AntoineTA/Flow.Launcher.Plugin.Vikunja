using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Flow.Launcher.Plugin.Vikunja
{
    public class TaskParserService
    {
        // Compiled regex patterns for better performance
        private static readonly Regex VikunjaLabelPattern = new Regex(@"\*(?:""([^""]+)""|'([^']+)'|(\w+))", RegexOptions.Compiled);
        private static readonly Regex VikunjaPriorityPattern = new Regex(@"!([1-5])", RegexOptions.Compiled);
        private static readonly Regex VikunjaProjectPattern = new Regex(@"\+(?:""([^""]+)""|'([^']+)'|(\w+))", RegexOptions.Compiled);
        
        private static readonly Regex TodoistProjectPattern = new Regex(@"#(?:""([^""]+)""|'([^']+)'|([^\s]+))", RegexOptions.Compiled);
        private static readonly Regex TodoistLabelPattern = new Regex(@"@(?:""([^""]+)""|'([^']+)'|([^\s]+))", RegexOptions.Compiled);
        private static readonly Regex TodoistPriorityPattern = new Regex(@"p([1-5])", RegexOptions.Compiled);
        
        private static readonly Regex WhitespacePattern = new Regex(@"\s+", RegexOptions.Compiled);
        public ParsedTask ParseTask(string input, ParsingMode mode)
        {
            return mode == ParsingMode.Vikunja 
                ? ParseVikunjaStyle(input) 
                : ParseTodoistStyle(input);
        }

        private ParsedTask ParseVikunjaStyle(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new ParsedTask { Title = string.Empty };

            var task = new ParsedTask();
            var text = input.Trim();

            // Extract labels (*label or *"label with space")
            var labelMatches = VikunjaLabelPattern.Matches(text);
            foreach (Match match in labelMatches)
            {
                var label = GetFirstNonEmptyGroup(match, 1, 2, 3);
                if (!string.IsNullOrEmpty(label))
                {
                    task.Labels.Add(label);
                }
            }
            text = VikunjaLabelPattern.Replace(text, "");

            // Extract priority (!1-5)  
            var priorityMatch = VikunjaPriorityPattern.Match(text);
            if (priorityMatch.Success)
            {
                task.Priority = int.Parse(priorityMatch.Groups[1].Value);
            }
            text = VikunjaPriorityPattern.Replace(text, "");

            // Extract project (+project or +"project with space")
            var projectMatch = VikunjaProjectPattern.Match(text);
            if (projectMatch.Success)
            {
                task.Project = GetFirstNonEmptyGroup(projectMatch, 1, 2, 3);
            }
            text = VikunjaProjectPattern.Replace(text, "");

            // Extract dates (various natural language formats)
            var dateMatch = ExtractDate(text);
            if (dateMatch.HasValue)
            {
                task.DueDate = dateMatch.Value.date;
                text = text.Replace(dateMatch.Value.originalText, "", StringComparison.OrdinalIgnoreCase);
            }

            // Clean up the remaining text as the title
            task.Title = CleanupText(text);

            return task;
        }

        private ParsedTask ParseTodoistStyle(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new ParsedTask { Title = string.Empty };

            var task = new ParsedTask();
            var text = input.Trim();
            var removedParts = new List<string>();

            // Extract project (#ProjectName or #"Project Name")
            var projectMatches = TodoistProjectPattern.Matches(text);
            foreach (Match match in projectMatches)
            {
                if (string.IsNullOrEmpty(task.Project)) // Only take the first project
                {
                    task.Project = GetFirstNonEmptyGroup(match, 1, 2, 3);
                }
                removedParts.Add(match.Value);
            }

            // Extract labels (@label or @"label name")
            var labelMatches = TodoistLabelPattern.Matches(text);
            foreach (Match match in labelMatches)
            {
                var label = GetFirstNonEmptyGroup(match, 1, 2, 3);
                if (!string.IsNullOrEmpty(label))
                {
                    task.Labels.Add(label);
                    removedParts.Add(match.Value);
                }
            }

            // Extract priority (p1-p5, same as Vikunja !1-5)
            var priorityMatch = TodoistPriorityPattern.Match(text);
            if (priorityMatch.Success && int.TryParse(priorityMatch.Groups[1].Value, out var todoistPriority))
            {
                task.Priority = todoistPriority; // Direct mapping: p1=1, p2=2, ..., p5=5
                removedParts.Add(priorityMatch.Value);
            }

            // Extract dates (various natural language formats)
            var dateMatch = ExtractDate(text);
            if (dateMatch.HasValue)
            {
                task.DueDate = dateMatch.Value.date;
                removedParts.Add(dateMatch.Value.originalText);
            }

            // Remove all parsed parts from the text
            foreach (var part in removedParts)
            {
                text = text.Replace(part, " ", StringComparison.OrdinalIgnoreCase);
            }

            // Clean up the remaining text as the title
            task.Title = CleanupText(text);

            return task;
        }

        private (DateTime date, string originalText)? ExtractDate(string text)
        {
            
            var now = DateTime.Now;
            var today = now.Date;

            // Common date patterns with time support
            var patterns = new Dictionary<string, Func<DateTime>>
            {
                { @"\btoday morning\b", () => today.AddHours(9) }, // 9 AM today
                { @"\btoday afternoon\b", () => today.AddHours(14) }, // 2 PM today
                { @"\btoday evening\b", () => today.AddHours(18) }, // 6 PM today
                { @"\bthis morning\b", () => today.AddHours(9) }, // 9 AM today
                { @"\bthis afternoon\b", () => today.AddHours(14) }, // 2 PM today
                { @"\bthis evening\b", () => today.AddHours(18) }, // 6 PM today
                { @"\btoday\b", () => today },
                { @"\btonight\b", () => today.AddHours(20) }, // 8 PM
                { @"\btomorrow morning\b", () => today.AddDays(1).AddHours(9) }, // 9 AM tomorrow
                { @"\btomorrow afternoon\b", () => today.AddDays(1).AddHours(14) }, // 2 PM tomorrow
                { @"\btomorrow evening\b", () => today.AddDays(1).AddHours(18) }, // 6 PM tomorrow
                { @"\btomorrow night\b", () => today.AddDays(1).AddHours(20) }, // 8 PM tomorrow
                { @"\btomorrow\b", () => today.AddDays(1) },
                { @"\bnext monday\b", () => GetNextWeekday(today, DayOfWeek.Monday) },
                { @"\bnext tuesday\b", () => GetNextWeekday(today, DayOfWeek.Tuesday) },
                { @"\bnext wednesday\b", () => GetNextWeekday(today, DayOfWeek.Wednesday) },
                { @"\bnext thursday\b", () => GetNextWeekday(today, DayOfWeek.Thursday) },
                { @"\bnext friday\b", () => GetNextWeekday(today, DayOfWeek.Friday) },
                { @"\bnext saturday\b", () => GetNextWeekday(today, DayOfWeek.Saturday) },
                { @"\bnext sunday\b", () => GetNextWeekday(today, DayOfWeek.Sunday) },
                { @"\bmonday morning\b", () => GetNextWeekday(today, DayOfWeek.Monday).AddHours(9) },
                { @"\bmonday afternoon\b", () => GetNextWeekday(today, DayOfWeek.Monday).AddHours(14) },
                { @"\bmonday evening\b", () => GetNextWeekday(today, DayOfWeek.Monday).AddHours(18) },
                { @"\bmonday\b", () => GetNextWeekday(today, DayOfWeek.Monday) },
                { @"\btuesday\b", () => GetNextWeekday(today, DayOfWeek.Tuesday) },
                { @"\bwednesday\b", () => GetNextWeekday(today, DayOfWeek.Wednesday) },
                { @"\bthursday\b", () => GetNextWeekday(today, DayOfWeek.Thursday) },
                { @"\bfriday\b", () => GetNextWeekday(today, DayOfWeek.Friday) },
                { @"\bsaturday\b", () => GetNextWeekday(today, DayOfWeek.Saturday) },
                { @"\bsunday\b", () => GetNextWeekday(today, DayOfWeek.Sunday) },
                { @"\bthis weekend\b", () => GetNextWeekday(today, DayOfWeek.Saturday) },
                { @"\bnext week\b", () => today.AddDays(7) },
                { @"\bnext month\b", () => today.AddMonths(1) },
                { @"\bend of month\b", () => new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month)) }
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern.Key, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var dateTime = pattern.Value();
                    var matchedText = match.Value;
                    
                    // Check if the matched pattern already includes time (e.g., "tomorrow afternoon")
                    bool alreadyHasTime = Regex.IsMatch(matchedText, 
                        @"\b(morning|afternoon|evening|night|noon|midnight)\b", 
                        RegexOptions.IgnoreCase);
                    
                    // Only check for additional time information if the pattern doesn't already include it
                    if (!alreadyHasTime)
                    {
                        var timeOfDay = ExtractTimeOfDay(text);
                        if (timeOfDay.HasValue)
                        {
                            dateTime = dateTime.Date.Add(timeOfDay.Value.time);
                            matchedText = match.Value + " " + timeOfDay.Value.matchedWord;
                        }
                    }
                    
                    return (dateTime, matchedText);
                }
            }

            // Try relative dates like "in 5 days", "in 2 weeks"
            var relativeMatch = Regex.Match(text, @"\bin (\d+) (day|days|week|weeks|month|months)\b", RegexOptions.IgnoreCase);
            if (relativeMatch.Success)
            {
                var number = int.Parse(relativeMatch.Groups[1].Value);
                var unit = relativeMatch.Groups[2].Value.ToLower();
                
                var date = unit.StartsWith("day") ? today.AddDays(number) :
                          unit.StartsWith("week") ? today.AddDays(number * 7) :
                          today.AddMonths(number);
                          
                return (date, relativeMatch.Value);
            }

            // Try specific date formats (date-only, no time support)
            var dateFormats = new[]
            {
                // Month Day Year patterns
                @"(\w+) (\d{1,2}), (\d{4})", // "Feb 17, 2026", "March 3, 2025"
                @"(\w+) (\d{1,2}) (\d{4})", // "Feb 17 2026", "March 3 2025"
                @"(\w+) (\d{1,2})(?:st|nd|rd|th), (\d{4})", // "Feb 2nd, 2026", "March 3rd, 2025"
                @"(\w+) (\d{1,2})(?:st|nd|rd|th) (\d{4})", // "Feb 2nd 2026", "March 3rd 2025"
                
                // Month Day (current/next year)
                @"(\w+) (\d{1,2})(?:st|nd|rd|th)", // "Feb 2nd", "March 3rd"
                @"(\w+) (\d{1,2})", // "Feb 17", "March 3", "Feb 4"
                
                // Day Month Year patterns
                @"(\d{1,2})(?:st|nd|rd|th) (\w+), (\d{4})", // "2nd Feb, 2026", "3rd March, 2025"
                @"(\d{1,2})(?:st|nd|rd|th) (\w+) (\d{4})", // "2nd Feb 2026", "3rd March 2025"
                @"(\d{1,2})(?:st|nd|rd|th) (\w+)", // "2nd Feb", "3rd March"
                @"(\d{1,2}) (\w+), (\d{4})", // "2 Feb, 2026", "3 March, 2025"
                @"(\d{1,2}) (\w+) (\d{4})", // "2 Feb 2026", "3 March 2025"
                @"(\d{1,2}) (\w+)", // "2 Feb", "3 March"
                
                // Numeric date patterns
                @"(\d{1,2})/(\d{1,2})/(\d{4})", // "2/3/2026", "12/25/2025"
                @"(\d{1,2})/(\d{1,2})", // "2/3", "12/25"
                
                // Day-only patterns
                @"(\d{1,2})th", // "17th"
                @"(\d{1,2})st", // "1st", "21st"
                @"(\d{1,2})nd", // "2nd", "22nd"
                @"(\d{1,2})rd" // "3rd", "23rd"
            };

            foreach (var format in dateFormats)
            {
                var match = Regex.Match(text, format, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    for (int i = 0; i < match.Groups.Count; i++)
                    {
                    }
                    
                    var parsed = TryParseSpecificDate(match, today);
                    if (parsed.HasValue)
                    {
                        
                        // Check if there's a time-of-day word in the remaining text
                        var timeOfDay = ExtractTimeOfDay(text);
                        string matchedText = match.Value;
                        if (timeOfDay.HasValue)
                        {
                            parsed = parsed.Value.Date.Add(timeOfDay.Value.time);
                            matchedText = match.Value + " " + timeOfDay.Value.matchedWord;
                        }
                        
                        return (parsed.Value, matchedText);
                    }
                    else
                    {
                    }
                }
            }

            // Check for standalone time (no date specified) - apply to today
            var standaloneTime = ExtractTimeOfDay(text);
            if (standaloneTime.HasValue)
            {
                var resultDateTime = today.Date.Add(standaloneTime.Value.time);
                
                // If the time has already passed today, use tomorrow
                // Note: midnight (00:00) should always be next day when specified alone
                if (resultDateTime <= DateTime.Now)
                {
                    resultDateTime = resultDateTime.AddDays(1);
                }
                
                return (resultDateTime, standaloneTime.Value.matchedWord);
            }

            return null;
        }

        private DateTime GetNextWeekday(DateTime from, DayOfWeek dayOfWeek)
        {
            var daysUntil = ((int)dayOfWeek - (int)from.DayOfWeek + 7) % 7;
            if (daysUntil == 0) daysUntil = 7; // Next week, not today
            return from.AddDays(daysUntil);
        }

        private DateTime? TryParseSpecificDate(Match match, DateTime today)
        {
            try
            {
                
                // Only handle date-only patterns (no time support)
                return ParseDateOnly(match, today);
            }
            catch (Exception)
            {
                // Ignore parsing errors
            }

            return null;
        }
        
        private (TimeSpan time, string matchedWord)? ExtractTimeOfDay(string text)
        {
            // First, check for AM/PM time patterns (e.g., "3pm", "4:30 PM", "9 A.M.")
            // Pattern matches: am, AM, a.m., A.M., pm, PM, p.m., P.M.
            var ampmPatterns = new[]
            {
                @"\b(?:at )?(\d{1,2}):(\d{2})\s*([ap]\.m\.|am|pm)",  // "4:30pm", "at 9:15 A.M."
                @"\b(?:at )?(\d{1,2})\s*([ap]\.m\.|am|pm)"           // "3pm", "at 4 A.M."
            };
            
            foreach (var pattern in ampmPatterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    int hour = int.Parse(match.Groups[1].Value);
                    int minute = 0;
                    string ampm;
                    
                    // Check if we have minutes (first pattern) or not (second pattern)
                    if (match.Groups[2].Value.Any(char.IsDigit))
                    {
                        minute = int.Parse(match.Groups[2].Value);
                        ampm = match.Groups[3].Value;
                    }
                    else
                    {
                        ampm = match.Groups[2].Value;
                    }
                    
                    // Convert to 24-hour format
                    bool isPM = ampm.ToLower().StartsWith("p");
                    if (isPM && hour != 12)
                        hour += 12;
                    else if (!isPM && hour == 12)
                        hour = 0;
                    
                    if (hour >= 0 && hour <= 23 && minute >= 0 && minute <= 59)
                    {
                        var timeSpan = new TimeSpan(hour, minute, 0);
                        return (timeSpan, match.Value);
                    }
                }
            }
            
            // Check for European time format with 'h' (e.g., "17h56", "4h")
            var europeanMatch = Regex.Match(text, @"\b(?:at )?(\d{1,2})h(\d{2})?", RegexOptions.IgnoreCase);
            if (europeanMatch.Success)
            {
                int hour = int.Parse(europeanMatch.Groups[1].Value);
                int minute = 0;
                if (europeanMatch.Groups[2].Success && !string.IsNullOrEmpty(europeanMatch.Groups[2].Value))
                {
                    minute = int.Parse(europeanMatch.Groups[2].Value);
                }
                
                if (hour >= 0 && hour <= 23 && minute >= 0 && minute <= 59)
                {
                    var timeSpan = new TimeSpan(hour, minute, 0);
                    return (timeSpan, europeanMatch.Value);
                }
            }
            
            // Check for 24-hour time format with colon (e.g., "9:20", "13:45")
            var time24Match = Regex.Match(text, @"\b(?:at )?(\d{1,2}):(\d{2})\b", RegexOptions.IgnoreCase);
            if (time24Match.Success)
            {
                int hour = int.Parse(time24Match.Groups[1].Value);
                int minute = int.Parse(time24Match.Groups[2].Value);
                
                if (hour >= 0 && hour <= 23 && minute >= 0 && minute <= 59)
                {
                    var timeSpan = new TimeSpan(hour, minute, 0);
                    return (timeSpan, time24Match.Value);
                }
            }
            
            // Map time-of-day keywords to specific times
            var timeOfDayPatterns = new Dictionary<string, TimeSpan>(StringComparer.OrdinalIgnoreCase)
            {
                { "morning", new TimeSpan(9, 0, 0) },      // 9:00 AM
                { "afternoon", new TimeSpan(14, 0, 0) },   // 2:00 PM
                { "evening", new TimeSpan(18, 0, 0) },     // 6:00 PM
                { "night", new TimeSpan(20, 0, 0) },       // 8:00 PM
                { "noon", new TimeSpan(12, 0, 0) },        // 12:00 PM
                { "midnight", new TimeSpan(0, 0, 0) }      // 12:00 AM
            };
            
            foreach (var pattern in timeOfDayPatterns)
            {
                // Match the keyword with optional "at" or "in the" prefix
                var match = Regex.Match(text, $@"\b(?:at |in the )?{pattern.Key}\b", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return (pattern.Value, match.Value);
                }
            }
            
            return null;
        }
        
        private DateTime? ParseDateOnly(Match match, DateTime today)
        {
            try
            {
                // Handle ordinal dates like "17th", "1st", "2nd", "3rd"
                if (match.Groups.Count == 2)
                {
                    var day = int.Parse(match.Groups[1].Value);
                    
                    // Try current month first
                    var thisMonth = new DateTime(today.Year, today.Month, day);
                    if (thisMonth > today)
                    {
                        return thisMonth;
                    }
                    
                    // If current month date has passed, try next month
                    var nextMonth = thisMonth.AddMonths(1);
                    return nextMonth;
                }

                // Handle month/day formats
                if (match.Groups.Count >= 3)
                {
                    // Try to parse as date with various formats
                    var formats = new[] { 
                        // MM/dd formats
                        "M/d/yyyy", "MM/dd/yyyy", "d/M/yyyy", "dd/MM/yyyy", "M/d", "MM/dd", "d/M", "dd/MM",
                        // Month Day Year with comma
                        "MMM d, yyyy", "MMMM d, yyyy", "MMM dd, yyyy", "MMMM dd, yyyy",
                        // Month Day Year without comma
                        "MMM d yyyy", "MMMM d yyyy", "MMM dd yyyy", "MMMM dd yyyy", 
                        // Month Day without year
                        "MMM d", "MMMM d", "MMM dd", "MMMM dd",
                        // Day Month Year with comma
                        "d MMM, yyyy", "dd MMM, yyyy", "d MMMM, yyyy", "dd MMMM, yyyy",
                        // Day Month Year without comma  
                        "d MMM yyyy", "dd MMM yyyy", "d MMMM yyyy", "dd MMMM yyyy",
                        // Day Month without year
                        "d MMM", "dd MMM", "d MMMM", "dd MMMM"
                    };
                    
                    // Handle ordinal cleanup for parsing (remove st, nd, rd, th suffixes)
                    var cleanedValue = match.Value;
                    cleanedValue = Regex.Replace(cleanedValue, @"(\d+)(?:st|nd|rd|th)\b", "$1", RegexOptions.IgnoreCase);
                    
                    if (DateTime.TryParseExact(cleanedValue, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                    {
                        
                        // Check if the input actually contained a year (by checking if year is in the original input)
                        bool hasExplicitYear = match.Value.Contains(parsed.Year.ToString());
                        
                        if (hasExplicitYear)
                        {
                            // User explicitly specified a year, respect their choice (even if it's in the past)
                            return parsed;
                        }
                        
                        // No year specified - ensure it's in the future
                        var futureDate = new DateTime(today.Year, parsed.Month, parsed.Day);
                        if (futureDate > today)
                        {
                            return futureDate;
                        }
                        else
                        {
                            // Date has passed this year, use next year
                            var nextYearDate = futureDate.AddYears(1);
                            return nextYearDate;
                        }
                    }
                    
                    // Try alternative parsing for formats like "Feb 3", "February 3"
                    // Clean up ordinals first
                    var alternativeCleanedValue = match.Value;
                    alternativeCleanedValue = Regex.Replace(alternativeCleanedValue, @"(\d+)(?:st|nd|rd|th)\b", "$1", RegexOptions.IgnoreCase);
                    
                    if (DateTime.TryParse(alternativeCleanedValue, out var alternativeParsed))
                    {
                        
                        // Check if the input contains an explicit year
                        bool hasExplicitYear = match.Value.Contains(alternativeParsed.Year.ToString());
                        
                        if (hasExplicitYear)
                        {
                            // User explicitly specified a year, respect their choice (even if it's in the past)
                            return alternativeParsed;
                        }
                        
                        // For dates without explicit years, ensure the date is in the future
                        var resultDate = alternativeParsed;
                        
                        // If the parsed date is in the current year and has already passed, move to next year
                        if (resultDate.Year == today.Year && resultDate.Date <= today.Date)
                        {
                            resultDate = resultDate.AddYears(1);
                        }
                        // If somehow the year is in the past, also move to future
                        else if (resultDate.Year < today.Year)
                        {
                            resultDate = new DateTime(today.Year + 1, resultDate.Month, resultDate.Day);
                        }
                        else
                        {
                        }
                        
                        return resultDate;
                    }
                }
            }
            catch (Exception)
            {
                // Ignore parsing errors
            }

            return null;
        }

        /// <summary>
        /// Gets the first non-empty group value from the specified group indices
        /// </summary>
        private static string GetFirstNonEmptyGroup(Match match, params int[] groupIndices)
        {
            foreach (var index in groupIndices)
            {
                if (index < match.Groups.Count && !string.IsNullOrEmpty(match.Groups[index].Value))
                {
                    return match.Groups[index].Value;
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Cleans up text by removing extra whitespace and trimming
        /// </summary>
        private static string CleanupText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Remove extra whitespace and trim
            return WhitespacePattern.Replace(text, " ").Trim();
        }
    }
}
