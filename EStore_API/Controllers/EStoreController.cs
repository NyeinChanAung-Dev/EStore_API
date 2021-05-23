using EStore_API.Helper;
using EStore_API.Manager;
using EStore_API.Models.ViewModel.DTO;
using EStore_API.Models.ViewModel.RequestModels;
using EStore_API.Models.ViewModel.ResponseModels;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EStore_API.Controllers
{
    [ApiController]
    public class EStoreController : ControllerBase
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IDistributedCache _distributedCache;
        private EStoreManager _estoremgr;
        private RefreshTokenManager _refreshtokenmgr;

        public EStoreController(IHttpContextAccessor httpContextAccessor, IConfiguration configuration, IDistributedCache distributedCache)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _distributedCache = distributedCache;
            _estoremgr = new EStoreManager(_configuration);
            _refreshtokenmgr = new RefreshTokenManager(_configuration);
        }

        
        [HttpPost]
        [Route("api/estore/GetAccessToken")]
        public IActionResult GetAccessToken(EstoreAccessTokenRequest _request)
        {
            try
            {
                var response = _estoremgr.GetAccessToken(_request);
                if (response.StatusCode == 200)
                {
                    _refreshtokenmgr.SaveRefreshToken(new SaveRefreshTokenDTO
                    {
                        ExpiryMinute = response.RefreshTokenExpireMinutes,
                        RefreshToken = response.RefreshToken,
                        UserId = 0
                    });
                    return Ok(response);
                }
                else
                {
                    return StatusCode(response.StatusCode, response.GetError());
                }
            }
            catch (Exception e)
            {
                return StatusCode(500, new Error("internal_error", e.Message));
            }
        }

        [HttpPost]
        [Route("api/estore/RefreshToken")]
        public IActionResult RefreshToken(RefreshTokenRequest _request)
        {
            try
            {
                var requestHeader = _httpContextAccessor.HttpContext.Request.Headers;
                string accessToken = requestHeader["Authorization"];
                var response = _refreshtokenmgr.RefreshToken(_request, accessToken);

                if (response.StatusCode == 200)
                {
                    return Ok(response);
                }
                else
                {
                    return StatusCode(response.StatusCode, response.GetError());
                }
            }
            catch (Exception e)
            {
                return StatusCode(500, new Error("internal_error", e.Message));
            }
        }

        [Authorize()]
        [HttpPost]
        [Route("api/estore/GetStoreEvoucherList")]
        public IActionResult GetStoreEvoucherList(GetEVoucherListingRequest _request)
        {
            try
            {

                var response = _estoremgr.GetStoreEvoucherList(_request);

                if (response != null && response.Count > 0)
                {
                    Response.Headers.Add("X-Pagination", PageListHelper.GetPagingMetadata(response));
                    return Ok(response);
                }
                else
                {
                    return NotFound(new Error("Not-Found", "No Record Available."));
                }
            }
            catch (Exception e)
            {
                return StatusCode(500, new Error("internal-error", e.Message));
            }
        }

        [Authorize()]
        [HttpPost]
        [Route("api/estore/GetEvoucherDetail")]
        public IActionResult GetEvoucherDetail(GetEVoucherDetailRequest _request)
        {
            try
            {
                var response = _estoremgr.GetEvoucherDetail(_request);
                if (response != null)
                {
                    return Ok(response);
                }
                else
                {
                    return NotFound(new Error("RecordNotFound", "Record Not Available."));
                }
            }
            catch (Exception e)
            {
                return StatusCode(500, new Error("internal-error", e.Message));
            }
        }

        [Authorize()]
        [HttpPost]
        [Route("api/estore/GetPaymentMethodList")]
        public IActionResult GetPaymentMethodList()
        {
            try
            {
                var paymentListCatch = _distributedCache.GetString("PaymentMethodList");
                if (string.IsNullOrEmpty(paymentListCatch))
                {
                    var response = _estoremgr.GetPaymentMethodList();
                    if (response != null && response.Count > 0)
                    {
                        _distributedCache.SetString("PaymentMethodList", JsonConvert.SerializeObject(response));
                        return Ok(response);
                    }
                    else
                    {
                        return NotFound(new Error("Not-Found", "No Record Available."));
                    }
                }
                else
                {

                    var response = JsonConvert.DeserializeObject<List<GetPaymentMethodListResponse>>(paymentListCatch);

                    return Ok(response);
                }

            }
            catch (Exception e)
            {
                return StatusCode(500, new Error("internal-error", e.Message));
            }
        }

        [Authorize()]
        [HttpPost]
        [Route("api/estore/BuyEVoucher")]
        public IActionResult BuyEVoucher(BuyEVoucherRequest _request)
        {
            try
            {
                var response = _estoremgr.BuyEVoucher(_request);
                if (response.StatusCode == 200)
                {
                    //Begin generate promocode when order success
                    var generatePromoJobId = BackgroundJob.Enqueue(() => _estoremgr.ScheduleGeneratePromoCode(new GeneratePromoCodeRequest
                    {
                        PurchaseOrder_No = response.OrderNo
                    }));
                    //after promocode was generated add all to purchase history
                    BackgroundJob.ContinueJobWith(generatePromoJobId, () => _estoremgr.ScheduleUpdatePaymentHistory());

                    return Ok(response);
                }
                else
                {
                    return StatusCode(response.StatusCode, response.GetError());
                }
            }
            catch (Exception e)
            {
                return StatusCode(500, new Error("internal-error", e.Message));
            }
        }

        [Authorize()]
        [HttpPost]
        [Route("api/estore/CheckStockAvaliable")]
        public IActionResult CheckStockAvaliable(CheckStockAvaliableRequest _request)
        {
            try
            {
                var response = _estoremgr.CheckStockAvaliable(_request);
                if (response != null)
                {
                    return Ok(response);
                }
                else
                {
                    return NotFound(new Error("RecordNotFound", "Record Not Available."));
                }
            }
            catch (Exception e)
            {
                return StatusCode(500, new Error("internal-error", e.Message));
            }
        }

        [Authorize()]
        [HttpPost]
        [Route("api/estore/CheckPromoCode")]
        public IActionResult CheckPromoCode(CheckPromoCodeRequest _request)
        {
            try
            {
                var response = _estoremgr.CheckPromoCode(_request);
                if (response != null)
                {
                    return Ok(response);
                }
                else
                {
                    return NotFound(new Error("RecordNotFound", "Record Not Available."));
                }
            }
            catch (Exception e)
            {
                return StatusCode(500, new Error("internal-error", e.Message));
            }
        }

        [Authorize()]
        [HttpPost]
        [Route("api/estore/GetPurchaseHistoryList")]
        public IActionResult GetPurchaseHistoryList(GetPurchaseHistoryRequest _request)
        {
            try
            {
                var response = _estoremgr.GetPurchaseHistory(_request);

                if (response != null && response.Count > 0)
                {
                    Response.Headers.Add("X-Pagination", PageListHelper.GetPagingMetadata(response));
                    return Ok(response);
                }
                else
                {
                    return NotFound(new Error("Not-Found", "No Record Available."));
                }
            }
            catch (Exception e)
            {
                return StatusCode(500, new Error("internal-error", e.Message));
            }
        }

    }
}
