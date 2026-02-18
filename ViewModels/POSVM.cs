
using Saffrat.Models;

namespace Saffrat.ViewModels
{
    public class POSVM
    {
        public ICollection<TaxRate> TaxRates { get; set; }
        public ICollection<Discount> Discounts { get; set; }
        public ICollection<Charge> Charges { get; set; }
        public ICollection<User> Waiters { get; set; }
        public ICollection<User> Drivers { get; set; }
        public ICollection<PaymentMethod> PaymentMethods { get; set; }
        public Customer DefaultCustomer { get; set; }
    }
}
