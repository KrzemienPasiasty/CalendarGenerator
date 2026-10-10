using SkiaSharp;
using System.Globalization;

namespace CalendarGenerator
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Config.Load("D:\\Programowanie\\c#\\CalendarGenerator\\config.json");

            Console.WriteLine("=== Weekly Schedule PNG Generator ===\n");

        EnterDate:
            Console.Write("Enter any date in target week (e.g. DD.MM.YYYY), or press Enter for today: ");
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

            if (!File.Exists(Config.CsvFilepath))
            {
                Console.WriteLine($"CSV file not found: {Config.CsvFilepath}");
                return;
            }
            if (!File.Exists(Config.AdditionalClassesFilepath))
            {
                Console.WriteLine($"CSV file not found: {Config.AdditionalClassesFilepath}");
                return;
            }

            List<ScheduleEntry> allEntries = LoadEntriesFromCsv(Config.CsvFilepath);
            allEntries.AddRange(LoadEntriesFromCsv(Config.AdditionalClassesFilepath));

            DateTime targetSunday = targetMonday.AddDays(7);

            List<ScheduleEntry> weeklyEntries = allEntries.Where(e => e.Date >= targetMonday && e.Date < targetSunday).ToList();

            Console.WriteLine($"Found {weeklyEntries.Count} class entries for the selected week.");

            GenerateScheduleImage(weeklyEntries);
            Console.WriteLine($"Successfully generated image: {Config.SaveFilepath}");
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
            var bg = Config.BackgroundColor;
            canvas.Clear(new SKColor(bg.R, bg.G, bg.B, bg.A));

            float scaleFactor = Config.Dpi / 96f;
            float pointsToPixels = Config.Dpi / 72f; // font sizes in config are in points

            using SKTypeface typeface = SKTypeface.FromFamilyName(Config.Font);
            using SKFont fontSmall = CreateFont(typeface, Config.SmallFontSize * pointsToPixels);
            using SKFont fontRegular = CreateFont(typeface, Config.StandardFontSize * pointsToPixels);
            using SKFont fontHeader = CreateFont(typeface, Config.BigFontSize * pointsToPixels);

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

                // Fill card background and draw outline
                fillPaint.Color = (Config.HightlightLectures && entry.Type == EClassType.Lecture)
                    ? SKColors.LightGray
                    : SKColors.White;
                canvas.DrawRect(cardRect, fillPaint);
                canvas.DrawRect(cardRect, borderPaint);

                // Top header text inside class box (e.g., "8:00, W")
                string cardHeader = $"{entry.StartTime:h\\:mm}, {entry.GetTypeCode()}";

                float textPadding = scaleFactor;
                SKRect headerTextRect = SKRect.Create(
                    cardRect.Left + textPadding,
                    cardRect.Top + textPadding,
                    cardRect.Width - 2 * textPadding,
                    fontSmall.Spacing + 2
                );

                SKRect nameTextRect = SKRect.Create(
                    cardRect.Left + textPadding,
                    headerTextRect.Bottom + textPadding * 0.2f,
                    cardRect.Width - 2 * textPadding,
                    cardRect.Height - headerTextRect.Height - 3 * textPadding
                );

                DrawTextWrapped(canvas, cardHeader, fontSmall, textPaint, headerTextRect);
                DrawTextWrapped(canvas, entry.SubjectName, fontRegular, textPaint, nameTextRect);
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
            canvas.DrawText(text, x, baseline, font, paint);
        }

        /// <summary>Right-aligns a single line of text to the top of the rect.</summary>
        private static void DrawTextTopRight(SKCanvas canvas, string text, SKFont font, SKPaint paint, SKRect rect)
        {
            SKFontMetrics metrics = font.Metrics;
            float x = rect.Right - font.MeasureText(text);
            float baseline = rect.Top - metrics.Ascent; // Ascent is negative
            canvas.DrawText(text, x, baseline, font, paint);
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
                    canvas.DrawText(line, rect.Left, y + ascent, font, paint);
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