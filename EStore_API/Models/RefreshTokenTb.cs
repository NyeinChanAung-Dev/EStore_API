using System;
using System.Collections.Generic;

#nullable disable

namespace EStore_API.Models
{
    public partial class RefreshTokenTb
    {
        public string RefreshToken { get; set; }
        public long RefreshTokenId { get; set; }
        public int UserId { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}
