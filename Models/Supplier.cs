using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Saffrat.Models
{
    public partial class Supplier
    {
        public Supplier()
        {
            Purchases = new HashSet<Purchase>();
        }

        public int Id { get; set; }
        public string SupplierName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [JsonIgnore]
        public virtual ICollection<Purchase> Purchases { get; set; }
    }
}
