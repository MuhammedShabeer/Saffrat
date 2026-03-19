using System.ComponentModel.DataAnnotations;

namespace Saffrat.Models
{
    /// <summary>
    /// Data model for custom payroll generation with configurable salary and paying amounts
    /// </summary>
    public class PayrollCustomData
    {
        /// <summary>
        /// Employee ID
        /// </summary>
        [Required]
        public int EmployeeId { get; set; }

        /// <summary>
        /// Custom salary amount for this month (may differ from employee's default salary)
        /// </summary>
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Custom salary must be greater than 0")]
        public decimal CustomSalary { get; set; }

        /// <summary>
        /// Initial paying/payment amount for flexible payment tracking
        /// This allows setting an initial payment amount at the time of payroll generation
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Initial paying amount cannot be negative")]
        public decimal InitialPayingAmount { get; set; } = 0;

        /// <summary>
        /// Employee name for UI display purposes
        /// </summary>
        public string EmployeeName { get; set; }

        /// <summary>
        /// Default salary for reference
        /// </summary>
        public decimal DefaultSalary { get; set; }
    }
}
