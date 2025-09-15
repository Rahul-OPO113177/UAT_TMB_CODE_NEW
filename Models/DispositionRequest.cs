namespace ServerCRM.Models
{
    public class DispositionRequest
    {
        public int dispositionId { get; set; }
        public int? subDispositionId { get; set; }   // nullable to avoid 400
        public string address { get; set; }
        public DateTime callBackDate { get; set; }
    }
}
