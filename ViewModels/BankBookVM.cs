using System;
using System.Collections.Generic;
using Saffrat.Models;

namespace Saffrat.ViewModels
{
    public class BankBookVM
    {
        public DateTime Date { get; set; }
        public decimal OpeningBalance { get; set; }
        public List<BankBookEntry> Transactions { get; set; } = new List<BankBookEntry>();
        public decimal ClosingBalance { get; set; }
        public string ReportType { get; set; } // "Cash Book" or "Day Book"
        public string AccountNames { get; set; }
    }

    public class BankBookEntry
    {
        public DateTime Date { get; set; }
        public string Reference { get; set; }
        public string AccountName { get; set; }
        public string Description { get; set; }
        public decimal Inflow { get; set; } // Debit
        public decimal Outflow { get; set; } // Credit
        public string SourceDocumentType { get; set; }
        public int? SourceDocumentId { get; set; }
    }
}
