using Microsoft.AspNetCore.Mvc.Rendering;

namespace ServerCRM.Models.InfoPage
{
    public class FieldData
    {
        public string FieldType { get; set; }
        public string CapturableField { get; set; }
        public string IsRequired { get; set; }
        public string SequenceWisefield { get; set; }
        public string FieldName { get; set; }
        public string isDependent { get; set; }
        public string SourceTableName { get; set; }
        public string IsfieldDependent { get; set; }
        public string FieldDependetName { get; set; }
        public string Isinitaldisplay { get; set; }
        public string DisplaySource { get; set; }
        public string DisplaySourceValue { get; set; }

        public List<SelectListItem> DependentData { get; set; }
    }
}
