using System;
using System.Collections.Generic;

namespace Saffrat.Models
{
    public partial class AppSetting
    {
        public int Id { get; set; }
        public string AppName { get; set; }
        public string Logo { get; set; }
        public string Favicon { get; set; }
        public string Preloader { get; set; }
        public int DefaultCustomer { get; set; }
        public int DefaultOrderType { get; set; }
        public int SaleAccount { get; set; }
        public int PurchaseAccount { get; set; }
        public int PayrollAccount { get; set; }
        public string Copyright { get; set; }
        public bool SendInvoiceEmail { get; set; }
        public bool SkipKitchenOrder { get; set; }
        public string Timezone { get; set; }
        public string DefaultLanguage { get; set; }
        public string DefaultRegion { get; set; }
        public string CurrencyName { get; set; }
        public string CurrencySymbol { get; set; }
        public int CurrencyPosition { get; set; }
        public string CompanyName { get; set; }
        public string CompanyEmail { get; set; }
        public string CompanyPhone { get; set; }
        public string CompanyAddress { get; set; }
        public string CompanyTaxNum { get; set; }
        public string MailProtocol { get; set; }
        public string MailEncryption { get; set; }
        public string MailHost { get; set; }
        public int MailPort { get; set; }
        public string MailUserName { get; set; }
        public string MailPassword { get; set; }
        public string ThemeColor { get; set; }
        public string ThemeSidebar { get; set; }
        public string ThemeNavbar { get; set; }
        public int PrinterMethod { get; set; } // 0: Browser, 1: USB
        public int PrinterPaperWidth { get; set; } // 80 or 58
        public string InvoiceLogo { get; set; }
        public bool PrintLogo { get; set; }
    }
}
