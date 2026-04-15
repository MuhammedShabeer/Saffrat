using System;

namespace Saffrat.ViewModels
{
    public class CustomerListItemVM
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
        public decimal TotalDue { get; set; }
    }
}
