namespace CalendarGenerator
{
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
