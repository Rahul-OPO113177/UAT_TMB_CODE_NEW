namespace ServerCRM.Models.InfoPage
{
    public class Disposition
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string DISP_TYPE { get; set; }
        public List<SubDisposition> SubDispositions { get; set; }
    }
}
