using EStore_API.Models.ViewModel.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace EStore_API.Models.ViewModel.ResponseModels
{
    public class BuyEVoucherResponse : ResponseBase
    {
        public string OrderNo { get; set; }
        public bool IsPurchaseSuccess { get; set; }
        public string ErrorResponse { get; set; }

    }
}
