using System;
using System.Collections.Generic;

namespace Saffrat.Models
{
    public partial class AuditLog
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Ip { get; set; }
        public string Service { get; set; }
        public string Action { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
