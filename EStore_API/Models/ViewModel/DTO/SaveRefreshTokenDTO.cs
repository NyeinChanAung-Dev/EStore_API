using System;
using System.Collections.Generic;
using System.Text;

namespace EStore_API.Models.ViewModel.DTO
{
    public class SaveRefreshTokenDTO
    {
        public int ExpiryMinute { get; set; }
        public string RefreshToken { get; set; }
        public int UserId { get; set; }
    }
}
