using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EStore_API.Models.ViewModel.RequestModels
{
    public class CheckStockAvaliableRequest
    {
        [Required]
        public string VoucherNo { get; set; }
    }
}
