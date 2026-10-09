namespace CalendarGenerator
{

    /// <summary>
    /// Represents a single class entry loaded from the schedule data.
    /// </summary>
    public class ScheduleEntry
    {
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string SubjectName { get; set; }
        public EClassType Type { get; set; }

        /// <summary>
        /// Parses the raw class type string from CSV and assigns the corresponding ClassType enum value.
        /// </summary>
        public static EClassType ParseClassType(string rawType)
        {
            if (string.IsNullOrWhiteSpace(rawType))
                return EClassType.Other;

            string normalized = rawType.ToLowerInvariant();

            if (normalized.Contains("wykład"))
                return EClassType.Lecture;
            else if (normalized.Contains("ćwiczenia laboratoryjne"))
                return EClassType.Laboratory;
            else if (normalized.Contains("ćwiczenia audytoryjne"))
                return EClassType.Tutorial;
            else if (normalized.Contains("lektorat"))
                return EClassType.Language;
            else if (normalized.Contains("zajęcia z wf"))
                return EClassType.PE;
            else if (normalized.Contains("ćwiczenia projektowe"))
                return EClassType.Project;

            return EClassType.Other;
        }

        /// <summary>
        /// Returns the abbreviated string code for rendering on the schedule.
        /// </summary>
        public string GetTypeCode()
        {
            return Type switch
            {
                EClassType.Lecture => "W",
                EClassType.Laboratory => "LAB",
                EClassType.Tutorial => "ĆW",
                EClassType.Language => "L",
                EClassType.PE => "WF",
                EClassType.Project => "P",
                _ => "Inne"
            };
        }

    }
}
