using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Saffrat.Models
{
    public partial class Employee
    {
        public Employee()
        {
            Attendances = new HashSet<Attendance>();
            EmployeeAttachments = new HashSet<EmployeeAttachment>();
            EmployeeDeductions = new HashSet<EmployeeDeduction>();
            EmployeeEarnings = new HashSet<EmployeeEarning>();
            LeaveRequests = new HashSet<LeaveRequest>();
            Payrolls = new HashSet<Payroll>();
        }

        [Key]
        public int? Id { get; set; }
        [Required]
        public int DepartmentId { get; set; }
        [Required]
        public int DesignationId { get; set; }
        [Required]
        public int ShiftId { get; set; }
        public string Image { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string Phone { get; set; }
        [Required]
        public string EmergencyContact { get; set; }
        [Required]
        public string PresentAddress { get; set; }
        [Required]
        public string PermanentAddress { get; set; }
        [Required]
        public string Gender { get; set; }
        [Required]
        public DateTime Dob { get; set; }
        [Required]
        public string Religion { get; set; }
        [Required]
        public string MaritalStatus { get; set; }
        [Required]
        public string Nidnumber { get; set; }
        [Required]
        public DateTime JoiningDate { get; set; }
        public DateTime? LeavingDate { get; set; }
        public string AccountHolderName { get; set; }
        public string AccountNumber { get; set; }
        public string BankName { get; set; }
        public string BankIdentifierCode { get; set; }
        public string BranchLocation { get; set; }
        public string TaxPayerId { get; set; }
        [Required]
        public string PayslipType { get; set; }
        [Required]
        public decimal Salary { get; set; }
        [Required]
        public bool Status { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }

        public virtual Department Department { get; set; }
        public virtual Designation Designation { get; set; }
        public virtual Shift Shift { get; set; }

        [JsonIgnore]
        public virtual ICollection<Attendance> Attendances { get; set; }
        [JsonIgnore]
        public virtual ICollection<EmployeeAttachment> EmployeeAttachments { get; set; }
        [JsonIgnore]
        public virtual ICollection<EmployeeDeduction> EmployeeDeductions { get; set; }
        [JsonIgnore]
        public virtual ICollection<EmployeeEarning> EmployeeEarnings { get; set; }
        [JsonIgnore]
        public virtual ICollection<LeaveRequest> LeaveRequests { get; set; }
        [JsonIgnore]
        public virtual ICollection<Payroll> Payrolls { get; set; }
    }
}
