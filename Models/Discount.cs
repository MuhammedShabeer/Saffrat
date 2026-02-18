using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Saffrat.Models
{
    public partial class Discount
    {
        public Discount()
        {
            RunningOrders = new HashSet<RunningOrder>();
        }

        [Key]
        public int? Id { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public decimal Value { get; set; }
        [Required]
        public bool IsPercentage { get; set; }
        public bool IsDefault { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [JsonIgnore]
        public virtual ICollection<RunningOrder> RunningOrders { get; set; }
    }
}
