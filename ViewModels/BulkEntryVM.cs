using System;
using System.Collections.Generic;
using Saffrat.Models;

namespace Saffrat.ViewModels
{
    public class BulkEntryRow
    {
        public BulkEntryRow()
        {
            EntryDate = DateTime.Today;
        }

        public DateTime EntryDate { get; set; }
        public string ReferenceNumber { get; set; }
        public int BankAccountId { get; set; }
        public string TransactionType { get; set; } // Inflow / Outflow
        public int OffsetAccountId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
    }

    public class BulkEntryVM
    {
        public List<BulkEntryRow> Entries { get; set; } = new List<BulkEntryRow>();
        public string SourceType { get; set; } // "Bank" or "Cash"
        
        // For dropdowns in the view
        public List<GLAccount> BankAccounts { get; set; } = new List<GLAccount>();
        public List<GLAccount> OffsetAccounts { get; set; } = new List<GLAccount>();
    }
}
