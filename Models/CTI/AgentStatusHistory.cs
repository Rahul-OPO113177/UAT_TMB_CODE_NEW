namespace ServerCRM.Models.CTI
{
    public class AgentStatusHistory
    {
        public string AgentId { get; set; }
        public int StatusId { get; set; }
        public string StatusLabel { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
    }
}
