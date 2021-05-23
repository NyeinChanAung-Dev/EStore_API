using System;
using System.Collections.Generic;
using System.Text;

namespace EStore_API.Models.ViewModel.RequestModels
{
    public class CheckPromoCodeRequest
    {
        public string PromoCode { get; set; }
        public string Phone { get; set; }
    }
}
