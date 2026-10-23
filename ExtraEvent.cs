namespace CalendarGenerator
{
    /// <summary>
    /// Events that appear in the calendar in the specific classes boxes.
    /// They represents events that will happen at this classes.
    /// They are represented with a name, description and priority.
    /// </summary>
    public class ExtraEvent
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public EEventPriority Priority { get; set; }
    }


    public enum EEventPriority
    {
        Low = 1,
        Medium = 2,
        High = 3
    }
}
