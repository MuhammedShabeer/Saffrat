using System;
using System.Collections.Generic;
using Saffrat.Models;

namespace Saffrat.ViewModels
{
    public class StatementOfAccountsVM
    {
        public List<GLAccount> Accounts { get; set; } = new List<GLAccount>();
        public int? SelectedAccountId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Search { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public List<StatementOfAccountRow> Transactions { get; set; } = new List<StatementOfAccountRow>();
    }

    public class StatementOfAccountRow
    {
        public int JournalEntryId { get; set; }
        public DateTime Date { get; set; }
        public string Reference { get; set; }
        public string Description { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal RunningBalance { get; set; }
        public string SourceDocumentType { get; set; }
        public int? SourceDocumentId { get; set; }
        public string AccountName { get; set; }
    }
}
