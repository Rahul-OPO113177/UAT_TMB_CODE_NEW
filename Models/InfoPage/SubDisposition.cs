namespace ServerCRM.Models.InfoPage
{
    public class SubDisposition
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string DISP_TYPE { get; set; }
        
        public List<SubSubDisposition> SubSubDispositions { get; set; } = new();
    }
}
