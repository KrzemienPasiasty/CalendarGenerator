using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
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

            
            
            using Bitmap bitmap = new Bitmap(widthPixels, heightPixels);
            using Graphics graphics = Graphics.FromImage(bitmap);

            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
            graphics.Clear(Config.BackgroundColor);

            float scaleFactor = Config.Dpi / 96f;
            using Font fontSmall = new Font(Config.Font, Config.SmallFontSize * scaleFactor, FontStyle.Regular);
            using Font fontRegular = new Font(Config.Font, Config.StandardFontSize * scaleFactor, FontStyle.Regular);
            using Font fontHeader = new Font(Config.Font, Config.BigFontSize * scaleFactor, FontStyle.Regular);

            float workX = marginPixels;
            float workY = marginPixels;
            float workWidth = widthPixels - 2 * marginPixels;
            float workHeight = heightPixels - 2 * marginPixels;

            float headerHeight = 40f * scaleFactor;
            float timeColumnWidth = 55f * scaleFactor;

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

            using StringFormat centerFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            // 1. Draw Day Headers
            float currentColumnX = workX + timeColumnWidth;
            using SolidBrush headerBrush = new SolidBrush(Color.FromArgb(255, 255, 255));
            graphics.FillRectangle(headerBrush, workX, workY, workWidth, headerHeight);

            for (int i = 0; i < 7; i++)
            {
                RectangleF headerRect = new RectangleF(currentColumnX, workY, columnWidths[i], headerHeight);
                graphics.DrawString(dayNames[i], fontHeader, Brushes.Black, headerRect, centerFormat);
                graphics.DrawLine(Pens.LightGray, currentColumnX, workY, currentColumnX, workY + workHeight);
                currentColumnX += columnWidths[i];
            }

            // 2. Draw Time Grid and Hour Labels
            float gridStartY = workY + headerHeight;
            using StringFormat timeFormat = new StringFormat
            {
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Near
            };

            for (int i = 0; i <= maxHour - minHour; i++)
            {
                float currentY = gridStartY + i * hourRowHeight;
                graphics.DrawLine(Pens.LightGray, workX, currentY, workX + workWidth, currentY);

                if (i < maxHour - minHour)
                {
                    string hourText = $"{minHour + i}:00";
                    RectangleF timeRect = new RectangleF(workX, currentY, timeColumnWidth - (Config.BorderMargin * scaleFactor), hourRowHeight);
                    graphics.DrawString(hourText, fontSmall, Brushes.Black, timeRect, timeFormat);
                }
            }

            // Draw outer border frame (5mm inside canvas)
            graphics.DrawRectangle(Pens.Black, workX, workY, workWidth, workHeight);

            // 3. Draw Class Boxes
            foreach (var entry in entries)
            {
                int dayIndex = ((int)entry.Date.DayOfWeek + 6) % 7; // Monday = 0

                float cardX = workX + timeColumnWidth + columnWidths.Take(dayIndex).Sum();
                float cardY = gridStartY + (float)(entry.StartTime.TotalHours - minHour) * hourRowHeight;
                float cardHeight = (float)(entry.EndTime.TotalHours - entry.StartTime.TotalHours) * hourRowHeight;

                float marginOffset = 3f * scaleFactor;
                RectangleF cardRect = new RectangleF(
                    cardX + marginOffset,
                    cardY + marginOffset,
                    columnWidths[dayIndex] - 2 * marginOffset,
                    cardHeight - 2 * marginOffset
                );

                // Fill card background with white and draw outline
                if (Config.HightlightLectures && entry.Type == EClassType.Lecture)
                    graphics.FillRectangle(Brushes.LightGray, cardRect);
                else
                    graphics.FillRectangle(Brushes.White, cardRect);
                graphics.DrawRectangle(Pens.Black, cardRect.X, cardRect.Y, cardRect.Width, cardRect.Height);

                // Top header text inside class box (e.g., "8:00, W")
                string cardHeader = $"{entry.StartTime:h\\:mm}, {entry.GetTypeCode()}";

                float textPadding = 4f * scaleFactor;
                RectangleF headerTextRect = new RectangleF(
                    cardRect.X + textPadding,
                    cardRect.Y + textPadding,
                    cardRect.Width - 2 * textPadding,
                    fontSmall.Height + 2
                );

                RectangleF nameTextRect = new RectangleF(
                    cardRect.X + textPadding,
                    headerTextRect.Bottom + textPadding,
                    cardRect.Width - 2 * textPadding,
                    cardRect.Height - headerTextRect.Height - 3 * textPadding
                );

                using StringFormat textWrapFormat = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    Trimming = StringTrimming.Word
                };

                graphics.DrawString(cardHeader, fontSmall, Brushes.Black, headerTextRect, textWrapFormat);
                graphics.DrawString(entry.SubjectName, fontRegular, Brushes.Black, nameTextRect, textWrapFormat);
            }

            bitmap.Save(Config.SaveFilepath, System.Drawing.Imaging.ImageFormat.Png);
        }
    }
}