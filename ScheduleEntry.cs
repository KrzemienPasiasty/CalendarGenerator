namespace CalendarGenerator
{

    /// <summary>
    /// Represents a single class loaded from the schedule data.
    /// </summary>
    public class ScheduleEntry
    {
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? SubjectName { get; set; }
        public EClassType Type { get; set; }
        public ExtraEvent? Event { get; set; }


        /// <summary>
        /// Parses the raw class type string from ICS or CSV and assigns the corresponding ClassType enum value.
        /// </summary>
        public static EClassType ParseClassType(string rawType)
        {
            if (string.IsNullOrWhiteSpace(rawType))
                return EClassType.Other;

            string normalized = rawType.Trim().ToLowerInvariant();

            if (normalized == "w" || normalized == "wyk" || normalized.Contains("wykład"))
                return EClassType.Lecture;
            else if (normalized == "cwl" || normalized == "ćwl" || normalized == "lab" || normalized.Contains("ćwiczenia laboratoryjne") || normalized.Contains("laboratorium"))
                return EClassType.Laboratory;
            else if (normalized == "cwa" || normalized == "ćwa" || normalized == "cw" || normalized == "ćw" || normalized.Contains("ćwiczenia audytoryjne"))
                return EClassType.Tutorial;
            else if (normalized == "lekt" || normalized == "lek" || normalized == "l" || normalized.Contains("lektorat"))
                return EClassType.Language;
            else if (normalized == "wf" || normalized.Contains("zajęcia z wf") || normalized.Contains("wychowanie fizyczne"))
                return EClassType.PE;
            else if (normalized == "cwp" || normalized == "ćwp" || normalized == "p" || normalized == "proj" || normalized.Contains("ćwiczenia projektowe") || normalized.Contains("projekt"))
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
