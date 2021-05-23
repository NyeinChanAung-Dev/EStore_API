using EStore_API.Models.ViewModel.DTO;
using EStore_API.Models.ViewModel.Enum;
using System.Collections.Generic;
using System.Text;

namespace EStore_API.Models.ViewModel.ResponseModels
{
    public class CheckPromoCodeResponse : ResponseBase
    {
        public PromoCodeStatus Status { get; set; }
        public decimal PromoAmount { get; set; }
    }
}
