using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Saffrat.Models
{
    public partial class Account
    {
        public Account()
        {
            Deposits = new HashSet<Deposit>();
            Expenses = new HashSet<Expense>();
            Transactions = new HashSet<Transaction>();
            ChildAccounts = new HashSet<Account>();
        }

        [Key]
        public int? Id { get; set; }
        [Required]
        public string AccountName { get; set; }
        [Required]
        public string AccountNumber { get; set; }
        public decimal Credit { get; set; }
        public decimal Debit { get; set; }
        public decimal Balance { get; set; }
        public string Note { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [StringLength(100)]
        public string AccountGroup { get; set; }
        [StringLength(100)]
        public string AccountType { get; set; }
        public int? ParentAccountId { get; set; }

        [JsonIgnore]
        public virtual Account ParentAccount { get; set; }
        [JsonIgnore]
        public virtual ICollection<Account> ChildAccounts { get; set; }

        [JsonIgnore]
        public virtual ICollection<Deposit> Deposits { get; set; }
        [JsonIgnore]
        public virtual ICollection<Expense> Expenses { get; set; }
        [JsonIgnore]
        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}
