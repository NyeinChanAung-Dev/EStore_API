using System;
using System.Collections.Generic;
using System.Text;

namespace EStore_API.Models.ViewModel.ResponseModels
{
    public class GetPaymentMethodListResponse
    {
        public string PaymentMethod { get; set;}
        public string Description { get; set; }
        public bool? HasDiscount { get; set; }
        public int DiscountPercentage { get; set; }
        public short Status { get; set; }
    }
}
