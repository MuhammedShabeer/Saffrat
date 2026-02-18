using Newtonsoft.Json;

namespace Saffrat.Models
{
    public partial class EmployeeAttachment
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string AttachmentName { get; set; }
        public string AttachmentType { get; set; }

        [JsonIgnore]
        public virtual Employee Employee { get; set; }
    }
}
