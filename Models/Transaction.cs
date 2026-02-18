using System;
using System.Collections.Generic;

namespace Saffrat.Models
{
    public partial class Transaction
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string TransactionReference { get; set; }
        public string TransactionType { get; set; }
        public string Description { get; set; }
        public decimal Credit { get; set; }
        public decimal Debit { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }

        public virtual Account Account { get; set; }
    }
}
