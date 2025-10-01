namespace ServerCRM.Models.CTI
{
    public class AgentStat
    {
        public string AgentId { get; set; }
        public int CurrentStatusID { get; set; }

        public string? ConnID { get; set; }
        public DateTime StatusStartTime { get; set; }

  
        public List<AgentStatusHistory> StatusHistory { get; set; } = new();
    }
}
