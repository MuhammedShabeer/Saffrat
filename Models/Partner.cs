using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Saffrat.Models
{
    public class Partner
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; }

        [StringLength(250)]
        public string ContactInfo { get; set; }

        public int? GLAccountId { get; set; }
        public virtual GLAccount GLAccount { get; set; }

        public decimal OwnershipPercentage { get; set; }

        public virtual ICollection<PartnerTransaction> PartnerTransactions { get; set; } = new List<PartnerTransaction>();

        [StringLength(150)]
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
