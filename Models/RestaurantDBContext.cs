using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Saffrat.Services;
using System.Threading;
using System.Threading.Tasks;

namespace Saffrat.Models
{
    public partial class RestaurantDBContext : DbContext
    {
        public RestaurantDBContext()
        {
        }

        public RestaurantDBContext(DbContextOptions<RestaurantDBContext> options)
            : base(options)
        {
        }



        public virtual DbSet<AppSetting> AppSettings { get; set; }
        public virtual DbSet<Attendance> Attendances { get; set; }
        public virtual DbSet<AuditLog> AuditLogs { get; set; }
        public virtual DbSet<Charge> Charges { get; set; }
        public virtual DbSet<Customer> Customers { get; set; }
        public virtual DbSet<Department> Departments { get; set; }

        public virtual DbSet<Designation> Designations { get; set; }
        public virtual DbSet<Discount> Discounts { get; set; }
        public virtual DbSet<EmailTemplate> EmailTemplates { get; set; }
        public virtual DbSet<Employee> Employees { get; set; }
        public virtual DbSet<EmployeeAttachment> EmployeeAttachments { get; set; }
        public virtual DbSet<EmployeeDeduction> EmployeeDeductions { get; set; }
        public virtual DbSet<EmployeeEarning> EmployeeEarnings { get; set; }

        public virtual DbSet<FoodGroup> FoodGroups { get; set; }
        public virtual DbSet<FoodItem> FoodItems { get; set; }
        public virtual DbSet<FoodItemIngredient> FoodItemIngredients { get; set; }
        public virtual DbSet<Holiday> Holidays { get; set; }
        public virtual DbSet<IngredientItem> IngredientItems { get; set; }
        public virtual DbSet<Language> Languages { get; set; }
        public virtual DbSet<LeaveRequest> LeaveRequests { get; set; }
        public virtual DbSet<Modifier> Modifiers { get; set; }
        public virtual DbSet<ModifierIngredient> ModifierIngredients { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<OrderDetail> OrderDetails { get; set; }
        public virtual DbSet<OrderItemModifier> OrderItemModifiers { get; set; }
        public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }
        public virtual DbSet<Payroll> Payrolls { get; set; }
        public virtual DbSet<PayrollDetail> PayrollDetails { get; set; }
        public virtual DbSet<PayrollPayment> PayrollPayments { get; set; }
        public virtual DbSet<Purchase> Purchases { get; set; }
        public virtual DbSet<PurchaseDetail> PurchaseDetails { get; set; }
        public virtual DbSet<RestaurantTable> RestaurantTables { get; set; }
        public virtual DbSet<RunningOrder> RunningOrders { get; set; }
        public virtual DbSet<RunningOrderDetail> RunningOrderDetails { get; set; }
        public virtual DbSet<RunningOrderItemModifier> RunningOrderItemModifiers { get; set; }
        public virtual DbSet<Shift> Shifts { get; set; }
        public virtual DbSet<StringResource> StringResources { get; set; }
        public virtual DbSet<Supplier> Suppliers { get; set; }
        public virtual DbSet<TaxRate> TaxRates { get; set; }

        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<UserToken> UserTokens { get; set; }
        public virtual DbSet<WorkPeriod> WorkPeriods { get; set; }

        // Accounting Engine DbSets
        public virtual DbSet<GLAccount> GLAccounts { get; set; }
        public virtual DbSet<JournalEntry> JournalEntries { get; set; }
        public virtual DbSet<LedgerEntry> LedgerEntries { get; set; }
        public virtual DbSet<Invoice> Invoices { get; set; }
        public virtual DbSet<Bill> Bills { get; set; }

