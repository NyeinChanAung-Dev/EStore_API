using EStore_API.Models.ViewModel.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace EStore_API.Models.ViewModel.ResponseModels
{
    public class CheckStockAvaliableResponse :ResponseBase
    {
        public bool isAvaliable { get; set; }
        public int RemainingQuantity { get; set; }
    }
}
