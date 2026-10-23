using SkiaSharp;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;

namespace CalendarGenerator
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Config.Load(".\\Data\\config.json");

            Console.WriteLine("=== Weekly Schedule PNG Generator ===\n");

            if (!File.Exists(Config.AdditionalClassesFilepath))
            {
                Console.WriteLine($"CSV file not found: {Config.AdditionalClassesFilepath}");
                return;
            }

            List<ScheduleEntry> allEntries;
            try
            {
                Console.WriteLine("Downloading schedule (.ics)...");
                allEntries = LoadEntriesFromIcs(Config.IcsUrl!);
                Console.WriteLine($"Loaded {allEntries.Count} total class entries.\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load ICS schedule from '{Config.IcsUrl}': {ex.Message}");
                return;
            }

        EnterDate:
            Console.Write("Enter any date in target week (e.g. DD.MM.YYYY), or press Enter for today:");
            string? dateInput = Console.ReadLine();

            DateTime inputDate;
            if (string.IsNullOrWhiteSpace(dateInput))
            {
                // Nothing or only spaces -> use current date
                inputDate = DateTime.Today;
            }
            else if (!DateTime.TryParseExact(dateInput.Trim(), new[] { "d.MM.yyyy", "dd.MM.yyyy", "d.M.yyyy", "dd.M.yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out inputDate))
            {
                Console.WriteLine("Invalid date format. Accepted date format: DD.MM.YYYY or D.MM.YYYY or D.M.YYYY\n\n");
                goto EnterDate;
            }

            // Monday of the week containing inputDate (week starts on Monday)
            int daysSinceMonday = ((int)inputDate.DayOfWeek + 6) % 7;
            DateTime targetMonday = inputDate.Date.AddDays(-daysSinceMonday);

            Console.WriteLine($"Monday of that week: {targetMonday:dd.MM.yyyy}");

            DateTime targetSunday = targetMonday.AddDays(7);

            List<ScheduleEntry> weeklyEntries = allEntries
                .Where(e => e.Date >= targetMonday && e.Date < targetSunday)
                .OrderBy(e => e.Date)
                .ThenBy(e => e.StartTime)
                .ToList();

            Console.WriteLine($"Found {weeklyEntries.Count} class entries for the selected week.");

        Chooser:
            Console.WriteLine("\n\nGenerate schedule image now: ANY KEY\nAdd additional classes: 1\nAdd extra events to classes: 2\n");
            ConsoleKeyInfo key = Console.ReadKey(true);
            if (key.KeyChar == '1')
            {
                Console.WriteLine("\n=== Add Additional Classes ===");
                List<ScheduleEntry> newEntries = new List<ScheduleEntry>();

                while (true)
                {
                    Console.Write("Enter name (or type 'q' to finish): ");
                    string? subject = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(subject) || subject.Trim().ToLower() == "q")
                    {
                        break;
                    }

                EnterClassDate:
                    Console.Write("Enter date (DD.MM.YYYY): ");
                    string? dateInputStr = Console.ReadLine();
                    if (!DateTime.TryParseExact(dateInputStr?.Trim(), new[] { "d.MM.yyyy", "dd.MM.yyyy", "d.M.yyyy", "dd.M.yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime classDate))
                    {
                        Console.WriteLine("Invalid date format. Please use DD.MM.YYYY.");
                        goto EnterClassDate;
                    }

                EnterStartTime:
                    Console.Write("Enter start time (HH:mm): ");
                    string? startInputStr = Console.ReadLine();
                    if (!TimeSpan.TryParse(startInputStr, out TimeSpan startTime))
                    {
                        Console.WriteLine("Invalid time format. Please use HH:mm.");
                        goto EnterStartTime;
                    }

                EnterEndTime:
                    Console.Write("Enter end time (HH:mm): ");
                    string? endInputStr = Console.ReadLine();
                    if (!TimeSpan.TryParse(endInputStr, out TimeSpan endTime) || endTime <= startTime)
                    {
                        Console.WriteLine("Invalid end time (must be after start time). Please use HH:mm.");
                        goto EnterEndTime;
                    }

                    newEntries.Add(new ScheduleEntry
                    {
                        Date = classDate,
                        StartTime = startTime,
                        EndTime = endTime,
                        SubjectName = subject.Trim(),
                        Type = EClassType.Other
                    });

                    Console.WriteLine("\nClass added successfully! Add another class? (Press any key to continue, or 'q' to finish):");
                    var nextKey = Console.ReadKey(true);
                    if (nextKey.KeyChar == 'q' || nextKey.KeyChar == 'Q')
                    {
                        break;
                    }
                    Console.WriteLine();
                }

                if (newEntries.Count > 0)
                {
                    bool fileExists = File.Exists(Config.AdditionalClassesFilepath);
                    List<string> linesToAppend = new List<string>();

                    // If the file doesn't exist yet, add a header row because LoadEntriesFromCsv skips index 0
                    if (!fileExists)
                    {
                        linesToAppend.Add(";;;SubjectName;Type;;;Date;StartTime;EndTime");
                    }

                    foreach (ScheduleEntry entry in newEntries)
                    {
                        // Matches the column indices expected by LoadEntriesFromCsv:
                        // columns[3] = SubjectName, columns[4] = Type ("Inne"), 
                        // columns[7] = Date, columns[8] = StartTime, columns[9] = EndTime
                        string line = $";;;{entry.SubjectName};Inne;;;{entry.Date:dd.MM.yyyy};{entry.StartTime:hh\\:mm};{entry.EndTime:hh\\:mm}";
                        linesToAppend.Add(line);
                    }

                    File.AppendAllLines(Config.AdditionalClassesFilepath, linesToAppend);
                    Console.WriteLine($"Successfully saved {newEntries.Count} new class(es) to {Config.AdditionalClassesFilepath}");
                }
                goto Chooser;

            }
            else if (key.KeyChar == '2')
            {
                Console.WriteLine("\n=== Add Extra Events to Classes ===");

                if (weeklyEntries.Count == 0)
                {
                    Console.WriteLine("No classes found for the selected week.");
                }
                else
                {
                    foreach (var entry in weeklyEntries)
                    {
                        Console.WriteLine($"\n----------------------------------------");
                        Console.WriteLine($"Class: {entry.SubjectName}");
                        Console.WriteLine($"Date: {entry.Date:dd.MM.yyyy} | Time: {entry.StartTime:h\\:mm} - {entry.EndTime:h\\:mm}");
                        Console.WriteLine($"----------------------------------------");
                        Console.Write("Do you want to add an extra event? Press 'y' to add, or any other key to skip: ");

                        var choiceKey = Console.ReadKey(true);

                        if (choiceKey.KeyChar == 'y' || choiceKey.KeyChar == 'Y')
                        {
                            Console.Write("\nEnter event name: ");
                            string? eventName = Console.ReadLine();

                            Console.Write("Enter event description: ");
                            string? eventDescription = Console.ReadLine();

                            Console.Write("Select priority - [1] Low, [2] Medium, [3] High: ");
                            var priorityKey = Console.ReadKey(true);
                            Console.WriteLine(priorityKey.KeyChar);

                            EEventPriority eventPriority;
                            if (int.TryParse(priorityKey.KeyChar.ToString(), out int priorityValue))
                                eventPriority = (EEventPriority)priorityValue;
                            else
                                eventPriority = EEventPriority.Medium;

                            // Instantiate ExtraEvent and attach it to the ScheduleEntry
                            entry.Event = new ExtraEvent
                            {
                                Name = string.IsNullOrWhiteSpace(eventName) ? "Extra Event" : eventName.Trim(),
                                Description = eventDescription?.Trim() ?? string.Empty,
                                Priority = eventPriority
                            };

                            Console.WriteLine("Successfully added extra event to this class!");
                        }
                        else
                        {
                            Console.WriteLine("Skipped.");
                        }
                    }
                }

                goto Chooser;
            }
            else
            { }
            // Reload all additional classes and append those matching the target week
            List<ScheduleEntry> additionalEntries = LoadEntriesFromCsv(Config.AdditionalClassesFilepath);
            weeklyEntries.AddRange(additionalEntries.Where(e => e.Date >= targetMonday && e.Date < targetSunday));
            
            
            GenerateScheduleImage(weeklyEntries);
            Console.WriteLine($"Successfully generated image: {Config.SaveFilepath}");
        }

        private static List<ScheduleEntry> LoadEntriesFromIcs(string source)
        {
            string icsContent;
            if (Uri.TryCreate(source, UriKind.Absolute, out Uri? uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                using HttpClient client = new HttpClient();
                icsContent = client.GetStringAsync(uri).GetAwaiter().GetResult();
            }
            else if (File.Exists(source))
            {
                icsContent = File.ReadAllText(source);
            }
            else
            {
                throw new FileNotFoundException($"ICS source not found or invalid URL: {source}");
            }

            return ParseIcsContent(icsContent);
        }

        private static List<ScheduleEntry> ParseIcsContent(string icsContent)
        {
            var entries = new List<ScheduleEntry>();

            // Unfold multi-line properties per RFC 5545 (lines starting with space or tab continue previous line)
            string[] rawLines = icsContent.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            var unfoldedLines = new List<string>(rawLines.Length);

            foreach (string line in rawLines)
            {
                if (line.Length > 0 && (line[0] == ' ' || line[0] == '\t') && unfoldedLines.Count > 0)
                {
                    unfoldedLines[unfoldedLines.Count - 1] += line.Substring(1);
                }
                else
                {
                    unfoldedLines.Add(line);
                }
            }

            bool inEvent = false;
            string? summary = null;
            DateTime? dtStart = null;
            DateTime? dtEnd = null;

            foreach (string line in unfoldedLines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                if (trimmed.Equals("BEGIN:VEVENT", StringComparison.OrdinalIgnoreCase))
                {
                    inEvent = true;
                    summary = null;
                    dtStart = null;
                    dtEnd = null;
                    continue;
                }

                if (trimmed.Equals("END:VEVENT", StringComparison.OrdinalIgnoreCase))
                {
                    if (inEvent && dtStart.HasValue && dtEnd.HasValue && !string.IsNullOrWhiteSpace(summary))
                    {
                        string rawType = string.Empty;
                        string subjectName = summary;

                        // USOS format: "<TypeCode> - <SubjectName>", e.g. "CWA - Mechanika 2" or "W - Zapis konstrukcji"
                        int separatorIndex = summary.IndexOf(" - ", StringComparison.Ordinal);
                        if (separatorIndex > 0)
                        {
                            rawType = summary.Substring(0, separatorIndex).Trim();
                            subjectName = summary.Substring(separatorIndex + 3).Trim();
                        }

                        entries.Add(new ScheduleEntry
                        {
                            Date = dtStart.Value.Date,
                            StartTime = dtStart.Value.TimeOfDay,
                            EndTime = dtEnd.Value.TimeOfDay,
                            SubjectName = subjectName,
                            Type = ScheduleEntry.ParseClassType(rawType)
                        });
                    }

                    inEvent = false;
                    continue;
                }

                if (!inEvent)
                    continue;

                int colonIndex = line.IndexOf(':');
                if (colonIndex <= 0)
                    continue;

                string propAndParams = line.Substring(0, colonIndex).Trim();
                string value = line.Substring(colonIndex + 1).Trim();
                string propName = propAndParams.Split(';')[0].Trim().ToUpperInvariant();

                if (propName == "SUMMARY")
                {
                    summary = value
                        .Replace("\\,", ",")
                        .Replace("\\;", ";")
                        .Replace("\\n", " ")
                        .Replace("\\\\", "\\")
                        .Trim();
                }
                else if (propName == "DTSTART")
                {
                    if (TryParseIcsDateTime(value, out DateTime parsedStart))
                    {
                        dtStart = parsedStart;
                    }
                }
                else if (propName == "DTEND")
                {
                    if (TryParseIcsDateTime(value, out DateTime parsedEnd))
                    {
                        dtEnd = parsedEnd;
                    }
                }
            }

            return entries;
        }

        private static bool TryParseIcsDateTime(string rawValue, out DateTime result)
        {
            rawValue = rawValue.Trim();

            if (rawValue.EndsWith("Z", StringComparison.OrdinalIgnoreCase))
            {
                if (DateTime.TryParseExact(
                    rawValue,
                    new[] { "yyyyMMdd'T'HHmmss'Z'", "yyyyMMdd'T'HHmm'Z'" },
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                    out DateTime utcDateTime))
                {
                    result = utcDateTime.ToLocalTime();
                    return true;
                }
            }

            string[] formats = { "yyyyMMdd'T'HHmmss", "yyyyMMdd'T'HHmm", "yyyyMMdd" };
            return DateTime.TryParseExact(rawValue, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
        }

        private static List<ScheduleEntry> LoadEntriesFromCsv(string filePath)
        {
            var entries = new List<ScheduleEntry>();
            string[] lines = File.ReadAllLines(filePath);

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                string[] columns = lines[i].Split(';');
                if (columns.Length < 10)
                {
                    continue;
                }

                string subjectName = columns[3].Trim(); // Column D
                string rawType = columns[4].Trim();     // Column E
                string rawDate = columns[7].Trim();     // Column H
                string rawStart = columns[8].Trim();    // Column I
                string rawEnd = columns[9].Trim();      // Column J

                if (DateTime.TryParseExact(rawDate, new[] { "d.MM.yyyy", "dd.MM.yyyy" },
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime classDate) &&
                    TimeSpan.TryParse(rawStart, out TimeSpan startTime) &&
                    TimeSpan.TryParse(rawEnd, out TimeSpan endTime))
                {
                    entries.Add(new ScheduleEntry
                    {
                        Date = classDate,
                        StartTime = startTime,
                        EndTime = endTime,
                        SubjectName = subjectName,
                        Type = ScheduleEntry.ParseClassType(rawType)
                    });
                }
            }

            return entries;
        }

        private static void GenerateScheduleImage(List<ScheduleEntry> entries)
        {
            // Calculating standard A4 landscape dimensions (1 : sqrt(2))
            float pageHeightMm = 210f;
            float millimetersToPixels = Config.Dpi / 25.4f;

            int heightPixels = (int)Math.Round(pageHeightMm * millimetersToPixels);
            int widthPixels = (int)Math.Round(heightPixels * Math.Sqrt(2));
            int marginPixels = (int)Math.Round(Config.BorderMargin * millimetersToPixels); // outer margin

            using SKSurface surface = SKSurface.Create(new SKImageInfo(widthPixels, heightPixels, SKColorType.Rgba8888, SKAlphaType.Premul));
            SKCanvas canvas = surface.Canvas;

            // Config.BackgroundColor is assumed to be a System.Drawing.Color; drop the conversion if it is already an SKColor            
            canvas.Clear(Config.BackgroundColor);

            float scaleFactor = Config.Dpi / 96f;
            float pointsToPixels = Config.Dpi / 72f; // font sizes in config are in points

            using SKTypeface typeface = SKTypeface.FromFamilyName(Config.Font);
            //using SKFont fontSmall = CreateFont(typeface, Config.SmallFontSize * pointsToPixels);
            SKFont fontSmall = new SKFont(typeface, Config.SmallFontSize * pointsToPixels);


            SKFont fontRegular = new SKFont(typeface, Config.StandardFontSize * pointsToPixels);
            SKFont fontHeader = new SKFont(typeface, Config.BigFontSize * pointsToPixels);

            using SKPaint textPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            using SKPaint fillPaint = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };
            using SKPaint gridPaint = new SKPaint { Color = SKColors.LightGray, Style = SKPaintStyle.Stroke, StrokeWidth = 1f, IsAntialias = true };
            using SKPaint borderPaint = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Stroke, StrokeWidth = 1f, IsAntialias = true };

            float workX = marginPixels;
            float workY = marginPixels;
            float workWidth = widthPixels - 2 * marginPixels;
            float workHeight = heightPixels - 2 * marginPixels;

            float headerHeight = 40f * scaleFactor;

            // Measure the widest typical time string and add proportional padding for the cell
            float cellPadding = 12f * scaleFactor;
            float timeColumnWidth = fontSmall.MeasureText("00:00") + cellPadding;

            float availableDaysWidth = workWidth - timeColumnWidth;

            // Monday-Friday have normal weight 1.0, Saturday-Sunday have narrowed weight 0.6
            float normalColumnWidth = availableDaysWidth / 6.2f;
            float narrowColumnWidth = normalColumnWidth * 0.6f;

            float[] columnWidths = new float[7];
            for (int i = 0; i < 7; i++)
            {
                columnWidths[i] = (i < 5) ? normalColumnWidth : narrowColumnWidth;
            }

            string[] dayNames = { "Poniedziałek", "Wtorek", "Środa", "Czwartek", "Piątek", "Sobota", "Niedziela" };

            int minHour = 7;
            int maxHour = entries.Any() ? Math.Max(18, entries.Max(e => e.EndTime.Hours) + 1) : 18;
            float hourRowHeight = (workHeight - headerHeight) / (maxHour - minHour);

            // 1. Draw Day Headers
            float currentColumnX = workX + timeColumnWidth;
            fillPaint.Color = SKColors.White;
            canvas.DrawRect(workX, workY, workWidth, headerHeight, fillPaint);

            for (int i = 0; i < 7; i++)
            {
                SKRect headerRect = SKRect.Create(currentColumnX, workY, columnWidths[i], headerHeight);
                DrawTextCentered(canvas, dayNames[i], fontHeader, textPaint, headerRect);
                canvas.DrawLine(currentColumnX, workY, currentColumnX, workY + workHeight, gridPaint);
                currentColumnX += columnWidths[i];
            }

            // 2. Draw Time Grid and Hour Labels
            float gridStartY = workY + headerHeight;

            for (int i = 0; i <= maxHour - minHour; i++)
            {
                float currentY = gridStartY + i * hourRowHeight;
                canvas.DrawLine(workX, currentY, workX + workWidth, currentY, gridPaint);

                if (i < maxHour - minHour)
                {
                    string hourText = $"{minHour + i}:00";
                    SKRect timeRect = SKRect.Create(workX, currentY, timeColumnWidth - (Config.BorderMargin * scaleFactor), hourRowHeight);
                    DrawTextTopRight(canvas, hourText, fontSmall, textPaint, timeRect);
                }
            }

            // Draw outer border frame
            canvas.DrawRect(workX, workY, workWidth, workHeight, borderPaint);

            // 3. Draw Class Boxes
            foreach (var entry in entries)
            {
                int dayIndex = ((int)entry.Date.DayOfWeek + 6) % 7; // Monday = 0

                float cardX = workX + timeColumnWidth + columnWidths.Take(dayIndex).Sum();
                float cardY = gridStartY + (float)(entry.StartTime.TotalHours - minHour) * hourRowHeight;
                float cardHeight = (float)(entry.EndTime.TotalHours - entry.StartTime.TotalHours) * hourRowHeight;

                float marginOffset = 2f * scaleFactor;
                SKRect cardRect = SKRect.Create(
                    cardX + marginOffset,
                    cardY + marginOffset,
                    columnWidths[dayIndex] - 2 * marginOffset,
                    cardHeight - 2 * marginOffset
                );
                Console.WriteLine("COLOR: " + Config.HightlightColor);
                // Fill card background and draw outline
                fillPaint.Color = (Config.HightlightLectures && entry.Type == EClassType.Lecture)
                    ? Config.HightlightColor
                    : SKColors.White;
                canvas.DrawRect(cardRect, fillPaint);
                canvas.DrawRect(cardRect, borderPaint);

                // Top header text inside class box (e.g., "8:00, W")
                string cardHeader = $"{entry.StartTime:h\\:mm}, {entry.GetTypeCode()}";
                float textPadding = 2f * scaleFactor;

                SKRect headerTextRect = SKRect.Create(
                    cardRect.Left + textPadding,
                    cardRect.Top + textPadding,
                    cardRect.Width - 2 * textPadding,
                    fontSmall.Spacing + 2
                );
                DrawTextWrapped(canvas, cardHeader, fontSmall, textPaint, headerTextRect);

                // Sequential Y pointer starting below the card header
                float currentY = headerTextRect.Bottom + (textPadding * 0.5f);
                float contentLeft = cardRect.Left + textPadding;
                float contentWidth = cardRect.Width - 2 * textPadding;

                // Draw Subject Name sequentially
                DrawWrappedTextBlock(canvas, entry.SubjectName, fontRegular, textPaint, contentLeft, contentWidth, ref currentY);

                // If Extra Event exists, render it sequentially below the subject name
                if (entry.Event != null)
                {
                    Console.WriteLine($"Rendering extra event for class '{entry.SubjectName}' on {entry.Date:dd.MM.yyyy} at {entry.StartTime:h\\:mm} - {entry.Event.Name}");

                    SKPaint eventPaint = new SKPaint();
                    SKPaint eventTextPaint;
                    if (entry.Event.Priority == EEventPriority.Low)
                    {
                        eventPaint.Color = Config.LowPriorityEventColor;
                        eventTextPaint = new SKPaint { Color = Config.LowPriorityEventColor, IsAntialias = true };
                    }
                    else if (entry.Event.Priority == EEventPriority.Medium)
                    {
                        eventPaint.Color = Config.MediumPriorityEventColor;
                        eventTextPaint = new SKPaint { Color = Config.MediumPriorityEventColor, IsAntialias = true };
                    }
                    else if (entry.Event.Priority == EEventPriority.High)
                    {
                        eventPaint.Color = Config.HighPriorityEventColor;
                        eventTextPaint = new SKPaint { Color = Config.HighPriorityEventColor, IsAntialias = true };
                    }
                    else
                    {
                        eventPaint.Color = SKColors.Black;
                        eventTextPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
                    }
                    eventPaint.StrokeWidth = 2f;
                    eventPaint.Style = SKPaintStyle.Stroke;

                    currentY += textPadding * 0.5f; // Small separator space
                    float startY = currentY;

                    // Draw Event Name using regular font (or bold if desired)
                    int linesTop = DrawWrappedTextBlock(canvas, $"• {entry.Event.Name}", fontRegular, eventTextPaint, contentLeft, contentWidth, ref currentY);
                    int linesBottom = 0;
                    // Draw Event Description using small font
                    if (!string.IsNullOrWhiteSpace(entry.Event.Description))
                    {
                        currentY += textPadding * 0.2f;
                        linesBottom = DrawWrappedTextBlock(canvas, entry.Event.Description, fontSmall, eventTextPaint, contentLeft, contentWidth, ref currentY);
                    }

                    SKRect eventBorder = SKRect.Create
                        (
                        cardRect.Left + textPadding,
                        startY,
                        cardRect.Width - 2 * textPadding,
                        fontSmall.Spacing * (linesBottom + linesTop) + textPadding * 2
                    );

                    canvas.DrawRect(eventBorder, eventPaint);
                }
            }

            // Encode and save as PNG
            using SKImage image = surface.Snapshot();
            using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
            using FileStream stream = File.Create(Config.SaveFilepath);
            data.SaveTo(stream);
        }

        // ---------------------------------------------------------------------
        // Text helpers (SkiaSharp has no built-in layout, so alignment and word
        // wrapping are done manually)
        // ---------------------------------------------------------------------

        /// <summary>
        /// 
        /// 
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="text"></param>
        /// <param name="font"></param>
        /// <param name="paint"></param>
        /// <param name="left"></param>
        /// <param name="maxW"></param>
        /// <param name="yPos"></param>
        /// <returns>number of text lines</returns>
        private static int DrawWrappedTextBlock(SKCanvas canvas, string text, SKFont font, SKPaint paint, float left, float maxW, ref float yPos)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            var lines = new List<string>();
            var paragraphs = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var paragraph in paragraphs)
            {
                var words = paragraph.Split(' ');
                var currentLine = string.Empty;

                foreach (var word in words)
                {
                    var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                    if (font.MeasureText(testLine) > maxW && !string.IsNullOrEmpty(currentLine))
                    {
                        lines.Add(currentLine);
                        currentLine = word;
                    }
                    else
                    {
                        currentLine = testLine;
                    }
                }

                if (!string.IsNullOrEmpty(currentLine))
                {
                    lines.Add(currentLine);
                }
            }

            // Retrieve font metrics for precise vertical spacing and baseline calculation
            SKFontMetrics metrics = font.Metrics;
            float lineSpacing = metrics.Descent - metrics.Ascent + metrics.Leading;

            foreach (var line in lines)
            {
                // Ascent is negative in a Y-down system, so subtracting it positions the baseline correctly
                float baselineY = yPos - metrics.Ascent;
                canvas.DrawText(line, new SKPoint(left, baselineY), SKTextAlign.Left, font, paint);

                // Dynamically advance Y coordinate for the next line
                yPos += lineSpacing;
            }
            return lines.Count;
        }


        private static SKFont CreateFont(SKTypeface typeface, float sizePixels)
        {
            return new SKFont(typeface, sizePixels)
            {
                Edging = SKFontEdging.Antialias,
                Subpixel = true
            };
        }

        /// <summary>Centers a single line of text horizontally and vertically inside the rect.</summary>
        private static void DrawTextCentered(SKCanvas canvas, string text, SKFont font, SKPaint paint, SKRect rect)
        {
            SKFontMetrics metrics = font.Metrics;
            float x = rect.MidX - font.MeasureText(text) / 2f;
            float baseline = rect.MidY - (metrics.Ascent + metrics.Descent) / 2f;
            canvas.DrawText(text, new SKPoint(x, baseline), SKTextAlign.Left, font, paint);
        }

        /// <summary>Right-aligns a single line of text to the top of the rect.</summary>
        private static void DrawTextTopRight(SKCanvas canvas, string text, SKFont font, SKPaint paint, SKRect rect)
        {
            SKFontMetrics metrics = font.Metrics;
            float x = rect.Right - font.MeasureText(text);
            float baseline = rect.Top - metrics.Ascent; // Ascent is negative
            canvas.DrawText(text, new SKPoint(x, baseline), SKTextAlign.Left, font, paint);
        }

        /// <summary>
        /// Draws left-aligned, word-wrapped text inside the rect. Lines that do not
        /// fully fit vertically are omitted (same behaviour as GDI+ DrawString).
        /// </summary>
        private static void DrawTextWrapped(SKCanvas canvas, string text, SKFont font, SKPaint paint, SKRect rect)
        {
            float lineHeight = font.Spacing;
            float ascent = -font.Metrics.Ascent;
            float y = rect.Top;

            foreach (string line in WrapText(text, font, rect.Width))
            {
                if (y + lineHeight > rect.Bottom)
                {
                    break;
                }

                if (line.Length > 0)
                {
                    canvas.DrawText(line, new SKPoint(rect.Left, y + ascent), SKTextAlign.Left, font, paint);
                }

                y += lineHeight;
            }
        }

        private static IEnumerable<string> WrapText(string text, SKFont font, float maxWidth)
        {
            foreach (string paragraph in text.Replace("\r", "").Split('\n'))
            {
                string line = "";

                foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    string candidate = line.Length == 0 ? word : line + " " + word;
                    if (font.MeasureText(candidate) <= maxWidth)
                    {
                        line = candidate;
                        continue;
                    }

                    if (line.Length > 0)
                    {
                        yield return line;
                        line = "";
                    }

                    // A single word wider than the box gets broken across lines
                    string remaining = word;
                    while (remaining.Length > 1 && font.MeasureText(remaining) > maxWidth)
                    {
                        int fit = Math.Max(1, (int)font.BreakText(remaining, maxWidth));
                        yield return remaining.Substring(0, fit);
                        remaining = remaining.Substring(fit);
                    }

                    line = remaining;
                }

                yield return line;
            }
        }
    }
}
