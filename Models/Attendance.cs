using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Saffrat.Models
{
    public partial class Attendance
    {
        [Key]
        public int? Id { get; set; }
        public int EmployeeId { get; set; }
        public int ShiftId { get; set; }
        public DateTime AttendaceDate { get; set; }
        [Required]
        public TimeSpan ClockIn { get; set; }
        [Required]
        public TimeSpan ClockOut { get; set; }
        [Required]
        public string Status { get; set; }
        public string Note { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [JsonIgnore]
        public virtual Employee Employee { get; set; }
        [JsonIgnore]
        public virtual Shift Shift { get; set; }
    }
}
