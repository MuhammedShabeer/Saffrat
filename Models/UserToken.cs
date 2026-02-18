using System;
using System.Collections.Generic;

namespace Saffrat.Models
{
    public partial class UserToken
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string TokenType { get; set; }
        public string Token { get; set; }
        public DateTime Expiry { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