        public virtual DbSet<StockAdjustment> StockAdjustments { get; set; }
        public virtual DbSet<Partner> Partners { get; set; }
        public virtual DbSet<PartnerTransaction> PartnerTransactions { get; set; }
        public virtual DbSet<DeletedOrder> DeletedOrders { get; set; }
        public virtual DbSet<FoodItemStock> FoodItemStocks { get; set; }
        public virtual DbSet<InventoryTransaction> InventoryTransactions { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=DESKTOP-8SFJU5F\\SQLEXPRESS;Database=RestaurantDB;Trusted_Connection=True;encrypt=false");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {




            modelBuilder.Entity<AppSetting>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.AppName)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.CompanyAddress)
                    .IsRequired()
                    .HasMaxLength(250);

                entity.Property(e => e.CompanyEmail)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.CompanyName)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.CompanyPhone)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.CompanyTaxNum)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.Copyright)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.CurrencyName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.CurrencySymbol)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.DefaultLanguage)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.DefaultRegion)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Favicon)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Logo)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.MailEncryption)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.MailHost)
                    .IsRequired()
                    .HasMaxLength(250);

                entity.Property(e => e.MailPassword)
                    .IsRequired()
                    .HasMaxLength(250);

                entity.Property(e => e.MailProtocol)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.MailUserName)
                    .IsRequired()
                    .HasMaxLength(250);

                entity.Property(e => e.Preloader)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.ThemeColor)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.ThemeNavbar)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.ThemeSidebar)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.Timezone)
                    .IsRequired()
                    .HasMaxLength(300);

            });

            modelBuilder.Entity<Attendance>(entity =>
            {
                entity.Property(e => e.AttendaceDate).HasColumnType("date");

                entity.Property(e => e.Note).HasMaxLength(500);

                entity.Property(e => e.Status).HasMaxLength(50);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.Property(e => e.UpdatedBy).HasMaxLength(150);

                entity.HasOne(d => d.Employee)
                    .WithMany(p => p.Attendances)
                    .HasForeignKey(d => d.EmployeeId)
                    .HasConstraintName("FK_Attendances_Employees");

                entity.HasOne(d => d.Shift)
                    .WithMany(p => p.Attendances)
                    .HasForeignKey(d => d.ShiftId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Attendances_Shifts");
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.Property(e => e.Action)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.Ip)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.Service)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(150);
            });

            modelBuilder.Entity<Charge>(entity =>
            {
                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.Property(e => e.UpdatedBy).HasMaxLength(150);

                entity.Property(e => e.Value).HasColumnType("decimal(18, 2)");
            });

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.Property(e => e.Address)
                    .IsRequired()
                    .HasMaxLength(250);

                entity.Property(e => e.CustomerName)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.Email).HasMaxLength(150);

                entity.Property(e => e.Phone)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.Property(e => e.UpdatedBy).HasMaxLength(150);
            });

            modelBuilder.Entity<Department>(entity =>
            {
                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.Property(e => e.UpdatedBy).HasMaxLength(150);
            });



            modelBuilder.Entity<Designation>(entity =>
            {
                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.Property(e => e.UpdatedBy).HasMaxLength(150);
            });

            modelBuilder.Entity<Discount>(entity =>
            {
                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.Property(e => e.UpdatedBy).HasMaxLength(150);

                entity.Property(e => e.Value).HasColumnType("decimal(18, 2)");
            });

            modelBuilder.Entity<EmailTemplate>(entity =>
            {
                entity.Property(e => e.DefaultTemplate).IsRequired();

                entity.Property(e => e.Description).IsRequired();

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.Subject)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.Template).IsRequired();

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.Property(e => e.UpdatedBy).HasMaxLength(150);
            });

            modelBuilder.Entity<Employee>(entity =>
            {
                entity.Property(e => e.AccountHolderName).HasMaxLength(150);

                entity.Property(e => e.AccountNumber).HasMaxLength(100);

                entity.Property(e => e.BankIdentifierCode).HasMaxLength(150);

                entity.Property(e => e.BankName).HasMaxLength(150);

                entity.Property(e => e.BranchLocation).HasMaxLength(250);

                entity.Property(e => e.Dob)
                    .HasColumnType("date")
                    .HasColumnName("DOB");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.EmergencyContact)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Gender)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.Image).HasMaxLength(255);

                entity.Property(e => e.JoiningDate).HasColumnType("date");

                entity.Property(e => e.LeavingDate).HasColumnType("date");

                entity.Property(e => e.MaritalStatus)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.Nidnumber)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("NIDNumber");

                entity.Property(e => e.PayslipType)
                    .IsRequired()
                    .HasMaxLength(250);

                entity.Property(e => e.PermanentAddress)
                    .IsRequired()
                    .HasMaxLength(250);

                entity.Property(e => e.Phone)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.PresentAddress)
                    .IsRequired()
                    .HasMaxLength(250);

                entity.Property(e => e.Religion)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.Salary).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.TaxPayerId).HasMaxLength(100);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.Property(e => e.UpdatedBy).HasMaxLength(150);

                entity.HasOne(d => d.Department)
                    .WithMany(p => p.Employees)
                    .HasForeignKey(d => d.DepartmentId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Employees_Departments");

                entity.HasOne(d => d.Designation)
                    .WithMany(p => p.Employees)
                    .HasForeignKey(d => d.DesignationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Employees_Designations");

                entity.HasOne(d => d.Shift)
                    .WithMany(p => p.Employees)
                    .HasForeignKey(d => d.ShiftId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Employees_Shifts");
            });

            modelBuilder.Entity<EmployeeAttachment>(entity =>
            {
                entity.Property(e => e.AttachmentName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.AttachmentType)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasOne(d => d.Employee)
                    .WithMany(p => p.EmployeeAttachments)
                    .HasForeignKey(d => d.EmployeeId)
                    .HasConstraintName("FK_EmployeeAttachments_Employees");
            });

            modelBuilder.Entity<EmployeeDeduction>(entity =>
            {
                entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(250);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.Property(e => e.UpdatedBy).HasMaxLength(150);

                entity.HasOne(d => d.Employee)
                    .WithMany(p => p.EmployeeDeductions)
                    .HasForeignKey(d => d.EmployeeId)
                    .HasConstraintName("FK_EmployeeDeductions_Employees");
            });

            modelBuilder.Entity<EmployeeEarning>(entity =>
            {
                entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(250);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.Property(e => e.UpdatedBy).HasMaxLength(150);

                entity.HasOne(d => d.Employee)
                    .WithMany(p => p.EmployeeEarnings)
                    .HasForeignKey(d => d.EmployeeId)
                    .HasConstraintName("FK_EmployeeEarnings_Employees");
            });



            modelBuilder.Entity<FoodGroup>(entity =>
            {
                entity.Property(e => e.GroupName)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.ArabicName)
                    .HasMaxLength(150);

                entity.Property(e => e.Image).HasMaxLength(255);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.Property(e => e.UpdatedBy).HasMaxLength(150);
            });

            modelBuilder.Entity<FoodItem>(entity =>
            {
                entity.Property(e => e.Description).HasMaxLength(500);

                entity.Property(e => e.Image).HasMaxLength(255);

                entity.Property(e => e.ItemName)
                    .IsRequired()
                    .HasMaxLength(250);

                entity.Property(e => e.ArabicName)
                    .HasMaxLength(250);

                entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.Property(e => e.UpdatedBy).HasMaxLength(150);

                entity.HasOne(d => d.Group)
                    .WithMany(p => p.FoodItems)
                    .HasForeignKey(d => d.GroupId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_FoodItems_FoodGroups");
            });

            modelBuilder.Entity<FoodItemIngredient>(entity =>
            {
                entity.Property(e => e.Quantity).HasColumnType("decimal(18, 2)");

                entity.HasOne(d => d.FoodItem)
                    .WithMany(p => p.FoodItemIngredients)
                    .HasForeignKey(d => d.FoodItemId)
                    .HasConstraintName("FK_FoodItemIngredients_FoodItems");

                entity.HasOne(d => d.Ingredient)
                    .WithMany(p => p.FoodItemIngredients)
                    .HasForeignKey(d => d.IngredientId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_FoodItemIngredients_IngredientItems");
            });

            modelBuilder.Entity<Holiday>(entity =>
            {
                entity.Property(e => e.FromDate).HasColumnType("date");

                entity.Property(e => e.Note).HasMaxLength(500);

                entity.Property(e => e.ToDate).HasColumnType("date");

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.Property(e => e.UpdatedBy).HasMaxLength(150);
            });

            modelBuilder.Entity<IngredientItem>(entity =>
            {
                entity.Property(e => e.AlertQuantity).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Description).HasMaxLength(500);

                entity.Property(e => e.ItemName)
                    .IsRequired()
                    .HasMaxLength(250);

                entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Quantity).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Unit)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.Property(e => e.UpdatedBy).HasMaxLength(150);
            });

            modelBuilder.Entity<Language>(entity =>
            {
                entity.Property(e => e.Culture)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(150);
            });

            modelBuilder.Entity<LeaveRequest>(entity =>
            {
                entity.Property(e => e.Description).HasMaxLength(500);

                entity.Property(e => e.EndDate).HasColumnType("date");

                entity.Property(e => e.LeaveType)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.StartDate).HasColumnType("date");

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.Property(e => e.UpdatedBy).HasMaxLength(150);

                entity.HasOne(d => d.Employee)
                    .WithMany(p => p.LeaveRequests)
                    .HasForeignKey(d => d.EmployeeId)
                    .HasConstraintName("FK_LeaveRequests_Employees");
            });

            modelBuilder.Entity<Modifier>(entity =>
            {
                entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.Property(e => e.UpdatedBy).HasMaxLength(150);
            });

            modelBuilder.Entity<ModifierIngredient>(entity =>
            {
                entity.Property(e => e.Quantity).HasColumnType("decimal(18, 2)");

                entity.HasOne(d => d.Ingredient)
                    .WithMany(p => p.ModifierIngredients)
                    .HasForeignKey(d => d.IngredientId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ModifierIngredients_IngredientItems");

                entity.HasOne(d => d.Modifier)
                    .WithMany(p => p.ModifierIngredients)
                    .HasForeignKey(d => d.ModifierId)
                    .HasConstraintName("FK_ModifierIngredients_Modifiers");
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.ChargeTotal).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.ClosedAt).HasColumnType("datetime");

                entity.Property(e => e.ClosedBy).HasMaxLength(150);

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.DiscountTotal).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.DueAmount).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Note).HasMaxLength(250);

                entity.Property(e => e.PaidAmount).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.PaymentMethod).HasMaxLength(50);
                entity.Property(e => e.PriceType).HasMaxLength(150);
                entity.Property(e => e.SubTotal).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.TableName).HasMaxLength(150);

                entity.Property(e => e.TaxTotal).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.WaiterOrDriver).HasMaxLength(150);

                entity.HasOne(d => d.Customer)
                    .WithMany(p => p.Orders)
                    .HasForeignKey(d => d.CustomerId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Orders_Customers");
            });

            modelBuilder.Entity<OrderDetail>(entity =>
            {
                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModifierTotal).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");

                entity.HasOne(d => d.Item)
                    .WithMany(p => p.OrderDetails)
                    .HasForeignKey(d => d.ItemId)
                    .HasConstraintName("FK_OrderDetails_FoodItems");

                entity.HasOne(d => d.Order)
                    .WithMany(p => p.OrderDetails)
                    .HasForeignKey(d => d.OrderId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_OrderDetails_Orders");

                entity.Property(e => e.FocQuantity).HasColumnType("decimal(18, 2)").HasDefaultValue(0);
            });

            modelBuilder.Entity<OrderItemModifier>(entity =>
            {
                entity.Property(e => e.ModifierTotal).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");

                entity.HasOne(d => d.Modifier)
                    .WithMany(p => p.OrderItemModifiers)
                    .HasForeignKey(d => d.ModifierId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_OrderItemModifiers_Modifiers");

                entity.HasOne(d => d.OrderDetail)
                    .WithMany(p => p.OrderItemModifiers)
                    .HasForeignKey(d => d.OrderDetailId)
                    .HasConstraintName("FK_OrderItemModifiers_OrderDetails");
            });

            modelBuilder.Entity<PaymentMethod>(entity =>
            {
                entity.Property(e => e.Title).HasMaxLength(150);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.Property(e => e.UpdatedBy).HasMaxLength(150);
            });

            modelBuilder.Entity<Payroll>(entity =>
            {
                entity.Property(e => e.GeneratedAt).HasColumnType("datetime");

                entity.Property(e => e.GeneratedBy).HasMaxLength(150);

                entity.Property(e => e.NetSalary).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.PaymentStatus)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.PayrollType)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.Salary).HasColumnType("decimal(18, 2)");

                entity.HasOne(d => d.Employee)
                    .WithMany(p => p.Payrolls)
                    .HasForeignKey(d => d.EmployeeId)
                    .HasConstraintName("FK_Payrolls_Employees");
            });

            modelBuilder.Entity<PayrollDetail>(entity =>
            {
                entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.AmountType)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.HasOne(d => d.Payroll)
                    .WithMany(p => p.PayrollDetails)
                    .HasForeignKey(d => d.PayrollId)
                    .HasConstraintName("FK_PayrollDetails_Payrolls");
            });

            modelBuilder.Entity<Purchase>(entity =>
            {
                entity.Property(e => e.Description).HasMaxLength(500);

                entity.Property(e => e.DueAmount).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.InvoiceNo)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.PaidAmount).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.PaymentType)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.PurchaseDate).HasColumnType("date");

                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.Property(e => e.UpdatedBy).HasMaxLength(150);

                entity.HasOne(d => d.Supplier)
                    .WithMany(p => p.Purchases)
                    .HasForeignKey(d => d.SupplierId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Purchases_Suppliers");
            });

            modelBuilder.Entity<PurchaseDetail>(entity =>
            {
                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.PurchasePrice).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Quantity).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");

                entity.HasOne(d => d.IngredientItem)
                    .WithMany(p => p.PurchaseDetails)
                    .HasForeignKey(d => d.IngredientItemId)
                    .HasConstraintName("FK_PurchaseDetails_IngredientItems");

                entity.HasOne(d => d.Purchase)
                    .WithMany(p => p.PurchaseDetails)
                    .HasForeignKey(d => d.PurchaseId)
                    .HasConstraintName("FK_PurchaseDetails_Purchases");
            });

            modelBuilder.Entity<RestaurantTable>(entity =>
            {
                entity.Property(e => e.Image)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.TableName)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.Property(e => e.UpdatedBy).HasMaxLength(150);
            });

            modelBuilder.Entity<RunningOrder>(entity =>
            {
                entity.Property(e => e.ChargeTotal).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.DiscountTotal).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.DueAmount).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Note).HasMaxLength(250);

                entity.Property(e => e.PaidAmount).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.PaymentMethod).HasMaxLength(150);
                entity.Property(e => e.PriceType).HasMaxLength(150);
                entity.Property(e => e.SubTotal).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.TableName).HasMaxLength(150);

                entity.Property(e => e.TaxTotal).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.WaiterOrDriver).HasMaxLength(150);

                entity.HasOne(d => d.Charges)
                    .WithMany(p => p.RunningOrders)
                    .HasForeignKey(d => d.ChargesId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_RunningOrders_Charges");

                entity.HasOne(d => d.Customer)
                    .WithMany(p => p.RunningOrders)
                    .HasForeignKey(d => d.CustomerId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_RunningOrders_Customers");

                entity.HasOne(d => d.Discount)
                    .WithMany(p => p.RunningOrders)
                    .HasForeignKey(d => d.DiscountId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_RunningOrders_Discounts");

                entity.HasOne(d => d.Tax)
                    .WithMany(p => p.RunningOrders)
                    .HasForeignKey(d => d.TaxId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_RunningOrders_TaxRates");
            });

            modelBuilder.Entity<RunningOrderDetail>(entity =>
            {
                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ModifierTotal).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");

                entity.HasOne(d => d.Item)
                    .WithMany(p => p.RunningOrderDetails)
                    .HasForeignKey(d => d.ItemId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_RunningOrderDetails_FoodItems");

                entity.HasOne(d => d.Order)
                    .WithMany(p => p.RunningOrderDetails)
                    .HasForeignKey(d => d.OrderId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_RunningOrderDetails_RunningOrders");

                entity.Property(e => e.FocQuantity).HasColumnType("decimal(18, 2)").HasDefaultValue(0);
            });

            modelBuilder.Entity<RunningOrderItemModifier>(entity =>
            {
                entity.Property(e => e.ModifierTotal).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");

                entity.HasOne(d => d.Modifier)
                    .WithMany(p => p.RunningOrderItemModifiers)
                    .HasForeignKey(d => d.ModifierId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_RunningOrderItemModifiers_Modifiers");

                entity.HasOne(d => d.OrderDetail)
                    .WithMany(p => p.RunningOrderItemModifiers)
                    .HasForeignKey(d => d.OrderDetailId)
                    .HasConstraintName("FK_RunningOrderItemModifiers_RunningOrderDetails");
            });

            modelBuilder.Entity<Shift>(entity =>
            {
                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.Property(e => e.UpdatedBy).HasMaxLength(150);
            });

            modelBuilder.Entity<StringResource>(entity =>
            {
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.Value).IsRequired();

                entity.HasOne(d => d.Language)
                    .WithMany(p => p.StringResources)
                    .HasForeignKey(d => d.LanguageId)
                    .HasConstraintName("FK_StringResources_Languages");
            });

            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.Property(e => e.Address)
                    .IsRequired()
                    .HasMaxLength(250);

                entity.Property(e => e.Email).HasMaxLength(150);

                entity.Property(e => e.Phone)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.SupplierName)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.Property(e => e.UpdatedBy).HasMaxLength(150);
            });

            modelBuilder.Entity<TaxRate>(entity =>
            {
                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.Property(e => e.UpdatedBy).HasMaxLength(150);

                entity.Property(e => e.Value).HasColumnType("decimal(18, 2)");
            });



            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.FullName)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.LastLogin).HasColumnType("datetime");

                entity.Property(e => e.Password)
                    .IsRequired()
                    .HasMaxLength(300);

                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.Property(e => e.UpdatedBy).HasMaxLength(150);

                entity.Property(e => e.UserName)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.PermittedPriceTypes).IsRequired(false);
                entity.Property(e => e.PermittedOrderTypes).IsRequired(false);
            });

            modelBuilder.Entity<UserToken>(entity =>
            {
                entity.Property(e => e.Expiry).HasColumnType("datetime");

                entity.Property(e => e.GeneratedAt).HasColumnType("datetime");

                entity.Property(e => e.Token)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.TokenType)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(150);
            });

            modelBuilder.Entity<WorkPeriod>(entity =>
            {
                entity.Property(e => e.ClosingBalance).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.EndAt).HasColumnType("datetime");

                entity.Property(e => e.EndBy).HasMaxLength(150);

                entity.Property(e => e.OpeningBalance).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.StartedAt).HasColumnType("datetime");

                entity.Property(e => e.StartedBy)
                    .IsRequired()
                    .HasMaxLength(150);
            });


            modelBuilder.Entity<StockAdjustment>(entity =>
            {
                entity.Property(e => e.Quantity).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.CreatedAt).HasColumnType("datetime");
                entity.Property(e => e.EntryDate).HasColumnType("datetime");
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Reason).HasMaxLength(500);
                entity.Property(e => e.CreatedBy).HasMaxLength(150);

                entity.HasOne(d => d.IngredientItem)
                    .WithMany()
                    .HasForeignKey(d => d.IngredientItemId)
                    .HasConstraintName("FK_StockAdjustments_IngredientItems");

                entity.HasOne(d => d.JournalEntry)
                    .WithMany()
                    .HasForeignKey(d => d.JournalEntryId)
                    .HasConstraintName("FK_StockAdjustments_JournalEntries");
            });

            modelBuilder.Entity<Partner>(entity =>
            {
                entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
                entity.Property(e => e.ContactInfo).HasMaxLength(250);
                entity.Property(e => e.OwnershipPercentage).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.CreatedAt).HasColumnType("datetime");
                entity.Property(e => e.CreatedBy).HasMaxLength(150);

                entity.HasOne(d => d.GLAccount)
                    .WithMany()
                    .HasForeignKey(d => d.GLAccountId)
                    .HasConstraintName("FK_Partners_GLAccounts");
            });

            modelBuilder.Entity<PartnerTransaction>(entity =>
            {
                entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.CreatedAt).HasColumnType("datetime");
                entity.Property(e => e.EntryDate).HasColumnType("datetime");
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Note).HasMaxLength(500);
                entity.Property(e => e.CreatedBy).HasMaxLength(150);

                entity.HasOne(d => d.Partner)
                    .WithMany(p => p.PartnerTransactions)
                    .HasForeignKey(d => d.PartnerId)
                    .HasConstraintName("FK_PartnerTransactions_Partners");

                entity.HasOne(d => d.JournalEntry)
                    .WithMany()
                    .HasForeignKey(d => d.JournalEntryId)
                    .HasConstraintName("FK_PartnerTransactions_JournalEntries");
            });

            modelBuilder.Entity<DeletedOrder>(entity =>
            {
                entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.CreatedAt).HasColumnType("datetime");
                entity.Property(e => e.DeletedAt).HasColumnType("datetime");
                entity.Property(e => e.DeletedBy).IsRequired().HasMaxLength(150);
                entity.Property(e => e.DeletionReason).IsRequired();
                entity.Property(e => e.DetailsJson).IsRequired();
                entity.Property(e => e.PaymentMethod).HasMaxLength(150);
                entity.Property(e => e.PriceType).HasMaxLength(150);
                entity.Property(e => e.TableName).HasMaxLength(150);
                entity.Property(e => e.Note).HasMaxLength(250);
                entity.Property(e => e.WaiterOrDriver).HasMaxLength(150);

                entity.HasOne(d => d.Customer)
                    .WithMany()
                    .HasForeignKey(d => d.CustomerId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_DeletedOrders_Customers");
            });

            modelBuilder.Entity<FoodItemStock>(entity =>
            {
                entity.Property(e => e.Quantity).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
                entity.HasKey(e => e.Id);

                entity.HasOne(d => d.FoodItem)
                    .WithMany()
                    .HasForeignKey(d => d.FoodItemId)
                    .HasConstraintName("FK_FoodItemStocks_FoodItems");
            });

            modelBuilder.Entity<InventoryTransaction>(entity =>
            {
                entity.Property(e => e.QuantityChange).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.EntryDate).HasColumnType("datetime");
                entity.Property(e => e.Type).IsRequired().HasMaxLength(100);
                entity.HasKey(e => e.Id);

                entity.HasOne(d => d.FoodItem)
                    .WithMany()
                    .HasForeignKey(d => d.FoodItemId)
                    .HasConstraintName("FK_InventoryTransactions_FoodItems");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    }
}
