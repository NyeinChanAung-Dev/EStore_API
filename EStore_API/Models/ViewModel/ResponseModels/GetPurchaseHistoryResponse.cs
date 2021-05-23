using System;
using System.Collections.Generic;
using System.Text;

namespace EStore_API.Models.ViewModel.ResponseModels
{
    public class GetPurchaseHistoryResponse
    {
        public int PurchaseHistoryId { get; set; }
        public bool IsUsed { get; set; }
        public string PromoCode { get; set; }
        public string QR_Image_Path { get; set; }
    }
}
