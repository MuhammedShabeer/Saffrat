namespace Saffrat.ViewModels
{
    public class TrialBalanceModel
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; }
        public string AccountGroup { get; set; }
        public int Category { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal ClosingBalance { get; set; }
    }
}
