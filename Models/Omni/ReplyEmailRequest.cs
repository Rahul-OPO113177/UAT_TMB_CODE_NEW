namespace ServerCRM.Models.Omni
{
    public class ReplyEmailRequest
    {
        public string To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string OriginalBody { get; set; }
        public string Disposition { get; set; }
        public string SubDisposition { get; set; }
        public string SubSubDisposition { get; set; }
        public string Remark { get; set; }
    }
}
